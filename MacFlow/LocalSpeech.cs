using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Threading;
using Windows.Foundation;
using Windows.Storage.Streams;
using WinSpeech=Windows.Media.SpeechSynthesis.SpeechSynthesizer;
namespace MacFlow
{
    internal sealed class SpeechWave{internal byte[] Bytes;internal string Voice;}
    // Windows OneCore voices, then desktop SAPI. Both work locally without a key.
    internal sealed class LocalSpeech : IDisposable
    {
        private readonly string directory;private MediaPlayer player;private string file;
        private CancellationTokenSource cancel;private readonly DispatcherTimer timeout=new DispatcherTimer{Interval=TimeSpan.FromSeconds(1)};
        private DateTime deadline;private int generation;private bool disposed,speaking;
        internal bool Busy{get;private set;}
        internal event Action<bool> SpeakingChanged;internal event Action<string> Failed;
        internal LocalSpeech(string profile){directory=Path.Combine(profile,"speech-cache");timeout.Tick+=delegate{if(DateTime.UtcNow>deadline){Stop();Error("La salida de voz no respondió.");}};}
        private static async Task<T> Read<T>(IAsyncOperation<T> action,CancellationToken stop)
        {
            var deadline=DateTime.UtcNow.AddSeconds(20);try{while(action.Status==AsyncStatus.Started){stop.ThrowIfCancellationRequested();if(DateTime.UtcNow>deadline)throw new TimeoutException("La síntesis local tardó demasiado.");await Task.Delay(25,stop);}stop.ThrowIfCancellationRequested();return action.GetResults();}finally{if(action.Status==AsyncStatus.Started)action.Cancel();action.Close();}
        }
        internal static async Task<SpeechWave> Synthesize(string text,CancellationToken stop)
        {
            Exception modernError;
            try
            {
                var voices=WinSpeech.AllVoices;var voice=voices.FirstOrDefault(v=>v.Language.StartsWith("es",StringComparison.OrdinalIgnoreCase))??voices.FirstOrDefault();if(voice==null)throw new InvalidOperationException("No hay voces de Windows instaladas.");
                using(var synth=new WinSpeech()){synth.Voice=voice;using(var stream=await Read(synth.SynthesizeTextToStreamAsync(text),stop)){if(stream.Size<44||stream.Size>32*1024*1024)throw new IOException("Audio de voz inválido o demasiado grande.");using(var reader=new DataReader(stream)){await Read(reader.LoadAsync((uint)stream.Size),stop);var bytes=new byte[(int)stream.Size];reader.ReadBytes(bytes);return new SpeechWave{Bytes=bytes,Voice=voice.DisplayName};}}}
            }
            catch(OperationCanceledException){throw;}catch(Exception ex){modernError=ex;}
            return await Task.Run(delegate
            {
                stop.ThrowIfCancellationRequested();try{using(var synth=new System.Speech.Synthesis.SpeechSynthesizer())using(var memory=new MemoryStream()){var voices=synth.GetInstalledVoices().Where(v=>v.Enabled).ToArray();var voice=voices.FirstOrDefault(v=>v.VoiceInfo.Culture.TwoLetterISOLanguageName=="es")??voices.FirstOrDefault();if(voice==null)throw new InvalidOperationException("No hay voces SAPI instaladas.");synth.SelectVoice(voice.VoiceInfo.Name);synth.SetOutputToWaveStream(memory);synth.Speak(text);stop.ThrowIfCancellationRequested();return new SpeechWave{Bytes=memory.ToArray(),Voice=voice.VoiceInfo.Name};}}catch(OperationCanceledException){throw;}catch(Exception ex){Log.Write("Voz OneCore: "+modernError.Message+"; SAPI: "+ex.Message);throw new InvalidOperationException("No hay una voz local disponible. Revisá Configuración de Windows → Hora e idioma → Voz.",ex);}
            },stop);
        }
        internal async void Speak(string text)
        {
            if(disposed||String.IsNullOrWhiteSpace(text))return;Stop();Busy=true;int token=++generation;var request=new CancellationTokenSource();cancel=request;
            try
            {
                var wave=await Synthesize(text.Substring(0,Math.Min(6000,text.Length)),request.Token);if(disposed||token!=generation)return;Directory.CreateDirectory(directory);file=Path.Combine(directory,"speech-"+Guid.NewGuid().ToString("N")+".wav");File.WriteAllBytes(file,wave.Bytes);var audio=new MediaPlayer();player=audio;
                audio.MediaOpened+=delegate{if(disposed||token!=generation)return;if(!audio.HasAudio){Stop();Error("Windows no encontró audio en la respuesta.");return;}audio.Play();speaking=true;var changed=SpeakingChanged;if(changed!=null)changed(true);};
                audio.MediaEnded+=delegate{if(token==generation)Stop();};audio.MediaFailed+=delegate(object s,System.Windows.Media.ExceptionEventArgs e){if(token==generation){Stop();Error("No se pudo reproducir la voz. Revisá el dispositivo de sonido.");}};
                deadline=DateTime.UtcNow.AddMinutes(8);timeout.Start();audio.Open(new Uri(file));
            }
            catch(OperationCanceledException){}catch(Exception ex){if(!disposed&&token==generation){Stop();Error(ex.Message);}}
            finally{if(cancel==request)cancel=null;request.Dispose();}
        }
        private void Error(string text){var failed=Failed;if(failed!=null)failed(text);}
        internal void Stop(){++generation;timeout.Stop();if(cancel!=null){cancel.Cancel();cancel=null;}if(player!=null){player.Stop();player.Close();player=null;}if(file!=null){try{File.Delete(file);}catch(IOException){}catch(UnauthorizedAccessException){}file=null;}Busy=false;if(speaking){speaking=false;var changed=SpeakingChanged;if(changed!=null)changed(false);}}
        public void Dispose(){if(disposed)return;disposed=true;Stop();}
    }
}

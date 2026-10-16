using System;
using System.Linq;
using System.Speech.Recognition;
using System.Threading.Tasks;
using System.Windows.Threading;
namespace MacFlow
{
    internal static class WakePhrase
    {
        internal static bool Accept(string text,float confidence){string value=LocalCommands.Normalize(text);return confidence>=.78f&&(value=="hey siri"||value=="oye siri");}
    }
    // A two-phrase offline grammar, never a background dictation grammar.
    // Audio is not saved or transmitted. The microphone is detached on pause.
    internal sealed class VoiceWake : IDisposable
    {
        private readonly Dispatcher dispatcher;private readonly Action activate;private readonly Action<bool,string> status;
        private SpeechRecognitionEngine engine;private bool wanted,running,initializing,failed,disposed;private DateTime lastActivation;
        internal VoiceWake(Dispatcher dispatcher,Action activate,Action<bool,string> status){this.dispatcher=dispatcher;this.activate=activate;this.status=status;}
        internal void Update(bool enabled)
        {
            if(disposed)return;if(wanted==enabled&&(!enabled||running||initializing||failed))return;wanted=enabled;if(!enabled){if(running)try{engine.RecognizeAsyncCancel();}catch{}else DetachInput();status(false,"Hey Siri en pausa");return;}
            if(failed||running||initializing)return;if(engine==null){Initialize();return;}Start();
        }
        private async void Initialize()
        {
            initializing=true;SpeechRecognitionEngine created=null;
            try
            {
                created=await Task.Run(delegate{var info=SpeechRecognitionEngine.InstalledRecognizers().FirstOrDefault(r=>r.Culture.TwoLetterISOLanguageName=="es");if(info==null)throw new InvalidOperationException("Falta el reconocimiento de voz español de Windows. Usá Windows para escribir.");var value=new SpeechRecognitionEngine(info);try{var grammar=new GrammarBuilder{Culture=info.Culture};grammar.Append(new Choices("hey siri","oye siri"));value.LoadGrammar(new Grammar(grammar));return value;}catch{value.Dispose();throw;}});
                if(disposed){created.Dispose();return;}engine=created;engine.SpeechRecognized+=Recognized;engine.RecognizeCompleted+=Completed;if(wanted)Start();
            }
            catch(Exception ex){if(!disposed){failed=true;status(false,"Hey Siri no disponible: "+ex.Message);Log.Write("Activación local de voz no disponible: "+ex.Message);}}
            finally{initializing=false;}
        }
        private void Start()
        {
            try{engine.SetInputToDefaultAudioDevice();engine.RecognizeAsync(RecognizeMode.Multiple);running=true;status(true,"Micrófono activo · decí Hey Siri · se puede desactivar en Configuración");}
            catch(Exception ex){failed=true;running=false;DetachInput();status(false,"Hey Siri no disponible: "+ex.Message);}
        }
        private void Recognized(object sender,SpeechRecognizedEventArgs args)
        {
            if(!WakePhrase.Accept(args.Result.Text,args.Result.Confidence))return;Post(delegate{if(!wanted||disposed||DateTime.UtcNow-lastActivation<TimeSpan.FromSeconds(3))return;lastActivation=DateTime.UtcNow;Update(false);activate();});
        }
        private void Completed(object sender,RecognizeCompletedEventArgs args)
        {
            Post(delegate{if(disposed)return;running=false;DetachInput();if(args.Error!=null){failed=true;status(false,"Hey Siri se detuvo: "+args.Error.Message);}else if(wanted&&!failed)Start();});
        }
        private void Post(Action action){if(!dispatcher.HasShutdownStarted)dispatcher.BeginInvoke(action);}
        private void DetachInput(){if(engine!=null)try{engine.SetInputToNull();}catch{}}
        public void Dispose(){if(disposed)return;disposed=true;wanted=false;if(engine!=null){engine.SpeechRecognized-=Recognized;engine.RecognizeCompleted-=Completed;try{engine.RecognizeAsyncCancel();}catch{}DetachInput();engine.Dispose();engine=null;}status(false,"Micrófono apagado");}
    }
}

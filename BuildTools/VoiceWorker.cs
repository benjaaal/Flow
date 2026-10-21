using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using NAudio.Wave;
using Vosk;
internal static class VoiceWorker
{
    private sealed class Word{public double conf;}
    private sealed class Result{public string text;public Word[] result;}
    private static readonly object gate=new object(),outputGate=new object();
    private static Model model,dictation;private static string profile;private static VoskRecognizer recognizer;private static WaveInEvent microphone;private static string mode="pause";private static DateTime started;
    private static readonly JavaScriptSerializer json=new JavaScriptSerializer();
    private const string grammar="[\"hey siri\",\"oye siri\",\"ey siri\",\"hey siria\",\"oye siria\",\"[unk]\"]";
    private static void Say(string value){lock(outputGate){Console.WriteLine(value);Console.Out.Flush();}}
    private static string ModelFolder(string cache,string language="es")
    {
        string root=Path.GetFullPath(Path.Combine(cache,"speech-model-"+language)),folder=Path.Combine(root,language=="es"?"vosk-model-small-es-0.42":"vosk-model-small-en-us-0.15");if(File.Exists(Path.Combine(root,"ready"))&&File.Exists(Path.Combine(folder,"am","final.mdl"))&&File.Exists(Path.Combine(folder,"conf","model.conf")))return folder;Directory.CreateDirectory(root);
        using(var zip=ZipFile.OpenRead(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,language=="es"?"voice-es.zip":"voice-en.zip")))foreach(var entry in zip.Entries){string target=Path.GetFullPath(Path.Combine(root,entry.FullName));if(!target.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new IOException("Archivo de modelo fuera de la carpeta permitida");if(entry.FullName.EndsWith("/")){Directory.CreateDirectory(target);continue;}Directory.CreateDirectory(Path.GetDirectoryName(target));entry.ExtractToFile(target,true);}File.WriteAllText(Path.Combine(root,"ready"),"es-0.42");return folder;
    }
    private static bool Wake(Result result){return result!=null&&new[]{"hey siri","oye siri","ey siri","hey siria","oye siria"}.Contains(result.text)&&result.result!=null&&result.result.Length==2&&result.result.Average(w=>w.conf)>=.78&&result.result[1].conf>=.85&&result.result[0].conf>=.55;}
    private static void SetMode(string next)
    {
        WaveInEvent old;lock(gate){mode="pause";old=microphone;microphone=null;}if(old!=null){old.StopRecording();old.Dispose();}
        lock(gate){if(recognizer!=null){recognizer.Dispose();recognizer=null;}if(next=="pause")return;if(next=="dictate"&&dictation==null)dictation=new Model(ModelFolder(profile,"es"));recognizer=next=="wake"?new VoskRecognizer(model,16000,grammar):new VoskRecognizer(dictation,16000);recognizer.SetWords(true);mode=next;started=DateTime.UtcNow;microphone=new WaveInEvent{WaveFormat=new WaveFormat(16000,16,1),BufferMilliseconds=100,NumberOfBuffers=3};microphone.DataAvailable+=Audio;microphone.RecordingStopped+=delegate(object s,StoppedEventArgs e){if(e.Exception!=null)Say("ERROR:El micrófono no está disponible: "+e.Exception.Message);};microphone.StartRecording();Say(next=="wake"?"WAKE_READY":"DICTATION_READY");}
    }
    private static void Audio(object sender,WaveInEventArgs e)
    {
        lock(gate)
        {
            if(mode=="pause"||recognizer==null)return;
            try{bool final=recognizer.AcceptWaveform(e.Buffer,e.BytesRecorded);if(!final)return;var result=json.Deserialize<Result>(recognizer.Result());if(mode=="wake"){if(Wake(result)){mode="pause";Say("WAKE");}}else if(!String.IsNullOrWhiteSpace(result.text)){mode="pause";Say("TEXT:"+Convert.ToBase64String(Encoding.UTF8.GetBytes(result.text)));}}
            catch(Exception ex){mode="pause";Say("ERROR:"+ex.Message);}
        }
    }
    private static int Main(string[] args)
    {
        try
        {
            if(args.Length==1&&args[0]=="--fixture"){Say("READY");string command;bool woke=false;while((command=Console.ReadLine())!=null&&command!="stop"){if(command=="wake"){Say("WAKE_READY");if(!woke){woke=true;Say("WAKE");}}else if(command=="dictate"){Say("DICTATION_READY");Say("TEXT:"+Convert.ToBase64String(Encoding.UTF8.GetBytes("centro de control")));}}return 0;}
            Vosk.Vosk.SetLogLevel(-1);if(args.Length<1)return 2;profile=args[0];string folder=ModelFolder(profile,args.Length==3&&args[2]!="wake"?"es":"en");using(model=new Model(folder))
            {
                if(args.Length==3){using(var wav=new WaveFileReader(args[1]))using(var speech=args[2]=="wake"?new VoskRecognizer(model,16000,grammar):new VoskRecognizer(model,16000)){speech.SetWords(true);if(wav.WaveFormat.SampleRate!=16000||wav.WaveFormat.BitsPerSample!=16||wav.WaveFormat.Channels!=1)throw new Exception("La prueba debe ser PCM16 mono a 16 kHz");var buffer=new byte[3200];int read;while((read=wav.Read(buffer,0,buffer.Length))>0)if(speech.AcceptWaveform(buffer,read))Say("PROBE:"+speech.Result());string final=speech.FinalResult();Say("PROBE:"+final);if(args[2]=="wake")Say("MATCH:"+Wake(json.Deserialize<Result>(final)));return 0;}}
                Say("READY");string line;while((line=Console.ReadLine())!=null&&line!="stop"){if(line!="wake"&&line!="dictate"&&line!="pause")continue;try{SetMode(line);}catch(Exception ex){SetMode("pause");Say("ERROR:No se pudo abrir el micrófono. Revisá los permisos para aplicaciones de escritorio. "+ex.Message);}}SetMode("pause");
            }return 0;
        }
        catch(Exception ex){Say("ERROR:No se pudo preparar el reconocimiento local: "+ex.Message);return 1;}
        finally{SetMode("pause");if(dictation!=null)dictation.Dispose();}
    }
}

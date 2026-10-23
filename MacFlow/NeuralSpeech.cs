using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
namespace MacFlow
{
    // Free local inference. Downloads only official, hash-pinned assets once.
    // No text is sent to a speech service. The child exists only during synthesis.
    internal static class NeuralSpeech
    {
        private static readonly SemaphoreSlim install=new SemaphoreSlim(1,1);
        private const string RuntimeHash="F3C58906402B24F3A96D92145F58ACBA6D86C9B5DB896D207F78DC80811EFCEA",ModelHash="3EF40A71EA63852CD8AB7E6FA7D2ECDCFA67A0B47C9C48E3F10E02EE02083EA0",ConfigHash="1AFC81F703C0E4CB3B4D7C0DCA096B8B54A98806807F0170CF5EB5557723C12D";
        internal static string Clean(string text)
        {text=text??"";int sources=text.IndexOf("\nFuentes",StringComparison.OrdinalIgnoreCase);if(sources>0)text=text.Substring(0,sources);text=Regex.Replace(text,@"https?://\S+","");text=Regex.Replace(text,@"[*#`]+","");return text.Length>2400?text.Substring(0,2400):text;}
        private static bool Valid(string path,string hash){if(!File.Exists(path))return false;using(var sha=SHA256.Create())using(var file=File.OpenRead(path))return BitConverter.ToString(sha.ComputeHash(file)).Replace("-","")==hash;}
        private static async Task Download(string url,string path,string hash,CancellationToken stop)
        {ServicePointManager.SecurityProtocol|=SecurityProtocolType.Tls12;using(var client=new WebClient())using(stop.Register(client.CancelAsync)){await client.DownloadFileTaskAsync(new Uri(url),path);stop.ThrowIfCancellationRequested();if(!Valid(path,hash))throw new IOException("La voz descargada no superó la comprobación de integridad.");}}
        internal static async Task<SpeechWave> Synthesize(string profile,string text,CancellationToken stop,Action<string> progress)
        {
            string home=Path.Combine(profile,"neural-voice-v1"),exe=Path.Combine(home,"piper","piper.exe"),model=Path.Combine(home,"voice.onnx");
            await install.WaitAsync(stop);
            try
            {
                bool ready=await Task.Run(()=>File.Exists(exe)&&Valid(model,ModelHash)&&Valid(model+".json",ConfigHash),stop);
                if(!ready)
                {
                    if(progress!=null)progress("Descargando voz natural gratuita · 85 MB, sólo la primera vez…");
                    string stage=home+".stage-"+Guid.NewGuid().ToString("N");Directory.CreateDirectory(stage);
                    try
                    {
                        string zip=Path.Combine(stage,"piper.zip");await Download("https://github.com/rhasspy/piper/releases/download/2023.11.14-2/piper_windows_amd64.zip",zip,RuntimeHash,stop);
                        using(var archive=ZipFile.OpenRead(zip)){long total=0;foreach(var entry in archive.Entries){stop.ThrowIfCancellationRequested();string name=entry.FullName.Replace('\\','/');if(!name.StartsWith("piper/",StringComparison.Ordinal)||name.Contains("..")||name.Contains(":"))throw new IOException("Ruta no válida en el motor de voz.");total+=entry.Length;if(total>128000000)throw new IOException("Motor de voz demasiado grande.");string target=Path.Combine(stage,name.Replace('/',Path.DirectorySeparatorChar));if(String.IsNullOrEmpty(entry.Name)){Directory.CreateDirectory(target);continue;}Directory.CreateDirectory(Path.GetDirectoryName(target));entry.ExtractToFile(target);}}
                        File.Delete(zip);await Download("https://huggingface.co/rhasspy/piper-voices/resolve/main/es/es_MX/claude/high/es_MX-claude-high.onnx",Path.Combine(stage,"voice.onnx"),ModelHash,stop);await Download("https://huggingface.co/rhasspy/piper-voices/resolve/main/es/es_MX/claude/high/es_MX-claude-high.onnx.json",Path.Combine(stage,"voice.onnx.json"),ConfigHash,stop);
                        File.WriteAllText(Path.Combine(stage,"SOURCES.txt"),"Piper 2023.11.14-2: https://github.com/rhasspy/piper/releases/tag/2023.11.14-2\nVoice/model card: https://huggingface.co/rhasspy/piper-voices/tree/main/es/es_MX/claude/high\nDataset: Apache-2.0; runtime licenses and corresponding sources: https://github.com/rhasspy/piper/tree/2023.11.14-2\n");
                        stop.ThrowIfCancellationRequested();if(Directory.Exists(home))Directory.Move(home,home+".old-"+Guid.NewGuid().ToString("N"));Directory.Move(stage,home);
                    }
                    finally{if(Directory.Exists(stage)&&Path.GetFullPath(stage).StartsWith(Path.GetFullPath(profile)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))Directory.Delete(stage,true);}
                }
            }
            finally{install.Release();}
            if(progress!=null)progress("Preparando voz natural local…");
            return await Task.Run(delegate
            {
                string output=Path.Combine(profile,"speech-"+Guid.NewGuid().ToString("N")+".wav");
                try
                {
                    var info=new ProcessStartInfo(exe,"--model \""+model+"\" --output_file \""+output+"\" --length_scale 1.02"){WorkingDirectory=Path.GetDirectoryName(exe),UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true};
                    using(var child=Process.Start(info))
                    {var errors=child.StandardError.ReadToEndAsync();var messages=child.StandardOutput.ReadToEndAsync();byte[] input=new UTF8Encoding(false).GetBytes(Clean(text)+"\n");child.StandardInput.BaseStream.Write(input,0,input.Length);child.StandardInput.BaseStream.Flush();child.StandardInput.Close();var watch=Stopwatch.StartNew();while(!child.WaitForExit(50)){if(stop.IsCancellationRequested||watch.Elapsed.TotalSeconds>60){child.Kill();child.WaitForExit();stop.ThrowIfCancellationRequested();throw new IOException("La voz natural tardó demasiado.");}}stop.ThrowIfCancellationRequested();if(child.ExitCode!=0)throw new IOException("No se pudo sintetizar la voz natural.");}
                    var bytes=File.ReadAllBytes(output);if(bytes.Length<44||bytes.Length>32000000)throw new IOException("Audio de voz inválido.");return new SpeechWave{Bytes=bytes,Voice="Voz natural local · español"};
                }
                finally{if(File.Exists(output))File.Delete(output);}
            },stop);
        }
    }
}

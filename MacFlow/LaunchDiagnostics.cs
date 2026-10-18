using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
namespace MacFlow
{
    [DataContract] internal sealed class LaunchRecord
    {
        [DataMember] public string Version,StartedUtc,Mode,Phase;
        [DataMember] public double ElapsedMs,FirstFrameMs;
    }
    internal static class LaunchDiagnostics
    {
        private static readonly Stopwatch watch=Stopwatch.StartNew();
        private static LaunchRecord record;
        private static string path;
        private static bool firstFrameRecorded;
        internal static void Begin(string folder,bool background)
        {
            watch.Restart();firstFrameRecorded=false;
            record=new LaunchRecord{Version=Product.VersionText,StartedUtc=DateTime.UtcNow.ToString("o",CultureInfo.InvariantCulture),Mode=background?"Windows":"Manual",Phase="Proceso iniciado"};path=Path.Combine(folder,"last-launch.json");Mark("Proceso iniciado");
        }
        internal static void Mark(string phase)
        {
            if(record==null||phase=="Dock visible"&&firstFrameRecorded)return;
            record.Phase=phase;record.ElapsedMs=Math.Round(watch.Elapsed.TotalMilliseconds,1);
            if(phase=="Dock visible"){record.FirstFrameMs=record.ElapsedMs;firstFrameRecorded=true;}
            try{Directory.CreateDirectory(Path.GetDirectoryName(path));var temp=path+".tmp";using(var file=File.Create(temp))new DataContractJsonSerializer(typeof(LaunchRecord)).WriteObject(file,record);if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}catch(Exception ex){Log.Write("Diagnóstico de inicio: "+ex.Message);}
        }
        internal static LaunchRecord Read(string folder)
        {try{using(var file=File.OpenRead(Path.Combine(folder,"last-launch.json")))return (LaunchRecord)new DataContractJsonSerializer(typeof(LaunchRecord)).ReadObject(file);}catch{return null;}}
    }
}

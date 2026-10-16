using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Windows;
namespace MacFlow
{
    internal static class ModuleStore
    {
        internal static T Read<T>(string file) where T:new()
        {foreach(var candidate in new[]{file,file+".bak"})try{if(!File.Exists(candidate))continue;if(new FileInfo(candidate).Length>262144)throw new IOException("Configuración demasiado grande");using(var s=File.OpenRead(candidate)){var value=(T)new DataContractJsonSerializer(typeof(T)).ReadObject(s);if(value!=null)return value;}}catch(Exception ex){Log.Write("Módulo: "+ex.Message);}return new T();}
        internal static void Write<T>(string file,T value)
        {Directory.CreateDirectory(Path.GetDirectoryName(file));string temp=file+".new";using(var s=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None)){new DataContractJsonSerializer(typeof(T)).WriteObject(s,value);s.Flush(true);}if(File.Exists(file))File.Replace(temp,file,file+".bak");else File.Move(temp,file);}
    }
    [DataContract] internal sealed class WidgetTask
    {[DataMember]public string Text="";[DataMember]public bool Done;}
    [DataContract] internal sealed class WidgetSpec
    {
        [DataMember]public string Id=Guid.NewGuid().ToString("N"),Kind="Calendar",Size="S",Text="",City="",AppTarget="",AppName="";
        [DataMember]public int Column,Row,MonthOffset,TimerMinutes=5;
        [DataMember]public List<WidgetTask> Tasks=new List<WidgetTask>();
        [DataMember]public bool Running,Completed;
        [DataMember]public DateTime StartedUtc;
        [DataMember]public double Seconds;
        [DataMember]public double Latitude,Longitude;
        [DataMember]public bool HasLocation;
        [DataMember]public string AppExecutable="",AppIconPath="";
        [DataMember]public string Surface="Clear";
        [DataMember]public string Monitor="";
        public override string ToString(){return WidgetCatalog.Name(Kind);}
        internal double Elapsed(DateTime now){return Math.Max(0,Seconds+(Running?Math.Max(0,(now-StartedUtc).TotalSeconds):0));}
        internal double Remaining(DateTime now){return Math.Max(0,TimerMinutes*60-Elapsed(now));}
        internal void Toggle(DateTime now){if(Running){Seconds=Elapsed(now);Running=false;}else{if(Completed){Seconds=0;Completed=false;}StartedUtc=now;Running=true;}}
        internal void Reset(){Running=Completed=false;Seconds=0;}
    }
    [DataContract] internal sealed class WidgetState
    {
        [DataMember]public List<WidgetSpec> Items=new List<WidgetSpec>();
        [DataMember]public int LayoutRevision;
        internal void Validate()
        {
            if(Items==null)Items=new List<WidgetSpec>();Items=Items.Where(x=>x!=null&&WidgetCatalog.Kinds.Contains(x.Kind)).Take(16).ToList();var ids=new HashSet<string>();
            foreach(var x in Items){if(x.Surface!="Dark"&&x.Surface!="Light")x.Surface="Clear";if(String.IsNullOrEmpty(x.Id)||!ids.Add(x.Id)){x.Id=Guid.NewGuid().ToString("N");ids.Add(x.Id);}if(x.Size!="S"&&x.Size!="M"&&x.Size!="L")x.Size="S";x.Column=Math.Max(0,Math.Min(64,x.Column));x.Row=Math.Max(0,Math.Min(64,x.Row));x.MonthOffset=Math.Max(-120,Math.Min(120,x.MonthOffset));x.TimerMinutes=Math.Max(1,Math.Min(1440,x.TimerMinutes));x.Text=(x.Text??"").Substring(0,Math.Min(4000,(x.Text??"").Length));if(x.Tasks==null)x.Tasks=new List<WidgetTask>();x.Tasks=x.Tasks.Where(t=>t!=null&&!String.IsNullOrWhiteSpace(t.Text)).Take(32).ToList();foreach(var t in x.Tasks)t.Text=t.Text.Substring(0,Math.Min(160,t.Text.Length));if(Double.IsNaN(x.Seconds)||Double.IsInfinity(x.Seconds)||x.Seconds<0)x.Reset();if(x.StartedUtc.Kind!=DateTimeKind.Utc)x.StartedUtc=DateTime.SpecifyKind(x.StartedUtc,DateTimeKind.Utc);if(x.Running&&x.StartedUtc==DateTime.MinValue)x.Reset();if(Double.IsNaN(x.Latitude)||Double.IsNaN(x.Longitude)||Math.Abs(x.Latitude)>90||Math.Abs(x.Longitude)>180){x.HasLocation=false;x.Latitude=x.Longitude=0;}}
        }
    }
    internal static class WidgetCatalog
    {
        internal static readonly string[] Kinds={"Digital","Analog","Calendar","Weather","Battery","Performance","Music","Notes","Tasks","Timer","Stopwatch","Shortcuts","Network"};
        internal static readonly string[] Names={"Reloj digital","Reloj analógico","Calendario","Clima","Batería","Actividad","Música","Notas","Tareas","Temporizador","Cronómetro","Aplicaciones","Conexión"};
        internal static string Name(string kind){int i=Array.IndexOf(Kinds,kind);return i<0?kind:Names[i];}
        internal static Size Dimensions(string size){return size=="L"?new Size(340,340):size=="M"?new Size(340,164):new Size(164,164);}
        internal static Rect Bounds(WidgetSpec x){var d=Dimensions(x.Size);return new Rect(16+x.Column*88,58+x.Row*88,d.Width,d.Height);}
        internal static bool Place(WidgetSpec item,IEnumerable<WidgetSpec> all,Size available,int requestedColumn,int requestedRow)
        {
            var d=Dimensions(item.Size);if(d.Width>available.Width-32||d.Height>available.Height-150)return false;int columns=Math.Max(1,(int)((available.Width-32-d.Width)/88)+1),rows=Math.Max(1,(int)((available.Height-150-d.Height)/88)+1);
            Func<int,int,bool> free=(c,r)=>!all.Where(x=>x.Id!=item.Id&&String.Equals(x.Monitor??"",item.Monitor??"",StringComparison.OrdinalIgnoreCase)).Any(x=>Bounds(x).IntersectsWith(new Rect(16+c*88,58+r*88,d.Width,d.Height)));
            int desiredC=Math.Max(0,Math.Min(columns-1,requestedColumn)),desiredR=Math.Max(0,Math.Min(rows-1,requestedRow));if(free(desiredC,desiredR)){item.Column=desiredC;item.Row=desiredR;return true;}
            for(int r=0;r<rows;r++)for(int c=0;c<columns;c++)if(free(c,r)){item.Column=c;item.Row=r;return true;}return false;
        }
    }
}

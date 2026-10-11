using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
namespace MacFlow
{
    // A reversible visibility lease. No Explorer injection, style edits or
    // file patches. Persist visibility before each asynchronous hide request.
    public sealed class TaskbarSurfaces
    {
        private delegate void EventCallback(IntPtr hook,uint kind,IntPtr hwnd,int objectId,int child,uint thread,uint time);
        [DllImport("user32.dll")]private static extern IntPtr SetWinEventHook(uint first,uint last,IntPtr module,EventCallback callback,uint process,uint thread,uint flags);
        [DllImport("user32.dll")]private static extern bool UnhookWinEvent(IntPtr hook);
        [DllImport("user32.dll",CharSet=CharSet.Unicode)]private static extern int GetClassName(IntPtr h,StringBuilder name,int length);
        private sealed class Entry {public IntPtr Handle;public uint Process;public bool Visible;}
        private readonly string file;
        private readonly Func<IEnumerable<IntPtr>> discover;
        private readonly Func<IntPtr,bool> valid;
        private readonly bool events;
        private readonly Dictionary<IntPtr,Entry> entries=new Dictionary<IntPtr,Entry>();
        private readonly EventCallback callback;
        private IntPtr hook;
        private bool active;
        private DateTime enumerated=DateTime.MinValue;
        public TaskbarSurfaces(string path):this(path,Discover,IsShell,true){}
        internal TaskbarSurfaces(string path,Func<IEnumerable<IntPtr>> discovery,Func<IntPtr,bool> validation,bool watch)
        {file=path;discover=discovery;valid=validation;events=watch;callback=delegate(IntPtr h,uint e,IntPtr hwnd,int objectId,int child,uint thread,uint time){if(active&&objectId==0&&child==0&&valid(hwnd)){try{TrackAndHide(hwnd);}catch(Exception ex){Log.Write("Ocultación de barra: "+ex.Message);}}};}
        internal static bool TaskbarClass(string name){return name=="Shell_TrayWnd"||name=="Shell_SecondaryTrayWnd";}
        private static string Class(IntPtr h){var name=new StringBuilder(100);GetClassName(h,name,name.Capacity);return name.ToString();}
        private static bool IsShell(IntPtr h)
        {if(!Native.IsWindow(h)||!TaskbarClass(Class(h)))return false;uint pid;Native.GetWindowThreadProcessId(h,out pid);var path=Native.ProcessPath(pid);return String.Equals(path,Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),"explorer.exe"),StringComparison.OrdinalIgnoreCase);}
        private static IEnumerable<IntPtr> Discover()
        {var windows=new List<IntPtr>();Native.EnumWindows(delegate(IntPtr h,IntPtr arg){if(IsShell(h))windows.Add(h);return true;},IntPtr.Zero);return windows;}
        public void Acquire()
        {
            if(active){Maintain();return;}active=true;enumerated=DateTime.MinValue;
            if(events)hook=SetWinEventHook(0x8002,0x8002,IntPtr.Zero,callback,0,0,2);
            try{Maintain();}catch{Restore();throw;}
        }
        public void Maintain()
        {
            if(!active)return;
            if(DateTime.UtcNow-enumerated>TimeSpan.FromSeconds(1)){enumerated=DateTime.UtcNow;foreach(var h in discover())TrackAndHide(h);}
            foreach(var entry in entries.Values.ToArray())if(Identity(entry)&&valid(entry.Handle)&&Native.IsWindowVisible(entry.Handle))Native.ShowWindowAsync(entry.Handle,0);
        }
        private bool Identity(Entry entry){uint pid;Native.GetWindowThreadProcessId(entry.Handle,out pid);return Native.IsWindow(entry.Handle)&&pid==entry.Process;}
        private void TrackAndHide(IntPtr h)
        {Track(h,true);}
        public void Remember(){foreach(var h in discover())Track(h,false);}
        private void Track(IntPtr h,bool hide)
        {
            if(!valid(h))return;uint pid;Native.GetWindowThreadProcessId(h,out pid);Entry entry;
            if(!entries.TryGetValue(h,out entry)||entry.Process!=pid){var previous=entry;entry=new Entry{Handle=h,Process=pid,Visible=Native.IsWindowVisible(h)};entries[h]=entry;try{Persist();}catch{if(previous==null)entries.Remove(h);else entries[h]=previous;throw;}}
            if(hide&&Native.IsWindowVisible(h))Native.ShowWindowAsync(h,0);
        }
        private void Persist()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file));string temporary=file+".new";
            using(var stream=new FileStream(temporary,FileMode.Create,FileAccess.Write,FileShare.None))using(var writer=new StreamWriter(stream))
            {writer.WriteLine("MacFlow taskbar visibility v1");foreach(var entry in entries.Values)writer.WriteLine(entry.Handle.ToInt64().ToString(CultureInfo.InvariantCulture)+"|"+entry.Process+"|"+(entry.Visible?1:0));writer.Flush();stream.Flush(true);}
            if(File.Exists(file))File.Replace(temporary,file,null);else File.Move(temporary,file);
        }
        public void Restore()
        {active=false;if(hook!=IntPtr.Zero){UnhookWinEvent(hook);hook=IntPtr.Zero;}RestoreRecorded(file,valid,discover);entries.Clear();}
        public static void RestoreFile(string path){RestoreRecorded(path,IsShell,Discover);}
        public static void RecoverVisible()
        {foreach(var h in Discover())if(IsShell(h)&&!Native.IsWindowVisible(h))Native.ShowWindowAsync(h,4);}
        private static void RestoreRecorded(string path,Func<IntPtr,bool> validation,Func<IEnumerable<IntPtr>> discovery)
        {
            try
            {
                if(!File.Exists(path))return;var lines=File.ReadAllLines(path);if(lines.Length==0||lines[0]!="MacFlow taskbar visibility v1")throw new IOException("Registro de visibilidad inválido");
                bool applied=false,showAfterRestart=false;
                foreach(var line in lines.Skip(1))
                {
                    var parts=line.Split('|');long handle;uint pid;int visible;
                    if(parts.Length!=3||!Int64.TryParse(parts[0],out handle)||handle==0||!UInt32.TryParse(parts[1],out pid)||pid==0||!Int32.TryParse(parts[2],out visible)||visible<0||visible>1)throw new IOException("Registro de visibilidad inválido");
                    var h=new IntPtr(handle);uint current;Native.GetWindowThreadProcessId(h,out current);
                    if(!Native.IsWindow(h)||current!=pid){if(visible==1)showAfterRestart=true;continue;}
                    if(!validation(h))continue;applied=true;
                    if(visible==1&&!Native.IsWindowVisible(h)&&!Native.ShowWindowAsync(h,4))throw new IOException("Windows no aceptó restaurar la barra");
                }
                if(!applied&&showAfterRestart)foreach(var h in discovery())if(validation(h)&&!Native.IsWindowVisible(h)&&!Native.ShowWindowAsync(h,4))throw new IOException("Windows no aceptó restaurar la barra nueva");
                File.Delete(path);
            }
            catch(Exception ex){Log.Write("Restauración de visibilidad: "+ex.Message);}
        }
    }
}

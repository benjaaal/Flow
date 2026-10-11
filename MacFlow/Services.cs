using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace MacFlow
{
    public sealed class TaskbarLease
    {
        private readonly string file;
        private readonly TaskbarSurfaces surfaces;
        public TaskbarLease(string directory) { file = Path.Combine(directory, "taskbar.restore");surfaces=new TaskbarSurfaces(file+".visibility"); }
        public void Acquire()
        {
            if (File.Exists(file)) {surfaces.Acquire();return;}
            var original = Native.TaskbarState(); var applied = original | 1;
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            using (var stream = new FileStream(file, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            using (var writer = new StreamWriter(stream)) { writer.Write(original + "|" + applied); writer.Flush(); stream.Flush(true); }
            try
            {
                Process.Start(new ProcessStartInfo(Assembly.GetExecutingAssembly().Location, "--watch " + Process.GetCurrentProcess().Id + " \"" + file + "\"") { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden });
                surfaces.Remember();
                if (!Native.SetTaskbarState(applied)) { Restore(); throw new InvalidOperationException("Windows no aceptó el ocultamiento automático. Podés activarlo desde Configuración > Personalización > Barra de tareas."); }
                surfaces.Acquire();
            }
            catch { Restore(); throw; }
        }
        public void Maintain(){surfaces.Maintain();}
        public void Restore() { surfaces.Restore();RestoreFile(file); }
        public static void RestoreFile(string file)
        {
            try
            {
                TaskbarSurfaces.RestoreFile(file+".visibility");
                if (!File.Exists(file)) return;
                var parts = File.ReadAllText(file).Split('|'); uint oldState, applied;
                if (parts.Length != 2 || !UInt32.TryParse(parts[0], out oldState) || !UInt32.TryParse(parts[1], out applied) || oldState > 3 || applied > 3) return;
                var current = Native.TaskbarState();
                // Respect a change made independently by the user while MacFlow was running.
                if (current == applied && !Native.SetTaskbarState(oldState)) { Log.Write("Restauración de barra de tareas pendiente"); return; }
                if((current==applied||current==oldState)&&(oldState&1)==0)TaskbarSurfaces.RecoverVisible();
                File.Delete(file);
            }
            catch (Exception ex) { Log.Write("Restauración: " + ex.Message); }
        }
    }

    public static class Startup
    {
        public static bool Enabled
        {
            get { bool enabled; TryRead(out enabled); return enabled; }
            set
            {
                using (var key = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run"))
                { if (value) key.SetValue("MacFlow", "\"" + Assembly.GetExecutingAssembly().Location + "\" --background"); else key.DeleteValue("MacFlow", false); }
            }
        }
        public static bool TryRead(out bool enabled)
        {
            enabled = false;
            try { using (var key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run")) enabled = key != null && key.GetValue("MacFlow") != null; return true; }
            catch (Exception ex) { Log.Write("Inicio automático no disponible: " + ex.Message); return false; }
        }
    }

    public static class Catalog
    {
        public static List<AppEntry> Scan()
        {
            var list = new List<AppEntry>();
            list.Add(new AppEntry { Name = "Explorador de archivos", Target = "explorer.exe", Executable = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe") });
            list.Add(new AppEntry { Name = "Configuración de Windows", Target = "ms-settings:", Executable=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),"ImmersiveControlPanel","SystemSettings.exe") });
            object shell = null, links = null;
            try
            {
                links = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));
                foreach (var root in new[] { Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu) })
                {
                    foreach (var file in SafeLinks(root))
                    {
                        object shortcut = null;
                        try
                        {
                            shortcut = ((dynamic)links).CreateShortcut(file); string exe = ((dynamic)shortcut).TargetPath;
                            string name = Path.GetFileNameWithoutExtension(file);
                            if (name.IndexOf("uninstall", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("desinstal", StringComparison.OrdinalIgnoreCase) >= 0 || name.StartsWith("MacFlow")) continue;
                            list.Add(new AppEntry { Name = name, Target = file, Executable = exe });
                        }
                        catch { }
                        finally { if (shortcut != null && Marshal.IsComObject(shortcut)) Marshal.ReleaseComObject(shortcut); }
                    }
                }
                shell = Activator.CreateInstance(Type.GetTypeFromProgID("Shell.Application"));
                object folder = ((dynamic)shell).NameSpace("shell:AppsFolder");
                if (folder != null)
                {
                    object items = ((dynamic)folder).Items();
                    try
                    {
                        int count = ((dynamic)items).Count;
                        for (int i = 0; i < count; i++)
                        {
                            object item = ((dynamic)items).Item(i);
                            try
                            {
                                string name = ((dynamic)item).Name, id = ((dynamic)item).Path;
                                if (!String.IsNullOrWhiteSpace(id) && id.Contains("!") && !name.StartsWith("MacFlow")) {
                                    string icon=null;try{icon=PackageIcon((string)((dynamic)item).ExtendedProperty("System.AppUserModel.PackageInstallPath"),(string)((dynamic)item).ExtendedProperty("System.Tile.SmallLogoPath"));}catch{}
                                    list.Add(new AppEntry { Name = name, Target = "shell:AppsFolder\\" + id,IconPath=icon });
                                }
                            }
                            catch { }
                            finally { if (item != null) Marshal.ReleaseComObject(item); }
                        }
                    }
                    finally { Marshal.ReleaseComObject(items); Marshal.ReleaseComObject(folder); }
                }
            }
            catch (Exception ex) { Log.Write("Catálogo: " + ex.Message); }
            finally { if (shell != null) Marshal.ReleaseComObject(shell); if (links != null) Marshal.ReleaseComObject(links); }
            return list.GroupBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase).Select(p => p.OrderByDescending(a=>a.Target.StartsWith("shell:AppsFolder\\",StringComparison.OrdinalIgnoreCase)).ThenByDescending(a=>!String.IsNullOrEmpty(a.Executable)&&File.Exists(a.Executable)).First()).OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
        }
        private static string PackageIcon(string package,string logo)
        {
            // Read the app's registered local logo; never copy or modify its package.
            if(String.IsNullOrWhiteSpace(package)||String.IsNullOrWhiteSpace(logo))return null;
            string root=Path.GetFullPath(package).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
            string path=Path.GetFullPath(Path.Combine(root,logo));if(!path.StartsWith(root,StringComparison.OrdinalIgnoreCase))return null;
            string prefix=Path.Combine(Path.GetDirectoryName(path),Path.GetFileNameWithoutExtension(path));
            foreach(string suffix in new[]{".targetsize-96_altform-unplated.png",".targetsize-256_altform-unplated.png",".targetsize-96.png",".scale-200.png",".png"})if(File.Exists(prefix+suffix))return prefix+suffix;
            return Directory.GetFiles(Path.GetDirectoryName(path),Path.GetFileNameWithoutExtension(path)+"*.png").OrderByDescending(p=>p.Contains("unplated")).FirstOrDefault();
        }
        private static IEnumerable<string> SafeLinks(string root)
        {
            var result = new List<string>(); if (!Directory.Exists(root)) return result;
            var queue = new Queue<string>(); queue.Enqueue(root);
            while (queue.Count > 0)
            {
                string dir = queue.Dequeue();
                try
                {
                    result.AddRange(Directory.GetFiles(dir, "*.lnk"));
                    foreach (var sub in Directory.GetDirectories(dir)) if ((File.GetAttributes(sub) & FileAttributes.ReparsePoint) == 0) queue.Enqueue(sub);
                }
                catch { }
            }
            return result;
        }
        public static void Launch(AppEntry entry)
        {
            if (entry.Target.StartsWith("shell:AppsFolder\\", StringComparison.OrdinalIgnoreCase)) Process.Start(new ProcessStartInfo("explorer.exe", "\"" + entry.Target + "\"") { UseShellExecute = true });
            else Process.Start(new ProcessStartInfo(entry.Target) { UseShellExecute = true });
        }
    }

    public static class Icons
    {
        [StructLayout(LayoutKind.Sequential)] private struct ImageSize {public int Width,Height;}
        [ComImport,Guid("BCC18B79-BA16-442F-80C4-8A59C30C463B"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] private interface ImageFactory
        {[PreserveSig]int GetImage(ImageSize size,uint flags,out IntPtr bitmap);}
        [DllImport("shell32.dll",CharSet=CharSet.Unicode)] private static extern int SHCreateItemFromParsingName(string name,IntPtr context,ref Guid iid,out ImageFactory item);
        [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr value);
        private static readonly Dictionary<string, ImageSource> Cache = new Dictionary<string, ImageSource>(StringComparer.OrdinalIgnoreCase);
        private static readonly object Gate=new object();
        private static readonly System.Collections.Concurrent.BlockingCollection<string> Requests=new System.Collections.Concurrent.BlockingCollection<string>();
        private static readonly Dictionary<string,List<Action<ImageSource>>> Pending=new Dictionary<string,List<Action<ImageSource>>>(StringComparer.OrdinalIgnoreCase);
        private static bool workerStarted;
        public static void Request(string path,Action<ImageSource> ready)
        {
            if(String.IsNullOrEmpty(path))return;
            lock(Gate)
            {
                List<Action<ImageSource>> callbacks;
                if(Pending.TryGetValue(path,out callbacks)){callbacks.Add(ready);return;}
                Pending[path]=new List<Action<ImageSource>> {ready};Requests.Add(path);
                if(workerStarted)return;workerStarted=true;
                var worker=new Thread(delegate()
                {
                    foreach(var requested in Requests.GetConsumingEnumerable())
                    {
                        ImageSource result=null;try{result=Get(requested);}catch{}
                        List<Action<ImageSource>> waiting;lock(Gate){waiting=Pending[requested];Pending.Remove(requested);}
                        var app=System.Windows.Application.Current;if(app==null||app.Dispatcher.HasShutdownStarted)continue;
                        var icon=result;app.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,new Action(delegate{foreach(var callback in waiting)callback(icon);}));
                    }
                });worker.IsBackground=true;worker.SetApartmentState(ApartmentState.STA);worker.Start();
            }
        }
        private static void Remember(string path,ImageSource image){lock(Gate){if(Cache.Count>384)Cache.Clear();Cache[path]=image;}}
        public static void Warm(IEnumerable<AppEntry> apps)
        {foreach(var app in apps)Get(!String.IsNullOrEmpty(app.IconPath)?app.IconPath:!String.IsNullOrEmpty(app.Target)&&app.Target.StartsWith("shell:AppsFolder\\",StringComparison.OrdinalIgnoreCase)?app.Target:app.Executable??app.Target);}
        public static ImageSource Get(string path)
        {
            if (String.IsNullOrEmpty(path)) return null;
            ImageSource image;lock(Gate){if(Cache.TryGetValue(path,out image))return image;}
            // Shell/PNG extraction must never hold the UI thread or cache lock.
            if(System.Windows.Application.Current!=null&&System.Windows.Application.Current.Dispatcher.CheckAccess())return null;
            return GetLocked(path);
        }
        private static ImageSource GetLocked(string path)
        {
            ImageSource image; lock(Gate){if (Cache.TryGetValue(path, out image)) return image;}
            if(path.EndsWith(".png",StringComparison.OrdinalIgnoreCase))try{var bitmapImage=new BitmapImage();bitmapImage.BeginInit();bitmapImage.CacheOption=BitmapCacheOption.OnLoad;bitmapImage.DecodePixelWidth=96;bitmapImage.UriSource=new Uri(path,UriKind.Absolute);bitmapImage.EndInit();bitmapImage.Freeze();Remember(path,bitmapImage);return bitmapImage;}catch{}
            ImageFactory item=null;IntPtr bitmap=IntPtr.Zero;
            try
            {
                Guid iid=typeof(ImageFactory).GUID;
                // Full extraction is warmed by the catalogue STA thread; UI uses cache only.
                uint flags=System.Windows.Application.Current!=null&&System.Windows.Application.Current.Dispatcher.CheckAccess()?0x11u:1u;
                string parsingName=path;
                if(SHCreateItemFromParsingName(parsingName,IntPtr.Zero,ref iid,out item)==0&&item.GetImage(new ImageSize {Width=96,Height=96},flags,out bitmap)==0&&bitmap!=IntPtr.Zero)
                {image=System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(bitmap,IntPtr.Zero,System.Windows.Int32Rect.Empty,BitmapSizeOptions.FromEmptyOptions());image.Freeze();Remember(path,image);return image;}
            }
            catch{}finally{if(bitmap!=IntPtr.Zero)DeleteObject(bitmap);if(item!=null)Marshal.ReleaseComObject(item);}
            var info = new Native.FILEINFO();
            try
            {
                if (Native.SHGetFileInfo(path, 0, ref info, (uint)Marshal.SizeOf(typeof(Native.FILEINFO)), 0x100) != IntPtr.Zero && info.hIcon != IntPtr.Zero)
                {
                    image = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(info.hIcon, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromWidthAndHeight(64, 64)); image.Freeze();
                    Remember(path,image); return image;
                }
            }
            catch { }
            finally { if (info.hIcon != IntPtr.Zero) Native.DestroyIcon(info.hIcon); }
            return null;
        }
    }
}

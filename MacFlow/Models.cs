using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace MacFlow
{
    internal static class Product
    {
        internal static Version Version {get{var v=typeof(Product).Assembly.GetName().Version;return new Version(v.Major,v.Minor,v.Build);}}
        internal static string VersionText {get{return Version.ToString();}}
        internal static readonly string DefaultFeedUrl=ReadChannel();
        private static string ReadChannel()
        {
            try{using(var stream=typeof(Product).Assembly.GetManifestResourceStream("MacFlow.Channel.json")){if(stream==null)return "";var channel=(DefaultUpdateChannel)new DataContractJsonSerializer(typeof(DefaultUpdateChannel)).ReadObject(stream);return UpdateService.ValidFeed(channel.FeedUrl)?channel.FeedUrl:"";}}catch{return "";}
        }
    }
    [DataContract] internal sealed class DefaultUpdateChannel{[DataMember]public string FeedUrl;}
    [DataContract]
    public sealed class Preferences
    {
        [DataMember] public string Theme = "Light";
        [DataMember] public string Accent = "#8DAEFF";
        [DataMember] public int IconSize = 32;
        [DataMember] public string IconAppearance="Auto";
        [DataMember] public bool ContainedDockApplied;
        public bool UpgradeContainedDock()
        {
            if(ContainedDockApplied)return false;
            IconSize=Math.Min(72,Math.Max(32,IconSize)+4);ContainedDockApplied=true;return true;
        }
        [DataMember] public bool Animations = true;
        [DataMember] public bool? NativeDockAnimations=true;
        [DataMember] public bool AutoHideTaskbar = false;
        [DataMember] public bool ShowTopBar = true;
        [DataMember] public bool HideInFullscreen = true;
        [DataMember] public double GlassOpacity = .09;
        [DataMember] public bool LiveGlass = true;
        [DataMember] public bool? Sounds=true;
        [DataMember] public bool DockAutoHide = false;
        internal bool AnyDockAutoHide{get{return DockAutoHide||HideDockInApps||HideDockInCodex;}}
        internal void SetDockAutoHide(bool enabled){DockAutoHide=enabled;if(!enabled){HideDockInApps=false;HideDockInCodex=false;}}
        [DataMember] public bool HideDockInApps = true;
        [DataMember] public string DockPosition = "Bottom";
        [DataMember] public bool ShowNetwork = true;
        [DataMember] public bool ShowBattery = true;
        [DataMember] public bool ShowVolume = true;
        [DataMember] public bool LowPower = true;
        [DataMember] public bool WindowsMenu = true;
        [DataMember] public string WindowsKeyAction="Search";
        [DataMember] public bool KeyboardSearchApplied;
        [DataMember] public bool? VoiceWakeEnabled=true;
        internal bool UpgradeKeyboardSearch(){if(KeyboardSearchApplied)return false;KeyboardSearchApplied=true;WindowsKeyAction="Search";return true;}
        [DataMember] public bool? ControlCenterGesture=true;[DataMember]public bool? ShowActivityIsland=true;
        [DataMember] public bool ProtectDockEdge = true;
        [DataMember] public bool HideDockInCodex = true;
        [DataMember] public bool AppleWindowControls = false;
        [DataMember] public bool LidAnimation = true;
        [DataMember] public bool WelcomeAfterUpdate = true;
        [DataMember] public bool? ShowSessionGreeting = true;
        [DataMember] public string LastWelcomedVersion = "1.0.0";
        [DataMember] public bool CheckUpdatesAutomatically = true;
        internal bool NeedsUpdateWelcome(Version current){Version previous;return WelcomeAfterUpdate&&(!Version.TryParse(LastWelcomedVersion,out previous)||current>previous);}
        [DataMember] public string WallpaperPath = "";
        [DataMember] public string Hotkey = "AltSpace";
        [DataMember] public bool ShowWidget = false;
        [DataMember] public string UpdateRepository = "";
        [DataMember] public string LocalUpdateFolder = "";
        [DataMember] public string UpdateFeedUrl = Product.DefaultFeedUrl;
        [DataMember] public List<AppEntry> Pins = new List<AppEntry>();
        [OnDeserializing] private void Defaults(StreamingContext context) { HideDockInApps=true;LiveGlass=true;GlassOpacity=.18; DockPosition="Bottom"; ShowNetwork=ShowBattery=ShowVolume=true;WindowsMenu=ProtectDockEdge=HideDockInCodex=LidAnimation=WelcomeAfterUpdate=CheckUpdatesAutomatically=true;LastWelcomedVersion="1.0.0";AppleWindowControls=false;WallpaperPath=""; Hotkey="AltSpace"; UpdateRepository="";LocalUpdateFolder="";UpdateFeedUrl=""; }
        public bool MovePin(string source,string target)
        {
            int from=Pins.FindIndex(p=>p.Key==source),to=Pins.FindIndex(p=>p.Key==target);
            if(from<0||to<0||from==to)return false;var item=Pins[from];Pins.RemoveAt(from);Pins.Insert(to,item);return true;
        }
        public void Validate()
        {
            if (Theme != "Dark" && Theme != "Light" && Theme != "Auto") Theme = "Dark";
            if(WindowsKeyAction!="Disabled"&&WindowsKeyAction!="MacFlow"&&WindowsKeyAction!="Windows"&&WindowsKeyAction!="Search")WindowsKeyAction="Search";
            IconSize = Math.Max(32, Math.Min(72, IconSize));
            if(IconAppearance!="Auto"&&IconAppearance!="Light"&&IconAppearance!="Dark"&&IconAppearance!="Original")IconAppearance="Auto";
            GlassOpacity = Double.IsNaN(GlassOpacity)||Double.IsInfinity(GlassOpacity)?.09:Math.Max(.06, Math.Min(.65, GlassOpacity));
            if (DockPosition != "Bottom" && DockPosition != "Left" && DockPosition != "Right") DockPosition = "Bottom";
            if (Hotkey != "AltSpace" && Hotkey != "CtrlAltSpace" && Hotkey != "CtrlSpace") Hotkey = "AltSpace";
            if (Accent == null || !System.Text.RegularExpressions.Regex.IsMatch(Accent, "^#[0-9A-Fa-f]{6}$")) Accent = "#8DAEFF";
            if (Pins == null) Pins = new List<AppEntry>();
            Pins.RemoveAll(delegate(AppEntry p) { return p == null || String.IsNullOrWhiteSpace(p.Target) || String.IsNullOrWhiteSpace(p.Name); });
            if (Pins.Count > 24) Pins.RemoveRange(24, Pins.Count - 24);
        }
    }

    [DataContract]
    public sealed class AppEntry
    {
        [DataMember] public string Name;
        [DataMember] public string Target;
        [DataMember] public string Executable;
        [DataMember] public string IconPath;
        public string Key { get { return AppIdentity.Key(String.IsNullOrEmpty(Executable)?Target:Executable); } }
        public override string ToString() { return Name; }
    }

    public sealed class WindowEntry
    {
        public IntPtr Handle;
        public uint ProcessId;
        public string Title;
        public string Executable;
        public string Key { get { return AppIdentity.Key(Executable); } }
    }

    internal static class AppIdentity
    {
        internal static string Key(string executable)
        {
            string value=(executable??"").Trim().Replace('/','\\').ToLowerInvariant();
            string file=Path.GetFileName(value);
            // These launchers install each update into a different directory.
            // Keep Player, Studio, Codex and ChatGPT as separate applications.
            if(value.Contains("\\roblox\\versions\\")){
                if(file=="robloxplayerbeta.exe"||file=="robloxplayerlauncher.exe")return "app:roblox:player";
                if(file=="robloxstudiobeta.exe"||file=="robloxstudiolauncherbeta.exe")return "app:roblox:studio";
            }
            if(file=="codex.exe"&&value.Contains("\\openai.codex_"))return "app:openai:codex";
            if(file=="chatgpt.exe"&&value.Contains("\\openai.chatgpt"))return "app:openai:chatgpt";
            return value;
        }
        internal static List<AppEntry> DockEntries(IEnumerable<AppEntry> pins,IEnumerable<WindowEntry> windows)
        {
            var result=new List<AppEntry>();var keys=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach(var entry in pins)if(keys.Add(entry.Key))result.Add(entry);
            foreach(var window in windows)if(!String.IsNullOrEmpty(window.Executable)&&keys.Add(window.Key))result.Add(new AppEntry{Name=Path.GetFileNameWithoutExtension(window.Executable),Target=window.Executable,Executable=window.Executable});
            return result;
        }
    }

    public sealed class SettingsStore
    {
        public readonly string DirectoryPath;
        public string PathName { get { return Path.Combine(DirectoryPath, "settings.json"); } }
        public SettingsStore(string path) { DirectoryPath = path; }
        public Preferences Load()
        {
            try
            {
                using (var stream = File.OpenRead(PathName))
                {
                    var result = (Preferences)new DataContractJsonSerializer(typeof(Preferences)).ReadObject(stream);
                    result.Validate();
                    if(Product.DefaultFeedUrl=="https://github.com/benjaaal/Flow/releases/latest/download/MacFlow-update.json")
                    {
                        if(String.Equals(result.UpdateRepository,"colanabenjamin-spec/Flow",StringComparison.OrdinalIgnoreCase))result.UpdateRepository="benjaaal/Flow";
                        if(String.Equals(result.UpdateFeedUrl,"https://github.com/colanabenjamin-spec/Flow/releases/latest/download/MacFlow-update.json",StringComparison.OrdinalIgnoreCase))result.UpdateFeedUrl=Product.DefaultFeedUrl;
                    }
                    if(String.IsNullOrWhiteSpace(result.UpdateFeedUrl)&&String.IsNullOrWhiteSpace(result.UpdateRepository))result.UpdateFeedUrl=Product.DefaultFeedUrl;return result;
                }
            }
            catch (Exception ex)
            {
                if(!(ex is FileNotFoundException)&&!(ex is DirectoryNotFoundException))Log.Write("Lectura de preferencias: " + ex.Message);
                try{using(var backup=File.OpenRead(PathName+".bak")){var restored=(Preferences)new DataContractJsonSerializer(typeof(Preferences)).ReadObject(backup);restored.Validate();if(String.IsNullOrWhiteSpace(restored.UpdateFeedUrl)&&String.IsNullOrWhiteSpace(restored.UpdateRepository))restored.UpdateFeedUrl=Product.DefaultFeedUrl;Log.Write("Preferencias recuperadas desde la copia anterior.");return restored;}}catch(FileNotFoundException){}catch(DirectoryNotFoundException){}catch(Exception backupError){Log.Write("Copia de preferencias no disponible: "+backupError.Message);}return new Preferences();
            }
        }
        public void Save(Preferences value)
        {
            value.Validate(); Directory.CreateDirectory(DirectoryPath);
            var temp = PathName + ".tmp";
            using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                new DataContractJsonSerializer(typeof(Preferences)).WriteObject(stream, value); stream.Flush(true);
            }
            if (File.Exists(PathName))
            {
                bool previousValid=false;try{using(var old=File.OpenRead(PathName)){previousValid=new DataContractJsonSerializer(typeof(Preferences)).ReadObject(old) is Preferences;}}catch{}
                // A damaged primary must not replace the last valid backup.
                File.Replace(temp, PathName, previousValid?PathName+".bak":null);
            }
            else File.Move(temp, PathName);
        }
    }

    public static class Log
    {
        public static string DirectoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MacFlow");
        private static readonly object Gate = new object();
        public static void Write(string message)
        {
            try { lock (Gate) { Directory.CreateDirectory(DirectoryPath); var p = Path.Combine(DirectoryPath, "MacFlow.log"); if (File.Exists(p) && new FileInfo(p).Length > 512000) File.Delete(p); File.AppendAllText(p, DateTime.Now.ToString("s") + " " + message + Environment.NewLine, Encoding.UTF8); } } catch { }
        }
    }
}

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Win32;
namespace MacFlow
{
    internal sealed class StartupStatus
    {
        internal bool Requested,Valid,Available;
        internal string Description,Target;
    }
    // Per-user Startup shortcuts are a documented Windows mechanism. Never
    // write StartupApproved flags, schedules, services or machine-wide keys.
    public static class Startup
    {
        internal static async System.Threading.Tasks.Task WaitForDesktop(Func<bool> ready,Func<int,System.Threading.Tasks.Task> delay)
        {
            int checks=0;while(!ready()&&checks++<60)await delay(250);
            // Explorer's HWND can precede its first composed frame at logon.
            await delay(750);
        }
        private const string RunKey="Software\\Microsoft\\Windows\\CurrentVersion\\Run";
        private static string LinkPath {get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup),"MacFlow.lnk");}}
        public static bool Enabled {get{bool enabled;TryRead(out enabled);return enabled;}set{Configure(value,ResolveExecutable());}}
        internal static bool Requested {get{return Inspect().Requested;}}
        public static bool TryRead(out bool enabled){var status=Inspect();enabled=status.Valid;return status.Available;}
        internal static StartupStatus Inspect()
        {
            var status=new StartupStatus();bool registryReadable=false;string command=null;bool shortcutPresent=false;
            try{using(var key=Registry.CurrentUser.OpenSubKey(RunKey)){command=key==null?null:key.GetValue("MacFlow") as string;}registryReadable=true;}catch(Exception ex){Log.Write("No se pudo consultar el inicio antiguo: "+ex.Message);}
            string link=null;
            try
            {
                link=LinkPath;status.Available=Directory.Exists(Path.GetDirectoryName(link));shortcutPresent=File.Exists(link);
                if(shortcutPresent){string arguments;status.Target=ReadShortcut(link,out arguments);status.Valid=ValidTarget(status.Target)&&ValidArguments(arguments);}
            }
            catch(Exception ex){Log.Write("Acceso de inicio no disponible: "+ex.Message);}
            status.Available|=registryReadable;status.Requested=shortcutPresent||!String.IsNullOrWhiteSpace(command);
            if(!status.Valid&&!String.IsNullOrWhiteSpace(command))
            {
                string target;bool valid=TryCommand(command,out target)&&ValidTarget(target);if(valid){status.Valid=true;status.Target=target;}else if(!shortcutPresent)status.Target=target;
            }
            status.Description=!status.Available?"Windows no permitió consultar el inicio.":!status.Requested?registryReadable?"Sin acceso de inicio configurado.":"No se pudo consultar el inicio anterior. Podés crear un acceso por usuario.":!status.Valid?"El acceso apunta a una ruta que ya no existe o no es válida. Activá y aplicá para repararlo.":"Acceso registrado. Windows puede deshabilitarlo desde Aplicaciones de inicio.";
            return status;
        }
        internal static string ResolveExecutable()
        {
            string installed=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Programs","MacFlow","MacFlow.exe"),marker=Path.Combine(Path.GetDirectoryName(installed),"install.manifest");
            try{if(ValidTarget(installed)&&File.Exists(marker)){using(var reader=new StreamReader(marker)){if(reader.ReadLine()=="MacFlow install v1")return installed;}}}catch{}
            string current=Assembly.GetExecutingAssembly().Location;if(ValidTarget(current))return current;
            throw new IOException("Instalá MacFlow antes de activar el inicio automático.");
        }
        public static void Configure(bool enabled,string executable)
        {
            string link=LinkPath;
            if(enabled)
            {
                if(!ValidTarget(executable))throw new IOException("No se encontró MacFlow en la ruta de inicio.");
                if(File.Exists(link)){string ignored;var existing=ReadShortcut(link,out ignored);if(!IsMacFlowPath(existing))throw new IOException("Existe un acceso ajeno llamado MacFlow. No se reemplazó.");}
                Directory.CreateDirectory(Path.GetDirectoryName(link));CreateShortcut(link,executable);
            }
            else if(File.Exists(link))
            {
                string ignored;var existing=ReadShortcut(link,out ignored);if(!IsMacFlowPath(existing))throw new IOException("El acceso llamado MacFlow pertenece a otro programa.");File.Delete(link);
            }
            // Remove only our old entry. Failure cannot invalidate a working
            // shortcut; the process mutex prevents a duplicate desktop.
            try{using(var key=Registry.CurrentUser.OpenSubKey(RunKey,true)){if(key!=null)key.DeleteValue("MacFlow",false);}}
            catch(Exception ex){Log.Write("No se pudo quitar el acceso de inicio antiguo: "+ex.Message);if(!enabled)throw new IOException("Windows no permitió quitar el inicio antiguo. Revisá Aplicaciones de inicio.",ex);}
            Log.Write(enabled?"Inicio automático: acceso verificado a "+executable:"Inicio automático desactivado.");
        }
        internal static void CreateShortcut(string link,string executable)
        {
            object shell=null,shortcut=null;
            try
            {
                if(!ValidTarget(executable))throw new IOException("El destino de inicio no es MacFlow.");
                if(File.Exists(link)&&(File.GetAttributes(link)&FileAttributes.ReparsePoint)!=0)throw new IOException("El acceso de inicio está enlazado.");
                shell=Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));shortcut=((dynamic)shell).CreateShortcut(link);
                ((dynamic)shortcut).TargetPath=Path.GetFullPath(executable);((dynamic)shortcut).Arguments="--background";((dynamic)shortcut).WorkingDirectory=Path.GetDirectoryName(Path.GetFullPath(executable));((dynamic)shortcut).IconLocation=executable+",0";((dynamic)shortcut).Description="MacFlow · Inicio automático";((dynamic)shortcut).Save();
            }
            finally{if(shortcut!=null)Marshal.ReleaseComObject(shortcut);if(shell!=null)Marshal.ReleaseComObject(shell);}
            string arguments;string saved=ReadShortcut(link,out arguments);if(!String.Equals(Path.GetFullPath(saved),Path.GetFullPath(executable),StringComparison.OrdinalIgnoreCase)||arguments!="--background")throw new IOException("Windows no guardó correctamente el acceso de inicio.");
        }
        internal static string ReadShortcut(string link,out string arguments)
        {
            object shell=null,shortcut=null;arguments="";
            try{if((File.GetAttributes(link)&FileAttributes.ReparsePoint)!=0)throw new IOException("Acceso de inicio enlazado.");shell=Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));shortcut=((dynamic)shell).CreateShortcut(link);arguments=(string)((dynamic)shortcut).Arguments;return (string)((dynamic)shortcut).TargetPath;}
            finally{if(shortcut!=null)Marshal.ReleaseComObject(shortcut);if(shell!=null)Marshal.ReleaseComObject(shell);}
        }
        internal static bool TryCommand(string command,out string executable)
        {
            executable=null;if(String.IsNullOrWhiteSpace(command))return false;command=command.Trim();string args;
            if(command[0]=='"'){int end=command.IndexOf('"',1);if(end<2)return false;executable=command.Substring(1,end-1);args=command.Substring(end+1).Trim();}
            else{int end=command.IndexOf(' ');executable=end<0?command:command.Substring(0,end);args=end<0?"":command.Substring(end+1).Trim();}
            return IsMacFlowPath(executable)&&ValidArguments(args);
        }
        private static bool ValidArguments(string args){return String.IsNullOrWhiteSpace(args)||String.Equals(args.Trim(),"--background",StringComparison.Ordinal);}
        internal static bool ValidTarget(string target){return IsMacFlowPath(target)&&File.Exists(target);}
        private static bool IsMacFlowPath(string target){try{return !String.IsNullOrWhiteSpace(target)&&Path.IsPathRooted(target)&&String.Equals(Path.GetFileName(target),"MacFlow.exe",StringComparison.OrdinalIgnoreCase);}catch{return false;}}
    }
}

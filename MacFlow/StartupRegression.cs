using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
namespace MacFlow
{
    internal static class StartupRegression
    {
        internal static void Run(string directory,List<string> results)
        {
            Directory.CreateDirectory(directory);string folder=Path.Combine(directory,"ruta con espacios");Directory.CreateDirectory(folder);
            string executable=Path.Combine(folder,"MacFlow.exe");File.Copy(Assembly.GetExecutingAssembly().Location,executable,true);string parsed;
            Check(Startup.TryCommand("\""+executable+"\" --background",out parsed)&&parsed==executable&&Startup.ValidTarget(parsed),"reconoce una ruta real con espacios y argumento de inicio",results);
            Check(Startup.TryCommand("\""+executable+"\"",out parsed)&&Startup.ValidTarget(parsed),"conserva compatibilidad con un acceso antiguo sin argumentos",results);
            Check(Startup.TryCommand("C:\\MacFlow\\MacFlow.exe --background",out parsed),"una ruta sin espacios puede estar sin comillas",results);
            Check(!Startup.TryCommand(executable+" --background",out parsed),"una ruta con espacios sin comillas no se informa como válida",results);
            string missing=Path.Combine(directory,"carpeta eliminada","MacFlow.exe");Check(Startup.TryCommand("\""+missing+"\" --background",out parsed)&&!Startup.ValidTarget(parsed),"una entrada existente a un archivo eliminado no se presenta como inicio funcionando",results);
            Check(!Startup.TryCommand("\"C:\\Windows\\explorer.exe\" --background",out parsed),"rechaza destinos ajenos a MacFlow",results);
            Check(!Startup.TryCommand("\""+executable+"\" --watch 1 archivo",out parsed),"rechaza argumentos distintos del inicio automático",results);
            Check(!Startup.TryCommand("\""+executable,out parsed)&&!Startup.TryCommand("",out parsed),"rechaza comillas incompletas y comandos vacíos",results);
            string link=Path.Combine(directory,"MacFlow-test.lnk");Startup.CreateShortcut(link,executable);string arguments;string target=Startup.ReadShortcut(link,out arguments);
            Check(File.Exists(link)&&String.Equals(target,executable,StringComparison.OrdinalIgnoreCase)&&arguments=="--background","crea y vuelve a leer un acceso nativo real; no modifica el inicio de Windows",results);
            bool rejected=false;try{Startup.CreateShortcut(Path.Combine(directory,"invalid.lnk"),missing);}catch(IOException){rejected=true;}Check(rejected&&!File.Exists(Path.Combine(directory,"invalid.lnk")),"no genera accesos a rutas inexistentes",results);
            int attempts=0,waited=0;Startup.WaitForDesktop(delegate{return ++attempts>=4;},delegate(int ms){waited+=ms;return System.Threading.Tasks.Task.FromResult(0);}).GetAwaiter().GetResult();Check(attempts==4&&waited==1500,"espera Explorer y su primer cuadro sin bloquear el hilo de interfaz",results);
            attempts=waited=0;Startup.WaitForDesktop(delegate{attempts++;return false;},delegate(int ms){waited+=ms;return System.Threading.Tasks.Task.FromResult(0);}).GetAwaiter().GetResult();Check(attempts==61&&waited==15750,"Explorer ausente no impide iniciar indefinidamente",results);
            waited=0;Startup.WaitForDesktop(delegate{return true;},delegate(int ms){waited+=ms;return System.Threading.Tasks.Task.FromResult(0);}).GetAwaiter().GetResult();Check(waited==750,"escritorio disponible evita una espera larga al iniciar",results);
        }
        private static void Check(bool condition,string message,List<string> results){if(!condition)throw new Exception("Inicio: "+message);results.Add("PASS Inicio: "+message);}
    }
}

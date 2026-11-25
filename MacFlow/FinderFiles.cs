using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualBasic.FileIO;
namespace MacFlow
{
    internal static class FinderFiles
    {
        internal static string ChildName(string parent,string name)
        {
            if(String.IsNullOrWhiteSpace(name)||name=="."||name==".."||name.IndexOfAny(Path.GetInvalidFileNameChars())>=0||name.EndsWith(".")||name.EndsWith(" "))throw new IOException("El nombre no es válido para Windows.");
            string result=Path.GetFullPath(Path.Combine(parent,name));if(Path.GetDirectoryName(result).TrimEnd('\\')!=Path.GetFullPath(parent).TrimEnd('\\'))throw new IOException("El nombre no puede cambiar de carpeta.");return result;
        }
        internal static void ValidateSource(string path)
        {
            if(!Path.IsPathRooted(path)||String.Equals(Path.GetPathRoot(path).TrimEnd('\\'),path.TrimEnd('\\'),StringComparison.OrdinalIgnoreCase))throw new IOException("No se puede modificar una raíz de disco.");
            var attributes=File.GetAttributes(path);if((attributes&(FileAttributes.ReparsePoint|FileAttributes.System))!=0)throw new IOException("No se modifica un enlace ni un archivo protegido del sistema.");
        }
        internal static Task Run(Action operation)
        {
            var completion=new TaskCompletionSource<bool>();var thread=new Thread(delegate(){try{operation();completion.SetResult(true);}catch(Exception ex){completion.SetException(ex);}}){IsBackground=true};thread.SetApartmentState(ApartmentState.STA);thread.Start();return completion.Task;
        }
        internal static void Transfer(string source,string folder,bool move)
        {
            ValidateSource(source);if(!Directory.Exists(folder))throw new DirectoryNotFoundException("La carpeta de destino no existe.");
            string full=Path.GetFullPath(source).TrimEnd('\\'),destination=ChildName(folder,Path.GetFileName(full));
            if(String.Equals(full,destination,StringComparison.OrdinalIgnoreCase)||destination.StartsWith(full+"\\",StringComparison.OrdinalIgnoreCase))throw new IOException("El destino no puede ser el origen ni estar dentro de él.");
            if(File.Exists(destination)||Directory.Exists(destination))throw new IOException("Ya existe «"+Path.GetFileName(destination)+"». Elegí otra carpeta o renombrá el archivo.");
            if(Directory.Exists(source)){if(move)FileSystem.MoveDirectory(source,destination,UIOption.AllDialogs,UICancelOption.ThrowException);else FileSystem.CopyDirectory(source,destination,UIOption.AllDialogs,UICancelOption.ThrowException);}
            else{if(move)FileSystem.MoveFile(source,destination,UIOption.AllDialogs,UICancelOption.ThrowException);else FileSystem.CopyFile(source,destination,UIOption.AllDialogs,UICancelOption.ThrowException);}
        }
        internal static void Rename(string source,string name){ValidateSource(source);string target=ChildName(Path.GetDirectoryName(source),name);if(Directory.Exists(source))Directory.Move(source,target);else File.Move(source,target);}
        internal static void Trash(string source){ValidateSource(source);if(Directory.Exists(source))FileSystem.DeleteDirectory(source,UIOption.AllDialogs,RecycleOption.SendToRecycleBin,UICancelOption.ThrowException);else FileSystem.DeleteFile(source,UIOption.AllDialogs,RecycleOption.SendToRecycleBin,UICancelOption.ThrowException);}
    }
}

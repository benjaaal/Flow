using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
namespace MacFlow
{
    [DataContract]
    public sealed class StorageLocation
    {
        [DataMember] public string DataDirectory;
        [DataMember] public string InstallDirectory;
    }
    // Configuration belongs to this copy of MacFlow. Never change Windows'
    // LOCALAPPDATA, Documents, shell folders, services or global environment.
    public static class StoragePaths
    {
        private static readonly StorageLocation current=Read(AppDomain.CurrentDomain.BaseDirectory);
        public static string DataDirectory {get{return current.DataDirectory;}}
        public static string InstallDirectory {get{return current.InstallDirectory;}}
        public static string TemporaryDirectory {get{return Path.Combine(DataDirectory,"Temp");}}
        public static StorageLocation Read(string directory)
        {
            string file=Path.Combine(directory,"macflow.storage.json");
            for(var parent=Directory.GetParent(Path.GetFullPath(directory));!File.Exists(file)&&parent!=null;parent=parent.Parent){string candidate=Path.Combine(parent.FullName,"macflow.storage.json");if(File.Exists(candidate)){file=candidate;break;}}
            if(!File.Exists(file))return new StorageLocation{
                DataDirectory=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MacFlow"),
                InstallDirectory=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Programs","MacFlow")};
            StorageLocation value;
            using(var stream=File.OpenRead(file))value=(StorageLocation)new DataContractJsonSerializer(typeof(StorageLocation)).ReadObject(stream);
            if(value==null)throw new IOException("La configuración de almacenamiento de MacFlow está vacía.");
            value.DataDirectory=Validate(value.DataDirectory);value.InstallDirectory=Validate(value.InstallDirectory);
            if(String.Equals(value.DataDirectory,value.InstallDirectory,StringComparison.OrdinalIgnoreCase))throw new IOException("Los datos y el programa deben usar carpetas diferentes.");
            return value;
        }
        private static string Validate(string value)
        {
            if(String.IsNullOrWhiteSpace(value)||!Path.IsPathRooted(value)||value.StartsWith(@"\\",StringComparison.Ordinal))throw new IOException("MacFlow necesita una ruta local absoluta de almacenamiento.");
            string full=Path.GetFullPath(value).TrimEnd(Path.DirectorySeparatorChar);
            if(full.Length<=3||String.Equals(full,Environment.GetFolderPath(Environment.SpecialFolder.Windows),StringComparison.OrdinalIgnoreCase))throw new IOException("No se puede usar una raíz de disco ni Windows como carpeta de MacFlow.");
            return full;
        }
        public static int WriteDiagnostic(string output)
        {
            using(var stream=File.Create(Path.GetFullPath(output)))new DataContractJsonSerializer(typeof(StorageLocation)).WriteObject(stream,current);
            return Directory.Exists(DataDirectory)&&Directory.Exists(InstallDirectory)?0:2;
        }
    }
}

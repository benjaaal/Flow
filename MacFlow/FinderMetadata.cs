using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
namespace MacFlow
{
    [DataContract] internal sealed class FinderMetadata
    {
        [DataMember] public List<string> Favorites=new List<string>();
        [DataMember] public Dictionary<string,string> Tags=new Dictionary<string,string>();
        [DataMember] public bool ShowHidden;
        internal static FinderMetadata Load(string file){var value=ModuleStore.Read<FinderMetadata>(file);if(value.Favorites==null)value.Favorites=new List<string>();if(value.Tags==null)value.Tags=new Dictionary<string,string>();return value;}
        internal string Tag(string path){string value;return Tags.TryGetValue(path,out value)?value:"";}
        internal List<StackEntry> Scan(string path,CancellationToken cancel)
        {
            var result=new List<StackEntry>();foreach(var item in new DirectoryInfo(path).EnumerateFileSystemInfos()){
                cancel.ThrowIfCancellationRequested();try{if((item.Attributes&FileAttributes.System)!=0||!ShowHidden&&(item.Attributes&FileAttributes.Hidden)!=0)continue;bool folder=(item.Attributes&FileAttributes.Directory)!=0;result.Add(new StackEntry{Path=item.FullName,Name=item.Name,Folder=folder,Modified=item.LastWriteTimeUtc,Size=folder?0:((FileInfo)item).Length});}catch(IOException){}catch(UnauthorizedAccessException){}
            }return result.OrderByDescending(i=>i.Folder).ThenBy(i=>i.Name,StringComparer.CurrentCultureIgnoreCase).ToList();
        }
    }
}

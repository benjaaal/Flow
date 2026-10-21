using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
namespace MacFlow
{
    [DataContract] internal sealed class UpdatePackage
    {
        [DataMember] public string Product="MacFlow";
        [DataMember] public string Architecture="x64";
        [DataMember] public string Version;
        [DataMember] public string Installer;
        [DataMember] public string Sha256;
        [DataMember] public long Size;
        [DataMember] public string Notes;
        [DataMember] public string FeedUrl;
        internal Uri Download;
        internal string LocalFile;
        internal Version ParsedVersion {get{Version value;if(!System.Version.TryParse(Version,out value)||value.Build<0||value.Revision>0)throw new IOException("La versión del paquete no es válida.");return new Version(value.Major,value.Minor,value.Build);}}
    }
    [DataContract] internal sealed class ReleaseInfo { [DataMember] public string tag_name;[DataMember] public List<ReleaseAsset> assets;[DataMember] public string body;[DataMember] public bool draft;[DataMember] public bool prerelease; }
    [DataContract] internal sealed class ReleaseAsset { [DataMember] public string name;[DataMember] public string browser_download_url;[DataMember] public string digest;[DataMember] public long size; }
    public sealed class InstallerMetadataProbe:MarshalByRefObject
    {
        public bool HasPayload(string file){return Assembly.ReflectionOnlyLoadFrom(file).GetManifestResourceNames().Contains("MacFlow.Payload.zip");}
    }
    internal static class UpdateService
    {
        internal const long MaximumSize=256*1024*1024;
        private sealed class Client:WebClient
        {
            protected override WebRequest GetWebRequest(Uri address){if(address.Scheme!="https")throw new IOException("El canal necesita HTTPS.");var request=base.GetWebRequest(address);request.Timeout=20000;var http=request as HttpWebRequest;if(http!=null)http.ReadWriteTimeout=20000;return request;}
            protected override WebResponse GetWebResponse(WebRequest request,IAsyncResult result){var response=base.GetWebResponse(request,result);if(response.ResponseUri.Scheme!="https"){response.Close();throw new IOException("El canal redirigió a una conexión sin HTTPS.");}return response;}
        }
        internal static bool ValidRepository(string value){return Regex.IsMatch(value??"","^[A-Za-z0-9][A-Za-z0-9_.-]*/[A-Za-z0-9][A-Za-z0-9_.-]*$");}
        internal static bool ValidFeed(string value){Uri uri;return Uri.TryCreate(value,UriKind.Absolute,out uri)&&uri.Scheme=="https"&&!String.IsNullOrEmpty(uri.Host)&&String.IsNullOrEmpty(uri.UserInfo)&&String.IsNullOrEmpty(uri.Fragment);}
        internal static void Validate(UpdatePackage package)
        {
            if(package==null||package.Product!="MacFlow"||package.Architecture!="x64")throw new IOException("Este paquete no corresponde a MacFlow para Windows de 64 bits.");
            string name="MacFlow-Setup-"+package.ParsedVersion+".exe";
            if(package.Installer!=name||Path.GetFileName(package.Installer)!=package.Installer)throw new IOException("El nombre del instalador no coincide con la versión.");
            if(!Regex.IsMatch(package.Sha256??"","^[a-fA-F0-9]{64}$")||package.Size<=0)throw new IOException("El paquete no tiene SHA-256 o tamaño válidos.");
            if(package.Size>MaximumSize)throw new IOException("El paquete supera el tamaño admitido por esta versión de MacFlow.");
            if(!String.IsNullOrWhiteSpace(package.FeedUrl)&&!ValidFeed(package.FeedUrl))throw new IOException("El enlace del canal de actualización no es válido.");
        }
        internal static UpdatePackage FromRelease(ReleaseInfo release,string repository,Version current)
        {
            if(!ValidRepository(repository)||release==null||release.draft||release.prerelease)throw new IOException("La publicación no es una versión estable válida.");
            Version version;if(!System.Version.TryParse((release.tag_name??"").TrimStart('v','V'),out version)||version.Build<0||version.Revision>0)throw new IOException("La publicación no informa una versión válida.");version=new Version(version.Major,version.Minor,version.Build);if(version<=current)return null;
            var asset=release.assets==null?null:release.assets.FirstOrDefault(a=>a.name=="MacFlow-Setup-"+version+".exe");
            if(asset==null||!Regex.IsMatch(asset.digest??"","^sha256:[a-fA-F0-9]{64}$"))throw new IOException("La publicación no incluye un instalador verificable.");
            Uri url;string prefix="/"+repository+"/releases/download/";
            if(!Uri.TryCreate(asset.browser_download_url,UriKind.Absolute,out url)||url.Scheme!="https"||url.Host!="github.com"||!String.IsNullOrEmpty(url.UserInfo)||!url.AbsolutePath.StartsWith(prefix,StringComparison.OrdinalIgnoreCase)||!url.AbsolutePath.EndsWith("/"+asset.name,StringComparison.Ordinal)||!String.IsNullOrEmpty(url.Query))throw new IOException("El origen del instalador no corresponde al canal elegido.");
            var package=new UpdatePackage{Version=version.ToString(),Installer=asset.name,Sha256=asset.digest.Substring(7),Size=asset.size,Notes=release.body,Download=url};Validate(package);return package;
        }
        internal static async Task<UpdatePackage> Check(string repository,CancellationToken token)
        {
            if(!ValidRepository(repository))throw new IOException("Todavía no hay un canal de actualizaciones publicado configurado.");
            ServicePointManager.SecurityProtocol=SecurityProtocolType.Tls12;
            using(var client=new Client())using(token.Register(client.CancelAsync))
            {
                client.Headers["User-Agent"]="MacFlow/"+Product.VersionText;client.Headers["Accept"]="application/vnd.github+json";
                byte[] json=await client.DownloadDataTaskAsync("https://api.github.com/repos/"+repository+"/releases/latest");token.ThrowIfCancellationRequested();if(json.Length>1024*1024)throw new IOException("La respuesta de actualización es demasiado grande.");
                using(var stream=new MemoryStream(json))return FromRelease((ReleaseInfo)new DataContractJsonSerializer(typeof(ReleaseInfo)).ReadObject(stream),repository,Product.Version);
            }
        }
        internal static UpdatePackage ReadLocal(string manifest,Version current=null,bool requireNewer=true)
        {
            var info=new FileInfo(manifest);if(!info.Exists||info.Length>65536)throw new IOException("El manifiesto de actualización no es válido.");UpdatePackage package;
            using(var stream=new MemoryStream(System.Text.Encoding.UTF8.GetBytes(File.ReadAllText(manifest))))package=(UpdatePackage)new DataContractJsonSerializer(typeof(UpdatePackage)).ReadObject(stream);Validate(package);if(requireNewer&&package.ParsedVersion<=(current??Product.Version))throw new IOException("Ese paquete tiene la misma versión o una anterior a la instalada.");
            package.LocalFile=Path.Combine(info.DirectoryName,package.Installer);return package;
        }
        internal static UpdatePackage CheckLocalFolder(string folder,Version current)
        {
            if(String.IsNullOrWhiteSpace(folder))return null;
            if(!Path.IsPathRooted(folder)||folder.StartsWith("\\\\",StringComparison.Ordinal))throw new IOException("El canal local necesita una carpeta de esta computadora.");
            if(!Directory.Exists(folder))throw new IOException("La carpeta de actualizaciones ya no está disponible. Elegí el nuevo instalador .exe o conectá un canal HTTPS en Configuración.");
            string manifest=Path.Combine(Path.GetFullPath(folder),"MacFlow-update.json");if(!File.Exists(manifest))return null;
            var package=ReadLocal(manifest,current,false);return package.ParsedVersion>current?package:null;
        }
        internal static bool HasChannel(string repository,string folder,string feed=""){return ValidRepository(repository)||ValidFeed(feed)||!String.IsNullOrWhiteSpace(folder);}
        internal static async Task<UpdatePackage> Check(string repository,string folder,CancellationToken token)
        {return await Check(repository,folder,"",token);}
        internal static async Task<UpdatePackage> Check(string repository,string folder,string feed,CancellationToken token)
        {
            token.ThrowIfCancellationRequested();UpdatePackage best=null;Exception failure=null;bool succeeded=false;
            if(!String.IsNullOrWhiteSpace(folder))try{best=await Task.Run(()=>CheckLocalFolder(folder,Product.Version),token);succeeded=true;}catch(Exception ex){if(ex is OperationCanceledException)throw;failure=ex;}
            if(ValidFeed(feed))try{var online=await CheckFeed(feed,token);succeeded=true;if(online!=null&&(best==null||online.ParsedVersion>best.ParsedVersion))best=online;}catch(Exception ex){if(ex is OperationCanceledException)throw;failure=ex;}
            if(ValidRepository(repository))try{var online=await Check(repository,token);succeeded=true;if(online!=null&&(best==null||online.ParsedVersion>best.ParsedVersion))best=online;}catch(Exception ex){if(ex is OperationCanceledException)throw;failure=ex;}
            token.ThrowIfCancellationRequested();if(!succeeded&&failure!=null)throw failure;return best;
        }
        internal static UpdatePackage ParseFeed(byte[] json,Uri origin,Version current)
        {
            if(origin==null||!ValidFeed(origin.AbsoluteUri)||json==null||json.Length==0||json.Length>65536)throw new IOException("El manifiesto del canal HTTPS no es válido.");
            UpdatePackage package;using(var stream=new MemoryStream(json))package=(UpdatePackage)new DataContractJsonSerializer(typeof(UpdatePackage)).ReadObject(stream);Validate(package);
            if(package.ParsedVersion<=current)return null;
            package.Download=new Uri(origin,package.Installer);package.FeedUrl=origin.AbsoluteUri;return package;
        }
        internal static string ResolveFeed(string feed)
        {
            const string latest="https://github.com/benjaaal/Flow/releases/latest/download/MacFlow-update.json";
            const string complete="https://github.com/benjaaal/Flow/releases/download/v26.5.0/MacFlow-update.json";
#if CORE_BRIDGE
            return String.Equals(feed,latest,StringComparison.OrdinalIgnoreCase)?complete:feed;
#else
            return String.Equals(feed,complete,StringComparison.OrdinalIgnoreCase)?latest:feed;
#endif
        }
        internal static async Task<UpdatePackage> CheckFeed(string feed,CancellationToken token)
        {
            feed=ResolveFeed(feed);
            if(!ValidFeed(feed))throw new IOException("El enlace del canal debe usar HTTPS.");ServicePointManager.SecurityProtocol=SecurityProtocolType.Tls12;
            var request=(HttpWebRequest)WebRequest.Create(feed);request.Timeout=20000;request.ReadWriteTimeout=20000;request.UserAgent="MacFlow/"+Product.VersionText;
            using(token.Register(request.Abort))using(var response=(HttpWebResponse)await request.GetResponseAsync())
            {
                if(!ValidFeed(response.ResponseUri.AbsoluteUri)||response.ContentLength>65536)throw new IOException("La respuesta del canal HTTPS no es válida.");
                using(var source=response.GetResponseStream())using(var memory=new MemoryStream())
                {var buffer=new byte[8192];int count;while((count=await source.ReadAsync(buffer,0,buffer.Length,token))>0){if(memory.Length+count>65536)throw new IOException("El manifiesto de actualización es demasiado grande.");memory.Write(buffer,0,count);}token.ThrowIfCancellationRequested();return ParseFeed(memory.ToArray(),new Uri(feed),Product.Version);}
            }
        }
        internal static UpdatePackage ReadInstaller(string file,Version current=null,bool requireNewer=true,CancellationToken token=default(CancellationToken))
        {
            var info=new FileInfo(file);if(!info.Exists||!String.Equals(info.Extension,".exe",StringComparison.OrdinalIgnoreCase)||info.Length<=0||info.Length>MaximumSize)throw new IOException("Elegí un instalador .exe de MacFlow válido.");
            var metadata=System.Diagnostics.FileVersionInfo.GetVersionInfo(info.FullName);Version version;
            if(metadata.ProductName!="MacFlow"||!System.Version.TryParse(metadata.FileVersion,out version)||version.Build<0)throw new IOException("Este archivo no es un instalador de MacFlow.");
            version=new Version(version.Major,version.Minor,version.Build);if(requireNewer&&version<=(current??Product.Version))throw new IOException("Ese instalador tiene la misma versión o una anterior a la instalada.");
            var package=new UpdatePackage{Version=version.ToString(),Installer="MacFlow-Setup-"+version+".exe",Size=info.Length,LocalFile=info.FullName};
            using(var stream=File.OpenRead(info.FullName))using(var hash=SHA256.Create()){var buffer=new byte[65536];int count;while((count=stream.Read(buffer,0,buffer.Length))>0){token.ThrowIfCancellationRequested();hash.TransformBlock(buffer,0,count,null,0);}hash.TransformFinalBlock(new byte[0],0,0);package.Sha256=BitConverter.ToString(hash.Hash).Replace("-","");}
            Validate(package);VerifyInstallerIdentity(info.FullName,package);token.ThrowIfCancellationRequested();return package;
        }
        internal static void Verify(string file,UpdatePackage package,CancellationToken token)
        {
            Validate(package);var info=new FileInfo(file);if(!info.Exists||info.Length!=package.Size)throw new IOException("La descarga está incompleta o el tamaño no coincide.");
            using(var stream=File.OpenRead(file))using(var hash=SHA256.Create())
            {
                var buffer=new byte[65536];int count;while((count=stream.Read(buffer,0,buffer.Length))>0){token.ThrowIfCancellationRequested();hash.TransformBlock(buffer,0,count,null,0);}hash.TransformFinalBlock(new byte[0],0,0);
                string actual=BitConverter.ToString(hash.Hash).Replace("-","");if(!String.Equals(actual,package.Sha256,StringComparison.OrdinalIgnoreCase))throw new IOException("La integridad no coincide. Este instalador no se ejecutará.");
            }
        }
        internal static async Task<string> Prepare(UpdatePackage package,string cache,IProgress<int> progress,CancellationToken token)
        {
            Validate(package);string folder=Path.Combine(cache,Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);string partial=Path.Combine(folder,"download.partial"),file=Path.Combine(folder,package.Installer);bool complete=false;
            try
            {
                if(package.LocalFile!=null)await Task.Run(delegate{token.ThrowIfCancellationRequested();if(new FileInfo(package.LocalFile).Length!=package.Size)throw new IOException("El tamaño del paquete local no coincide.");File.Copy(package.LocalFile,partial);},token);
                else
                {
                    if(package.Download==null)throw new IOException("El paquete no tiene una fuente disponible.");
                    using(var client=new Client())using(token.Register(client.CancelAsync))
                    {client.DownloadProgressChanged+=delegate(object sender,DownloadProgressChangedEventArgs e){if(e.BytesReceived>package.Size||e.BytesReceived>MaximumSize)client.CancelAsync();if(progress!=null)progress.Report(Math.Min(99,e.ProgressPercentage));};await client.DownloadFileTaskAsync(package.Download,partial);}
                }
                await Task.Run(delegate{Verify(partial,package,token);VerifyInstallerIdentity(partial,package);},token);token.ThrowIfCancellationRequested();File.Move(partial,file);
                if(ValidFeed(package.FeedUrl))using(var manifest=File.Create(Path.Combine(folder,"MacFlow-update.json")))new DataContractJsonSerializer(typeof(UpdatePackage)).WriteObject(manifest,package);
                complete=true;if(progress!=null)progress.Report(100);return file;
            }
            finally{if(!complete){if(File.Exists(partial))File.Delete(partial);if(File.Exists(file))File.Delete(file);string manifest=Path.Combine(folder,"MacFlow-update.json");if(File.Exists(manifest))File.Delete(manifest);if(!Directory.EnumerateFileSystemEntries(folder).Any())Directory.Delete(folder);}}
        }
        internal static void VerifyInstallerIdentity(string file,UpdatePackage package)
        {
            var metadata=System.Diagnostics.FileVersionInfo.GetVersionInfo(file);Version version;
            if(metadata.ProductName!="MacFlow"||!System.Version.TryParse(metadata.FileVersion,out version)||new Version(version.Major,version.Minor,version.Build)!=package.ParsedVersion)throw new IOException("El archivo no identifica la versión esperada de MacFlow.");
            var domain=AppDomain.CreateDomain("MacFlow.PackageMetadata",null,new AppDomainSetup{ApplicationBase=Path.GetDirectoryName(typeof(UpdateService).Assembly.Location)});
            try{var probe=(InstallerMetadataProbe)domain.CreateInstanceAndUnwrap(typeof(InstallerMetadataProbe).Assembly.FullName,typeof(InstallerMetadataProbe).FullName);if(!probe.HasPayload(Path.GetFullPath(file)))throw new IOException("El archivo no contiene el paquete del instalador de MacFlow.");}
            finally{AppDomain.Unload(domain);}
        }
    }
}

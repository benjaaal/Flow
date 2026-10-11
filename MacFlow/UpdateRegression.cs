using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
namespace MacFlow
{
    internal static class UpdateRegression
    {
        private static void Assert(bool condition,string message,List<string> results){if(!condition)throw new Exception(message);results.Add("PASS Actualizaciones: "+message);}
        private static bool Reject(Action action){try{action();return false;}catch(IOException){return true;}}
        internal static void Run(string directory,List<string> results)
        {
            Directory.CreateDirectory(directory);string source=Directory.GetParent(Path.GetDirectoryName(typeof(UpdateRegression).Assembly.Location)).FullName,manifest=Path.Combine(source,"MacFlow-update.json");
            var package=UpdateService.ReadLocal(manifest,new Version(1,0,0));UpdateService.Verify(package.LocalFile,package,CancellationToken.None);UpdateService.VerifyInstallerIdentity(package.LocalFile,package);
            Assert((package.FeedUrl??"")==Product.DefaultFeedUrl,"el canal de publicación se incluye dentro del EXE, sin necesitar manifiesto adjunto al instalar",results);
            var existingStore=new SettingsStore(Path.Combine(directory,"existing-profile"));existingStore.Save(new Preferences{Theme="Dark",IconSize=44,UpdateFeedUrl="",UpdateRepository=""});var existing=existingStore.Load();
            Assert(existing.UpdateFeedUrl==Product.DefaultFeedUrl&&existing.IconSize==44,"una instalación anterior recibe el canal compilado sin perder preferencias",results);
            existing.UpdateFeedUrl="https://example.invalid/custom/MacFlow-update.json";existingStore.Save(existing);Assert(existingStore.Load().UpdateFeedUrl==existing.UpdateFeedUrl,"respeta un canal que el usuario ya eligió",results);
            Assert(package.ParsedVersion==Product.Version,"el manifiesto del EXE construido coincide con su versión, contenido y SHA-256",results);
            string renamed=Path.Combine(directory,"MacFlow enviado por WhatsApp.exe");File.Copy(package.LocalFile,renamed,true);
            var standalone=UpdateService.ReadInstaller(renamed,new Version(1,0,0));UpdateService.Verify(standalone.LocalFile,standalone,CancellationToken.None);
            Assert(standalone.ParsedVersion==Product.Version&&standalone.LocalFile==Path.GetFullPath(renamed),"acepta el instalador solo, renombrado y en otra carpeta, sin manifiesto ni rutas del desarrollador",results);
            Assert(Reject(()=>UpdateService.ReadInstaller(renamed)),"el instalador solo también evita repetir o bajar la versión",results);
            Assert(Reject(()=>UpdateService.ReadInstaller(typeof(UpdateRegression).Assembly.Location,new Version(1,0,0))),"rechaza el ejecutable portable que no contiene instalador",results);
            Assert(UpdateService.ValidFeed("https://example.invalid/updates/MacFlow-update.json")&&!UpdateService.ValidFeed("http://example.invalid/update.json")&&!UpdateService.ValidFeed("https://user:pass@example.invalid/update.json"),"el canal compartible exige HTTPS y no acepta credenciales dentro del enlace",results);
            var origin=new Uri("https://example.invalid/updates/MacFlow-update.json");var remote=UpdateService.ParseFeed(File.ReadAllBytes(manifest),origin,new Version(1,0,0));
            Assert(remote.Download.AbsoluteUri=="https://example.invalid/updates/"+package.Installer&&remote.FeedUrl==origin.AbsoluteUri,"el manifiesto HTTPS resuelve el instalador en su servidor, sin depender de una carpeta local",results);
            var githubFeed=new Uri("https://github.com/FixtureOwner/MacFlow/releases/latest/download/MacFlow-update.json");var githubPackage=UpdateService.ParseFeed(File.ReadAllBytes(manifest),githubFeed,new Version(1,0,0));Assert(githubPackage.Download.AbsoluteUri=="https://github.com/FixtureOwner/MacFlow/releases/latest/download/"+package.Installer,"el instalador usa el enlace estable de Releases y no una ruta temporal de la redirección",results);
            Assert(UpdateService.ParseFeed(File.ReadAllBytes(manifest),origin,Product.Version)==null&&Reject(()=>UpdateService.ParseFeed(new byte[65537],origin,Product.Version)),"el canal HTTPS respeta versión y límite del manifiesto",results);
            Assert(Reject(()=>UpdateService.CheckLocalFolder(Path.Combine(directory,"no-existe"),Product.Version)),"una carpeta perdida informa cómo elegir el EXE en lugar de afirmar que está actualizado",results);
            Assert(UpdateService.CheckLocalFolder(source,new Version(1,0,0)).ParsedVersion==Product.Version,"el canal local detecta el instalador construido sin internet",results);
            Assert(UpdateService.CheckLocalFolder(source,Product.Version)==null&&UpdateService.CheckLocalFolder(directory,Product.Version)==null,"el canal local no repite la misma versión y tolera una carpeta sin paquete",results);
            Assert(Reject(()=>UpdateService.CheckLocalFolder("carpeta-relativa",Product.Version)),"el canal local rechaza una ruta ambigua",results);
            Assert(Reject(()=>UpdateService.ReadLocal(manifest)),"la misma versión o una anterior no se presenta como actualización",results);
            var current=new ReleaseInfo{tag_name="v"+Product.VersionText};Assert(UpdateService.FromRelease(current,"MacFlowProject/MacFlow",Product.Version)==null,"compara contra la versión real instalada",results);
            string next=new Version(Product.Version.Major,Product.Version.Minor,Product.Version.Build+1).ToString();var asset=new ReleaseAsset{name="MacFlow-Setup-"+next+".exe",digest="sha256:"+new string('a',64),size=100,browser_download_url="https://github.com/MacFlowProject/MacFlow/releases/download/v"+next+"/MacFlow-Setup-"+next+".exe"};var release=new ReleaseInfo{tag_name="v"+next,assets=new List<ReleaseAsset>{asset}};
            Assert(UpdateService.FromRelease(release,"MacFlowProject/MacFlow",Product.Version).ParsedVersion>Product.Version,"identifica una publicación estable más nueva",results);
            asset.browser_download_url=asset.browser_download_url.Replace("MacFlowProject/MacFlow","otro/proyecto");Assert(Reject(()=>UpdateService.FromRelease(release,"MacFlowProject/MacFlow",Product.Version)),"rechaza el instalador de otro canal",results);
            var malformed=new UpdatePackage{Version=next,Installer="../MacFlow-Setup-"+next+".exe",Sha256=new string('a',64),Size=100};Assert(Reject(()=>UpdateService.Validate(malformed)),"rechaza rutas que salen del paquete",results);
            string corrupt=Path.Combine(directory,"corrupt.exe");File.Copy(package.LocalFile,corrupt,true);using(var stream=new FileStream(corrupt,FileMode.Open,FileAccess.ReadWrite)){long index=stream.Length/2;stream.Position=index;int value=stream.ReadByte();stream.Position=index;stream.WriteByte((byte)(value^1));}
            Assert(Reject(()=>UpdateService.Verify(corrupt,package,CancellationToken.None)),"detecta un instalador alterado antes de ejecutarlo",results);
            File.WriteAllBytes(corrupt,new byte[]{1,2,3});Assert(Reject(()=>UpdateService.Verify(corrupt,package,CancellationToken.None)),"detecta una descarga incompleta",results);
            string cache=Path.Combine(directory,"cache");string prepared=Task.Run(async delegate{return await UpdateService.Prepare(package,cache,null,CancellationToken.None);}).GetAwaiter().GetResult();Assert(File.Exists(prepared)&&Path.GetDirectoryName(prepared)!=Path.GetDirectoryName(package.LocalFile),"prepara una copia privada y verificada del instalador real, sin instalarlo",results);
            int before=Directory.GetDirectories(cache).Length;package.LocalFile=corrupt;bool failed=false;try{Task.Run(async delegate{return await UpdateService.Prepare(package,cache,null,CancellationToken.None);}).GetAwaiter().GetResult();}catch(IOException){failed=true;}
            Assert(failed&&Directory.GetDirectories(cache).Length==before&&File.Exists(prepared),"un paquete rechazado limpia su descarga parcial y conserva el anterior",results);
            bool canceled=false;using(var cancel=new CancellationTokenSource()){cancel.Cancel();try{Task.Run(async delegate{return await UpdateService.Prepare(package,cache,null,cancel.Token);}).GetAwaiter().GetResult();}catch(OperationCanceledException){canceled=true;}}Assert(canceled&&Directory.GetDirectories(cache).Length==before,"cancelar no deja archivos parciales ni ejecuta un instalador",results);
        }
    }
}

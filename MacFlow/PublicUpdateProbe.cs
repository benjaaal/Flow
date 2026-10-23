using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
namespace MacFlow
{
    internal static class PublicUpdateProbe
    {
        internal static int Run(string directory)
        {
            directory = Path.GetFullPath(directory); Directory.CreateDirectory(directory); var results = new List<string>();
            try { Task.Run(async delegate {
                string feed = Product.DefaultFeedUrl; if (!UpdateService.ValidFeed(feed)) throw new IOException("No hay canal HTTPS integrado.");
                await UpdateService.CheckFeed(feed, CancellationToken.None); results.Add("PASS consulta real desde UpdateService.CheckFeed");
                byte[] json; using (var client = new WebClient()) { client.Headers["User-Agent"] = "MacFlow/" + Product.VersionText; json = await client.DownloadDataTaskAsync(feed); }
                var package = UpdateService.ParseFeed(json, new Uri(feed), new Version(1, 0, 0)); if (package == null) throw new IOException("El canal no devolvió una actualización.");
                if (package.Download.AbsoluteUri != new Uri(new Uri(feed), package.Installer).AbsoluteUri) throw new IOException("La dirección de descarga no es estable."); results.Add("PASS manifiesto público " + package.Version + "; URL estable de descarga");
                string file = await UpdateService.Prepare(package, directory, null, CancellationToken.None); results.Add("PASS descarga real con tamaño, SHA-256, versión, producto y payload verificados");
                string target = Path.Combine(directory, "published-package"); Directory.CreateDirectory(target); File.Copy(file, Path.Combine(target, package.Installer), true); File.WriteAllBytes(Path.Combine(target, "MacFlow-update.json"), json);
                using (var client = new WebClient()) await client.DownloadFileTaskAsync(new Uri(new Uri(feed), package.Installer + ".config"), Path.Combine(target, package.Installer + ".config"));
                results.Add("PASS configuración publicada descargada; no se ejecuta ni instala el paquete");
            }).GetAwaiter().GetResult(); File.WriteAllLines(Path.Combine(directory, "public-update-test.txt"), results); return 0; }
            catch (Exception ex) { results.Add("FAIL " + ex); File.WriteAllLines(Path.Combine(directory, "public-update-test.txt"), results); return 1; }
        }
    }
}

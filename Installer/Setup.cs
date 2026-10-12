using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using MacFlow;

namespace MacFlowSetup
{
    public static class Program
    {
        public static string Target { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "MacFlow"); } }
        private static string cleanupDirectory;
        [STAThread]
        public static int Main(string[] args)
        {
            if (args.Length == 2 && args[0] == "--setup-test") return Installer.Test(args[1]);
            if (args.Length == 2 && args[0] == "--update-visual") return UpdatePresentation.Visual(args[1]);
            bool worker = args.Length == 2 && args[0] == "--uninstall-worker";
            if (worker && !String.Equals(Path.GetFullPath(args[1]), Target, StringComparison.OrdinalIgnoreCase)) return 2;
            if (Path.GetFileName(Assembly.GetExecutingAssembly().Location).Equals("Uninstall.exe", StringComparison.OrdinalIgnoreCase) && !worker)
            {
                var temp = Path.Combine(Path.GetTempPath(), "MacFlow-Uninstall-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(temp);
                var dest = Path.Combine(temp, "MacFlow-Uninstall.exe"); File.Copy(Assembly.GetExecutingAssembly().Location, dest);
                var config = Assembly.GetExecutingAssembly().Location + ".config"; if (File.Exists(config)) File.Copy(config, dest + ".config");
                Process.Start(new ProcessStartInfo(dest, "--uninstall-worker \"" + Target + "\"") { UseShellExecute = false }); return 0;
            }
            if (worker) cleanupDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var app = new Application(); Design.Dark = true; Design.InstallTheme();
            app.DispatcherUnhandledException += delegate(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e) { MessageBox.Show(e.Exception.Message, "MacFlow"); e.Handled = true; };
            var window = new Wizard(worker,args.Contains("--update")||File.Exists(Path.Combine(Target,"MacFlow.exe"))); app.MainWindow = window; int result = app.Run(window);
            if (cleanupDirectory != null) Cleanup(); return result;
        }
        private static void Cleanup()
        {
            try
            {
                var root = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                var dir = Path.GetFullPath(cleanupDirectory); var name = Path.GetFileName(dir);
                if (!dir.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !System.Text.RegularExpressions.Regex.IsMatch(name, "^MacFlow-Uninstall-[a-f0-9]{32}$")) return;
                // A hidden native PowerShell helper waits until the mapped executable has exited.
                string quoted = dir.Replace("'", "''");
                string script = "$ErrorActionPreference='SilentlyContinue'; Wait-Process -Id " + Process.GetCurrentProcess().Id + "; $target='" + quoted + "'; if ((Split-Path -Leaf $target) -match '^MacFlow-Uninstall-[a-f0-9]{32}$') { Remove-Item -LiteralPath (Join-Path $target 'MacFlow-Uninstall.exe') -Force; Remove-Item -LiteralPath (Join-Path $target 'MacFlow-Uninstall.exe.config') -Force; Remove-Item -LiteralPath $target -ErrorAction SilentlyContinue }";
                var shell = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe");
                Process.Start(new ProcessStartInfo(shell, "-NoProfile -NonInteractive -WindowStyle Hidden -EncodedCommand " + Convert.ToBase64String(Encoding.Unicode.GetBytes(script))) { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden });
            }
            catch { }
        }
    }
    public sealed class Wizard : Window
    {
        private readonly bool uninstall;
        private readonly bool updating;
        private bool installing;
        private bool installCommitting;
        private CancellationTokenSource installCancellation;
        private readonly StackPanel body;
        private bool startup, desktopShortcut = true, removeSettings = true;
        public Wizard(bool removing,bool isUpdate=false)
        {
            updating=isUpdate&&!removing;
            uninstall = removing; Title = removing ? "Desinstalar MacFlow" : "Instalar MacFlow"; Width = 620; Height = 510; ResizeMode = ResizeMode.NoResize; WindowStartupLocation = WindowStartupLocation.CenterScreen; Background = Design.Brush("#17202F"); WindowStyle = WindowStyle.SingleBorderWindow;
            var root = new DockPanel { Margin = new Thickness(32) }; var header = new StackPanel(); header.Children.Add(Design.Text("◈  MacFlow", 35, true)); var sub = Design.Text(removing ? "Volvé a tu escritorio original." : "Una nueva forma de sentir tu escritorio.", 16, false); sub.Foreground = Design.Muted; sub.Margin = new Thickness(0, 8, 0, 25); header.Children.Add(sub); DockPanel.SetDock(header, System.Windows.Controls.Dock.Top); root.Children.Add(header);
            body = new StackPanel(); root.Children.Add(body); Content = root; SourceInitialized += delegate { Native.Glass(this, true); };Closing+=delegate(object sender,System.ComponentModel.CancelEventArgs args){if(installing){args.Cancel=true;if(!installCommitting&&installCancellation!=null)installCancellation.Cancel();}}; Intro();
        }
        private void Intro()
        {
            body.Children.Clear();
            if (uninstall)
            {
                var text = Design.Text("El desinstalador cerrará MacFlow, restaurará la barra de tareas\ny quitará sus archivos, accesos directos y entrada de inicio.", 15, false); text.TextWrapping = TextWrapping.Wrap; body.Children.Add(text);
                body.Children.Add(Design.Check("Eliminar también mis preferencias y registros", true, delegate(bool value) { removeSettings = value; }));
                body.Children.Add(Design.Button("Desinstalar MacFlow", Remove, true)); body.Children.Add(Design.Button("Cancelar", Close, false)); return;
            }
            var intro = Design.Text("Dock animado, barra superior, buscador y centro de control.\nAplicación nativa para Windows 11 de 64 bits.", 15, false); intro.TextWrapping = TextWrapping.Wrap; body.Children.Add(intro);
            var location = Design.Text("Se instalará para tu usuario en:\n" + Program.Target, 12, false); location.TextWrapping = TextWrapping.Wrap; location.Foreground = Design.Muted; location.Margin = new Thickness(0, 16, 0, 12); body.Children.Add(location);
            body.Children.Add(Design.Check("Iniciar MacFlow automáticamente con Windows", Startup.Requested, delegate(bool value) { startup = value; })); startup = Startup.Requested;
            body.Children.Add(Design.Check("Crear acceso directo en el escritorio", true, delegate(bool value) { desktopShortcut = value; }));
            var note = Design.Text(updating?"Pantalla negra en todos los monitores y preparación de 2 minutos.\nWindows × 3 o Esc cancela antes de guardar los cambios.":"El ocultamiento de la barra de tareas se activa desde MacFlow.\nNo se necesitan permisos de administrador.", 12, false); note.Foreground = Design.Muted; note.Margin = new Thickness(0, 8, 0, 10); body.Children.Add(note);
            var row = new StackPanel { Orientation = Orientation.Horizontal }; row.Children.Add(Design.Button(updating?"Actualizar":"Instalar", Install, true)); row.Children.Add(Design.Button("Cancelar", Close, false)); body.Children.Add(row);
        }
        private async void Install()
        {
            if(installing)return;installing=true;body.IsEnabled=false;installCancellation=new CancellationTokenSource();UpdatePresentation presentation=null;bool stopped=false;
            try
            {
                if(updating)presentation=new UpdatePresentation(installCancellation);
                await System.Threading.Tasks.Task.Run(delegate
                {
                    Installer.StopDesktop();stopped=true;installCancellation.Token.ThrowIfCancellationRequested();if(presentation!=null)Dispatcher.Invoke(new Action(presentation.Cover));
                    Installer.Install(Program.Target,true,startup,desktopShortcut,installCancellation.Token,presentation==null?(Action)null:presentation.WaitBeforeCommit,delegate{Dispatcher.Invoke(new Action(delegate{installCancellation.Token.ThrowIfCancellationRequested();installCommitting=true;if(presentation!=null)presentation.Commit();}));});
                });
                if(presentation!=null){presentation.Complete();await System.Threading.Tasks.Task.Delay(400);presentation.Dispose();presentation=null;}
                CompleteInstall(delegate{Process.Start(new ProcessStartInfo(Path.Combine(Program.Target,"MacFlow.exe")){UseShellExecute=true});});
            }
            catch(OperationCanceledException){if(presentation!=null){presentation.Dispose();presentation=null;}if(stopped&&File.Exists(Path.Combine(Program.Target,"MacFlow.exe")))try{Process.Start(new ProcessStartInfo(Path.Combine(Program.Target,"MacFlow.exe")){UseShellExecute=true});}catch{}Intro();MessageBox.Show("Actualización cancelada. Se conservó la instalación anterior.","MacFlow");}
            catch (Exception ex) { if(presentation!=null){presentation.Dispose();presentation=null;}MessageBox.Show("No se pudo instalar MacFlow.\n" + ex.Message, "MacFlow", MessageBoxButton.OK, MessageBoxImage.Error); }
            finally{if(presentation!=null)presentation.Dispose();installing=false;installCommitting=false;body.IsEnabled=true;installCancellation.Dispose();installCancellation=null;}
        }
        internal void CompleteInstall(Action launch)
        {
            // Closing previously vetoed our own successful Close() while
            // installing was still true, leaving an empty success page behind.
            installing=false;installCommitting=false;Hide();Close();launch();
        }
        internal static void TestCompletion(List<string> results)
        {
            if(Application.Current==null)new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};Design.Dark=true;Design.InstallTheme();
            var wizard=new Wizard(false,true){ShowActivated=false,Left=-2400,Top=-2400,WindowStartupLocation=WindowStartupLocation.Manual};wizard.SourceInitialized+=delegate{Native.NoActivate(wizard);};wizard.Show();wizard.installing=wizard.installCommitting=true;int closed=0,launched=0;wizard.Closed+=delegate{closed++;};wizard.CompleteInstall(delegate{if(wizard.IsVisible||closed!=1)throw new Exception("El instalador no cerró antes de iniciar MacFlow");launched++;});if(closed!=1||launched!=1||wizard.installing||wizard.installCommitting)throw new Exception("Finalización del asistente bloqueada");results.Add("PASS asistente de actualización cierra antes de iniciar MacFlow, sin página de instalación terminada.");
        }
        private void Remove()
        {
            try
            {
                Installer.StopDesktop(); var data = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MacFlow"); new TaskbarLease(data).Restore(); Wallpapers.Restore(data);
                if (File.Exists(Path.Combine(data, "taskbar.restore"))||File.Exists(Path.Combine(data,"taskbar.restore.visibility"))) throw new IOException("La restauración de la barra de tareas está pendiente. Ejecutá MacFlow.exe --restore y volvé a intentar la desinstalación.");
                Installer.Remove(Program.Target, true, removeSettings); body.Children.Clear(); body.Children.Add(Design.Text("MacFlow se desinstaló", 25, true)); var t = Design.Text("Tu barra de tareas está restaurada.\nGracias por usar MacFlow.", 15, false); t.Margin = new Thickness(0, 22, 0, 24); body.Children.Add(t); body.Children.Add(Design.Button("Finalizar", Close, true));
            }
            catch (Exception ex) { MessageBox.Show("No se pudo completar la desinstalación.\n" + ex.Message, "MacFlow", MessageBoxButton.OK, MessageBoxImage.Error); }
        }
    }
    public static class Installer
    {
        private const string RegistryPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\MacFlow";
        private const string Marker = "MacFlow install v1";
        private static readonly string[] Owned = { "MacFlow.exe", "MacFlow.exe.config", "MacFlow.ico", "README.txt", "LICENSE.txt", "THIRD-PARTY-NOTICES.md", "Uninstall.exe", "Uninstall.exe.config", "install.manifest" };
        private static string Identity { get { return System.Security.Principal.WindowsIdentity.GetCurrent().User.Value; } }
        public static void StopDesktop()
        {
            try { using (var stop = EventWaitHandle.OpenExisting("Local\\MacFlow.Stop." + Identity)) stop.Set(); } catch (WaitHandleCannotBeOpenedException) { }
            try
            {
                using (var mutex = Mutex.OpenExisting("Local\\MacFlow.Instance." + Identity))
                {
                    bool entered = false; try { entered = mutex.WaitOne(10000); } catch (AbandonedMutexException) { entered = true; }
                    if (!entered) throw new IOException("MacFlow todavía está abierto. Cerralo desde la bandeja e intentá nuevamente."); mutex.ReleaseMutex();
                }
            }
            catch (WaitHandleCannotBeOpenedException) { }
        }
        public static void CheckDirectory(string path)
        {
            path = Path.GetFullPath(path); if (path.Length <= Path.GetPathRoot(path).Length) throw new IOException("Ruta de instalación inválida.");
            var info = new DirectoryInfo(path);
            while (info != null) { if (info.Exists && (info.Attributes & FileAttributes.ReparsePoint) != 0) throw new IOException("No se permiten enlaces o uniones en la ruta de instalación."); info = info.Parent; }
        }
        public static void Install(string target, bool register, bool startup, bool shortcut,CancellationToken cancellation=default(CancellationToken),Action beforeCommit=null,Action commitStarted=null,Action<int> copied=null)
        {
            cancellation.ThrowIfCancellationRequested();
            target = Path.GetFullPath(target); CheckDirectory(target);
            if (Directory.Exists(target) && Directory.EnumerateFileSystemEntries(target).Any() && !HasMarker(target)) throw new IOException("La carpeta de destino contiene archivos que no pertenecen a MacFlow.");
            Directory.CreateDirectory(target); var stage = Path.Combine(target, ".stage-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(stage);
            var backup = Path.Combine(stage, "backup"); Directory.CreateDirectory(backup); var changed = new List<string>(); bool committed = false;
            try
            {
                using (var payload = Assembly.GetExecutingAssembly().GetManifestResourceStream("MacFlow.Payload.zip"))
                {
                    if (payload == null) throw new IOException("Falta el paquete de instalación.");
                    using (var zip = new ZipArchive(payload, ZipArchiveMode.Read))
                    {
                        foreach (var entry in zip.Entries)
                        {
                            cancellation.ThrowIfCancellationRequested();
                            if (!Owned.Contains(entry.FullName) || entry.FullName != Path.GetFileName(entry.FullName) || entry.Length > 20000000) throw new IOException("Archivo no permitido en el paquete.");
                            entry.ExtractToFile(Path.Combine(stage, entry.FullName));
                        }
                    }
                }
                File.Copy(Assembly.GetExecutingAssembly().Location, Path.Combine(stage, "Uninstall.exe"));
                File.Copy(Path.Combine(stage, "MacFlow.exe.config"), Path.Combine(stage, "Uninstall.exe.config"));
                File.WriteAllLines(Path.Combine(stage, "install.manifest"), new[] { Marker, Product.VersionText }.Concat(Owned));
                if(beforeCommit!=null)beforeCommit();cancellation.ThrowIfCancellationRequested();
                foreach (var name in Owned)
                {
                    cancellation.ThrowIfCancellationRequested();
                    string dest = Path.Combine(target, name); if (File.Exists(dest)) { if ((File.GetAttributes(dest) & FileAttributes.ReparsePoint) != 0) throw new IOException("Archivo de destino enlazado."); File.Copy(dest, Path.Combine(backup, name)); }
                    if (!File.Exists(Path.Combine(stage, name))) throw new IOException("Paquete incompleto: " + name);
                    changed.Add(name); File.Copy(Path.Combine(stage, name), dest, true);
                    if(copied!=null)copied(changed.Count);
                }
                cancellation.ThrowIfCancellationRequested();if(commitStarted!=null)commitStarted();cancellation.ThrowIfCancellationRequested();
                if (register) Register(target, startup, shortcut); committed = true;
            }
            finally
            {
                if (!committed)
                {
                    foreach (var name in changed) { var dest = Path.Combine(target, name); var old = Path.Combine(backup, name); if (File.Exists(old)) File.Copy(old, dest, true); else if (File.Exists(dest)) File.Delete(dest); }
                }
                foreach (var name in Owned) { var a = Path.Combine(stage, name); if (File.Exists(a)) File.Delete(a); var b = Path.Combine(backup, name); if (File.Exists(b)) File.Delete(b); }
                Directory.Delete(backup); Directory.Delete(stage); if (!Directory.EnumerateFileSystemEntries(target).Any()) Directory.Delete(target);
            }
        }
        private static bool HasMarker(string path)
        {
            var marker = Path.Combine(path, "install.manifest"); return File.Exists(marker) && File.ReadLines(marker).FirstOrDefault() == Marker;
        }
        private static void Register(string target, bool startup, bool desktop)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(RegistryPath))
            {
                key.SetValue("DisplayName", "MacFlow"); key.SetValue("DisplayVersion", Product.VersionText); key.SetValue("Publisher", "MacFlow"); key.SetValue("InstallLocation", target); key.SetValue("DisplayIcon", Path.Combine(target, "MacFlow.exe")); key.SetValue("UninstallString", "\"" + Path.Combine(target, "Uninstall.exe") + "\""); key.SetValue("NoModify", 1, RegistryValueKind.DWord); key.SetValue("NoRepair", 1, RegistryValueKind.DWord); key.SetValue("EstimatedSize", (int)(Owned.Where(n => File.Exists(Path.Combine(target, n))).Sum(n => new FileInfo(Path.Combine(target, n)).Length) / 1024), RegistryValueKind.DWord);
            }
            Startup.Configure(startup,Path.Combine(target,"MacFlow.exe"));
            var menu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "MacFlow"); Directory.CreateDirectory(menu);
            Shortcut(Path.Combine(menu, "MacFlow.lnk"), Path.Combine(target, "MacFlow.exe")); Shortcut(Path.Combine(menu, "Desinstalar MacFlow.lnk"), Path.Combine(target, "Uninstall.exe"));
            if (desktop) Shortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "MacFlow.lnk"), Path.Combine(target, "MacFlow.exe"));
            try{RememberLocalChannel(Assembly.GetExecutingAssembly().Location,Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MacFlow"));}catch(Exception ex){Log.Write("Canal local no conectado: "+ex.Message);}
        }
        internal static bool RememberLocalChannel(string setupFile,string data)
        {
            string folder=Path.GetDirectoryName(Path.GetFullPath(setupFile)),manifest=Path.Combine(folder,"MacFlow-update.json");if(!File.Exists(manifest))return false;
            var package=UpdateService.ReadLocal(manifest,null,false);if(package.ParsedVersion!=Product.Version||!String.Equals(Path.GetFullPath(package.LocalFile),Path.GetFullPath(setupFile),StringComparison.OrdinalIgnoreCase))return false;
            UpdateService.Verify(setupFile,package,CancellationToken.None);var store=new SettingsStore(data);var preferences=store.Load();if(UpdateService.ValidFeed(package.FeedUrl))preferences.UpdateFeedUrl=package.FeedUrl;else preferences.LocalUpdateFolder=folder;store.Save(preferences);return true;
        }
        private static void Shortcut(string file, string target)
        {
            object shell = null, shortcut = null;
            try { shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell")); shortcut = ((dynamic)shell).CreateShortcut(file); ((dynamic)shortcut).TargetPath = target; ((dynamic)shortcut).WorkingDirectory = Path.GetDirectoryName(target); ((dynamic)shortcut).IconLocation = target + ",0"; ((dynamic)shortcut).Save(); }
            finally { if (shortcut != null) System.Runtime.InteropServices.Marshal.ReleaseComObject(shortcut); if (shell != null) System.Runtime.InteropServices.Marshal.ReleaseComObject(shell); }
        }
        public static void Remove(string target, bool unregister, bool settings)
        {
            target = Path.GetFullPath(target); CheckDirectory(target); if (!HasMarker(target)) throw new IOException("No se encontró una instalación válida de MacFlow.");
            // Never recurse or delete user files. The fixed ownership list cannot escape target.
            foreach (var name in Owned) { var file = Path.Combine(target, name); if (File.Exists(file)) { if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0) throw new IOException("Archivo enlazado: " + name); File.Delete(file); } }
            if (!Directory.EnumerateFileSystemEntries(target).Any()) Directory.Delete(target);
            if (unregister)
            {
                Startup.Configure(false,Path.Combine(target,"MacFlow.exe"));
                Registry.CurrentUser.DeleteSubKeyTree(RegistryPath, false);
                var menu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "MacFlow"); CheckDirectory(menu);
                foreach (var name in new[] { "MacFlow.lnk", "Desinstalar MacFlow.lnk" }) if (File.Exists(Path.Combine(menu, name))) File.Delete(Path.Combine(menu, name));
                if (Directory.Exists(menu) && !Directory.EnumerateFileSystemEntries(menu).Any()) Directory.Delete(menu);
                string shortcut = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "MacFlow.lnk"); if (File.Exists(shortcut)) File.Delete(shortcut);
                if (settings)
                {
                    var data = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MacFlow"); CheckDirectory(data);
                    foreach (var name in new[] { "settings.json", "settings.json.tmp", "MacFlow.log", "widgets.json", "widgets.json.new", "widgets.json.bak", "stage-manager.json", "stage-manager.json.new", "stage-manager.json.bak" }) { var file = Path.Combine(data, name); if (File.Exists(file)) File.Delete(file); }
                    var wallpapers=Path.Combine(data,"Wallpapers");CheckDirectory(wallpapers);
                    var generated=Enumerable.Range(0,4).Select(i=>"MacFlow-"+i+".jpg").Concat(new[]{"MacFlow-Aqua-1.png","MacFlow-Midnight-1.png","MacFlow-Sierra-1.png"});foreach(var name in generated){var file=Path.Combine(wallpapers,name);if(File.Exists(file)){if((File.GetAttributes(file)&FileAttributes.ReparsePoint)!=0)throw new IOException("Fondo enlazado.");File.Delete(file);}}
                    if(Directory.Exists(wallpapers)&&!Directory.EnumerateFileSystemEntries(wallpapers).Any())Directory.Delete(wallpapers);
                    if (Directory.Exists(data) && !Directory.EnumerateFileSystemEntries(data).Any()) Directory.Delete(data);
                }
            }
        }
        public static int Test(string directory)
        {
            directory = Path.GetFullPath(directory); Directory.CreateDirectory(directory); var results = new List<string>();
            try
            {
                var target = Path.Combine(directory, "MacFlow"); Install(target, false, false, false);
                if (!Owned.All(n => File.Exists(Path.Combine(target, n)))) throw new Exception("Faltan archivos instalados"); results.Add("PASS extracción del payload y manifiesto completo");
                Install(target, false, false, false); results.Add("PASS actualización de instalación existente");
                File.WriteAllText(Path.Combine(target,"README.txt"),"Versión anterior de prueba");var originals=Owned.ToDictionary(name=>name,name=>File.ReadAllBytes(Path.Combine(target,name)));
                using(var cancellation=new CancellationTokenSource())
                {
                    bool canceled=false;try{Install(target,false,false,false,cancellation.Token,delegate{cancellation.Cancel();});}catch(OperationCanceledException){canceled=true;}
                    if(!canceled||!Owned.All(name=>originals[name].SequenceEqual(File.ReadAllBytes(Path.Combine(target,name))))||Directory.EnumerateDirectories(target,".stage-*").Any())throw new Exception("Cancelar antes de aplicar perdió la instalación anterior");results.Add("PASS cancelar preparación: conserva archivos anteriores y limpia el paquete temporal.");
                }
                using(var cancellation=new CancellationTokenSource())
                {
                    bool canceled=false;try{Install(target,false,false,false,cancellation.Token,null,null,count=>{if(count==4)cancellation.Cancel();});}catch(OperationCanceledException){canceled=true;}
                    if(!canceled||!Owned.All(name=>originals[name].SequenceEqual(File.ReadAllBytes(Path.Combine(target,name)))))throw new Exception("Cancelar durante la copia no restauró archivos");results.Add("PASS cancelar durante la copia: restaura byte por byte los archivos reemplazados.");
                }
                var taps=new UpdateTapCounter();var time=DateTime.UtcNow;if(taps.Tap(time)||taps.Tap(time.AddMilliseconds(400))||!taps.Tap(time.AddMilliseconds(800))||taps.Tap(time.AddSeconds(8))||taps.Tap(time.AddSeconds(12.1)))throw new Exception("Triple Windows o reinicio por pausa");results.Add("PASS Windows × 3: sólo cancela en la tercera pulsación; una pausa reinicia la secuencia.");
                using(var cancellation=new CancellationTokenSource())
                {
                    cancellation.Cancel();bool canceled=false;var watch=Stopwatch.StartNew();try{UpdatePresentation.WaitPreparation(DateTime.UtcNow.AddSeconds(120),cancellation.Token);}catch(OperationCanceledException){canceled=true;}if(!canceled||watch.ElapsedMilliseconds>500||UpdatePresentation.PreparationSeconds!=120)throw new Exception("Preparación no cancelable");results.Add("PASS preparación de 120 segundos configurada; cancelación inmediata, sin esperar 2 minutos en las pruebas.");
                }
                var profile=Path.Combine(directory,"profile");var store=new SettingsStore(profile);store.Save(new Preferences{Theme="Dark",IconSize=36,GlassOpacity=.113});
                string executable=Assembly.GetExecutingAssembly().Location;if(!RememberLocalChannel(executable,profile)||store.Load().IconSize!=36||Math.Abs(store.Load().GlassOpacity-.113)>.001)throw new Exception("Canal local o preferencias alteradas");results.Add("PASS canal local vinculado al instalador real, conservando preferencias");
                string oldFolder=store.Load().LocalUpdateFolder;if(RememberLocalChannel(Path.Combine(profile,"cache","MacFlow.exe"),profile)||store.Load().LocalUpdateFolder!=oldFolder)throw new Exception("Se perdió el canal al preparar un paquete");results.Add("PASS un instalador sin manifiesto conserva el canal anterior");
                var linked=UpdateService.ReadLocal(Path.Combine(Path.GetDirectoryName(executable),"MacFlow-update.json"),null,false);linked.FeedUrl="https://example.invalid/updates/MacFlow-update.json";string feedFolder=Path.Combine(profile,"shared-feed");Directory.CreateDirectory(feedFolder);string feedInstaller=Path.Combine(feedFolder,linked.Installer);File.Copy(executable,feedInstaller,true);
                using(var stream=File.Create(Path.Combine(feedFolder,"MacFlow-update.json")))new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(UpdatePackage)).WriteObject(stream,linked);
                if(!RememberLocalChannel(feedInstaller,profile)||store.Load().UpdateFeedUrl!=linked.FeedUrl||store.Load().LocalUpdateFolder!=oldFolder||store.Load().IconSize!=36)throw new Exception("El canal compartido no se conserva al actualizar");results.Add("PASS instala enlace HTTPS desde paquete compartido y conserva carpeta anterior y preferencias; sin red.");
                File.WriteAllText(Path.Combine(target, "user-file.txt"), "preserve"); Remove(target, false, false);
                if (Owned.Any(n => File.Exists(Path.Combine(target, n))) || !File.Exists(Path.Combine(target, "user-file.txt"))) throw new Exception("Desinstalación incorrecta"); results.Add("PASS desinstalación: elimina archivos propios y conserva archivos ajenos");
                bool refused = false; try { Install(target, false, false, false); } catch (IOException) { refused = true; } if (!refused) throw new Exception("No rechazó la carpeta ocupada"); results.Add("PASS rechazo de carpeta ajena sin manifiesto");
                Wizard.TestCompletion(results);File.Delete(Path.Combine(target, "user-file.txt")); Directory.Delete(target); File.WriteAllLines(Path.Combine(directory, "installer-tests.txt"), results); return 0;
            }
            catch (Exception ex) { results.Add("FAIL " + ex); File.WriteAllLines(Path.Combine(directory, "installer-tests.txt"), results); return 1; }
        }
    }
}

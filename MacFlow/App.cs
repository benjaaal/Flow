using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace MacFlow
{
    public static class Program
    {
        public static string Identity { get { return System.Security.Principal.WindowsIdentity.GetCurrent().User.Value; } }
        public static string StopEvent { get { return "Local\\MacFlow.Stop." + Identity; } }
        public static string SettingsEvent { get { return "Local\\MacFlow.Settings." + Identity; } }
        public static string WelcomeEvent { get { return "Local\\MacFlow.Welcome." + Identity; } }
        internal static bool SignalSettings(string name)
        {try{using(var signal=EventWaitHandle.OpenExisting(name)){return signal.Set();}}catch(WaitHandleCannotBeOpenedException){return false;}}
        public static string MutexName { get { return "Local\\MacFlow.Instance." + Identity; } }
        [STAThread]
        public static int Main(string[] args)
        {
            var data = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MacFlow");
            if (args.Length == 3 && args[0] == "--watch")
            {
                int pid; if (!Int32.TryParse(args[1], out pid)) return 2;
                string expected = Path.Combine(data, "taskbar.restore"); if (!String.Equals(Path.GetFullPath(args[2]), expected, StringComparison.OrdinalIgnoreCase)) return 2;
                try { Process.GetProcessById(pid).WaitForExit(); } catch (ArgumentException) { } catch (Exception ex) { Log.Write("Supervisor: " + ex.Message); return 2; }
                TaskbarLease.RestoreFile(expected); return 0;
            }
            if (args.Length > 0 && args[0] == "--restore") { new TaskbarLease(data).Restore();TaskbarSurfaces.RecoverVisible(); return 0; }
            if (args.Length == 2 && args[0] == "--self-test") return SelfTests.Run(args[1]);
            if (args.Length == 2 && args[0] == "--icons-smoke") return IconsVisual.Run(args[1]);
            if (args.Length == 2 && args[0] == "--media-smoke") return MediaRegression.Probe(args[1]);
            if (args.Length == 2 && args[0] == "--connections-smoke") return ConnectivityVisual.Run(args[1]);
            if (args.Length == 2 && args[0] == "--public-update-test") return PublicUpdateProbe.Run(args[1]);
            if (args.Length == 2 && args[0] == "--features-smoke") return NewFeaturesVisual.Run(args[1]);
            if (args.Length == 2 && args[0] == "--panel-dock-smoke") return PanelDockVisual.Run(args[1]);
            if (args.Length == 2 && args[0] == "--glass-smoke") return GlassVisual.Run(args[1]);
            if (args.Length == 2 && args[0] == "--welcome-smoke") return WelcomeVisual.Run(args[1]);
            if (args.Length == 2 && args[0] == "--fold-smoke") return FoldVisual.Run(args[1]);
            if (args.Length == 2 && args[0] == "--wallpaper-integration") return WallpaperIntegration.Run(args[1]);
            if (args.Length == 2 && args[0] == "--topbar-smoke") return TopBarVisual.Run(args[1]);
            if (args.Length == 2 && args[0] == "--controls-smoke") return ControlsVisual.Run(args[1]);
            if (args.Length == 2 && args[0] == "--dock-smoke") return DockVisual.Run(args[1]);
            if (args.Length == 2 && args[0] == "--caption-test") return CaptionRegression.Run(args[1]);
            bool preview = args.Length == 2 && args[0] == "--preview";
            bool smoke = args.Length == 2 && (args[0] == "--smoke" || preview);
            if (smoke) Log.DirectoryPath = Path.Combine(Path.GetFullPath(args[1]), "profile");
            bool created;
            using (var mutex = new Mutex(true, smoke ? MutexName + ".Smoke" : MutexName, out created))
            {
                if (!created) { if(!smoke&&!(args.Length>0&&args[0]=="--background")){for(int attempt=0;attempt<20;attempt++){if(SignalSettings(args.Length>0&&args[0]=="--welcome"?WelcomeEvent:SettingsEvent))return 0;Thread.Sleep(50);}}return 0; }
                var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown }; Desktop desktop = null;
                app.DispatcherUnhandledException += delegate(object sender, DispatcherUnhandledExceptionEventArgs e)
                {
                    Log.Write("Error UI: " + e.Exception); if (!smoke) new TaskbarLease(data).Restore(); e.Handled = true;
                    if(!smoke)MessageBox.Show("MacFlow encontró un error y restauró la barra de tareas.\n" + e.Exception.Message, "MacFlow");else File.WriteAllText(Path.Combine(args[1],"smoke.txt"),"FAIL: "+e.Exception);app.Shutdown(1);
                };
                AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e) { Log.Write("Error de proceso: " + e.ExceptionObject); if (!smoke) new TaskbarLease(data).Restore(); };
                using (var stop = new EventWaitHandle(false, EventResetMode.AutoReset, smoke ? StopEvent + ".Smoke" : StopEvent))
                using (var settings = new EventWaitHandle(false, EventResetMode.AutoReset, smoke ? SettingsEvent + ".Smoke" : SettingsEvent))
                using (var greeting = new EventWaitHandle(false, EventResetMode.AutoReset, smoke ? WelcomeEvent + ".Smoke" : WelcomeEvent))
                {
                    var wait = ThreadPool.RegisterWaitForSingleObject(stop, delegate(object state, bool timeout) { app.Dispatcher.BeginInvoke(new Action(delegate { app.Shutdown(); })); }, null, Timeout.Infinite, true);
                    var settingsWait=ThreadPool.RegisterWaitForSingleObject(settings,delegate(object state,bool timeout){if(!app.Dispatcher.HasShutdownStarted)app.Dispatcher.BeginInvoke(new Action(delegate{if(desktop!=null)desktop.OpenSettings();}));},null,Timeout.Infinite,false);
                    var greetingWait=ThreadPool.RegisterWaitForSingleObject(greeting,delegate(object state,bool timeout){if(!app.Dispatcher.HasShutdownStarted)app.Dispatcher.BeginInvoke(new Action(delegate{if(desktop!=null)desktop.OpenWelcome();}));},null,Timeout.Infinite,false);
                    app.Startup += delegate
                    {
                        if(smoke&&!preview){var fixture=new VisualTestBackground {WindowState=WindowState.Normal,Width=SystemParameters.PrimaryScreenWidth,Height=SystemParameters.PrimaryScreenHeight,Left=0,Top=0,ShowActivated=false,IsHitTestVisible=false};fixture.SourceInitialized+=delegate {Native.NoActivate(fixture);};fixture.Show();}
                        if(!smoke)Log.Write((args.Length>0&&args[0]=="--background"?"Inicio automático solicitado: ":"Inicio manual solicitado: ")+Product.VersionText);
                        desktop = new Desktop(smoke ? Path.Combine(Path.GetFullPath(args[1]), "profile") : data, smoke,smoke&&!preview,args.Length>0&&args[0]=="--background");
                        if(!smoke)Log.Write("MacFlow listo: "+Product.VersionText);
                        if (preview) desktop.OpenSettings();
                        else if (smoke)
                        {
                            Directory.CreateDirectory(args[1]);desktop.Preferences.HideInFullscreen=false; desktop.OpenSettings();
                            var clock = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) }; int stage = 0, loadingTicks=0;bool stageBusy=false;TimeSpan idleCpu=TimeSpan.Zero;DateTime idleStart=DateTime.UtcNow;MotionProbe motion=null;GlassMenu testMenu=null;ConnectivityPanel connectionFixture=null;
                            clock.Tick += async delegate
                            {
                                if(stageBusy)return;stageBusy=true;
                                try
                                {
                                    if(stage==0&&desktop.AppCount==0&&loadingTicks++<10)return;
                                    if (stage == 0) { if(LiquidMaterial.ForWindow(desktop.Settings)==null)throw new Exception("Settings glass material is missing");File.WriteAllText(Path.Combine(args[1],"panel-material.txt"),"Configuración: "+desktop.Settings.Tag+"; lateral translúcido, contenido con contraste estable; controles reales, sin imágenes de interfaz.");Render(desktop.Settings, Path.Combine(args[1], "settings.png")); Render(desktop.Dock, Path.Combine(args[1], "dock.png")); await CaptureOwnWindow(desktop.Settings,Path.Combine(args[1],"settings-desktop.png")); await CaptureOwnWindow(desktop.Dock,Path.Combine(args[1],"dock-desktop.png")); LiquidMaterial.SetLensForTest(desktop.Dock,0);desktop.Settings.Close(); desktop.OpenSearch(); }
                                    if (stage == 1) { await CaptureOwnWindow(desktop.Dock,Path.Combine(args[1],"dock-blur-only.png"));LiquidMaterial.SetLensForTest(desktop.Dock,6);Render(desktop.Search, Path.Combine(args[1], "spotlight.png")); await CaptureOwnWindow(desktop.Search,Path.Combine(args[1],"spotlight-desktop.png")); ((Spotlight)desktop.Search).SetQuery("2*(3+4)");if(((Spotlight)desktop.Search).ResultCount!=1)throw new Exception("No calculation result");desktop.Search.Close(); desktop.OpenControl(); }
                                    if (stage == 2) { Render(desktop.Control, Path.Combine(args[1], "control.png")); await CaptureOwnWindow(desktop.Control,Path.Combine(args[1],"control-desktop.png"));desktop.Control.Close();desktop.OpenLaunchpad(); }
                                    if (stage == 3) {
                                        var applications=(Launchpad)desktop.Launcher;applications.ZoomButton.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));await Task.Delay(280);
                                        if(applications.WindowState!=WindowState.Maximized)throw new Exception("Applications zoom button failed");applications.ZoomButton.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));await Task.Delay(280);
                                        if(applications.WindowState!=WindowState.Normal)throw new Exception("Applications zoom restore failed");applications.MinimizeButton.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));await Task.Delay(350);
                                        if(desktop.Launcher!=applications||applications.WindowState!=WindowState.Minimized)throw new Exception("Applications minimize closed the window");desktop.OpenLaunchpad();await Task.Delay(350);
                                        if(desktop.Launcher!=applications||applications.WindowState!=WindowState.Normal)throw new Exception("Dock did not restore minimized Applications");
                                        Render(desktop.Launcher,Path.Combine(args[1],"applications.png")); await CaptureOwnWindow(desktop.Launcher,Path.Combine(args[1],"applications-desktop.png"));
                                        desktop.Launcher.RaiseEvent(new System.Windows.Input.KeyEventArgs(System.Windows.Input.Keyboard.PrimaryDevice,PresentationSource.FromVisual(desktop.Launcher),0,System.Windows.Input.Key.Escape){RoutedEvent=System.Windows.Input.Keyboard.PreviewKeyDownEvent});
                                        await Task.Delay(450);if(desktop.Launcher!=null)throw new Exception("Applications Escape did not close the panel");
                                        desktop.OpenLaunchpad();await Task.Delay(280);((Launchpad)desktop.Launcher).CloseButton.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
                                        await Task.Delay(450);if(desktop.Launcher!=null)throw new Exception("Applications close button did not close the panel");
                                        desktop.OpenLaunchpad();await Task.Delay(280);desktop.OpenLaunchpad();await Task.Delay(450);if(desktop.Launcher!=null)throw new Exception("Applications toggle did not close the panel");
                                        File.WriteAllText(Path.Combine(args[1],"applications-close.txt"),"PASS: ampliar/restaurar; minimizar conserva la ventana y el Dock la recupera; Escape, control rojo y segundo clic en Aplicaciones cierran el panel real.");desktop.OpenOverview();
                                    }
                                    if (stage == 4) { if(desktop.Overview==null)throw new Exception("Window overview closed before capture");Render(desktop.Overview,Path.Combine(args[1],"windows.png"));await CaptureOwnWindow(desktop.Overview,Path.Combine(args[1],"windows-desktop.png"));desktop.Overview.Close();desktop.Preferences.Theme="Dark";desktop.Preferences.ShowWidget=true;desktop.Preferences.DockPosition="Left";desktop.ApplyPreferences(desktop.Preferences,null);desktop.OpenControl(); }
                                    if (stage == 5) { Render(desktop.Control,Path.Combine(args[1],"control-dark.png"));await CaptureOwnWindow(desktop.Control,Path.Combine(args[1],"control-dark-desktop.png"));desktop.Control.Close();File.WriteAllText(Path.Combine(args[1], "smoke.txt"), "PASS: Dock, settings, Spotlight calculation, control, Launchpad, window thumbnails, dark theme, vertical Dock and widget constructed and rendered; " + desktop.AppCount + " installed apps; " + desktop.Windows.Count + " external windows; no taskbar/startup changes. Backdrop: "+desktop.Dock.Tag+". Captures use an isolated test wallpaper window, not the user wallpaper.");var process=Process.GetCurrentProcess();File.WriteAllText(Path.Combine(args[1],"performance.txt"),"Private memory MB: "+(process.PrivateMemorySize64/1048576)+"\nWorking set MB: "+(process.WorkingSet64/1048576)+"\nProcess CPU seconds through UI suite (includes startup): "+process.TotalProcessorTime.TotalSeconds.ToString("F2")+"\nWPF render tier: "+(RenderCapability.Tier>>16)+"\nShader 3 support: "+RenderCapability.IsPixelShaderVersionSupported(3,0)+"\nBackdrop: adaptive 2–6 Hz; half-resolution texture; immediate refresh on foreground change; independent animation clock."); desktop.Preferences.ShowWidget=false;desktop.Preferences.DockPosition="Bottom";desktop.Preferences.Theme="Light";desktop.ApplyPreferences(desktop.Preferences,null);idleCpu=Process.GetCurrentProcess().TotalProcessorTime;idleStart=DateTime.UtcNow; }
                                    if(stage==7){var p=Process.GetCurrentProcess();p.Refresh();double elapsed=(DateTime.UtcNow-idleStart).TotalSeconds;double cpu=(p.TotalProcessorTime-idleCpu).TotalSeconds;File.AppendAllText(Path.Combine(args[1],"performance.txt"),"\nQuiet Dock + top bar seconds: "+elapsed.ToString("F1")+"\nQuiet CPU (one logical core %): "+(100*cpu/elapsed).ToString("F2")+"\nQuiet CPU (machine %): "+(100*cpu/elapsed/Environment.ProcessorCount).ToString("F2")+"\nQuiet private MB: "+(p.PrivateMemorySize64/1048576)+"\nLogical processors: "+Environment.ProcessorCount);await CaptureOwnWindow(desktop.Dock,Path.Combine(args[1],"dock-ready-desktop.png"));motion=new MotionProbe(desktop);}
                                    if(stage==8){File.AppendAllText(Path.Combine(args[1],"performance.txt"),motion.Report());motion.Dispose();testMenu=new GlassMenu("MacFlow",new List<GlassCommand>{new GlassCommand("Aplicaciones",desktop.OpenLaunchpad),new GlassCommand("Buscar",desktop.OpenSearch),new GlassCommand("Centro de control",desktop.OpenControl),new GlassCommand("Configuración",desktop.OpenSettings)},false);testMenu.Left=60;testMenu.Top=60;testMenu.Show();Design.Reveal(testMenu);}
                                    if(stage==9){await CaptureOwnWindow(testMenu,Path.Combine(args[1],"menu-desktop.png"));testMenu.Close();desktop.Preferences.LiveGlass=false;desktop.ApplyPreferences(desktop.Preferences,null);}
                                    if(stage==10){await CaptureOwnWindow(desktop.Dock,Path.Combine(args[1],"dock-clear-fallback.png"));if(!String.Equals(desktop.Dock.Tag,"Clear glass fallback"))throw new Exception("Fallback unavailable");File.AppendAllText(Path.Combine(args[1],"smoke.txt")," Menú de cristal y alternativa compatible con capturas: PASS.");desktop.Preferences.LiveGlass=true;desktop.ApplyPreferences(desktop.Preferences,null);desktop.OpenSettings();((SettingsWindow)desktop.Settings).SelectPage(1);}
                                    if(stage==11){if(!((SettingsWindow)desktop.Settings).WallpaperPreviewReady)throw new Exception("Wallpaper preview not ready");Render(desktop.Settings,Path.Combine(args[1],"wallpapers.png"));await CaptureOwnWindow(desktop.Settings,Path.Combine(args[1],"wallpapers-desktop.png"));File.AppendAllText(Path.Combine(args[1],"smoke.txt")," Galería y vista previa de fondos: PASS.");desktop.Settings.Close();desktop.OpenWindowsMenu();}
                                    if(stage==12){if(GlassMenu.Current==null||GlassMenu.Current.Title!="MacFlow · Inicio")throw new Exception("Windows menu missing");await CaptureOwnWindow(GlassMenu.Current,Path.Combine(args[1],"windows-menu-desktop.png"));GlassMenu.Current.Close();desktop.ConfirmPower(PowerAction.Shutdown);}
                                    if(stage==13){var prompt=(PowerPrompt)desktop.PowerConfirmation;if(prompt==null||prompt.Confirmed)throw new Exception("Power prompt missing or bypassed");await CaptureOwnWindow(prompt,Path.Combine(args[1],"power-prompt-desktop.png"));prompt.Close();File.AppendAllText(Path.Combine(args[1],"smoke.txt")," Menú Windows y confirmación de energía cancelada: PASS. No power actions executed.");connectionFixture=ConnectionFixture(true);}
                                    if(stage==14){if(!connectionFixture.Ready||connectionFixture.ItemCount!=3)throw new Exception("Wi-Fi fixture not ready");Render(connectionFixture,Path.Combine(args[1],"wifi.png"));await CaptureOwnWindow(connectionFixture,Path.Combine(args[1],"wifi-desktop.png"));connectionFixture.SwitchMode(false);}
                                    if(stage==15){if(!connectionFixture.Ready||connectionFixture.ItemCount!=3||connectionFixture.WifiMode)throw new Exception("Bluetooth fixture not ready");Render(connectionFixture,Path.Combine(args[1],"bluetooth.png"));await CaptureOwnWindow(connectionFixture,Path.Combine(args[1],"bluetooth-desktop.png"));connectionFixture.Close();desktop.Preferences.Theme="Dark";desktop.ApplyPreferences(desktop.Preferences,null);connectionFixture=ConnectionFixture(true);}
                                    if(stage==16){if(!connectionFixture.Ready)throw new Exception("Dark connection fixture not ready");await CaptureOwnWindow(connectionFixture,Path.Combine(args[1],"wifi-dark-desktop.png"));connectionFixture.Close();File.AppendAllText(Path.Combine(args[1],"smoke.txt")," Paneles Wi-Fi/Bluetooth reales renderizados en claro/oscuro: PASS. Sólo datos de prueba inyectados; no búsquedas inalámbricas, conexiones, emparejamientos ni cambios de permisos.");clock.Stop();app.Shutdown();}
                                    stage++;
                                }
                                catch (Exception ex) { File.WriteAllText(Path.Combine(args[1], "smoke.txt"), "FAIL stage "+stage+": " + ex); clock.Stop(); app.Shutdown(1); }
                                finally{stageBusy=false;}
                            }; clock.Start();
                        }
                        else if (args.Length>0&&args[0]=="--welcome") desktop.OpenWelcome();
                        else if(desktop.WelcomeForNewVersion()) { }
                        else if (args.Length == 0 || !File.Exists(desktop.Store.PathName)) desktop.OpenSettings();
                    };
                    app.Exit += delegate { wait.Unregister(null);settingsWait.Unregister(null);greetingWait.Unregister(null); if (desktop != null) desktop.Dispose(); };
                    return app.Run();
                }
            }
        }
        private static ConnectivityPanel ConnectionFixture(bool wifi)
        {
            var panel=new ConnectivityPanel(wifi,ConnectivityRegression.Sample,mode=>{},item=>{}){ShowActivated=false,IsHitTestVisible=false,ShowInTaskbar=false,Left=SystemParameters.PrimaryScreenWidth-440,Top=60};panel.SourceInitialized+=delegate{Native.NoActivate(panel);};panel.Show();Design.Reveal(panel);return panel;
        }
        private static void Render(Window window, string path)
        {
            window.UpdateLayout();
            var bitmap = new RenderTargetBitmap(Math.Max(1, (int)Math.Ceiling(window.ActualWidth)), Math.Max(1, (int)Math.Ceiling(window.ActualHeight)), 96, 96, PixelFormats.Pbgra32); bitmap.Render(window);
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); using (var f = File.Create(path)) encoder.Save(f);
        }
        internal static async System.Threading.Tasks.Task CaptureOwnWindow(Window window,string path)
        {
            // Integration-test evidence only: capture the pixels of our own window,
            // including the Windows compositor backdrop, never an entire desktop.
            try
            {
                await LiquidMaterial.CaptureOwnWindow(window,path);
                if(!File.Exists(path))throw new IOException("No native capture was produced: "+path);
            }
            catch(Exception ex){File.WriteAllText(path+".unavailable.txt",ex.Message);throw;}
        }
    }
    public static class SelfTests
    {
        public static int Run(string directory)
        {
            directory = Path.GetFullPath(directory); Directory.CreateDirectory(directory); Log.DirectoryPath = directory; var results = new List<string>();
            try
            {
                var store = new SettingsStore(Path.Combine(directory, "settings-tests")); var p = new Preferences { IconSize = 999, Theme = "bad", Accent = "invalid" }; p.Pins.Add(new AppEntry { Name = "Test", Target = "explorer.exe" }); store.Save(p); var restored = store.Load();
                Assert(restored.IconSize == 72 && restored.Theme == "Dark" && restored.Accent == "#8DAEFF" && restored.Pins.Count == 1, "Preferencias: persistencia y validación", results);
                restored.IconSize = 32; store.Save(restored); Assert(store.Load().IconSize == 32, "Reemplazo atómico de preferencias existentes", results);
                File.WriteAllText(store.PathName, "{invalid json"); Assert(store.Load().IconSize == 32, "Recuperación de JSON corrupto", results);
                File.WriteAllText(store.PathName,"{\"IconSize\":36,\"Animations\":true}");Assert(store.Load().NeedsUpdateWelcome(Product.Version),"Bienvenida: perfiles anteriores detectan el cambio de versión",results);restored.WelcomeAfterUpdate=false;store.Save(restored);Assert(!store.Load().NeedsUpdateWelcome(Product.Version),"Bienvenida: se conserva la elección de desactivarla",results);restored.WelcomeAfterUpdate=true;restored.LastWelcomedVersion=Product.VersionText;store.Save(restored);Assert(!store.Load().NeedsUpdateWelcome(Product.Version),"Bienvenida: reiniciar la misma versión no repite Hola",results);Assert(restored.NeedsUpdateWelcome(new Version(Product.Version.Major,Product.Version.Minor,Product.Version.Build+1)),"Bienvenida: una nueva versión vuelve a mostrar Hola",results);
                UpdateRegression.Run(Path.Combine(directory,"updates"),results);
                NewFeaturesRegression.Run(Path.Combine(directory,"features"),results);
                StartupRegression.Run(Path.Combine(directory,"startup"),results);
                Assert(LiquidMaterial.LightInkForLuminance(.2,false,false)&&!LiquidMaterial.LightInkForLuminance(.8,false,true),"Cristal: tinta legible sobre material oscuro y claro",results);
                Assert(LiquidMaterial.LightInkForLuminance(.55,true,true)&&!LiquidMaterial.LightInkForLuminance(.48,true,false),"Cristal: histéresis evita parpadeos de texto cerca del umbral",results);
                double maxSpring=0;bool finiteSpring=true;for(int sample=0;sample<=200;sample++){double position=SpringEase.Progress(sample/200.0);finiteSpring&=!Double.IsNaN(position)&&!Double.IsInfinity(position);maxSpring=Math.Max(maxSpring,position);}
                Assert(finiteSpring&&SpringEase.Progress(0)==0&&SpringEase.Progress(1)==1&&maxSpring<=1.02,"Movimiento: resorte continuo con extremos exactos y rebote menor al 2%",results);
                var recovery=new ShellRecovery();int acquireAttempts=0,restores=0;DateTime logon=DateTime.UtcNow;Action acquisition=delegate{acquireAttempts++;if(acquireAttempts==1)throw new IOException("Explorer iniciando");};Action restoration=delegate{restores++;};
                Assert(!recovery.Attempt(true,false,true,logon,acquisition,restoration)&&!recovery.Attempt(true,true,false,logon,acquisition,restoration)&&acquireAttempts==0,"Inicio: no oculta Windows hasta tener Dock y Explorer disponibles",results);
                Assert(!recovery.Attempt(true,true,true,logon,acquisition,restoration)&&restores==1&&!recovery.Acquired,"Inicio: un fallo devuelve Windows y conserva la solicitud de reemplazo",results);
                Assert(!recovery.Attempt(true,true,true,logon.AddMilliseconds(500),acquisition,restoration)&&acquireAttempts==1,"Inicio: reintentos limitados sin bucle de CPU",results);
                Assert(recovery.Attempt(true,true,true,logon.AddSeconds(1),acquisition,restoration)&&recovery.Acquired&&acquireAttempts==2,"Inicio: Explorer tardío permite recuperar el Dock sin reiniciar MacFlow",results);
                recovery.Reset(restoration);Assert(!recovery.Acquired&&restores==2&&recovery.Attempt(true,true,true,logon.AddSeconds(2),acquisition,restoration),"Inicio: reinicio de Explorer puede adquirir una nueva barra",results);
                Assert(!recovery.Attempt(false,true,true,logon.AddSeconds(3),acquisition,restoration)&&!recovery.Acquired&&restores==3,"Inicio: desactivar restaura Windows y cancela la recuperación",results);
                var apps = Catalog.Scan(); Assert(apps.Count >= 2 && apps.Exists(a => a.Target == "explorer.exe"), "Catálogo de aplicaciones real: " + apps.Count, results);
                var windows = Native.Windows(); Assert(windows.TrueForAll(w => w.Handle != IntPtr.Zero && !String.IsNullOrEmpty(w.Title)), "Enumeración de ventanas reales: " + windows.Count, results);
                var state = Native.TaskbarState(); Assert(state <= 3, "Lectura de barra de tareas sin modificar: " + state, results);
                using (var audio = new Audio()) { Assert(!audio.Available || (audio.Volume >= 0 && audio.Volume <= 1), "Lectura de audio sin modificar", results); }
                Assert(Icons.Get(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe")) != null, "Extracción y liberación de iconos nativos", results);
                var storeApp=apps.Find(a=>!String.IsNullOrEmpty(a.IconPath));Assert(storeApp==null||Icons.Get(storeApp.IconPath)!=null,"Carga de iconos de paquetes instalados, cuando están disponibles",results);
                string answer;Assert(Calculator.TryEvaluate("2*(3+4)",out answer)&&answer=="14" && Calculator.TryEvaluate("2^3^2",out answer)&&answer=="512" && !Calculator.TryEvaluate("1/0",out answer) && !Calculator.TryEvaluate("System.Exit()",out answer),"Calculadora: precedencia, potencias y rechazo de expresiones inválidas",results);
                var order=new Preferences();order.Pins.Add(new AppEntry{Name="A",Target="a.exe"});order.Pins.Add(new AppEntry{Name="B",Target="b.exe"});order.Pins.Add(new AppEntry{Name="C",Target="c.exe"});Assert(order.MovePin("a.exe","c.exe")&&order.Pins[2].Name=="A"&&!order.MovePin("unknown","b.exe"),"Reordenación de pins y rechazo de destinos ajenos",results);
                File.WriteAllText(store.PathName,"{\"Theme\":\"Light\",\"IconSize\":48}");var upgraded=store.Load();Assert(upgraded.ShowBattery&&upgraded.ShowNetwork&&upgraded.GlassOpacity==.18&&upgraded.DockPosition=="Bottom","Migración de preferencias anteriores",results);
                var backgrounds=WallpaperLibrary.Load(Path.Combine(directory,"wallpaper-tests"));Assert(backgrounds.Count==7&&backgrounds.TrueForAll(w=>File.Exists(w.File)&&w.Thumbnail.IsFrozen&&w.Thumbnail.PixelWidth<=380),"Fondos: tres diseños nuevos integrados, galería anterior conservada y miniaturas limitadas",results);
                upgraded.WallpaperPath=backgrounds[1].File;upgraded.LidAnimation=false;store.Save(upgraded);Assert(store.Load().WallpaperPath==backgrounds[1].File&&!store.Load().LidAnimation,"Fondos y tapa: selección persiste al reiniciar",results);
                var wallpaperState=new Dictionary<string,string>{{"A","original-a"},{"B","original-b"}};string wallpaperDirectory=Path.Combine(directory,"wallpaper-transaction-tests");Action<string,string> wallpaperSet=delegate(string monitor,string path){if(monitor==null){wallpaperState["A"]=wallpaperState["B"]=path;}else wallpaperState[monitor]=path;};
                Wallpapers.Apply(wallpaperDirectory,backgrounds[0].File,()=>new List<string>{"A","B"},m=>wallpaperState[m],wallpaperSet);Assert(wallpaperState["A"]==backgrounds[0].File&&wallpaperState["B"]==backgrounds[0].File,"Fondos: aplica y verifica todas las pantallas del proveedor aislado",results);
                byte[] wallpaperJournal=File.ReadAllBytes(Path.Combine(wallpaperDirectory,"wallpaper.restore"));bool wallpaperFailed=false;
                try{Wallpapers.Apply(wallpaperDirectory,backgrounds[1].File,()=>new List<string>{"A","B"},m=>wallpaperState[m],delegate(string monitor,string path){if(monitor==null){wallpaperState["A"]=path;throw new IOException("Fallo de prueba");}wallpaperState[monitor]=path;});}catch(IOException){wallpaperFailed=true;}
                Assert(wallpaperFailed&&wallpaperState["A"]==backgrounds[0].File&&wallpaperState["B"]==backgrounds[0].File&&wallpaperJournal.SequenceEqual(File.ReadAllBytes(Path.Combine(wallpaperDirectory,"wallpaper.restore"))),"Fondos: fallo parcial revierte cambios y conserva el fondo anterior",results);
                LidRegression.Run(results);
                Assert(upgraded.WindowsMenu,"Menú Windows: migración de configuración",results);
                Assert(!upgraded.AppleWindowControls,"Controles Apple: migración evita superponer botones a otras apps",results);
                upgraded.AppleWindowControls=false;store.Save(upgraded);Assert(!store.Load().AppleWindowControls,"Controles Apple: conserva la opción desactivada",results);
                Assert(upgraded.HideDockInCodex&&upgraded.ProtectDockEdge,"Dock: protección y ocultación en Codex al migrar",results);
                upgraded.HideDockInCodex=false;store.Save(upgraded);Assert(!store.Load().HideDockInCodex,"Dock: conserva la opción de ocultación desactivada",results);
                Assert(Desktop.IsCodex(@"C:\Apps\Codex.exe")&&!Desktop.IsCodex(@"C:\Apps\chrome.exe"),"Dock: reconoce el proceso de Codex sin leer su interfaz",results);
                var contained=new Preferences{IconSize=40};Assert(contained.UpgradeContainedDock()&&contained.IconSize==44&&!contained.UpgradeContainedDock()&&contained.IconSize==44,"Dock: aumenta cuatro píxeles una sola vez",results);store.Save(contained);contained=store.Load();Assert(contained.ContainedDockApplied&&!contained.UpgradeContainedDock()&&contained.IconSize==44,"Dock: la actualización de tamaño sobrevive al reinicio",results);
                double spring60=1,velocity60=0,spring120=1,velocity120=0;for(int i=0;i<30;i++)DockMotion.SpringStep(ref spring60,ref velocity60,1.28,1.0/60);for(int i=0;i<60;i++)DockMotion.SpringStep(ref spring120,ref velocity120,1.28,1.0/120);Assert(Math.Abs(spring60-spring120)<.000001&&Math.Abs(spring60-1.28)<.001,"Dock: resorte conserva tiempos con distintas frecuencias de cuadros",results);
                double interrupted=1,velocity=0;DockMotion.SpringStep(ref interrupted,ref velocity,1.28,.016);double forwardVelocity=velocity;DockMotion.SpringStep(ref interrupted,ref velocity,1,.001);Assert(forwardVelocity>0&&velocity>0&&interrupted>1,"Dock: cambio de destino conserva el movimiento sin salto",results);
                Assert(!Desktop.ShouldAutoHide(true,true,false,false)&&Desktop.ShouldAutoHide(false,true,false,false)&&!Desktop.ShouldAutoHide(false,false,false,false)&&!Desktop.ShouldAutoHide(false,true,true,false)&&!Desktop.ShouldAutoHide(false,true,false,true),"Dock: escritorio siempre visible; autoocultado en cualquier app; borde y menú del Dock lo recuperan",results);
                string settingsSignal="Local\\MacFlow.Settings.Test."+Guid.NewGuid();using(var signal=new EventWaitHandle(false,EventResetMode.AutoReset,settingsSignal)){Assert(Program.SignalSettings(settingsSignal)&&signal.WaitOne(50)&&!signal.WaitOne(0),"Configuración: segundo inicio envía una única solicitud a la sesión",results);}Assert(!Program.SignalSettings(settingsSignal),"Configuración: solicitud sin sesión no crea un evento huérfano",results);
                var tap=new WindowsTapState();Assert(!tap.Process(0x5B,true,false)&&tap.Process(0x5B,false,false),"Menú Windows: pulsación izquierda completa",results);
                Assert(!tap.Process(0x5C,true,false)&&!tap.Process(0x5C,true,false)&&tap.Process(0x5C,false,false),"Menú Windows: derecha y repetición sin abrir antes de soltar",results);
                tap.Process(0x5B,true,false);tap.Process(0x45,true,false);tap.Process(0x45,false,false);Assert(!tap.Process(0x5B,false,false),"Menú Windows: conserva Win+E",results);
                tap.Process(0x5B,true,true);Assert(!tap.Process(0x5B,false,true),"Menú Windows: conserva combinaciones con modificadores",results);
                tap.Process(0x5B,true,false);tap.CancelTap();Assert(!tap.Process(0x5B,false,false),"Menú Windows: conserva clic con Windows presionado",results);
                tap.Process(0x5B,true,false);tap.Process(0x5C,true,false);tap.Process(0x5C,false,false);Assert(!tap.Process(0x5B,false,false),"Menú Windows: dos teclas Windows no abren un menú accidental",results);
                Assert(PowerActions.ExitFlags(PowerAction.Shutdown)==8&&PowerActions.ExitFlags(PowerAction.Restart)==2&&PowerActions.ExitFlags(PowerAction.SignOut)==0,"Energía: solicitudes sin FORCE ni FORCEIFHUNG; no se ejecutan en tests",results);
                var decoded=WallpaperLibrary.Decode(backgrounds[0].File,1000);Assert(decoded.IsFrozen&&decoded.PixelWidth==1000,"Fondos: vista previa decodificada sin bloquear el archivo",results);
                using(var closeTest=new System.Windows.Forms.Form())
                {
                    bool cancel=true;closeTest.FormClosing+=delegate(object sender,System.Windows.Forms.FormClosingEventArgs e){e.Cancel=cancel;};var handle=closeTest.Handle;
                    var entry=new WindowEntry {Handle=handle,ProcessId=(uint)Process.GetCurrentProcess().Id};
                    Assert(!Native.RequestClose(new WindowEntry{Handle=handle,ProcessId=UInt32.MaxValue}),"Cierre: rechazo de identidad de proceso incorrecta",results);
                    Assert(Native.RequestClose(entry),"Cierre: solicitud nativa asíncrona",results);System.Windows.Forms.Application.DoEvents();Assert(Native.IsWindow(handle),"Cierre: la aplicación conserva el derecho de cancelar",results);
                    cancel=false;Assert(Native.RequestClose(entry),"Cierre: segundo intento aceptado",results);System.Windows.Forms.Application.DoEvents();Assert(!Native.IsWindow(handle),"Cierre: ventana propia de prueba cerrada sin terminar procesos",results);
                }
                using(var minimizeTest=new MinimizeTestForm{ShowInTaskbar=false,StartPosition=System.Windows.Forms.FormStartPosition.Manual,Location=new System.Drawing.Point(-2000,-2000),Size=new System.Drawing.Size(100,80)})
                {
                    minimizeTest.Show();var entry=new WindowEntry{Handle=minimizeTest.Handle,ProcessId=(uint)Process.GetCurrentProcess().Id};
                    Assert(!Native.RequestMinimize(null)&&!Native.RequestMinimize(new WindowEntry{Handle=entry.Handle,ProcessId=UInt32.MaxValue}),"Minimizar: rechaza ventanas inválidas e identidad incorrecta",results);
                    Assert(Native.RequestMinimize(entry),"Minimizar: solicitud nativa asíncrona aceptada",results);System.Windows.Forms.Application.DoEvents();
                    Assert(Native.IsIconic(entry.Handle),"Minimizar: ventana propia real minimizada sin cambiar su estilo",results);Assert(Native.IsDesktopContext(entry.Handle)&&Native.IsDesktopContext(IntPtr.Zero),"Escritorio: Dock visible si el foco anterior está minimizado o en transición",results);
                    Assert(Native.RequestMinimize(entry),"Minimizar: repetir una solicitud conserva el estado",results);Native.ShowWindowAsync(entry.Handle,9);System.Windows.Forms.Application.DoEvents();
                    Assert(!Native.IsIconic(entry.Handle),"Minimizar: se puede restaurar la misma ventana",results);Assert(!Native.RequestZoom(new WindowEntry{Handle=entry.Handle,ProcessId=UInt32.MaxValue})&&!Native.Activate(new WindowEntry{Handle=entry.Handle,ProcessId=UInt32.MaxValue}),"Controles: ampliar y activar rechazan identidades incorrectas",results);minimizeTest.MaximizeBox=false;Assert(!Native.RequestZoom(entry),"Controles: respeta una ventana que no admite maximizar",results);minimizeTest.Close();
                }
                Assert(Native.IsDesktopProxy(new Native.RECT(),Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),"explorer.exe"))&&!Native.IsDesktopProxy(new Native.RECT(),"fake.exe")&&!Native.IsDesktopProxy(new Native.RECT{Right=500,Bottom=400},Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),"explorer.exe")),"Escritorio: reconoce sólo un proxy sin superficie de Explorer, no una ventana de archivos",results);if(Application.Current==null)new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};Design.Animations=true;
                using(var primary=new MinimizeTestForm{ShowInTaskbar=false,Location=new System.Drawing.Point(-2000,-2000),StartPosition=System.Windows.Forms.FormStartPosition.Manual})
                using(var secondary=new MinimizeTestForm{ShowInTaskbar=false,Location=new System.Drawing.Point(-2000,-2000),StartPosition=System.Windows.Forms.FormStartPosition.Manual})
                {
                    primary.Show();secondary.Show();var handles=new[]{primary.Handle,secondary.Handle};uint ownPid=(uint)Process.GetCurrentProcess().Id;
                    Func<IntPtr,bool> own=delegate(IntPtr h){uint pid;Native.GetWindowThreadProcessId(h,out pid);return Native.IsWindow(h)&&pid==ownPid;};
                    var surfaceFile=Path.Combine(directory,"fixture-visibility.txt");var surfaces=new TaskbarSurfaces(surfaceFile,delegate{return handles;},own,false);surfaces.Acquire();Pump(150);
                    Assert(!primary.Visible&&!secondary.Visible&&File.Exists(surfaceFile),"Barra: oculta superficies de prueba y guarda recuperación antes",results);
                    primary.Show();surfaces.Maintain();Pump(150);Assert(!primary.Visible,"Barra: oculta una superficie que reaparece",results);
                    surfaces.Restore();Pump(150);Assert(primary.Visible&&secondary.Visible&&!File.Exists(surfaceFile),"Barra: restaura ambas superficies sin activar otras ventanas",results);
                    secondary.Hide();surfaces.Acquire();Pump(150);surfaces.Restore();Pump(150);Assert(primary.Visible&&!secondary.Visible,"Barra: respeta una superficie que ya estaba oculta",results);
                    secondary.Show();surfaces.Remember();secondary.Hide();surfaces.Acquire();Pump(150);surfaces.Restore();Pump(150);Assert(primary.Visible&&secondary.Visible,"Barra: guarda visibilidad antes de liberar el espacio de Windows",results);
                    Assert(TaskbarSurfaces.TaskbarClass("Shell_TrayWnd")&&TaskbarSurfaces.TaskbarClass("Shell_SecondaryTrayWnd")&&!TaskbarSurfaces.TaskbarClass("Notepad"),"Barra: filtro restringido a las superficies de Explorer",results);
                }
                var panel=TestWindow();int calls=0;panel.Show();Design.Dismiss(panel,delegate{calls++;});panel.BeginAnimation(UIElement.OpacityProperty,null);Pump(500);
                Assert(!panel.IsVisible&&calls==1,"Panel: cierre garantizado si se interrumpe la animación; callback una vez",results);
                panel=TestWindow();calls=0;panel.Show();Design.Dismiss(panel,delegate{calls++;});panel.Close();Pump(400);
                Assert(calls==0,"Panel: cierre externo cancela la acción pendiente",results);
                var shell=Native.GetShellWindow();Assert(shell==IntPtr.Zero||Native.IsDesktopWindow(shell),"Dock: reconoce el escritorio real de Windows",results);
                var namedDesktop=TestWindow();namedDesktop.Title="Escritorio";namedDesktop.Show();Assert(!Native.IsDesktopWindow(new System.Windows.Interop.WindowInteropHelper(namedDesktop).Handle)&&!Native.IsDesktopWindow(IntPtr.Zero),"Dock: una app llamada Escritorio no evita el ocultamiento",results);namedDesktop.Close();
                panel=TestWindow();panel.Content=new System.Windows.Controls.Border();panel.Show();Design.Minimize(panel);Design.Minimize(panel);Pump(400);Assert(panel.WindowState==WindowState.Minimized&&panel.IsHitTestVisible,"Paneles: minimizar termina realmente y recupera controles",results);
                bool liveBackdrop=LiquidMaterial.LiveBackdrop;LiquidMaterial.LiveBackdrop=false;panel.WindowState=WindowState.Normal;Pump(400);Assert(panel.WindowState==WindowState.Normal&&panel.Opacity==1&&panel.IsHitTestVisible,"Paneles: restaurar vuelve a mostrar el contenido",results);panel.Close();LiquidMaterial.LiveBackdrop=liveBackdrop;
                panel=TestWindow();panel.Content=new System.Windows.Controls.Border();panel.Show();Design.Minimize(panel);panel.Hide();Pump(400);panel.Show();Assert(panel.IsHitTestVisible&&((FrameworkElement)panel.Content).Opacity==1,"Paneles: ocultar durante animación no deja controles bloqueados",results);panel.Close();
                var dock=TestWindow();dock.Content=new System.Windows.Controls.Border();dock.Show();var visibility=new DockVisibility(dock);visibility.Hide();Pump(70);visibility.Show();Pump(400);
                Assert(dock.IsVisible&&dock.IsHitTestVisible&&((FrameworkElement)dock.Content).Opacity>.99,"Dock: invertir la animación no oculta el Dock al reaparecer",results);
                visibility.Hide();Pump(70);visibility.ShowImmediately();Pump(400);Assert(dock.IsVisible&&dock.IsHitTestVisible&&((FrameworkElement)dock.Content).Opacity==1,"Dock: escritorio interrumpe ocultación y lo muestra sin espera ni rebote",results);visibility.Hide();Pump(400);Assert(!dock.IsVisible,"Dock: finaliza el descenso y libera la zona de clics",results);dock.Close();
                var wholeDock=Design.Panel("MacFlow Dock · Prueba",100,70);wholeDock.Left=wholeDock.Top=-2000;wholeDock.ShowActivated=false;wholeDock.Content=Design.Glass(new System.Windows.Controls.Border(),18,true,true);wholeDock.Show();Pump(600);var wholeVisibility=new DockVisibility(wholeDock);Native.RECT initial,middle;Native.GetWindowRect(new System.Windows.Interop.WindowInteropHelper(wholeDock).Handle,out initial);
                wholeVisibility.Hide();bool descended=false;
                // Observe native movement rather than one rounded pixel at a
                // fixed instant. Offscreen composition can defer early ticks.
                for(int sample=0;sample<30&&!descended;sample++){Pump(10);Native.GetWindowRect(new System.Windows.Interop.WindowInteropHelper(wholeDock).Handle,out middle);descended=wholeDock.IsVisible&&middle.Top>initial.Top&&Math.Abs(wholeVisibility.RestingBounds().Top-initial.Top)<=1;}
                Assert(descended,"Dock: el descenso mueve también el cristal nativo; oclusión usa la posición de reposo",results);Pump(400);Assert(!wholeDock.IsVisible,"Dock: descenso nativo termina sin dejar una burbuja inmóvil",results);
                wholeVisibility.Show();Pump(90);Native.GetWindowRect(new System.Windows.Interop.WindowInteropHelper(wholeDock).Handle,out middle);Assert(wholeDock.IsVisible&&middle.Top>initial.Top,"Dock: reaparece desde abajo con toda la superficie",results);Pump(360);Native.GetWindowRect(new System.Windows.Interop.WindowInteropHelper(wholeDock).Handle,out middle);Assert(middle.Top==initial.Top&&!wholeDock.HasAnimatedProperties,"Dock: rebote termina exactamente en su posición, sin reloj activo",results);
                wholeVisibility.Hide();Pump(100);wholeVisibility.ShowImmediately();Pump(450);Native.GetWindowRect(new System.Windows.Interop.WindowInteropHelper(wholeDock).Handle,out middle);Assert(wholeDock.IsVisible&&middle.Top==initial.Top,"Dock: volver al escritorio cancela el descenso nativo inmediatamente",results);wholeDock.Close();
                var closePanel=Design.Panel("MacFlow · Prueba de cierre",100,80);closePanel.Left=closePanel.Top=-2000;closePanel.ShowActivated=false;closePanel.Content=Design.Glass(new System.Windows.Controls.Border(),22,false);closePanel.Show();int nativeClosed=0;closePanel.Closed+=delegate{nativeClosed++;};Design.Reveal(closePanel);Pump(60);Design.Dismiss(closePanel);
                // Assert at the action boundary. A background timer can run
                // late on offscreen windows, past the entire close duration.
                Assert(closePanel.IsVisible&&!closePanel.IsHitTestVisible&&DependencyPropertyHelper.GetValueSource(closePanel,UIElement.OpacityProperty).IsAnimated,"Paneles: cerrar durante apertura conserva la transición y bloquea acciones duplicadas",results);Pump(430);Assert(!closePanel.IsVisible&&nativeClosed==1,"Paneles: cierre de cristal nativo finaliza una vez",results);
                var actionHost=DockAppMotion.Host(new System.Windows.Controls.Border());var hoverScale=new ScaleTransform(1.27,1.27);
                var appIcon=new System.Windows.Controls.Border{Child=actionHost,RenderTransform=hoverScale};var appFixture=TestWindow();appFixture.Content=appIcon;appFixture.Show();
                var actionTransforms=(TransformGroup)actionHost.RenderTransform;var actionScale=(ScaleTransform)actionTransforms.Children[0];var actionShift=(TranslateTransform)actionTransforms.Children[1];
                DockAppMotion.Play(appIcon,false);Pump(460);
                Assert(Math.Abs(actionHost.Opacity-1)<.001&&Math.Abs(actionShift.Y)<.001&&Math.Abs(actionScale.ScaleX-1)<.001,"Apertura: icono vuelve nítido y a su posición",results);
                Assert(Math.Abs(hoverScale.ScaleX-1.27)<.001,"Apertura: conserva la ampliación del cursor",results);
                DockAppMotion.Play(appIcon,true);Pump(330);
                Assert(Math.Abs(actionHost.Opacity-1)<.001&&Math.Abs(actionShift.Y)<.001&&Math.Abs(actionScale.ScaleY-1)<.001,"Cierre: icono fijado conserva visibilidad y tamaño",results);
                DockAppMotion.Play(appIcon,true,true);Pump(245);Assert(actionHost.Opacity<.01,"Cierre: icono no fijado permanece oculto hasta retirarlo, sin reaparecer",results);DockAppMotion.Play(appIcon,true,true);Pump(70);DockAppMotion.Play(appIcon,false);Pump(470);
                Assert(Math.Abs(actionHost.Opacity-1)<.001&&Math.Abs(actionShift.Y)<.001,"Apps: reapertura interrumpida recupera el icono",results);
                DockAppMotion.Play(appIcon,false);Design.Animations=false;DockAppMotion.Play(appIcon,true);
                Assert(!actionHost.HasAnimatedProperties&&!actionScale.HasAnimatedProperties&&!actionShift.HasAnimatedProperties,"Apps: desactivar animaciones cancela los relojes",results);DockAppMotion.Play(appIcon,true,true);Assert(actionHost.Opacity==0&&!actionHost.HasAnimatedProperties,"Apps: movimiento reducido retira inmediatamente un icono que deja de estar abierto",results);Design.Animations=true;appFixture.Close();
                ClickRegression.Run(results);
                ConnectivityRegression.Run(directory,results);
                MediaRegression.Run(results);
                results.Add("PASS: " + results.Count + " comprobaciones"); File.WriteAllLines(Path.Combine(directory, "self-tests.txt"), results); return 0;
            }
            catch (Exception ex) { results.Add("FAIL: " + ex); File.WriteAllLines(Path.Combine(directory, "self-tests.txt"), results); return 1; }
        }
        private static void Assert(bool value, string name, List<string> results) { if (!value) throw new Exception(name); results.Add("PASS " + name); }
        private static Window TestWindow(){return new Window{Width=30,Height=30,Left=-2000,Top=-2000,ShowActivated=false,ShowInTaskbar=false,WindowStyle=WindowStyle.None,AllowsTransparency=true,Background=System.Windows.Media.Brushes.Transparent};}
        private sealed class MinimizeTestForm:System.Windows.Forms.Form{protected override bool ShowWithoutActivation{get{return true;}}}
        private static void Pump(int milliseconds){var frame=new DispatcherFrame();var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(milliseconds)};timer.Tick+=delegate{timer.Stop();frame.Continue=false;};timer.Start();Dispatcher.PushFrame(frame);}
    }
}

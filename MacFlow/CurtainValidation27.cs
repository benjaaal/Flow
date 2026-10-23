using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
namespace MacFlow
{
    // Real owned HWNDs. The recorder is optional and external to the product.
    // The test never locks Windows, acquires the taskbar or changes wallpaper.
    internal static class CurtainValidation27
    {
        internal static int Run(string directory)
        {
            string folder=Path.GetFullPath(directory);Directory.CreateDirectory(folder);Log.DirectoryPath=folder;
            var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};Desktop desktop=null;Window backdrop=null;PrivacyScreen curtain=null,privacy=null;
            Action<bool,string> check=delegate(bool ok,string label){File.AppendAllText(Path.Combine(folder,"checks.txt"),(ok?"PASS ":"FAIL ")+label+Environment.NewLine);if(!ok)throw new Exception(label);};
            app.DispatcherUnhandledException+=delegate(object sender,System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e){File.AppendAllText(Path.Combine(folder,"checks.txt"),e.Exception.ToString());e.Handled=true;app.Shutdown(1);};
            app.Startup+=async delegate
            {
                try
                {
                    Motion.TestClientAreaAnimations=true;Design.Animations=true;Design.InstallTheme();string profile=Path.Combine(folder,"profile");
                    var prefs=new Preferences{AutoHideTaskbar=false,ShowWidget=false,CheckUpdatesAutomatically=false,VoiceWakeEnabled=false,HideDockInApps=false,HideDockInCodex=false,HideInFullscreen=false};new SettingsStore(profile).Save(prefs);
                    desktop=new Desktop(profile,true,true);var art=WallpaperLibrary.Load(profile).First(w=>w.Name=="Seda");desktop.PresentationWallpaper=WallpaperLibrary.Decode(art.File,1920);
                    var scene=new Grid();scene.Children.Add(new Image{Source=desktop.PresentationWallpaper,Stretch=Stretch.UniformToFill});
                    scene.Children.Add(new TextBlock{Text="MacFlow · escritorio de prueba",FontSize=14,Foreground=Brushes.White,Opacity=.65,Margin=new Thickness(30),VerticalAlignment=VerticalAlignment.Bottom});
                    backdrop=new Window{Title="MacFlow · Prueba de cortina",WindowStyle=WindowStyle.None,ResizeMode=ResizeMode.NoResize,ShowInTaskbar=false,Topmost=true,ShowActivated=false,Left=0,Top=0,Width=SystemParameters.PrimaryScreenWidth,Height=SystemParameters.PrimaryScreenHeight,Content=scene};backdrop.SourceInitialized+=delegate{Native.NoActivate(backdrop);};backdrop.Show();await Task.Delay(250);
                    var media=new MediaFixture();media.State.Title="Prueba local de reproducción";media.State.Artist="Controles conectados al proveedor de prueba";media.State.Playing=media.State.CanToggle=media.State.CanSeek=true;media.State.Artwork=art.Thumbnail;media.State.Duration=TimeSpan.FromSeconds(210);media.State.Position=TimeSpan.FromSeconds(43);
                    privacy=new PrivacyScreen(desktop,true,media){ShowActivated=false};privacy.Show();await Task.Delay(650);
                    check(!privacy.PlanetVisible&&privacy.MusicVisible,"composición de escritorio: foto y reproductor compacto, Tierra sigue siendo opcional");await Program.CaptureOwnWindow(privacy,Path.Combine(folder,"bloqueo-foto.png"));privacy.Recover();privacy=null;
                    desktop.Activity.Add("MacFlow · prueba local","Tarjeta de notificación con fondo de vidrio, sin contorno dibujado.");
                    curtain=desktop.CreateNotificationView();curtain.PullQuick(1);await Task.Delay(120);Native.RECT rect;Native.GetWindowRect(new WindowInteropHelper(curtain).Handle,out rect);
                    curtain.PullQuick(0);
                    File.WriteAllText(Path.Combine(folder,"record-bounds.txt"),rect.Left+","+rect.Top+","+(rect.Right-rect.Left)+","+(rect.Bottom-rect.Top));
                    for(int i=0;i<100&&!File.Exists(Path.Combine(folder,"record-start"));i++)await Task.Delay(100);
                    curtain.PullQuick(0);curtain.CompleteQuick(true);await Task.Delay(650);check(curtain.QuickCornerRadius<1,"al abrir completamente el borde se integra con la pantalla");await Program.CaptureOwnWindow(curtain,Path.Combine(folder,"notificaciones-foto.png"));await Task.Delay(500);
                    for(int i=0;i<=22;i++){curtain.PullQuick(1-i/22.0*.36);await Task.Delay(16);}
                    check(Math.Abs(curtain.BackdropScreenOffset)<1,"el fondo queda anclado a la pantalla durante el recorrido, sin imagen doble");
                    check(curtain.QuickCornerRadius>10,"el contorno nativo se curva durante el arrastre y deja ver el escritorio real");double release=curtain.Top;curtain.CompleteQuick(false,-740);check(Math.Abs(curtain.Top-release)<=1,"soltar conserva la posición visible sin salto inicial");
                    var renderTimes=new System.Collections.Generic.List<double>();var renderWatch=Stopwatch.StartNew();double lastRender=0;EventHandler rendered=delegate{double now=renderWatch.Elapsed.TotalMilliseconds;if(lastRender>0)renderTimes.Add(now-lastRender);lastRender=now;};CompositionTarget.Rendering+=rendered;
                    double previous=curtain.Top;bool monotonic=true;int frames=0;var watch=Stopwatch.StartNew();
                    while(WindowLife.Alive(curtain)){await Task.Delay(8);if(!WindowLife.Alive(curtain))break;monotonic&=curtain.Top<=previous+1;previous=curtain.Top;frames++;if(watch.ElapsedMilliseconds>900)throw new Exception("Cierre no termina");}
                    CompositionTarget.Rendering-=rendered;check(monotonic&&frames>1,"cierre asciende sin rebote ni inversión, con muestreo en memoria");if(renderTimes.Count>0){var sorted=renderTimes.OrderBy(x=>x).ToArray();File.WriteAllText(Path.Combine(folder,"curtain-motion.txt"),"WPF curved curtain exit samples: "+sorted.Length+"; median ms: "+sorted[sorted.Length/2].ToString("F2")+"; p95 ms: "+sorted[(int)((sorted.Length-1)*.95)].ToString("F2")+". Measures WPF delivery, not certified display FPS.");}
                    check(desktop.NotificationView==null&&!File.Exists(Path.Combine(profile,"privacy.dat")),"cierre libera la cortina sin crear PIN ni bloquear Windows");await Task.Delay(600);
                    curtain=desktop.CreateNotificationView();curtain.CompleteQuick(true);await Task.Delay(650);
                    curtain.PullQuick(.74);curtain.CompleteQuick(false,-950);await Task.Delay(80);curtain.CompleteQuick(true,850);await Task.Delay(650);check(WindowLife.Alive(curtain)&&curtain.QuickCornerRadius<1,"invertir el gesto recupera la vista con continuidad");await Task.Delay(500);
                    curtain.PullQuick(.78);curtain.CompleteQuick(false,-1400);await Task.Delay(650);check(!WindowLife.Alive(curtain),"un segundo cierre rápido termina y libera su HWND");curtain=null;
                    check(!desktop.TaskbarReplacementActive,"prueba no modifica la barra de Windows");File.AppendAllText(Path.Combine(folder,"checks.txt"),"PASS TOTAL · cortina y bloqueo sobre foto"+Environment.NewLine);
                    for(int i=0;i<200&&File.Exists(Path.Combine(folder,"record-start"))&&!File.Exists(Path.Combine(folder,"record-stop"));i++)await Task.Delay(100);
                    app.Shutdown();
                }
                catch(Exception ex){File.AppendAllText(Path.Combine(folder,"checks.txt"),"FAIL "+ex+Environment.NewLine);app.Shutdown(1);}
            };
            app.Exit+=delegate{if(privacy!=null)privacy.Recover();if(curtain!=null)curtain.Recover();if(desktop!=null)desktop.Dispose();WindowLife.Close(backdrop);Motion.TestClientAreaAnimations=null;};return app.Run();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace MacFlow
{
    // Isolated integration harness: real WPF controls and owned HWNDs. Never
    // registers an appbar, hides Explorer, changes wallpaper or captures input.
    internal static class RenewalValidation27
    {
        private static string folder;private static int count;
        private static void Check(bool ok,string label){if(!ok)throw new Exception(label);count++;File.AppendAllText(Path.Combine(folder,"checks.txt"),"PASS "+label+Environment.NewLine);}
        private static async Task Snapshot(Window window,string name){await Program.CaptureOwnWindow(window,Path.Combine(folder,name+".png"));}
        internal static int Run(string directory)
        {
            folder=Path.GetFullPath(directory);Directory.CreateDirectory(folder);Log.DirectoryPath=folder;
            var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};Desktop desktop=null;VisualTestBackground background=null;WelcomeWindow welcome=null;ActivityIsland island=null;
            app.DispatcherUnhandledException+=delegate(object sender,System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e){File.AppendAllText(Path.Combine(folder,"checks.txt"),"FAIL "+e.Exception);e.Handled=true;app.Shutdown(1);};
            app.Startup+=async delegate
            {
                try
                {
                    Motion.TestClientAreaAnimations=true;Design.Animations=true;Design.InstallTheme();
                    string profile=Path.Combine(folder,"profile");var prefs=new Preferences{Theme="Dark",ShowTopBar=true,ShowWidget=false,AutoHideTaskbar=false,HideDockInApps=false,HideDockInCodex=false,DockAutoHide=false,HideInFullscreen=false,CheckUpdatesAutomatically=false,VoiceWakeEnabled=false,ContainedDockApplied=true,AppleDesign27Applied=true,KeyboardSearchApplied=true};
                    prefs.Pins.Add(new AppEntry{Name="Archivos",Target="explorer.exe"});prefs.Pins.Add(new AppEntry{Name="Configuración",Target="ms-settings:"});new SettingsStore(profile).Save(prefs);
                    desktop=new Desktop(profile,true,true);var wallpaper=WallpaperLibrary.Load(profile)[0];desktop.PresentationWallpaper=new System.Windows.Media.Imaging.BitmapImage(new Uri(wallpaper.File));
                    background=new VisualTestBackground{WindowState=WindowState.Normal,Left=0,Top=0,Width=SystemParameters.PrimaryScreenWidth,Height=SystemParameters.PrimaryScreenHeight,ShowActivated=false,IsHitTestVisible=false};background.Content=new Image{Source=desktop.PresentationWallpaper,Stretch=Stretch.UniformToFill};background.SourceInitialized+=delegate{Native.NoActivate(background);};background.Show();await Task.Delay(800);
                    Check((double)desktop.TopBar.GetValue(Native.BarGap)==0,"barra superior continua: la isla no perfora su material");
                    await Snapshot(desktop.TopBar,"topbar");await Snapshot(desktop.Dock,"dock");
                    using(var probe=new MotionProbe(desktop)){await Task.Delay(5000);File.WriteAllText(Path.Combine(folder,"motion.txt"),probe.Report());}await Task.Delay(650);
                    Check(!desktop.TaskbarReplacementActive,"la prueba no adquiere ni modifica la barra de Windows");
                    Check(NotificationPullGesture.Accepts(12)&&!NotificationPullGesture.Accepts(140)&&NotificationPullGesture.Progress(4,800)==0,"gesto de esquina izquierda exige dirección y umbral; no roba clics del menú");
                    Check(PrivacyScreen.QuickShouldClose(30,900,800)&&!PrivacyScreen.QuickShouldClose(30,0,800)&&!PrivacyScreen.QuickShouldClose(150,-600,800),"cierre respeta impulso ascendente, arrastre corto y reversión del dedo");
                    desktop.Activity.Add("MacFlow · Prueba local","Las notificaciones se actualizan en vivo.");
                    var curtain=desktop.CreateNotificationView();curtain.PullQuick(1);double target=curtain.Top;curtain.PullQuick(.4);double partial=curtain.Top;
                    Check(curtain.NotificationView&&!curtain.AuthenticationVisible&&desktop.Privacy==null&&partial<target,"la cortina sigue el gesto sin abrir autenticación ni privacidad");
                    curtain.CompleteQuick(true);await Task.Delay(650);File.AppendAllText(Path.Combine(folder,"checks.txt"),"INFO cortina top="+curtain.Top+" esperado="+target+" avisos="+curtain.NotificationCount+" visible="+curtain.IsVisible+Environment.NewLine);Check(Math.Abs(curtain.Top-target)<=1&&curtain.NotificationCount==1,"soltar completa la cortina y muestra una notificación real del historial");
                    desktop.Activity.Add("Segunda prueba local","Actualización mientras la cortina está abierta.");Check(curtain.NotificationCount==2,"notificaciones recibidas se incorporan sin reabrir la vista");Check(curtain.ClockRefractive||!RenderCapability.IsPixelShaderVersionSupported(3,0),"reloj usa refracción del fondo propio con alternativa para GPU incompatible");await Snapshot(curtain,"notification-curtain");
                    curtain.PullQuick(.7);double releaseTop=curtain.Top;curtain.CompleteQuick(false,-900);Check(Math.Abs(curtain.Top-releaseTop)<=1,"soltar no reinicia la posición ni duplica el desplazamiento del contenido");await Task.Delay(70);curtain.CompleteQuick(true);await Task.Delay(700);Check(WindowLife.Alive(curtain)&&Math.Abs(curtain.Top-target)<=1,"invertir salida y apertura conserva una sola cortina");
                    curtain.RevealAccess();await Task.Delay(700);Check(!WindowLife.Alive(curtain)&&desktop.NotificationView==null&&!File.Exists(Path.Combine(profile,"privacy.dat")),"volver al escritorio no crea PIN ni bloquea la sesión");
                    using(var pull=new NotificationPullGesture(desktop.TopBar,delegate{return true;},desktop.CreateNotificationView))
                    {
                        pull.Pull(20);var shortView=desktop.NotificationView;pull.Release();await Task.Delay(700);Check(!WindowLife.Alive(shortView),"un arrastre corto cancela y libera su ventana");
                        pull.Pull(600);var longView=desktop.NotificationView;pull.Release();await Task.Delay(650);Check(WindowLife.Alive(longView)&&!longView.AuthenticationVisible,"un arrastre largo abre la vista directamente");longView.Recover();
                    }
                    desktop.Activity.Clear();
                    desktop.OpenSettings();var settings=(SettingsWindow)desktop.Settings;for(int i=0;i<17;i++){settings.SelectPage(i);await Task.Delay(45);}settings.SelectPage(0);await Task.Delay(450);await Snapshot(settings,"settings");WindowLife.Close(settings);
                    desktop.OpenLaunchpad();await Task.Delay(450);await Snapshot(desktop.Launcher,"applications");WindowLife.Close(desktop.Launcher);
                    desktop.OpenWidgets(null);await Task.Delay(400);Check(desktop.Widgets!=null,"catálogo de widgets conserva el controlador y sus datos");
                    var media=new MediaFixture();media.State.Title="Continuidad";media.State.Artist="Prueba de transición";media.State.Playing=media.State.CanToggle=true;media.State.Artwork=wallpaper.Thumbnail;
                    island=new ActivityIsland(desktop,media);desktop.Island=island;await Task.Delay(600);island.Toggle();
                    string frames=Path.Combine(folder,"island-frames");Directory.CreateDirectory(frames);
                    for(int i=0;i<55;i++){if(i==19)island.Toggle();if(i==31)island.Toggle();await Task.Delay(25);Program.Render(island.View,Path.Combine(frames,i.ToString("D3")+".png"));}
                    await Task.Delay(700);Check(island.Expanded&&island.ExpandedContentOpacity>.99,"la isla termina abierta e interactiva tras varias inversiones");await Snapshot(island.View,"dynamic-island");island.Dispose();island=null;desktop.Island=null;
                    welcome=new WelcomeWindow(new System.Drawing.Rectangle(100,80,760,540),false,desktop.PresentationWallpaper);welcome.ShowActivated=false;welcome.Show();
                    string helloFrames=Path.Combine(folder,"hello-frames");Directory.CreateDirectory(helloFrames);double last=0;
                    for(int i=0;i<78;i++){await Task.Delay(28);double progress=welcome.Lettering.Progress;Check(progress>=last-.0001,"trazo Hola continuo · "+i);last=progress;Program.Render(welcome,Path.Combine(helloFrames,i.ToString("D3")+".png"));if(welcome.Ready)break;}
                    await Task.Delay(400);Check(welcome.Ready&&welcome.ContentFits,"Hola termina con controles disponibles y sin relojes perpetuos");await Snapshot(welcome,"hello");welcome.DismissWelcome();await Task.Delay(600);Check(!WindowLife.Alive(welcome),"cerrar Hola libera la presentación");welcome=null;
                    desktop.Dispose();desktop=null;Check(GenieTransition.ActiveCount==0,"salida libera texturas temporales y ventanas de transición");File.AppendAllText(Path.Combine(folder,"checks.txt"),"PASS TOTAL "+count+Environment.NewLine);app.Shutdown();
                }
                catch(Exception ex){File.AppendAllText(Path.Combine(folder,"checks.txt"),"FAIL "+ex+Environment.NewLine);app.Shutdown(1);}
            };
            app.Exit+=delegate{if(island!=null)island.Dispose();if(welcome!=null)WindowLife.Close(welcome);if(desktop!=null)desktop.Dispose();WindowLife.Close(background);Motion.TestClientAreaAnimations=null;};return app.Run();
        }
    }
}

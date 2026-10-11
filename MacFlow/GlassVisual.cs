using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
namespace MacFlow
{
    internal static class GlassVisual
    {
        [DllImport("gdi32.dll")]private static extern IntPtr CreateRectRgn(int left,int top,int right,int bottom);
        [DllImport("gdi32.dll")]private static extern bool DeleteObject(IntPtr region);
        [DllImport("gdi32.dll")]private static extern int GetRgnBox(IntPtr region,out Native.RECT rect);
        [DllImport("user32.dll")]private static extern int GetWindowRgn(IntPtr window,IntPtr region);
        [DllImport("dwmapi.dll")]private static extern int DwmFlush();
        internal static int Run(string directory)
        {
            directory=Path.GetFullPath(directory);Directory.CreateDirectory(directory);Log.DirectoryPath=directory;
            var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};Desktop desktop=null;VisualTestBackground background=null;ConnectivityPanel wifi=null;var results=new List<string>();
            app.Startup+=async delegate
            {
                try
                {
                    var profile=Path.Combine(directory,"profile");new SettingsStore(profile).Save(new Preferences{Theme="Dark",IconSize=36,ContainedDockApplied=true,GlassOpacity=.113,HideInFullscreen=false,DockAutoHide=false,HideDockInCodex=false});
                    desktop=new Desktop(profile,true,true);background=new VisualTestBackground{WindowState=WindowState.Normal,ShowActivated=false,IsHitTestVisible=false,Topmost=true};background.SourceInitialized+=delegate{Native.NoActivate(background);};
                    background.Width=desktop.Dock.Width;background.Height=desktop.Dock.Height;background.Left=desktop.Dock.Left;background.Top=desktop.Dock.Top;background.Show();
                    await Task.Delay(700);await Program.CaptureOwnWindow(desktop.Dock,Path.Combine(directory,"dock-36.png"));if(desktop.Preferences.IconSize!=36||desktop.Dock.Height>90)throw new Exception("Dock agrandado");results.Add("PASS Dock de 36 px: dimensiones conservadas y captura nativa sobre fondo aislado.");
                    await CheckExit(background,directory,results);
                    // Controlled entries exercise the real app grid and filter;
                    // the sandbox cannot access the user's Start Menu catalog.
                    desktop.Apps=new List<AppEntry>();string[] names={"Explorador","Configuración","Bloc de notas","Calculadora","Paint","Terminal","Fotos","Calendario","Cámara","Correo","Mapas","Recortes"};
                    foreach(var name in names)desktop.Apps.Add(new AppEntry{Name=name,Target="ms-settings:"+name,IconPath=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),"System32",name=="Bloc de notas"?"notepad.exe":"shell32.dll")});
                    desktop.OpenLaunchpad();var filtered=(Launchpad)desktop.Launcher;filtered.SearchQuery="calculadora";if(filtered.VisibleAppCount!=1)throw new Exception("Filtro de Aplicaciones");filtered.SearchQuery="";if(filtered.VisibleAppCount!=12)throw new Exception("Cuadrícula de Aplicaciones");await Scene(desktop.Launcher,background,directory,"aplicaciones");
                    var launcher=(Launchpad)desktop.Launcher;launcher.MinimizeButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Task.Delay(450);if(launcher.WindowState!=WindowState.Minimized)throw new Exception("Minimizar Aplicaciones");desktop.OpenLaunchpad();await Task.Delay(450);if(desktop.Launcher!=launcher||launcher.WindowState!=WindowState.Normal)throw new Exception("Restaurar Aplicaciones");launcher.CloseButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Task.Delay(450);if(desktop.Launcher!=null)throw new Exception("Cerrar Aplicaciones");results.Add("PASS Aplicaciones: controles amarillos/rojos reales, restauración de la misma instancia y cierre finalizado.");
                    desktop.OpenControl();await Scene(desktop.Control,background,directory,"centro-control");var control=desktop.Control;Design.Minimize(control);await Task.Delay(450);desktop.OpenControl();await Task.Delay(450);if(desktop.Control!=control||control.WindowState!=WindowState.Normal)throw new Exception("Restaurar Centro de control");Design.Dismiss(control);await Task.Delay(450);if(desktop.Control!=null)throw new Exception("Cerrar Centro de control");results.Add("PASS Centro de control: módulos reales y restauración después de minimizar.");
                    wifi=new ConnectivityPanel(true,ConnectivityRegression.Sample,b=>{},item=>{},ConnectivityRegression.RadioFixture){ShowActivated=false,IsHitTestVisible=false,ShowInTaskbar=false,Left=40,Top=65};wifi.SourceInitialized+=delegate{Native.NoActivate(wifi);};wifi.Show();Design.Reveal(wifi);await Scene(wifi,background,directory,"wifi");if(!wifi.Ready||wifi.ItemCount!=3)throw new Exception("Wi-Fi no listo");
                    wifi.SwitchMode(false);await Task.Delay(500);await Scene(wifi,background,directory,"bluetooth");wifi.Close();wifi=null;results.Add("PASS Wi-Fi/Bluetooth con datos aislados. Sin conexiones ni cambios de radio.");
                    desktop.Preferences.LocalUpdateFolder=profile;desktop.Preferences.UpdateFeedUrl="https://example.invalid/updates/MacFlow-update.json";desktop.OpenSettings();var settingsDraft=(Preferences)typeof(SettingsWindow).GetField("draft",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(desktop.Settings);var draftStore=new SettingsStore(Path.Combine(directory,"settings-draft"));draftStore.Save(settingsDraft);var draftSaved=draftStore.Load();if(draftSaved.LocalUpdateFolder!=profile||draftSaved.UpdateFeedUrl!=desktop.Preferences.UpdateFeedUrl)throw new Exception("Configuración perdió el canal de actualizaciones");results.Add("PASS Configuración conserva y serializa canal local y HTTPS sin modificar el perfil del usuario.");await Scene(desktop.Settings,background,directory,"configuracion");if(Math.Abs(Native.GlassRadius(desktop.Settings)-16)>.01)throw new Exception("El overlay cambió el radio de Configuración");((SettingsWindow)desktop.Settings).SelectPage(1);await Task.Delay(1100);await Scene(desktop.Settings,background,directory,"fondos-nuevos");desktop.Settings.Close();results.Add("PASS Galería nueva: imágenes integradas, miniaturas y vista previa en Configuración. No cambia el fondo de Windows.");
                    wifi=new ConnectivityPanel(true,ConnectivityRegression.Sample,b=>{},item=>{},ConnectivityRegression.RadioFixture){ShowActivated=false,IsHitTestVisible=false,ShowInTaskbar=false,Left=40,Top=65};wifi.SourceInitialized+=delegate{Native.NoActivate(wifi);};wifi.Show();Design.Reveal(wifi);await Task.Delay(500);background.Content=new Border{Background=Brushes.White};await Scene(wifi,background,directory,"wifi-fondo-claro");
                    await Contrast(wifi,false);await Program.CaptureOwnWindow(wifi,Path.Combine(directory,"wifi-fondo-claro.png"));background.Content=new Border{Background=Design.Brush("#081326")};await Scene(wifi,background,directory,"wifi-fondo-oscuro");await Contrast(wifi,true);await Program.CaptureOwnWindow(wifi,Path.Combine(directory,"wifi-fondo-oscuro.png"));results.Add("PASS Texto adapta contraste sin capturar imágenes ni realimentar el material.");
                    await Scene(desktop.TopBar,background,directory,"barra-superior");
                    using(var motion=new MotionProbe(desktop)){await Task.Delay(3000);File.WriteAllText(Path.Combine(directory,"motion.txt"),motion.Report());}
                    var process=Process.GetCurrentProcess();var cpu=process.TotalProcessorTime;var started=DateTime.UtcNow;await Task.Delay(4000);double seconds=(DateTime.UtcNow-started).TotalSeconds,processorSeconds=(process.TotalProcessorTime-cpu).TotalSeconds;results.Add("INFO Reposo observado: "+(processorSeconds/seconds*100).ToString("F2")+"% de un núcleo, "+(process.WorkingSet64/1048576.0).ToString("F1")+" MiB. Medición de esta prueba, no garantía de FPS.");
                    wifi.Close();wifi=null;File.WriteAllLines(Path.Combine(directory,"visual-test.txt"),results);app.Shutdown();
                }
                catch(Exception ex){results.Add("FAIL "+ex);File.WriteAllLines(Path.Combine(directory,"visual-test.txt"),results);app.Shutdown(1);}
            };
            app.Exit+=delegate{if(wifi!=null)wifi.Close();if(desktop!=null)desktop.Dispose();if(background!=null)background.Close();};return app.Run();
        }
        private static async Task Scene(Window panel,VisualTestBackground background,string directory,string name)
        {
            await Task.Delay(500);background.Left=panel.Left;background.Top=panel.Top;background.Width=panel.ActualWidth;background.Height=panel.ActualHeight;
            await Program.CaptureOwnWindow(panel,Path.Combine(directory,name+".png"));
        }
        private static async Task CheckExit(VisualTestBackground background,string directory,List<string> results)
        {
            var panel=Design.Panel("MacFlow · Cierre de prueba",360,220);panel.Left=140;panel.Top=130;panel.ShowActivated=false;panel.IsHitTestVisible=false;panel.Content=Design.Glass(Design.Text("Cerrar suavemente",22,true),22,false);
            background.Left=100;background.Top=90;background.Width=440;background.Height=310;
            panel.SourceInitialized+=delegate{Native.NoActivate(panel);};panel.Show();await Program.CaptureOwnWindow(panel,Path.Combine(directory,"cierre-inicio.png"));
            Native.RECT capture,back;Native.GetWindowRect(new WindowInteropHelper(panel).Handle,out capture);Native.GetWindowRect(new WindowInteropHelper(background).Handle,out back);
            if(back.Left>capture.Left||back.Top>capture.Top||back.Right<capture.Right||back.Bottom<capture.Bottom+20)throw new Exception("La captura de cierre salió del fondo de prueba");
            var handle=new WindowInteropHelper(panel).Handle;int frames=0,closed=0;long previousArea=Int64.MaxValue;double next=.90;var sizes=new List<long>();var pictures=new List<System.Drawing.Bitmap>();
            Native.SetWindowPos(handle,new IntPtr(-1),0,0,0,0,0x13);Native.SetWindowPos(new WindowInteropHelper(background).Handle,handle,0,0,0,0,0x13);
            Action<Window> frame=delegate(Window window)
            {
                if(window!=panel)return;var region=CreateRectRgn(0,0,1,1);Native.RECT rect;try{if(GetWindowRgn(handle,region)==0)return;GetRgnBox(region,out rect);}finally{DeleteObject(region);}
                long area=(long)(rect.Right-rect.Left)*(rect.Bottom-rect.Top);long total=(long)(capture.Right-capture.Left)*(capture.Bottom-capture.Top);
                if(area>=total*next||area>=previousArea||area<4)return;previousArea=area;sizes.Add(area);frames++;next-=.25;
                DwmFlush();var bitmap=new System.Drawing.Bitmap(capture.Right-capture.Left,capture.Bottom-capture.Top);using(var graphics=System.Drawing.Graphics.FromImage(bitmap)){graphics.CopyFromScreen(capture.Left,capture.Top,0,0,bitmap.Size);}pictures.Add(bitmap);
            };
            panel.Closed+=delegate{closed++;};PanelMotion.Frame+=frame;
            try{Design.Dismiss(panel);await Task.Delay(550);if(closed!=1||Native.IsWindow(handle)||frames<2)throw new Exception("Salida nativa incompleta: cuadros="+frames+" cierres="+closed);for(int i=0;i<pictures.Count;i++)pictures[i].Save(Path.Combine(directory,"cierre-"+(i+1)+".png"),System.Drawing.Imaging.ImageFormat.Png);results.Add("PASS Cierre: cristal y contenido se recogen en "+frames+" cuadros comprobados; áreas nativas "+String.Join(",",sizes)+"; un solo cierre.");}
            finally{PanelMotion.Frame-=frame;foreach(var picture in pictures)picture.Dispose();if(panel.IsVisible)panel.Close();}
        }
        private static async Task Contrast(Window panel,bool light)
        {for(int i=0;i<20;i++){var ink=panel.Resources["GlassInk"] as SolidColorBrush;if(ink!=null&&(light?ink.Color.R>=220:ink.Color.R<=80))return;await Task.Delay(100);}throw new Exception("El texto no adapta contraste sobre fondo "+(light?"oscuro":"claro"));}
    }
}

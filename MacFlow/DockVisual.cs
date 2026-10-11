using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
namespace MacFlow
{
    internal static class DockVisual
    {
        [DllImport("user32.dll")]private static extern bool GetWindowDisplayAffinity(IntPtr h,out uint affinity);
        internal static int Run(string directory)
        {
            directory=Path.GetFullPath(directory);Directory.CreateDirectory(directory);Log.DirectoryPath=directory;
            var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};Desktop desktop=null;VisualTestBackground backdrop=null;int stage=0;bool busy=false;var report=new List<string>();
            app.DispatcherUnhandledException+=delegate(object sender,DispatcherUnhandledExceptionEventArgs e){File.WriteAllText(Path.Combine(directory,"visual-test.txt"),"FAIL: "+e.Exception);e.Handled=true;app.Shutdown(1);};
            app.Startup+=delegate
            {
                var store=new SettingsStore(Path.Combine(directory,"profile"));var p=new Preferences{IconSize=36,ContainedDockApplied=true,Theme="Dark",DockAutoHide=false,HideDockInCodex=false,ShowTopBar=false,HideInFullscreen=false};p.Pins.Add(new AppEntry{Name="Explorador",Target="explorer.exe"});p.Pins.Add(new AppEntry{Name="Configuración",Target="ms-settings:"});store.Save(p);
                backdrop=new VisualTestBackground{WindowState=WindowState.Normal,Width=SystemParameters.PrimaryScreenWidth,Height=180,Left=0,Top=SystemParameters.PrimaryScreenHeight-180,ShowActivated=false,ShowInTaskbar=false,IsHitTestVisible=false,Topmost=true};backdrop.SourceInitialized+=delegate{Native.NoActivate(backdrop);};backdrop.Show();
                desktop=new Desktop(store.DirectoryPath,true,true);
                var timer=new DispatcherTimer{Interval=TimeSpan.FromSeconds(2)};timer.Tick+=async delegate
                {
                    if(busy)return;busy=true;
                    try
                    {
                        if(stage==0){CheckAffinity(desktop.Dock,report);await Program.CaptureOwnWindow(desktop.Dock,Path.Combine(directory,"dock-36.png"));Peak(desktop);}
                        else if(stage==1){CheckBounds(desktop,report);await Program.CaptureOwnWindow(desktop.Dock,Path.Combine(directory,"dock-36-peak.png"));desktop.Dock.Hide();desktop.Preferences.IconSize=44;desktop.DrawDock();desktop.Dock.UpdateLayout();Peak(desktop);CheckBounds(desktop,report);desktop.Preferences.IconSize=72;desktop.DrawDock();desktop.Dock.UpdateLayout();Peak(desktop);CheckBounds(desktop,report);desktop.Preferences.DockPosition="Left";desktop.DrawDock();desktop.Dock.UpdateLayout();Peak(desktop);CheckBounds(desktop,report);desktop.Preferences.IconSize=36;desktop.Preferences.DockPosition="Bottom";desktop.DrawDock();desktop.Dock.Show();}
                        else if(stage==2){desktop.OpenSettings();var existing=desktop.Settings;existing.WindowState=WindowState.Minimized;desktop.OpenSettings();if(desktop.Settings!=existing||existing.WindowState!=WindowState.Normal)throw new Exception("Configuración no se restauró");CheckAffinity(existing,report);report.Add("PASS Configuración minimizada se restaura sin duplicar ventanas.");existing.Close();}
                        else{timer.Stop();report.Add("PASS: capturas normales sin exclusión de grabación; cristal nativo sin captura recursiva; todas las comprobaciones pertenecen a ventanas de prueba.");File.WriteAllLines(Path.Combine(directory,"visual-test.txt"),report);app.Shutdown();}stage++;
                    }
                    catch(Exception ex){timer.Stop();report.Add("FAIL "+ex);File.WriteAllLines(Path.Combine(directory,"visual-test.txt"),report);app.Shutdown(1);}finally{busy=false;}
                };timer.Start();
            };app.Exit+=delegate{if(desktop!=null)desktop.Dispose();if(backdrop!=null)backdrop.Close();};return app.Run();
        }
        private static void CheckAffinity(Window window,List<string> report)
        {uint affinity;if(!GetWindowDisplayAffinity(new WindowInteropHelper(window).Handle,out affinity)||affinity!=0)throw new Exception("Ventana excluida de grabación");if(window.AllowsTransparency)throw new Exception("Backend sin compositor");report.Add("PASS Afinidad normal de grabación: "+window.Title+"; "+window.Tag);}
        private static void Peak(Desktop desktop)
        {
            foreach(var cell in desktop.DockItems.Children.OfType<StackPanel>())
            {
                var icon=cell.Tag as Border;if(icon==null)continue;var hover=(ScaleTransform)((TransformGroup)icon.RenderTransform).Children[0];hover.ScaleX=hover.ScaleY=DockMotion.PeakScale(desktop.Preferences.IconSize);
                var host=(Border)icon.Child;var transform=(TransformGroup)host.RenderTransform;((ScaleTransform)transform.Children[0]).ScaleX=1.02;((ScaleTransform)transform.Children[0]).ScaleY=1.035;((TranslateTransform)transform.Children[1]).Y=-4;
            }
        }
        private static void CheckBounds(Desktop desktop,List<string> report)
        {
            var layer=(Grid)desktop.DockGlass.Child;int checkedIcons=0;
            foreach(var cell in desktop.DockItems.Children.OfType<StackPanel>())
            {
                var icon=cell.Tag as Border;if(icon==null)continue;var host=(Border)icon.Child;
                var rect=host.TransformToAncestor(layer).TransformBounds(new Rect(new Point(),host.RenderSize));
                if(rect.Left<-.01||rect.Top<-.01||rect.Right>layer.ActualWidth+.01||rect.Bottom>layer.ActualHeight+.01)throw new Exception("Icono fuera del cristal: "+desktop.Preferences.IconSize+" "+desktop.Preferences.DockPosition+" bounds="+rect+" layer="+layer.RenderSize);
                checkedIcons++;
            }
            if(checkedIcons<2||layer.Clip==null)throw new Exception("No se verificó contención real");report.Add("PASS "+checkedIcons+" iconos dentro del cristal a máximo zoom + impulso de apertura: "+desktop.Preferences.IconSize+" px · "+desktop.Preferences.DockPosition);
        }
    }
}

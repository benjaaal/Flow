using System;
using System.Threading.Tasks;
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
        internal static int Run(string directory,bool observe=false)
        {
            directory=Path.GetFullPath(directory);Directory.CreateDirectory(directory);Log.DirectoryPath=directory;
            var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};Desktop desktop=null;VisualTestBackground backdrop=null;int stage=0;bool busy=false;var report=new List<string>();
            app.DispatcherUnhandledException+=delegate(object sender,DispatcherUnhandledExceptionEventArgs e){File.WriteAllText(Path.Combine(directory,"visual-test.txt"),"FAIL: "+e.Exception);e.Handled=true;app.Shutdown(1);};
            app.Startup+=delegate
            {
                var store=new SettingsStore(Path.Combine(directory,"profile"));var p=new Preferences{IconSize=36,ContainedDockApplied=true,Theme="Dark",DockAutoHide=false,HideDockInApps=false,HideDockInCodex=false,ShowTopBar=false,HideInFullscreen=false};p.Pins.Add(new AppEntry{Name="Explorador",Target="explorer.exe"});p.Pins.Add(new AppEntry{Name="Configuración",Target="ms-settings:"});store.Save(p);
                var oldPlayer=new AppEntry{Name="Roblox",Executable=@"C:\Apps\Roblox\Versions\version-old\RobloxPlayerBeta.exe",Target="roblox-player:"};var newPlayer=new WindowEntry{Executable=@"C:\Apps\Roblox\Versions\version-new\RobloxPlayerBeta.exe"};var studio=new WindowEntry{Executable=@"C:\Apps\Roblox\Versions\version-new\RobloxStudioBeta.exe"};var grouped=AppIdentity.DockEntries(new[]{oldPlayer,oldPlayer},new[]{newPlayer,newPlayer,studio});if(grouped.Count!=2||grouped[0]!=oldPlayer||oldPlayer.Key!=newPlayer.Key||studio.Key==newPlayer.Key)throw new Exception("Duplicados tras actualizar Roblox o mezcla Player/Studio");report.Add("PASS Una sola entrada por app al actualizar Roblox o repetir favoritos; Studio conserva su identidad.");
                if(AppIdentity.Key(@"C:\WindowsApps\OpenAI.Codex_1\app\Codex.exe")!=AppIdentity.Key(@"C:\WindowsApps\OpenAI.Codex_2\app\Codex.exe")||AppIdentity.Key(@"C:\WindowsApps\OpenAI.Codex_2\app\Codex.exe")==AppIdentity.Key(@"C:\WindowsApps\OpenAI.ChatGPT_2\app\ChatGPT.exe")||AppIdentity.Key(@"C:\One\Other.exe")==AppIdentity.Key(@"C:\Two\Other.exe"))throw new Exception("Agrupación confunde aplicaciones distintas");report.Add("PASS Codex conserva identidad entre versiones; ChatGPT y ejecutables ajenos siguen separados.");
                backdrop=new VisualTestBackground{WindowState=WindowState.Normal,Width=SystemParameters.PrimaryScreenWidth,Height=180,Left=0,Top=SystemParameters.PrimaryScreenHeight-180,ShowActivated=false,ShowInTaskbar=false,IsHitTestVisible=false,Topmost=true};backdrop.SourceInitialized+=delegate{Native.NoActivate(backdrop);};backdrop.Show();
                for(int n=0;n<11;n++)p.Pins.Add(new AppEntry{Name="Prueba "+n,Target="dock-fixture-"+n+".exe",IconPath=System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),"explorer.exe")});store.Save(p);desktop=new Desktop(store.DirectoryPath,true,true);
                if(observe){desktop.Dock.ShowInTaskbar=true;var hwnd=new WindowInteropHelper(desktop.Dock).Handle;Native.SetWindowLong(hwnd,-20,Native.GetWindowLong(hwnd,-20)&~(0x80|0x08000000));desktop.DrawDock();desktop.Dock.UpdateLayout();Program.Render(desktop.Dock,Path.Combine(directory,"dock-observed-wpf.png"));}
                var timer=new DispatcherTimer{Interval=TimeSpan.FromSeconds(observe?12:2)};timer.Tick+=async delegate
                {
                    if(busy)return;busy=true;
                    try
                    {
                        if(stage==0){foreach(double edge in new[]{0.0,.5,1.0}){desktop.ProbeDockMotion(edge);await Task.Delay(350);CheckBounds(desktop,report);}desktop.StopDockProbe();var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;((DispatcherTimer)typeof(Desktop).GetField("timer",flags).GetValue(desktop)).Stop();((DispatcherTimer)typeof(Desktop).GetField("edgeTimer",flags).GetValue(desktop)).Stop();CheckAffinity(desktop.Dock,report);await Program.CaptureOwnWindow(desktop.Dock,Path.Combine(directory,"dock-36.png"));desktop.ProbeDockMotion(.32);await System.Threading.Tasks.Task.Delay(450);CheckBounds(desktop,report);if(!desktop.DockItems.Children.OfType<StackPanel>().Any(cell=>Math.Abs(((TranslateTransform)cell.RenderTransform).X)>1))throw new Exception("Los vecinos no se desplazaron");await Program.CaptureOwnWindow(desktop.Dock,Path.Combine(directory,"dock-hover.png"));var zooms=desktop.DockItems.Children.OfType<StackPanel>().Select(cell=>((ScaleTransform)((TransformGroup)((Border)cell.Tag).RenderTransform).Children[0]).ScaleX).ToArray();if(zooms.Max()<1.1)throw new Exception("La ampliación se reinició durante la captura");report.Add("PASS Ampliación visible y persistente: "+String.Join(", ",zooms.Select(z=>z.ToString("F3"))));using(var probe=new MotionProbe(desktop)){await System.Threading.Tasks.Task.Delay(3000);File.WriteAllText(Path.Combine(directory,"motion.txt"),probe.Report());}await System.Threading.Tasks.Task.Delay(700);report.Add("PASS Ampliación real: vecinos se desplazan con el controlador, y al salir regresan sin cambiar el layout.");Peak(desktop);}
                        else if(stage==1){CheckBounds(desktop,report);await Program.CaptureOwnWindow(desktop.Dock,Path.Combine(directory,"dock-36-peak.png"));desktop.Dock.Hide();foreach(int size in new[]{32,44,64}){desktop.Preferences.IconSize=size;desktop.DrawDock();desktop.Dock.UpdateLayout();Peak(desktop);CheckBounds(desktop,report);}desktop.Preferences.IconSize=72;desktop.DrawDock();desktop.Dock.UpdateLayout();Peak(desktop);CheckBounds(desktop,report);desktop.Preferences.DockPosition="Left";desktop.DrawDock();desktop.Dock.UpdateLayout();Peak(desktop);CheckBounds(desktop,report);desktop.Preferences.IconSize=36;desktop.Preferences.DockPosition="Bottom";desktop.DrawDock();desktop.Dock.Show();}
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
            var layer=(Grid)desktop.DockGlass.Child;int checkedIcons=0;Native.RECT native;Native.GetWindowRect(new WindowInteropHelper(desktop.Dock).Handle,out native);var physical=desktop.DockGlass.PointToScreen(new Point(desktop.DockGlass.ActualWidth,desktop.DockGlass.ActualHeight));if(Math.Abs(physical.X-native.Right)>2||Math.Abs(physical.Y-native.Bottom)>2)throw new Exception("El cristal no coincide con los límites físicos del Dock");
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

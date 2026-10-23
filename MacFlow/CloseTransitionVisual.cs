using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
namespace MacFlow
{
    internal static class CloseTransitionVisual
    {
        internal static int Run(string directory)
        {
            directory=Path.GetFullPath(directory);Directory.CreateDirectory(directory);Log.DirectoryPath=directory;var results=new List<string>();var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};Desktop desktop=null;Window source=null,background=null;int previews=0,frames=0;double last=0;
            Action<Window> started=delegate(Window preview){previews++;Native.RECT rect;Native.GetWindowRect(new WindowInteropHelper(preview).Handle,out rect);var target=PanelMotion.DockDestination(preview).Value;last=(new Point((rect.Left+rect.Right)/2.0,(rect.Top+rect.Bottom)/2.0)-target).Length;};
            Action<Window> frame=delegate(Window preview){if(preview.Title!="MacOS · Cerrar ventana"&&preview.Title!="MacOS · Minimizar ventana")return;frames++;Native.RECT rect;Native.GetWindowRect(new WindowInteropHelper(preview).Handle,out rect);var target=PanelMotion.DockDestination(preview).Value;double distance=(new Point((rect.Left+rect.Right)/2.0,(rect.Top+rect.Bottom)/2.0)-target).Length;if(distance>last+4)throw new Exception("La transición se aleja del Dock");last=distance;};
            app.Startup+=async delegate
            {
                try
                {
                    Motion.TestClientAreaAnimations=true;var profile=Path.Combine(directory,"profile");new SettingsStore(profile).Save(new Preferences{IconSize=36,ContainedDockApplied=true,DockAutoHide=false,HideDockInApps=false,HideDockInCodex=false,ShowTopBar=false});desktop=new Desktop(profile,true,true);
                    background=new Window{Title="MacOS · Fondo de prueba",WindowStyle=WindowStyle.None,Width=SystemParameters.PrimaryScreenWidth,Height=SystemParameters.PrimaryScreenHeight,ShowActivated=false,Background=Design.Brush("#314E62"),Left=0,Top=0};background.Show();
                    WindowCloseTransition.PreviewStarted+=started;PanelMotion.Frame+=frame;
                    source=Document();source.Show();await Task.Delay(300);var entry=Entry(source);int closes=0;source.Closed+=delegate{closes++;};if(!WindowCloseTransition.Request(entry,()=>{throw new Exception("Rechazo de cierre");}))throw new Exception("Solicitud rechazada");await Task.Delay(1100);
                    if(Native.Matches(entry)||closes!=1||previews!=1||frames<3||last>20)throw new Exception("Cierre incompleto: cierres "+closes+", vistas "+previews+", cuadros "+frames+", distancia "+last);results.Add("PASS Cierre normal único, representación propia y recorrido continuo hacia el Dock; "+frames+" cuadros.");
                    source=Document();source.Show();await Task.Delay(250);entry=Entry(source);previews=frames=0;if(!WindowCloseTransition.Request(entry,null,true))throw new Exception("Solicitud de minimizar rechazada");await Task.Delay(1100);if(!Native.Matches(entry)||!Native.IsIconic(entry.Handle)||previews!=1||frames<3||last>20)throw new Exception("Minimización incompleta: vistas="+previews+", cuadros="+frames+", distancia="+last);results.Add("PASS Minimización real sin cerrar la app, con representación propia hacia el Dock; "+frames+" cuadros.");Native.ShowWindowAsync(entry.Handle,4);await Task.Delay(200);source.Close();
                    source=Document();source.Show();await Task.Delay(200);entry=Entry(source);int style=Native.GetWindowLong(entry.Handle,-16),extended=Native.GetWindowLong(entry.Handle,-20);Native.RECT before;Native.GetWindowRect(entry.Handle,out before);source.Closing+=delegate(object s,System.ComponentModel.CancelEventArgs e){e.Cancel=true;};previews=0;
                    WindowCloseTransition.Request(entry,null);await Task.Delay(800);Native.RECT after;Native.GetWindowRect(entry.Handle,out after);if(!Native.Matches(entry)||!source.IsVisible||previews!=0||style!=Native.GetWindowLong(entry.Handle,-16)||extended!=Native.GetWindowLong(entry.Handle,-20)||before.Left!=after.Left||before.Top!=after.Top||before.Right!=after.Right||before.Bottom!=after.Bottom)throw new Exception("Un cierre cancelado alteró la ventana");results.Add("PASS Un cierre cancelado conserva ventana, geometría y estilos; no reproduce una despedida falsa.");
                    results.Add("Sólo ventanas de prueba del proceso actual; no se cierran programas del usuario.");File.WriteAllLines(Path.Combine(directory,"close-transitions.txt"),results);app.Shutdown();
                }
                catch(Exception ex){results.Add("FAIL "+ex);File.WriteAllLines(Path.Combine(directory,"close-transitions.txt"),results);app.Shutdown(1);}
            };
            app.Exit+=delegate{WindowCloseTransition.PreviewStarted-=started;PanelMotion.Frame-=frame;if(source!=null&&source.IsVisible){source.Hide();}if(background!=null)background.Close();if(desktop!=null)desktop.Dispose();};return app.Run();
        }
        private static Window Document()
        {var grid=new Grid{Background=Brushes.White};grid.RowDefinitions.Add(new RowDefinition());grid.RowDefinitions.Add(new RowDefinition());grid.Children.Add(new Border{Background=Design.Brush("#527FFF"),Child=new TextBlock{Text="Documento de prueba",FontSize=28,Foreground=Brushes.White,Margin=new Thickness(22)}});var lower=new Border{Background=Design.Brush("#FFBD60"),Child=new TextBlock{Text="Cerrar desde MacOS",FontSize=24,Margin=new Thickness(22)}};Grid.SetRow(lower,1);grid.Children.Add(lower);var window=new Window{Title="Documento · prueba",Width=480,Height=340,Left=330,Top=160,Content=grid,ShowActivated=false};window.SourceInitialized+=delegate{Native.NoActivate(window);};return window;}
        private static WindowEntry Entry(Window window){return new WindowEntry{Handle=new WindowInteropHelper(window).Handle,ProcessId=(uint)System.Diagnostics.Process.GetCurrentProcess().Id,Title=window.Title,Executable=System.Reflection.Assembly.GetExecutingAssembly().Location};}
    }
}

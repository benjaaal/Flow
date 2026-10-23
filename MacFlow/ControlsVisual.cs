using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;
namespace MacFlow
{
    internal static class ControlsVisual
    {
        internal static int Run(string directory)
        {
            directory=Path.GetFullPath(directory);Directory.CreateDirectory(directory);Log.DirectoryPath=directory;var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};Desktop desktop=null;Window fixture=null;VisualTestBackground backdrop=null;
            app.Startup+=delegate
            {
                backdrop=new VisualTestBackground{WindowState=WindowState.Normal,Width=920,Height=610,Left=(SystemParameters.PrimaryScreenWidth-920)/2,Top=SystemParameters.PrimaryScreenHeight*.12,ShowActivated=false,ShowInTaskbar=false,IsHitTestVisible=false};backdrop.SourceInitialized+=delegate{Native.NoActivate(backdrop);};backdrop.Show();
                fixture=new Window{Title="Aplicación de prueba",Width=320,Height=180,Left=-2000,Top=-2000,ShowActivated=false,ShowInTaskbar=false,Content=new TextBlock{Text="Ventana de prueba de MacOS",FontSize=20}};fixture.SourceInitialized+=delegate{Native.NoActivate(fixture);};fixture.Show();
                desktop=new Desktop(Path.Combine(directory,"profile"),true,true);desktop.Windows.Clear();desktop.Windows.Add(new WindowEntry{Handle=new WindowInteropHelper(fixture).Handle,ProcessId=(uint)Process.GetCurrentProcess().Id,Title="Aplicación de prueba",Executable=Process.GetCurrentProcess().MainModule.FileName});desktop.OpenOverview();
                var timer=new DispatcherTimer{Interval=TimeSpan.FromSeconds(3)};timer.Tick+=async delegate
                {
                    timer.Stop();try{await Program.CaptureOwnWindow(desktop.Overview,Path.Combine(directory,"window-controls.png"));File.WriteAllText(Path.Combine(directory,"visual-test.txt"),"PASS: panel WPF real con los tres controles por ventana; miniatura sólo de la ventana propia de prueba. Sin cambios de aplicaciones del usuario, barra de Windows o conexiones.");app.Shutdown();}catch(Exception ex){File.WriteAllText(Path.Combine(directory,"visual-test.txt"),"FAIL: "+ex);app.Shutdown(1);}
                };timer.Start();
            };app.Exit+=delegate{if(desktop!=null)desktop.Dispose();if(fixture!=null)fixture.Close();if(backdrop!=null)backdrop.Close();};return app.Run();
        }
    }
}

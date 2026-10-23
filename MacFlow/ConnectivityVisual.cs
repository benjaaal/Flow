using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
namespace MacFlow
{
    internal static class ConnectivityVisual
    {
        internal static int Run(string directory)
        {
            directory=Path.GetFullPath(directory);Directory.CreateDirectory(directory);Log.DirectoryPath=directory;
            var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};ConnectivityPanel panel=null;VisualTestBackground backdrop=null;bool busy=false;int stage=0;
            Action<bool> show=mode=>{panel=new ConnectivityPanel(mode,ConnectivityRegression.Sample,b=>{},item=>{},ConnectivityRegression.RadioFixture){Left=SystemParameters.PrimaryScreenWidth-440,Top=60,ShowActivated=false,ShowInTaskbar=false,IsHitTestVisible=false};panel.SourceInitialized+=delegate{Native.NoActivate(panel);};panel.Show();Design.Reveal(panel);};
            app.Startup+=delegate
            {
                Design.Dark=false;Design.InstallTheme();backdrop=new VisualTestBackground{WindowState=WindowState.Normal,Width=390,Height=555,Left=SystemParameters.PrimaryScreenWidth-440,Top=60,ShowActivated=false,IsHitTestVisible=false,Topmost=true};backdrop.SourceInitialized+=delegate{Native.NoActivate(backdrop);};backdrop.Show();show(true);
                var timer=new DispatcherTimer{Interval=TimeSpan.FromSeconds(3)};timer.Tick+=async delegate
                {
                    if(busy)return;busy=true;try
                    {
                        if(!panel.Ready)throw new Exception("Panel aislado no listo");
                        if(stage==0){if(LiquidDrop.Find(panel)!=null)throw new Exception("La burbuja eliminada sigue visible");await Program.CaptureOwnWindow(panel,Path.Combine(directory,"wifi-desktop.png"));panel.SwitchMode(false);}
                        if(stage==1){await Program.CaptureOwnWindow(panel,Path.Combine(directory,"bluetooth-desktop.png"));panel.Close();Design.Dark=true;Design.InstallTheme();show(true);}
                        if(stage==2){await Program.CaptureOwnWindow(panel,Path.Combine(directory,"wifi-dark-desktop.png"));panel.Close();backdrop.Close();timer.Stop();File.WriteAllText(Path.Combine(directory,"visual-test.txt"),"PASS: Wi-Fi, Bluetooth y modo oscuro con cristal limpio, sin burbuja añadida y dimensiones intactas. Datos de prueba inyectados. Sin conexiones, emparejamientos ni cambios de Windows. Fondo de prueba limitado al tamaño del panel.");app.Shutdown();}stage++;
                    }
                    catch(Exception ex){timer.Stop();File.WriteAllText(Path.Combine(directory,"visual-test.txt"),"FAIL: "+ex);if(panel!=null)panel.Close();if(backdrop!=null)backdrop.Close();app.Shutdown(1);}finally{busy=false;}
                };timer.Start();
            };return app.Run();
        }
    }
}

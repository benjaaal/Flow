using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
namespace MacFlow
{
    internal static class TopBarVisual
    {
        internal static int Run(string directory)
        {
            directory=Path.GetFullPath(directory);Directory.CreateDirectory(directory);Log.DirectoryPath=directory;var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};Desktop desktop=null;VisualTestBackground backdrop=null;bool busy=false;int stage=0;
            app.Startup+=delegate
            {
                backdrop=new VisualTestBackground{WindowState=WindowState.Normal,Width=SystemParameters.PrimaryScreenWidth,Height=80,Left=0,Top=0,ShowActivated=false,ShowInTaskbar=false,IsHitTestVisible=false};backdrop.SourceInitialized+=delegate{Native.NoActivate(backdrop);};backdrop.Show();desktop=new Desktop(Path.Combine(directory,"profile"),true,true);
                var timer=new DispatcherTimer{Interval=TimeSpan.FromSeconds(3)};timer.Tick+=async delegate
                {
                    if(busy)return;busy=true;try
                    {
                        if(stage==0){await Program.CaptureOwnWindow(desktop.TopBar,Path.Combine(directory,"topbar-light.png"));if(desktop.TopBar.Height!=Desktop.TopBarHeight||desktop.TopBar.Width<SystemParameters.PrimaryScreenWidth-20)throw new Exception("La barra no ocupa el ancho previsto");desktop.Preferences.Theme="Dark";desktop.ApplyPreferences(desktop.Preferences,null);}
                        else{await Program.CaptureOwnWindow(desktop.TopBar,Path.Combine(directory,"topbar-dark.png"));timer.Stop();File.WriteAllText(Path.Combine(directory,"visual-test.txt"),"PASS: barra superior continua de 40 puntos, fecha y hora a la derecha, modos claro/oscuro. Sin isla ni cambios de barra de Windows, conexiones o inicio automático.");app.Shutdown();}stage++;
                    }
                    catch(Exception ex){timer.Stop();File.WriteAllText(Path.Combine(directory,"visual-test.txt"),"FAIL: "+ex);app.Shutdown(1);}finally{busy=false;}
                };timer.Start();
            };app.Exit+=delegate{if(desktop!=null)desktop.Dispose();if(backdrop!=null)backdrop.Close();};return app.Run();
        }
    }
}

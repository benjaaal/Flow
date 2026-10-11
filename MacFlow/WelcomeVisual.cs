using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
namespace MacFlow
{
    internal static class WelcomeVisual
    {
        internal static int Run(string directory)
        {
            directory=Path.GetFullPath(directory);Directory.CreateDirectory(directory);Log.DirectoryPath=directory;
            var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};WelcomeWindow panel=null;UpdateWindow updater=null;
            app.Startup+=async delegate
            {
                try
                {
                    Design.Dark=true;Design.Animations=true;Design.InstallTheme();var rect=new System.Drawing.Rectangle(120,90,600,420);
                    panel=new WelcomeWindow(rect){ShowActivated=false};panel.SourceInitialized+=delegate{Native.NoActivate(panel);};panel.Show();
                    await System.Threading.Tasks.Task.Delay(80);Native.RECT during;Native.GetWindowRect(new WindowInteropHelper(panel).Handle,out during);if(during.Left!=rect.Left||during.Top!=rect.Top||during.Right-during.Left!=rect.Width||during.Bottom-during.Top!=rect.Height)throw new Exception("Hola no cubre exactamente los límites físicos");
                    await System.Threading.Tasks.Task.Delay(650);if(panel.Lettering.Progress<=0||panel.Lettering.Progress>=1)throw new Exception("No se está dibujando el saludo");await Program.CaptureOwnWindow(panel,Path.Combine(directory,"hola-writing.png"));
                    await System.Threading.Tasks.Task.Delay(1500);if(!panel.ContentFits)throw new Exception("Hola recorta texto o botón con el escalado de pantalla");if(panel.Lettering.Progress!=1||panel.HasAnimatedProperties)throw new Exception("El saludo no llega al reposo");await Program.CaptureOwnWindow(panel,Path.Combine(directory,"hola-fullscreen.png"));
                    int closed=0;panel.Closed+=delegate{closed++;};panel.StartButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));panel.DismissWelcome();
                    await System.Threading.Tasks.Task.Delay(110);Native.GetWindowRect(new WindowInteropHelper(panel).Handle,out during);if(!panel.IsVisible||during.Top!=rect.Top||during.Bottom-during.Top!=rect.Height||panel.ContentOpacity<=0||panel.ContentOpacity>=1)throw new Exception("Iniciar debe desvanecer el contenido manteniendo la pantalla completa");
                    await System.Threading.Tasks.Task.Delay(300);if(panel.IsVisible||closed!=1)throw new Exception("Iniciar no cierra exactamente una vez");
                    updater=new UpdateWindow(""){Left=rect.Left,Top=rect.Top,ShowActivated=false,IsHitTestVisible=false};updater.SourceInitialized+=delegate{Native.NoActivate(updater);};updater.Show();Design.Reveal(updater);await System.Threading.Tasks.Task.Delay(450);await Program.CaptureOwnWindow(updater,Path.Combine(directory,"updates-glass.png"));updater.Close();
                    File.WriteAllText(Path.Combine(directory,"visual-test.txt"),"PASS Hola cubre límites físicos completos sin usar el área de trabajo ni dejar hueco para la barra. PASS saludo vectorial progresivo y reposo. PASS Iniciar desvanece contenido, mantiene geometría y cierra una vez. Fixture aislada de 600x420; no cubre ni verifica monitores externos del usuario. Sin consultas de red, cambios de Windows ni instalación.");app.Shutdown();
                }
                catch(Exception ex){File.WriteAllText(Path.Combine(directory,"visual-test.txt"),"FAIL "+ex);if(panel!=null&&panel.IsVisible)panel.Close();if(updater!=null&&updater.IsVisible)updater.Close();app.Shutdown(1);}
            };return app.Run();
        }
    }
}
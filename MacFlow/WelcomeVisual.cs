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
    internal static class WelcomeVisual
    {
        private static void Check(bool condition,string message,List<string> report){if(!condition)throw new Exception(message);report.Add("PASS "+message);}
        internal static int Run(string directory)
        {
            directory=Path.GetFullPath(directory);Directory.CreateDirectory(directory);Log.DirectoryPath=directory;
            var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};WelcomeWindow panel=null;VisualTestBackground backdrop=null;var report=new List<string>();
            app.Startup+=async delegate
            {
                try
                {
                    Design.Dark=true;Design.Animations=true;LiquidMaterial.LiveBackdrop=true;Design.InstallTheme();var rect=new System.Drawing.Rectangle(120,90,900,620);
                    backdrop=new VisualTestBackground{WindowState=WindowState.Normal,Width=rect.Width,Height=rect.Height,Left=rect.Left,Top=rect.Top,ShowActivated=false,Topmost=true};backdrop.Show();await Task.Delay(120);
                    panel=new WelcomeWindow(rect){ShowActivated=false};panel.SourceInitialized+=delegate{Native.NoActivate(panel);};panel.Show();
                    await Task.Delay(100);Native.RECT during;Native.GetWindowRect(new WindowInteropHelper(panel).Handle,out during);
                    Check(during.Left==rect.Left&&during.Top==rect.Top&&during.Right-during.Left==rect.Width&&during.Bottom-during.Top==rect.Height,"Hola cubre límites físicos completos sin huecos.",report);
                    
                    await Program.CaptureOwnWindow(panel,Path.Combine(directory,"01-manzanita-glass.png"));
                    Check(panel.LogoVisible&&panel.Lettering.Progress==0&&!panel.StartButton.IsEnabled,"La manzanita aparece antes de dibujar Hola.",report);await Task.Delay(900);Check(panel.Lettering.Progress>0&&panel.Lettering.Progress<1,"Hola se dibuja después de la transición de la manzanita.",report);
                    await Program.CaptureOwnWindow(panel,Path.Combine(directory,"02-hola-writing.png"));
                    await Task.Delay(1050);Check(panel.ContentFits&&panel.Ready,"Contenido contenido y animación inicial en reposo; Iniciar disponible.",report);
                    Check((string)panel.Tag=="Welcome live compositor · recordable","El fondo proviene del compositor vivo; no se captura ni excluye la ventana de grabaciones.",report);
                    await Program.CaptureOwnWindow(panel,Path.Combine(directory,"03-hola-glass.png"));
                    await Task.Delay(1700);Check(panel.GreetingIndex==1&&panel.StartButton.IsEnabled,"Cambio automático a Hello conserva disponible Iniciar.",report);
                    await Program.CaptureOwnWindow(panel,Path.Combine(directory,"04-hello.png"));
                    panel.ChangeGreeting(10);await Task.Delay(1000);Check(panel.GreetingIndex==10&&panel.GreetingDirection==FlowDirection.RightToLeft,"Árabe respeta dirección de escritura y transición.",report);
                    await Program.CaptureOwnWindow(panel,Path.Combine(directory,"05-arabic.png"));
                    panel.ChangeGreeting(7);await Task.Delay(1000);Check(panel.GreetingIndex==7&&panel.GreetingDirection==FlowDirection.LeftToRight,"Japonés cambia sin alterar dimensiones de la escena.",report);
                    await Program.CaptureOwnWindow(panel,Path.Combine(directory,"06-japanese.png"));
                    int closed=0;panel.Closed+=delegate{closed++;};panel.StartButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));panel.DismissWelcome();
                    await Task.Delay(110);Native.GetWindowRect(new WindowInteropHelper(panel).Handle,out during);Check(panel.IsVisible&&during.Top==rect.Top&&during.Bottom-during.Top==rect.Height&&panel.ContentOpacity>0&&panel.ContentOpacity<1,"Iniciar desvanece manteniendo geometría completa.",report);
                    await Task.Delay(420);Check(!panel.IsVisible&&closed==1&&!panel.LanguageTimerRunning,"Cierra una vez y detiene el temporizador de idiomas.",report);
                    panel=new WelcomeWindow(rect){ShowActivated=false};panel.Show();await Task.Delay(250);panel.DismissWelcome();await Task.Delay(500);Check(!panel.IsVisible&&!panel.LanguageTimerRunning,"Se puede salir durante la entrada sin callbacks pendientes.",report);
                    backdrop.Content=new Grid{Background=Brushes.White};await Task.Delay(200);Design.Animations=false;Design.Dark=false;panel=new WelcomeWindow(new System.Drawing.Rectangle(120,90,420,320)){ShowActivated=false};panel.Show();await Task.Delay(500);
                    Check(panel.Ready&&panel.ContentFits&&!panel.LanguageTimerRunning,"Sin animaciones: Hola inmediato, sin ciclo de idiomas ni contenido recortado en pantalla pequeña.",report);
                    Check(!panel.Lettering.LightInk,"La tinta se adapta al fondo claro sin una capa opaca detrás del texto.",report);
                    await Program.CaptureOwnWindow(panel,Path.Combine(directory,"07-reduced-motion.png"));panel.DismissWelcome();Check(!panel.IsVisible,"Cierre inmediato con movimiento desactivado.",report);
                    Check(WelcomeWindow.Greetings.Length==30,"30 saludos locales sin red ni servicios de traducción.",report);
                    report.Add("Fixture aislada de 900x620 y 420x320; no modifica el escritorio, las preferencias ni la instalación del usuario.");File.WriteAllLines(Path.Combine(directory,"visual-test.txt"),report);backdrop.Close();app.Shutdown();
                }
                catch(Exception ex){report.Add("FAIL "+ex);File.WriteAllLines(Path.Combine(directory,"visual-test.txt"),report);if(panel!=null&&panel.IsVisible)panel.Close();if(backdrop!=null)backdrop.Close();app.Shutdown(1);}
            };return app.Run();
        }
    }
}

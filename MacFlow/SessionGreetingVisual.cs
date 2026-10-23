using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace MacFlow
{
    internal static class SessionGreetingVisual
    {
        internal static int Run(string folder)
        {
            folder=Path.GetFullPath(folder);Directory.CreateDirectory(folder);Log.DirectoryPath=folder;var report=new List<string>();var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};SessionGreeting panel=null;
            app.Startup+=async delegate
            {
                try
                {
                    Design.Animations=true;Motion.TestClientAreaAnimations=true;Design.InstallTheme();var visual=new DrawingVisual();using(var dc=visual.RenderOpen())dc.DrawRectangle(new LinearGradientBrush(Color.FromRgb(73,80,109),Color.FromRgb(115,78,83),45),null,new Rect(0,0,800,500));var photo=new RenderTargetBitmap(800,500,96,96,PixelFormats.Pbgra32);photo.Render(visual);photo.Freeze();int completed=0;
                    panel=new SessionGreeting(photo,new System.Drawing.Rectangle(100,70,800,500),delegate{completed++;}){IsHitTestVisible=false};panel.SourceInitialized+=delegate{Native.NoActivate(panel);};panel.Show();await Task.Delay(3000);await LiquidMaterial.CaptureOwnWindow(panel,Path.Combine(folder,"session.png"));
                    Native.RECT bounds;Native.GetWindowRect(new System.Windows.Interop.WindowInteropHelper(panel).Handle,out bounds);if(bounds.Left!=100||bounds.Top!=70||bounds.Right-bounds.Left!=800||bounds.Bottom-bounds.Top!=500)throw new Exception("La bienvenida no ocupa el monitor solicitado");report.Add("PASS bienvenida ocupa el rectángulo físico solicitado y conserva el fondo.");panel.Finish();panel.Finish();await Task.Delay(500);if(panel.IsVisible||completed!=1)throw new Exception("Continuar no finaliza una sola vez");report.Add("PASS Continuar cierra y devuelve el control exactamente una vez.");
                    panel=new SessionGreeting(photo,new System.Drawing.Rectangle(100,70,800,500),delegate{completed++;}){IsHitTestVisible=false};panel.SourceInitialized+=delegate{Native.NoActivate(panel);};panel.Show();await Task.Delay(4300);if(panel.IsVisible||completed!=2)throw new Exception("El saludo retuvo el escritorio");report.Add("PASS bienvenida breve se retira automáticamente; no retiene el escritorio.");
                    Motion.TestClientAreaAnimations=false;panel=new SessionGreeting(photo,new System.Drawing.Rectangle(100,70,800,500),delegate{completed++;}){IsHitTestVisible=false};panel.SourceInitialized+=delegate{Native.NoActivate(panel);};panel.Show();panel.Finish();if(panel.IsVisible||completed!=3)throw new Exception("Movimiento reducido no cierra inmediatamente");report.Add("PASS movimiento reducido finaliza sin reloj de salida.");File.WriteAllLines(Path.Combine(folder,"session-tests.txt"),report);app.Shutdown();
                }
                catch(Exception ex){report.Add("FAIL "+ex);File.WriteAllLines(Path.Combine(folder,"session-tests.txt"),report);if(panel!=null)panel.Close();app.Shutdown(1);}
            };return app.Run();
        }
    }
}

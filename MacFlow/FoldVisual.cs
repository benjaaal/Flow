using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
namespace MacFlow
{
    internal static class FoldVisual
    {
        internal static int Run(string directory)
        {
            directory=Path.GetFullPath(directory);Directory.CreateDirectory(directory);var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};Window window=null;int stage=0;bool busy=false;
            app.Startup+=delegate
            {
                // Own generated wallpaper and UI only, never captures the user's
                // desktop and never runs a full-screen test overlay.
                var source=WallpaperLibrary.Load(directory)[0].Thumbnail;
                var picture=new Image{Source=source,Stretch=Stretch.Fill};var fold=new LidFold();fold.SetValue(LidFold.PixelProperty,new Point(1.0/source.PixelWidth,1.0/source.PixelHeight));picture.Effect=fold;
                window=new Window{Title="MacFlow · Prueba de pliegue",WindowStyle=WindowStyle.None,Width=480,Height=270,Left=80,Top=90,ShowActivated=false,ShowInTaskbar=false,Topmost=true,Content=picture};window.SourceInitialized+=delegate{Native.NoActivate(window);};window.Show();
                var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(900)};
                timer.Tick+=async delegate
                {
                    if(busy)return;busy=true;
                    try
                    {
                        await Program.CaptureOwnWindow(window,Path.Combine(directory,"fold-"+stage+".png"));
                        if(stage==0)fold.Progress=.50;else if(stage==1)fold.Progress=.88;else {timer.Stop();window.Close();File.WriteAllText(Path.Combine(directory,"visual-test.txt"),"PASS: shader GPU de pliegue con tres estados; sólo ventana de prueba de 480x270. Sin capturar escritorio real, eventos físicos de tapa ni cambios de suspensión.");app.Shutdown();}stage++;
                    }
                    catch(Exception ex){timer.Stop();window.Close();File.WriteAllText(Path.Combine(directory,"visual-test.txt"),"FAIL: "+ex);app.Shutdown(1);}finally{busy=false;}
                };timer.Start();
            };return app.Run();
        }
    }
}

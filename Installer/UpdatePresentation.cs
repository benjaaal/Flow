using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MacFlow;
using Forms=System.Windows.Forms;
namespace MacFlowSetup
{
    internal sealed class UpdateTapCounter
    {
        private int count;private DateTime previous;
        internal bool Tap(DateTime now){if(now<previous||now-previous>TimeSpan.FromSeconds(4))count=0;previous=now;if(++count<3)return false;count=0;return true;}
    }
    internal sealed class UpdatePresentation : IDisposable
    {
        internal const int PreparationSeconds=120;
        private readonly List<Window> screens=new List<Window>();
        private readonly Dictionary<Window,System.Drawing.Rectangle> boundsByWindow=new Dictionary<Window,System.Drawing.Rectangle>();
        private HwndSource escapeSource;private IntPtr escapeHandle;
        private readonly CancellationTokenSource stop;
        private readonly UpdateTapCounter taps=new UpdateTapCounter();
        private readonly ProgressBar progress;
        private readonly DispatcherTimer timer;
        private readonly DateTime deadline;
        private WindowsMenuKey keyboard;
        private bool disposed,canCancel=true;
        internal UpdatePresentation(CancellationTokenSource cancellation,bool visualTest=false)
        {
            stop=cancellation;deadline=DateTime.UtcNow.AddSeconds(PreparationSeconds);
            var stack=new StackPanel {Width=310,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center};
            var logo=Design.AppleMark(74);((System.Windows.Shapes.Path)logo).Fill=Brushes.White;stack.Children.Add(new Border {Child=logo,HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(0,0,0,36)});
            progress=new ProgressBar {Minimum=0,Maximum=100,Height=5,Background=Design.Brush("#303030"),Foreground=Brushes.White,BorderThickness=new Thickness(0)};stack.Children.Add(progress);
            var monitors=Forms.Screen.AllScreens;
            foreach(var monitor in monitors)
            {
                if(visualTest&&!monitor.Primary)continue;
                var window=new Window {Title="MacFlow · Actualizando",Background=Brushes.Black,WindowStyle=WindowStyle.None,ResizeMode=ResizeMode.NoResize,AllowsTransparency=false,ShowInTaskbar=false,Topmost=true,ShowActivated=monitor.Primary,Width=visualTest?600:monitor.Bounds.Width,Height=visualTest?420:monitor.Bounds.Height};
                if(monitor.Primary)window.Content=stack;
                if(!visualTest)
                {
                    boundsByWindow.Add(window,monitor.Bounds);window.Loaded+=delegate {Cover();};
                    window.SourceInitialized+=delegate
                    {
                        var handle=new WindowInteropHelper(window).Handle;int disabled=1;Native.DwmSetWindowAttribute(handle,33,ref disabled,4);Native.DwmSetWindowAttribute(handle,2,ref disabled,4);int noBorder=unchecked((int)0xFFFFFFFE);Native.DwmSetWindowAttribute(handle,34,ref noBorder,4);
                        if(monitor.Primary){escapeHandle=handle;escapeSource=HwndSource.FromHwnd(handle);escapeSource.AddHook(Messages);Native.RegisterHotKey(handle,9417,0x4000,0x1B);}
                    };
                    window.Deactivated+=delegate {if(!disposed)Cover();};
                }
                else window.WindowStartupLocation=WindowStartupLocation.CenterScreen;
                window.KeyDown+=delegate(object sender,KeyEventArgs args){if(args.Key==Key.Escape){args.Handled=true;Cancel();}};
                window.Closing+=delegate(object sender,System.ComponentModel.CancelEventArgs args){if(!disposed){args.Cancel=true;Cancel();}};
                screens.Add(window);window.Show();
            }
            if(!visualTest)keyboard=new WindowsMenuKey(Application.Current.Dispatcher,delegate{if(taps.Tap(DateTime.UtcNow))Cancel();});
            timer=new DispatcherTimer {Interval=TimeSpan.FromMilliseconds(200)};timer.Tick+=delegate
            {
                if(!canCancel||stop.IsCancellationRequested)return;double seconds=Math.Max(0,(deadline-DateTime.UtcNow).TotalSeconds);
                SetProgress(10+(PreparationSeconds-seconds)/PreparationSeconds*75);
            };timer.Start();
        }
        private void Cancel(){if(disposed||!canCancel)return;stop.Cancel();}
        private IntPtr Messages(IntPtr handle,int message,IntPtr value,IntPtr data,ref bool handled){if(message==0x312&&value.ToInt32()==9417){handled=true;Cancel();}return IntPtr.Zero;}
        internal void Cover(){if(disposed)return;foreach(var entry in boundsByWindow){var rect=entry.Value;var handle=new WindowInteropHelper(entry.Key).Handle;if(handle!=IntPtr.Zero)Native.SetWindowPos(handle,new IntPtr(-1),rect.Left,rect.Top,rect.Width,rect.Height,0x50);}}
        private void SetProgress(double value){progress.BeginAnimation(System.Windows.Controls.Primitives.RangeBase.ValueProperty,new DoubleAnimation(progress.Value,value,TimeSpan.FromMilliseconds(220)) {EasingFunction=new CubicEase {EasingMode=EasingMode.EaseInOut}});}
        internal void WaitBeforeCommit()
        {WaitPreparation(deadline,stop.Token);}
        internal static void WaitPreparation(DateTime deadline,CancellationToken token)
        {
            while(DateTime.UtcNow<deadline){token.ThrowIfCancellationRequested();if(token.WaitHandle.WaitOne(100))token.ThrowIfCancellationRequested();}
            token.ThrowIfCancellationRequested();
        }
        internal void Commit()
        {Application.Current.Dispatcher.Invoke(new Action(delegate{stop.Token.ThrowIfCancellationRequested();canCancel=false;timer.Stop();SetProgress(95);}));}
        internal void Complete(){timer.Stop();SetProgress(100);}
        public void Dispose(){if(disposed)return;disposed=true;timer.Stop();if(keyboard!=null)keyboard.Dispose();if(escapeHandle!=IntPtr.Zero)Native.UnregisterHotKey(escapeHandle,9417);if(escapeSource!=null)escapeSource.RemoveHook(Messages);foreach(var window in screens)window.Close();screens.Clear();boundsByWindow.Clear();}
        internal static int Visual(string directory)
        {
            System.IO.Directory.CreateDirectory(directory);var app=new Application();UpdatePresentation presentation=null;var cancellation=new CancellationTokenSource();
            app.Startup+=delegate{presentation=new UpdatePresentation(cancellation,true);var timer=new DispatcherTimer {Interval=TimeSpan.FromSeconds(2)};timer.Tick+=async delegate{timer.Stop();try{await ProgramCapture(presentation.screens[0],System.IO.Path.Combine(directory,"actualizacion.png"));System.IO.File.WriteAllText(System.IO.Path.Combine(directory,"visual-test.txt"),"PASS Pantalla WPF negra real, manzana vectorial y barra. Fixture de 600x420; no cubre todos los monitores, no instala y no intercepta teclas.");presentation.Dispose();app.Shutdown();}catch(Exception ex){System.IO.File.WriteAllText(System.IO.Path.Combine(directory,"visual-test.txt"),"FAIL "+ex);presentation.Dispose();app.Shutdown(1);}};timer.Start();};return app.Run();
        }
        private static async System.Threading.Tasks.Task ProgramCapture(Window window,string path)
        {await System.Threading.Tasks.Task.Delay(100);var visual=new RenderTargetBitmap((int)window.ActualWidth,(int)window.ActualHeight,96,96,PixelFormats.Pbgra32);visual.Render(window);var encoder=new System.Windows.Media.Imaging.PngBitmapEncoder();encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(visual));using(var file=System.IO.File.Create(path))encoder.Save(file);}
    }
}

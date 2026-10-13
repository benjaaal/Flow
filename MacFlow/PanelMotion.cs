using System;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Windows.Threading;
namespace MacFlow
{
    // Analytical damped spring. Short, quiet settling instead of elastic bounce.
    // The end value is exact so removing its clock never changes the geometry.
    internal sealed class SpringEase : EasingFunctionBase
    {
        internal static double Progress(double time)
        {
            if(time<=0)return 0;if(time>=1)return 1;
            const double damping=.86,frequency=11;
            double damped=frequency*Math.Sqrt(1-damping*damping);
            Func<double,double> response=t=>1-Math.Exp(-damping*frequency*t)*(Math.Cos(damped*t)+damping/Math.Sqrt(1-damping*damping)*Math.Sin(damped*t));
            return response(time)/response(1);
        }
        protected override double EaseInCore(double time){return Progress(time);}
        protected override Freezable CreateInstanceCore(){return new SpringEase();}
    }
    // Move the HWND including its compositor material; never scale the UI over
    // an unmoving rectangle of glass. Base coordinates survive interruptions.
    internal static class PanelMotion
    {
        private static readonly DependencyProperty BaseTop=DependencyProperty.RegisterAttached("BaseTop",typeof(double),typeof(PanelMotion),new PropertyMetadata(Double.NaN));
        private static readonly DependencyProperty BaseLeft=DependencyProperty.RegisterAttached("BaseLeft",typeof(double),typeof(PanelMotion),new PropertyMetadata(Double.NaN));
        private static readonly DependencyProperty Closing=DependencyProperty.RegisterAttached("Closing",typeof(bool),typeof(PanelMotion),new PropertyMetadata(false));
        private static readonly DependencyProperty OriginalTransform=DependencyProperty.RegisterAttached("OriginalTransform",typeof(Transform),typeof(PanelMotion),new PropertyMetadata(null));
        private static readonly DependencyProperty OriginalOrigin=DependencyProperty.RegisterAttached("OriginalOrigin",typeof(Point),typeof(PanelMotion),new PropertyMetadata(new Point()));
        private static readonly DependencyProperty ClipPending=DependencyProperty.RegisterAttached("ClipPending",typeof(bool),typeof(PanelMotion),new PropertyMetadata(false));
        private static readonly DependencyProperty ExitProgress=DependencyProperty.RegisterAttached("ExitProgress",typeof(double),typeof(PanelMotion),new PropertyMetadata(0.0,ExitChanged));
        private static readonly DependencyProperty ExitStart=DependencyProperty.RegisterAttached("ExitStart",typeof(Native.RECT),typeof(PanelMotion),new PropertyMetadata(new Native.RECT()));
        private static readonly DependencyProperty ExitTarget=DependencyProperty.RegisterAttached("ExitTarget",typeof(Point),typeof(PanelMotion),new PropertyMetadata(new Point()));
        private static readonly DependencyProperty HasTarget=DependencyProperty.RegisterAttached("HasTarget",typeof(bool),typeof(PanelMotion),new PropertyMetadata(false));
        private static readonly DependencyProperty ExitState=DependencyProperty.RegisterAttached("ExitState",typeof(WindowState),typeof(PanelMotion),new PropertyMetadata(WindowState.Normal));
        internal static event Action<Window> Frame;
        internal static Func<Window,Point?> DockDestination;
        internal static bool IsClosing(Window window){return (bool)window.GetValue(Closing);}
        internal static bool FromTop(Window window)
        {return window.Title=="MacFlow · Centro de control"||window.Title=="MacFlow · Conexiones"||window.Title=="MacFlow · Widgets"||window.Title=="MacFlow · Stage Manager · Grupos"||window.Title=="MacFlow · Menú";}
        private static void ExitChanged(DependencyObject target,DependencyPropertyChangedEventArgs args)
        {
            var window=(Window)target;if(!IsClosing(window))return;double remaining=Math.Max(.001,1-(double)args.NewValue);
            bool menu=false;var content=window.Content as FrameworkElement;
            if(content!=null&&!menu){var transform=content.RenderTransform as ScaleTransform;if(transform!=null)transform.ScaleX=transform.ScaleY=remaining;}
            // Apply the native crop after WPF has submitted the matching scaled
            // content. Cropping first reveals an unscaled, cut-off text frame.
            if((bool)window.GetValue(ClipPending))return;window.SetValue(ClipPending,true);
            window.Dispatcher.BeginInvoke(DispatcherPriority.Loaded,new Action(delegate
            {
                window.SetValue(ClipPending,false);if(!IsClosing(window)||!window.IsVisible)return;
                double progress=(double)window.GetValue(ExitProgress);Native.SetGlassClip(window,menu?1:Math.Max(.001,1-progress));
                if((bool)window.GetValue(HasTarget))
                {
                    var start=(Native.RECT)window.GetValue(ExitStart);var targetPoint=(Point)window.GetValue(ExitTarget);
                    double x=start.Left+(targetPoint.X-(start.Left+start.Right)/2.0)*progress,y=start.Top+(targetPoint.Y-(start.Top+start.Bottom)/2.0)*progress;
                    // Move only our HWND. One clock drives movement, material
                    // clipping and content scale, including maximized panels.
                    Native.SetWindowPos(new System.Windows.Interop.WindowInteropHelper(window).Handle,IntPtr.Zero,(int)Math.Round(x),(int)Math.Round(y),0,0,0x15);
                }
                var changed=Frame;if(changed!=null)changed(window);
            }));
        }
        internal static void Open(Window window)
        {
            if(window.WindowState!=WindowState.Normal||Double.IsNaN(window.Top))return;
            Reset(window);double top=window.Top;window.SetValue(BaseTop,top);window.SetValue(BaseLeft,window.Left);
            bool menu=window.Title=="MacFlow · Menú";var motion=new DoubleAnimation(top+(FromTop(window)?-18:8),top,TimeSpan.FromMilliseconds(menu?Motion.Menu:Motion.Panel)){EasingFunction=FromTop(window)?(IEasingFunction)new SpringEase{EasingMode=EasingMode.EaseIn}:Motion.Ease()};
            Timeline.SetDesiredFrameRate(motion,60);motion.Completed+=delegate{if(window.IsVisible&&Math.Abs(window.Top-top)<.1)Reset(window);};window.BeginAnimation(Window.TopProperty,motion);
        }
        internal static void Close(Window window)
        {
            if(window.WindowState==WindowState.Minimized)return;
            double start=window.Top;if(Double.IsNaN((double)window.GetValue(BaseTop)))window.SetValue(BaseTop,start);
            double left=window.Left;if(Double.IsNaN((double)window.GetValue(BaseLeft)))window.SetValue(BaseLeft,left);
            window.SetValue(ExitState,window.WindowState);
            // Freeze any unfinished entry before capturing the actual native
            // coordinates. Windows preserves normal/maximized placement.
            window.BeginAnimation(Window.TopProperty,null);window.BeginAnimation(Window.LeftProperty,null);
            if(window.WindowState==WindowState.Normal){window.Top=start;window.Left=left;}
            if(!IsClosing(window)){var content=window.Content as FrameworkElement;if(content!=null){window.SetValue(OriginalTransform,content.RenderTransform);window.SetValue(OriginalOrigin,content.RenderTransformOrigin);content.RenderTransformOrigin=new Point(.5,.5);content.RenderTransform=new ScaleTransform(1,1);}window.SetValue(Closing,true);}
            Native.RECT bounds;Native.GetWindowRect(new System.Windows.Interop.WindowInteropHelper(window).Handle,out bounds);window.SetValue(ExitStart,bounds);
            bool menu=false;var destination=menu?(Point?)new Point((bounds.Left+bounds.Right)/2.0,(bounds.Top+bounds.Bottom)/2.0-4):DockDestination==null?null:DockDestination(window);window.SetValue(HasTarget,destination.HasValue);if(destination.HasValue)window.SetValue(ExitTarget,destination.Value);
            var collapse=new DoubleAnimation((double)window.GetValue(ExitProgress),.999,TimeSpan.FromMilliseconds(menu?Motion.Menu:Motion.WindowClose)){EasingFunction=new CubicEase{EasingMode=EasingMode.EaseInOut}};Timeline.SetDesiredFrameRate(collapse,60);window.BeginAnimation(ExitProgress,collapse);
        }
        internal static void Reset(Window window)
        {
            window.SetValue(Closing,false);window.BeginAnimation(ExitProgress,null);window.SetValue(ExitProgress,0.0);var content=window.Content as FrameworkElement;var previous=window.GetValue(OriginalTransform) as Transform;if(content!=null&&previous!=null){content.RenderTransform=previous;content.RenderTransformOrigin=(Point)window.GetValue(OriginalOrigin);window.ClearValue(OriginalTransform);window.ClearValue(OriginalOrigin);}Native.SetGlassClip(window,1);
            bool maximized=(WindowState)window.GetValue(ExitState)==WindowState.Maximized;
            if(maximized&&(bool)window.GetValue(HasTarget)){var bounds=(Native.RECT)window.GetValue(ExitStart);Native.SetWindowPos(new System.Windows.Interop.WindowInteropHelper(window).Handle,IntPtr.Zero,bounds.Left,bounds.Top,0,0,0x15);}
            double top=(double)window.GetValue(BaseTop);window.BeginAnimation(Window.TopProperty,null);if(!maximized&&!Double.IsNaN(top))window.Top=top;window.SetValue(BaseTop,Double.NaN);
            double left=(double)window.GetValue(BaseLeft);window.BeginAnimation(Window.LeftProperty,null);if(!maximized&&!Double.IsNaN(left))window.Left=left;window.SetValue(BaseLeft,Double.NaN);window.SetValue(HasTarget,false);window.SetValue(ExitState,WindowState.Normal);
        }
    }
}

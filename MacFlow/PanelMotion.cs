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
        private static readonly DependencyProperty Closing=DependencyProperty.RegisterAttached("Closing",typeof(bool),typeof(PanelMotion),new PropertyMetadata(false));
        private static readonly DependencyProperty OriginalTransform=DependencyProperty.RegisterAttached("OriginalTransform",typeof(Transform),typeof(PanelMotion),new PropertyMetadata(null));
        private static readonly DependencyProperty OriginalOrigin=DependencyProperty.RegisterAttached("OriginalOrigin",typeof(Point),typeof(PanelMotion),new PropertyMetadata(new Point()));
        private static readonly DependencyProperty ClipPending=DependencyProperty.RegisterAttached("ClipPending",typeof(bool),typeof(PanelMotion),new PropertyMetadata(false));
        private static readonly DependencyProperty ExitProgress=DependencyProperty.RegisterAttached("ExitProgress",typeof(double),typeof(PanelMotion),new PropertyMetadata(0.0,ExitChanged));
        internal static event Action<Window> Frame;
        internal static bool IsClosing(Window window){return (bool)window.GetValue(Closing);}
        private static void ExitChanged(DependencyObject target,DependencyPropertyChangedEventArgs args)
        {
            var window=(Window)target;if(!IsClosing(window))return;double remaining=Math.Max(.001,1-(double)args.NewValue);
            var content=window.Content as FrameworkElement;
            if(content!=null){var transform=content.RenderTransform as ScaleTransform;if(transform!=null)transform.ScaleX=transform.ScaleY=remaining;}
            // Apply the native crop after WPF has submitted the matching scaled
            // content. Cropping first reveals an unscaled, cut-off text frame.
            if((bool)window.GetValue(ClipPending))return;window.SetValue(ClipPending,true);
            window.Dispatcher.BeginInvoke(DispatcherPriority.Loaded,new Action(delegate
            {window.SetValue(ClipPending,false);if(!IsClosing(window)||!window.IsVisible)return;Native.SetGlassClip(window,Math.Max(.001,1-(double)window.GetValue(ExitProgress)));var changed=Frame;if(changed!=null)changed(window);}));
        }
        internal static void Open(Window window)
        {
            if(window.WindowState!=WindowState.Normal||Double.IsNaN(window.Top))return;
            Reset(window);double top=window.Top;window.SetValue(BaseTop,top);
            var motion=new DoubleAnimation(top+14,top,TimeSpan.FromMilliseconds(320)){EasingFunction=new SpringEase{EasingMode=EasingMode.EaseIn}};
            Timeline.SetDesiredFrameRate(motion,60);motion.Completed+=delegate{if(window.IsVisible&&Math.Abs(window.Top-top)<.1)Reset(window);};window.BeginAnimation(Window.TopProperty,motion);
        }
        internal static void Close(Window window)
        {
            if(window.WindowState!=WindowState.Normal||Double.IsNaN(window.Top))return;
            double start=window.Top;if(Double.IsNaN((double)window.GetValue(BaseTop)))window.SetValue(BaseTop,start);
            if(!IsClosing(window)){var content=window.Content as FrameworkElement;if(content!=null){window.SetValue(OriginalTransform,content.RenderTransform);window.SetValue(OriginalOrigin,content.RenderTransformOrigin);content.RenderTransformOrigin=new Point(.5,.5);content.RenderTransform=new ScaleTransform(1,1);}window.SetValue(Closing,true);}
            var collapse=new DoubleAnimation((double)window.GetValue(ExitProgress),.999,TimeSpan.FromMilliseconds(250)){EasingFunction=new CubicEase{EasingMode=EasingMode.EaseInOut}};Timeline.SetDesiredFrameRate(collapse,60);window.BeginAnimation(ExitProgress,collapse);
            var motion=new DoubleAnimation(start,start+12,TimeSpan.FromMilliseconds(240)){EasingFunction=new CubicEase{EasingMode=EasingMode.EaseInOut}};Timeline.SetDesiredFrameRate(motion,60);window.BeginAnimation(Window.TopProperty,motion);
        }
        internal static void Reset(Window window)
        {
            window.SetValue(Closing,false);window.BeginAnimation(ExitProgress,null);window.SetValue(ExitProgress,0.0);var content=window.Content as FrameworkElement;var previous=window.GetValue(OriginalTransform) as Transform;if(content!=null&&previous!=null){content.RenderTransform=previous;content.RenderTransformOrigin=(Point)window.GetValue(OriginalOrigin);window.ClearValue(OriginalTransform);window.ClearValue(OriginalOrigin);}Native.SetGlassClip(window,1);
            double top=(double)window.GetValue(BaseTop);window.BeginAnimation(Window.TopProperty,null);if(!Double.IsNaN(top))window.Top=top;window.SetValue(BaseTop,Double.NaN);
        }
    }
}

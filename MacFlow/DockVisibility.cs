using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace MacFlow
{
    // Animate cached WPF content, never move the native window every frame.
    public sealed class DockVisibility
    {
        private readonly Window window;
        private readonly DockSlide native;
        internal void PositionChanging(){if(native!=null)native.PositionChanging();}
        internal Native.RECT RestingBounds(){if(native!=null)return native.RestingBounds();Native.RECT rect;Native.GetWindowRect(new System.Windows.Interop.WindowInteropHelper(window).Handle,out rect);return rect;}
        private bool requested=true;
        private int generation;
        private readonly System.Windows.Threading.DispatcherTimer settle;
        public DockVisibility(Window dock){window=dock;if(!dock.AllowsTransparency)native=new DockSlide(dock);settle=new System.Windows.Threading.DispatcherTimer(System.Windows.Threading.DispatcherPriority.Background){Interval=TimeSpan.FromMilliseconds(320)};settle.Tick+=delegate{settle.Stop();if(!requested)window.Hide();};window.Closed+=delegate{settle.Stop();};}
        private TranslateTransform Shift(FrameworkElement content)
        {
            var group=content.RenderTransform as TransformGroup;
            if(group==null){group=new TransformGroup();content.RenderTransform=group;}
            var shift=group.Children.OfType<TranslateTransform>().FirstOrDefault();if(shift==null){shift=new TranslateTransform();group.Children.Add(shift);}return shift;
        }
        public void Show()
        {
            if(native!=null){native.Show();return;}
            if(requested&&window.IsVisible)return;settle.Stop();requested=true;generation++;bool wasHidden=!window.IsVisible;window.IsHitTestVisible=true;
            var content=window.Content as FrameworkElement;
            if(content!=null){var shift=Shift(content);if(wasHidden){content.BeginAnimation(UIElement.OpacityProperty,null);content.Opacity=0;shift.BeginAnimation(TranslateTransform.YProperty,null);shift.Y=22;}
                if(Design.Animations){content.BeginAnimation(UIElement.OpacityProperty,new DoubleAnimation(1,TimeSpan.FromMilliseconds(240)){EasingFunction=new CubicEase{EasingMode=EasingMode.EaseOut}});var motion=new DoubleAnimationUsingKeyFrames();motion.KeyFrames.Add(new EasingDoubleKeyFrame(-3.5,KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(180)),new CubicEase{EasingMode=EasingMode.EaseOut}));motion.KeyFrames.Add(new EasingDoubleKeyFrame(0,KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(300)),new QuadraticEase{EasingMode=EasingMode.EaseInOut}));Timeline.SetDesiredFrameRate(motion,60);shift.BeginAnimation(TranslateTransform.YProperty,motion);}
                else{content.BeginAnimation(UIElement.OpacityProperty,null);content.Opacity=1;shift.BeginAnimation(TranslateTransform.YProperty,null);shift.Y=0;}}
            if(wasHidden)window.Show();
        }
        public void Hide()
        {
            if(native!=null){native.Hide();return;}
            if(!requested||!window.IsVisible)return;requested=false;int request=++generation;window.IsHitTestVisible=false;var content=window.Content as FrameworkElement;
            if(!Design.Animations||content==null){window.Hide();return;}
            var shift=Shift(content);var fade=new DoubleAnimation(0,TimeSpan.FromMilliseconds(280)){EasingFunction=new CubicEase{EasingMode=EasingMode.EaseInOut}};
            var motion=new DoubleAnimationUsingKeyFrames();motion.KeyFrames.Add(new EasingDoubleKeyFrame(-2,KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(70)),new QuadraticEase{EasingMode=EasingMode.EaseOut}));motion.KeyFrames.Add(new EasingDoubleKeyFrame(22,KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(280)),new CubicEase{EasingMode=EasingMode.EaseIn}));Timeline.SetDesiredFrameRate(motion,60);
            fade.Completed+=delegate{if(request==generation&&!requested){settle.Stop();window.Hide();}};settle.Start();content.BeginAnimation(UIElement.OpacityProperty,fade);shift.BeginAnimation(TranslateTransform.YProperty,motion);
        }
        public void ShowImmediately()
        {
            if(native!=null){native.Show(true);return;}
            var content=window.Content as FrameworkElement;var shift=content==null?null:Shift(content);
            if(requested&&window.IsVisible&&content!=null&&!content.HasAnimatedProperties&&content.Opacity==1&&shift.Y==0&&!shift.HasAnimatedProperties)return;
            settle.Stop();requested=true;generation++;window.IsHitTestVisible=true;
            if(content!=null){content.BeginAnimation(UIElement.OpacityProperty,null);content.Opacity=1;shift.BeginAnimation(TranslateTransform.YProperty,null);shift.Y=0;}
            if(!window.IsVisible)window.Show();
        }
        public void HideImmediately(){if(native!=null){native.Hide(true);return;}settle.Stop();requested=false;generation++;window.Hide();window.IsHitTestVisible=true;}
    }
}

using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
namespace MacFlow
{
    // Move the complete owned compositor window, including its blur surface.
    internal sealed class DockSlide
    {
        private readonly Window window;
        private readonly DispatcherTimer fallback;
        private bool requested=true,moving;
        private int generation;
        private double resting;
        private DependencyProperty axis;
        internal DockSlide(Window value)
        {window=value;fallback=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(390)};fallback.Tick+=delegate{FinishHide(generation);};window.Closed+=delegate{fallback.Stop();};}
        private DependencyProperty Axis {get{return moving?axis:window.Width>window.Height?Window.TopProperty:Window.LeftProperty;}}
        private double Current {get{return (double)window.GetValue(Axis);}}
        private double Travel {get{if(Axis==Window.TopProperty)return window.Height+20;return window.Left<SystemParameters.PrimaryScreenWidth/2?-(window.Width+20):window.Width+20;}}
        private void Prepare(){if(!moving){axis=Axis;resting=Current;}moving=true;}
        private void Reset(){window.BeginAnimation(Axis,null);window.SetValue(Axis,resting);moving=false;}
        internal void PositionChanging(){if(!moving)return;generation++;fallback.Stop();Reset();}
        internal Native.RECT RestingBounds()
        {
            Native.RECT rect;Native.GetWindowRect(new WindowInteropHelper(window).Handle,out rect);if(!moving)return rect;
            var source=PresentationSource.FromVisual(window);double scale=source==null?1:source.CompositionTarget.TransformToDevice.M11;int delta=(int)Math.Round((resting-Current)*scale);
            if(Axis==Window.TopProperty){rect.Top+=delta;rect.Bottom+=delta;}else{rect.Left+=delta;rect.Right+=delta;}return rect;
        }
        internal void Show(bool immediately=false)
        {
            if(requested&&window.IsVisible&&!immediately)return;
            if(immediately&&requested&&window.IsVisible&&!moving)return;
            Prepare();fallback.Stop();requested=true;int request=++generation;window.IsHitTestVisible=true;
            double start=window.IsVisible?Current:resting+Travel;
            if(immediately||!Design.Animations){Reset();if(!window.IsVisible)window.Show();return;}
            window.BeginAnimation(Axis,null);window.SetValue(Axis,resting);
            var slide=new DoubleAnimationUsingKeyFrames();slide.KeyFrames.Add(new EasingDoubleKeyFrame(start,KeyTime.FromTimeSpan(TimeSpan.Zero)));
            slide.KeyFrames.Add(new EasingDoubleKeyFrame(resting+(Travel>0?-5:5),KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(270)),new CubicEase{EasingMode=EasingMode.EaseOut}));
            slide.KeyFrames.Add(new EasingDoubleKeyFrame(resting,KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(390)),new QuadraticEase{EasingMode=EasingMode.EaseInOut}));Timeline.SetDesiredFrameRate(slide,60);
            slide.Completed+=delegate{if(request==generation&&requested)Reset();};window.BeginAnimation(Axis,slide);if(!window.IsVisible)window.Show();
        }
        internal void Hide(bool immediately=false)
        {
            if(!requested&&!immediately)return;Prepare();requested=false;int request=++generation;window.IsHitTestVisible=false;
            if(immediately||!Design.Animations){window.Hide();Reset();window.IsHitTestVisible=true;return;}
            var slide=new DoubleAnimationUsingKeyFrames();slide.KeyFrames.Add(new EasingDoubleKeyFrame(Current,KeyTime.FromTimeSpan(TimeSpan.Zero)));
            slide.KeyFrames.Add(new EasingDoubleKeyFrame(resting+(Travel>0?-2:2),KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(90)),new QuadraticEase{EasingMode=EasingMode.EaseOut}));
            slide.KeyFrames.Add(new EasingDoubleKeyFrame(resting+Travel,KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(340)),new CubicEase{EasingMode=EasingMode.EaseIn}));Timeline.SetDesiredFrameRate(slide,60);slide.Completed+=delegate{FinishHide(request);};fallback.Start();window.BeginAnimation(Axis,slide);
        }
        private void FinishHide(int request){if(request!=generation||requested)return;fallback.Stop();window.Hide();Reset();window.IsHitTestVisible=true;}
    }
}

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
        private double Travel {get{if(Axis==Window.TopProperty)return window.Height+20;var area=DisplaySpace.For(window).Dips;return window.Left<area.Left+area.Width/2?-(window.Width+20):window.Width+20;}}
        private void Prepare(){if(!moving){axis=Axis;resting=Current;}moving=true;}
        private void Reset(){window.BeginAnimation(Axis,null);window.SetValue(Axis,resting);moving=false;}
        internal void PositionChanging(){if(!moving)return;bool hide=!requested;generation++;fallback.Stop();Reset();if(hide){window.Hide();window.IsHitTestVisible=true;}}
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
            if(immediately||!Motion.Enabled){Reset();if(!window.IsVisible)window.Show();return;}
            window.BeginAnimation(Axis,null);window.SetValue(Axis,resting);
            var slide=new DoubleAnimation(start,resting,TimeSpan.FromMilliseconds(240)){EasingFunction=new CubicEase{EasingMode=EasingMode.EaseOut}};Timeline.SetDesiredFrameRate(slide,60);
            slide.Completed+=delegate{if(request==generation&&requested)Reset();};window.BeginAnimation(Axis,slide);if(!window.IsVisible)window.Show();
        }
        internal void Hide(bool immediately=false)
        {
            if(!requested&&!immediately)return;Prepare();requested=false;int request=++generation;window.IsHitTestVisible=false;
            if(immediately||!Motion.Enabled){window.Hide();Reset();window.IsHitTestVisible=true;return;}
            var slide=new DoubleAnimation(Current,resting+Travel,TimeSpan.FromMilliseconds(300)){EasingFunction=new CubicEase{EasingMode=EasingMode.EaseInOut}};Timeline.SetDesiredFrameRate(slide,60);slide.Completed+=delegate{FinishHide(request);};fallback.Start();window.BeginAnimation(Axis,slide);
        }
        private void FinishHide(int request){if(request!=generation||requested)return;fallback.Stop();window.Hide();Reset();window.IsHitTestVisible=true;}
    }
}

using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
namespace MacFlow
{
    internal sealed class ControlPullGesture : IDisposable
    {
        private readonly Window bar;private readonly Func<bool> enabled;private readonly Func<Window> create;private Window panel;private Point origin;private bool dragging;private double progress;
        internal static double Progress(double distance,double height){return Math.Max(0,Math.Min(1,(distance-6)/Math.Max(100,height*.55)));}
        internal ControlPullGesture(Window bar,Func<bool> enabled,Func<Window> create)
        {this.bar=bar;this.enabled=enabled;this.create=create;bar.PreviewMouseRightButtonDown+=Down;bar.PreviewMouseMove+=Move;bar.PreviewMouseRightButtonUp+=Up;bar.LostMouseCapture+=Lost;}
        private void Down(object sender,MouseButtonEventArgs e)
        {var point=e.GetPosition(bar);if(!enabled()||point.X<bar.ActualWidth-300)return;origin=point;progress=0;dragging=bar.CaptureMouse();if(dragging)e.Handled=true;}
        private void Move(object sender,MouseEventArgs e)
        {
            if(!dragging)return;if(e.RightButton!=MouseButtonState.Pressed){End();return;}Pull(e.GetPosition(bar).Y-origin.Y);e.Handled=true;
        }
        internal void Pull(double distance)
        {if(panel==null&&distance<10)return;if(panel==null)panel=create();if(panel==null)return;progress=Progress(distance,panel.Height);panel.Top=46-(panel.Height+46)*(1-progress);panel.Opacity=.35+.65*progress;if(!CompositorGlass.Active(panel)&&panel.Top+panel.Height>0)Native.Glass(panel,Design.Dark);}
        internal void Release(){End();}
        private void Up(object sender,MouseButtonEventArgs e){if(!dragging)return;e.Handled=true;End();}
        private void Lost(object sender,MouseEventArgs e){if(dragging)End();}
        private void End()
        {
            dragging=false;if(Mouse.Captured==bar)bar.ReleaseMouseCapture();var window=panel;panel=null;if(window==null||!window.IsVisible)return;Settle(window,progress>=.25);
        }
        internal static void Settle(Window window,bool show)
        {
            double top=show?46:-window.Height-46,opacity=show?1:0;
            window.BeginAnimation(Window.TopProperty,null);window.BeginAnimation(UIElement.OpacityProperty,null);
            double previous=window.Top,alpha=window.Opacity;window.Top=top;window.Opacity=opacity;
            if(!Motion.Enabled){if(!show)window.Close();return;}
            var slide=new DoubleAnimation(previous,top,TimeSpan.FromMilliseconds(300)){EasingFunction=show?(IEasingFunction)new SpringEase{EasingMode=EasingMode.EaseIn}:Motion.Ease(true),FillBehavior=FillBehavior.Stop};Timeline.SetDesiredFrameRate(slide,60);slide.Completed+=delegate{if(!show)window.Close();};window.BeginAnimation(Window.TopProperty,slide);Motion.Fade(window,alpha,opacity,260);
        }
        public void Dispose(){dragging=false;if(Mouse.Captured==bar)bar.ReleaseMouseCapture();if(panel!=null&&panel.IsVisible)panel.Close();panel=null;bar.PreviewMouseRightButtonDown-=Down;bar.PreviewMouseMove-=Move;bar.PreviewMouseRightButtonUp-=Up;bar.LostMouseCapture-=Lost;}
    }
}

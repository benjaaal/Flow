using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
namespace MacFlow
{
    internal sealed class ControlPullGesture : IDisposable
    {
        private readonly Window bar;private readonly Func<bool> enabled;private readonly Func<Window> create;private Window panel;private Point origin;private bool dragging,armed;private MouseButton button;private double progress;
        internal static readonly DependencyProperty TargetTop=DependencyProperty.RegisterAttached("TargetTop",typeof(double),typeof(ControlPullGesture),new PropertyMetadata(Double.NaN));
        internal static double Progress(double distance,double height){return Math.Max(0,Math.Min(1,(distance-6)/Math.Max(100,height*.55)));}
        internal static bool Accepts(double x,double width,MouseButton button){return x>=width*.5&&(button==MouseButton.Left||button==MouseButton.Right);}
        internal ControlPullGesture(Window bar,Func<bool> enabled,Func<Window> create)
        {this.bar=bar;this.enabled=enabled;this.create=create;bar.PreviewMouseDown+=Down;bar.PreviewMouseMove+=Move;bar.PreviewMouseUp+=Up;bar.LostMouseCapture+=Lost;}
        private void Down(object sender,MouseButtonEventArgs e)
        {var point=e.GetPosition(bar);if(!enabled()||!Accepts(point.X,bar.ActualWidth,e.ChangedButton))return;origin=point;progress=0;button=e.ChangedButton;armed=true;}
        private void Move(object sender,MouseEventArgs e)
        {
            if(!armed&&!dragging)return;if((button==MouseButton.Left?e.LeftButton:e.RightButton)!=MouseButtonState.Pressed){End();return;}double distance=e.GetPosition(bar).Y-origin.Y;if(!dragging){if(distance<8)return;dragging=bar.CaptureMouse();if(!dragging){armed=false;return;}}Pull(distance);e.Handled=true;
        }
        internal void Pull(double distance)
        {if(panel!=null&&!WindowLife.Alive(panel))panel=null;if(panel==null&&distance<10)return;if(panel==null){panel=create();if(panel!=null&&Double.IsNaN((double)panel.GetValue(TargetTop)))panel.SetValue(TargetTop,DisplaySpace.UpperEdge(bar)+46);}if(panel==null)return;progress=Progress(distance,panel.Height);double target=(double)panel.GetValue(TargetTop);panel.Top=target-(panel.Height+46)*(1-progress);panel.Opacity=.35+.65*progress;if(!CompositorGlass.Active(panel)&&panel.Top+panel.Height>target-46)Native.Glass(panel,Design.Dark);}
        internal void Release(){End();}
        private void Up(object sender,MouseButtonEventArgs e){if(dragging)e.Handled=true;End();}
        private void Lost(object sender,MouseEventArgs e){if(dragging)End();}
        private void End()
        {
            dragging=armed=false;if(Mouse.Captured==bar)bar.ReleaseMouseCapture();var window=panel;panel=null;if(!WindowLife.Alive(window)||!window.IsVisible)return;Settle(window,progress>=.25);
        }
        internal static void Settle(Window window,bool show)
        {
            if(!WindowLife.Alive(window))return;double target=(double)window.GetValue(TargetTop);if(Double.IsNaN(target))target=DisplaySpace.UpperEdge(window)+46;double top=show?target:target-window.Height-92,opacity=show?1:0;
            window.BeginAnimation(Window.TopProperty,null);window.BeginAnimation(UIElement.OpacityProperty,null);
            double previous=window.Top,alpha=window.Opacity;window.Top=top;window.Opacity=opacity;
            if(!Motion.Enabled){if(!show)window.Close();return;}
            var slide=new DoubleAnimation(previous,top,TimeSpan.FromMilliseconds(300)){EasingFunction=show?(IEasingFunction)new SpringEase{EasingMode=EasingMode.EaseIn}:Motion.Ease(true),FillBehavior=FillBehavior.Stop};Timeline.SetDesiredFrameRate(slide,60);slide.Completed+=delegate{if(!show)WindowLife.Close(window);};window.BeginAnimation(Window.TopProperty,slide);Motion.Fade(window,alpha,opacity,260);
        }
        public void Dispose(){dragging=armed=false;if(Mouse.Captured==bar)bar.ReleaseMouseCapture();if(WindowLife.Alive(panel))WindowLife.Close(panel);panel=null;bar.PreviewMouseDown-=Down;bar.PreviewMouseMove-=Move;bar.PreviewMouseUp-=Up;bar.LostMouseCapture-=Lost;}
    }
}

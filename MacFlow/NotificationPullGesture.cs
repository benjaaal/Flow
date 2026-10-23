using System;
using System.Windows;
using System.Windows.Input;
namespace MacFlow
{
    // Direct manipulation from the top-left of the actual menu bar. A click
    // still opens its menu; only a deliberate downward drag claims the pointer.
    internal sealed class NotificationPullGesture : IDisposable
    {
        private readonly Window bar;private readonly Func<bool> enabled;private readonly Func<PrivacyScreen> create;
        private Point origin;private bool armed,dragging;private double progress;private PrivacyScreen panel;
        internal static bool Accepts(double x){return x>=0&&x<=96;}
        internal static double Progress(double distance,double height){return Math.Max(0,Math.Min(1,(distance-8)/Math.Max(160,height*.52)));}
        internal NotificationPullGesture(Window bar,Func<bool> enabled,Func<PrivacyScreen> create)
        {this.bar=bar;this.enabled=enabled;this.create=create;bar.PreviewMouseLeftButtonDown+=Down;bar.PreviewMouseMove+=Move;bar.PreviewMouseLeftButtonUp+=Up;bar.LostMouseCapture+=Lost;}
        private void Down(object sender,MouseButtonEventArgs e){var p=e.GetPosition(bar);if(!enabled()||!Accepts(p.X))return;origin=p;progress=0;armed=true;}
        private void Move(object sender,MouseEventArgs e)
        {if(!armed&&!dragging)return;if(e.LeftButton!=MouseButtonState.Pressed){End();return;}double distance=e.GetPosition(bar).Y-origin.Y;if(!dragging){if(distance<10)return;dragging=bar.CaptureMouse();if(!dragging){armed=false;return;}}Pull(distance);e.Handled=true;}
        internal void Pull(double distance)
        {if(panel!=null&&!WindowLife.Alive(panel))panel=null;if(panel==null&&distance<10)return;if(panel==null)panel=create();if(panel==null)return;progress=Progress(distance,panel.Height);panel.PullQuick(progress);}
        private void Up(object sender,MouseButtonEventArgs e){if(dragging)e.Handled=true;End();}
        private void Lost(object sender,MouseEventArgs e){if(dragging)End();}
        internal void Release(){End();}
        private void End(){var value=panel;panel=null;armed=dragging=false;if(Mouse.Captured==bar)bar.ReleaseMouseCapture();if(WindowLife.Alive(value))value.CompleteQuick(progress>=.18);}
        public void Dispose(){armed=dragging=false;if(Mouse.Captured==bar)bar.ReleaseMouseCapture();if(WindowLife.Alive(panel))panel.Recover();panel=null;bar.PreviewMouseLeftButtonDown-=Down;bar.PreviewMouseMove-=Move;bar.PreviewMouseLeftButtonUp-=Up;bar.LostMouseCapture-=Lost;}
    }
}

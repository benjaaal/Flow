using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
namespace MacFlow
{
    // Four narrow, click-through surfaces instead of a full-screen bitmap or
    // opaque overlay. Only the display invoking the assistant is illuminated.
    internal sealed class AssistantEdgeGlow : IDisposable
    {
        private readonly Window[] windows=new Window[4];private readonly EdgeLight[] lights=new EdgeLight[4];
        private readonly Stopwatch elapsed=new Stopwatch();private bool active,rendering,disposed;private int generation;private double lastFrame;
        internal Window[] Views{get{return windows;}}
        internal bool Rendering{get{return rendering;}}
        internal AssistantEdgeGlow()
        {
            for(int i=0;i<4;i++)
            {
                var view=new Window{Title="MacOS · Luz del asistente",WindowStyle=WindowStyle.None,ResizeMode=ResizeMode.NoResize,AllowsTransparency=true,Background=Brushes.Transparent,ShowActivated=false,ShowInTaskbar=false,Topmost=true,IsHitTestVisible=false};lights[i]=new EdgeLight(i);view.Content=lights[i];windows[i]=view;
                view.SourceInitialized+=delegate{Native.NoActivate(view);Native.RemoveGlassFrame(view);var h=new WindowInteropHelper(view).Handle;Native.SetWindowLong(h,-20,Native.GetWindowLong(h,-20)|0x20|0x80);};
            }
        }
        internal void Show(DisplayArea area)
        {
            if(disposed)return;bool entering=!active;active=true;++generation;const double thickness=30;
            for(int i=0;i<4;i++){var view=windows[i];view.Width=i%2==0?area.Width:thickness;view.Height=i%2==0?thickness:area.Height;double x=i==1?area.Width-thickness:0,y=i==2?area.Height-thickness:0;DisplaySpace.Place(view,area,x,y);view.BeginAnimation(UIElement.OpacityProperty,null);view.Opacity=entering&&Motion.Enabled?0:.78;if(!view.IsVisible)view.Show();if(entering&&Motion.Enabled){var fade=new DoubleAnimation(0,.78,TimeSpan.FromMilliseconds(330)){BeginTime=TimeSpan.FromMilliseconds(i==1?0:i==0?50:i==2?80:130),EasingFunction=Motion.Ease(),FillBehavior=FillBehavior.HoldEnd};view.BeginAnimation(UIElement.OpacityProperty,fade);}lights[i].SetTime(0);}
            if(entering){elapsed.Restart();lastFrame=-1;}if(Motion.Enabled&&!rendering){rendering=true;CompositionTarget.Rendering+=Frame;}else if(!Motion.Enabled)StopClock();
        }
        private void Frame(object sender,EventArgs args)
        {
            if(!active||disposed||!Motion.Enabled){StopClock();return;}double time=elapsed.Elapsed.TotalSeconds;if(time-lastFrame<1.0/30)return;lastFrame=time;foreach(var light in lights)light.SetTime(time);
        }
        private void StopClock(){if(rendering){CompositionTarget.Rendering-=Frame;rendering=false;}}
        internal void Hide()
        {
            if(disposed||!active)return;active=false;StopClock();int token=++generation;foreach(var view in windows){if(!Motion.Enabled){view.Hide();continue;}var fade=new DoubleAnimation(view.Opacity,0,TimeSpan.FromMilliseconds(180)){FillBehavior=FillBehavior.HoldEnd};fade.Completed+=delegate{if(!disposed&&!active&&generation==token)view.Hide();};view.BeginAnimation(UIElement.OpacityProperty,fade);}
        }
        public void Dispose(){if(disposed)return;disposed=true;active=false;StopClock();elapsed.Stop();foreach(var view in windows)view.Close();}
        private sealed class EdgeLight : FrameworkElement
        {
            private readonly LinearGradientBrush spectrum,falloff;private readonly TranslateTransform travel;private readonly int side;private double time;
            internal EdgeLight(int side)
            {
                this.side=side;travel=new TranslateTransform();bool horizontal=side%2==0;
                spectrum=new LinearGradientBrush(new GradientStopCollection{new GradientStop(Color.FromRgb(63,175,255),0),new GradientStop(Color.FromRgb(130,83,255),.22),new GradientStop(Color.FromRgb(255,81,207),.46),new GradientStop(Color.FromRgb(255,132,125),.68),new GradientStop(Color.FromRgb(255,204,116),.84),new GradientStop(Color.FromRgb(64,192,244),1)},horizontal?new Point(-.3,0):new Point(0,-.3),horizontal?new Point(1.3,0):new Point(0,1.3)){RelativeTransform=travel};
                Point start=side==1?new Point(1,0):side==2?new Point(0,1):new Point(0,0),end=side==0?new Point(0,1):side==3?new Point(1,0):new Point(0,0);
                falloff=new LinearGradientBrush(new GradientStopCollection{new GradientStop(Color.FromArgb(225,255,255,255),0),new GradientStop(Color.FromArgb(100,255,255,255),.20),new GradientStop(Color.FromArgb(30,255,255,255),.52),new GradientStop(Colors.Transparent,1)},start,end);falloff.Freeze();
            }
            internal void SetTime(double seconds){time=seconds;travel.X=side%2==0?.13*Math.Sin(time*.6+side):0;travel.Y=side%2!=0?.13*Math.Sin(time*.6+side):0;InvalidateVisual();}
            protected override void OnRender(DrawingContext dc){base.OnRender(dc);if(ActualWidth<=0||ActualHeight<=0)return;dc.PushOpacity(.9+.1*Math.Sin(time*1.3+side*.5));dc.PushOpacityMask(falloff);dc.DrawRectangle(spectrum,null,new Rect(0,0,ActualWidth,ActualHeight));dc.Pop();dc.Pop();}
        }
    }
}

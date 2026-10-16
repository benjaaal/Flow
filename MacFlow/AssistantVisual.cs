using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
namespace MacFlow
{
    internal enum AssistantStage { Hidden, Appearance, Listening, Working, Answer }

    // Original vector rendering derived from the supplied motion reference:
    // a glass lens, white orbiting lights, then a quiet lower reflection.
    // No video playback, bitmap sequence, screen capture or Apple resources.
    internal sealed class AssistantVisual : FrameworkElement, IDisposable
    {
        private readonly Stopwatch elapsed=new Stopwatch();
        private readonly Brush lens,reflection,horizon,halo,glint;
        private readonly Pen rim;
        private bool attached,disposed;private double lastFrame=-1;
        internal AssistantStage Stage{get;private set;}
        internal bool Rendering{get{return attached;}}
        internal AssistantVisual()
        {
            IsHitTestVisible=false;
            lens=Freeze(new RadialGradientBrush(new GradientStopCollection{new GradientStop(Color.FromRgb(3,4,8),0),new GradientStop(Color.FromRgb(4,7,12),.64),new GradientStop(Color.FromRgb(54,63,75),.94),new GradientStop(Color.FromRgb(163,178,190),1)}){GradientOrigin=new Point(.47,.23),Center=new Point(.5,.5),RadiusX=.5,RadiusY=.5});
            reflection=Freeze(new LinearGradientBrush(new GradientStopCollection{new GradientStop(Color.FromArgb(0,58,182,255),0),new GradientStop(Color.FromArgb(175,55,184,255),.2),new GradientStop(Color.FromArgb(245,188,238,255),.43),new GradientStop(Colors.White,.52),new GradientStop(Color.FromArgb(235,255,208,156),.63),new GradientStop(Color.FromArgb(105,255,153,91),.83),new GradientStop(Colors.Transparent,1)},new Point(0,.3),new Point(1,.7)));
            horizon=Freeze(new RadialGradientBrush(new GradientStopCollection{new GradientStop(Color.FromArgb(165,247,240,227),0),new GradientStop(Color.FromArgb(85,127,202,233),.32),new GradientStop(Color.FromArgb(28,197,149,219),.63),new GradientStop(Colors.Transparent,1)}){RadiusX=.5,RadiusY=.5});
            halo=Freeze(new RadialGradientBrush(new GradientStopCollection{new GradientStop(Color.FromArgb(225,242,250,255),0),new GradientStop(Color.FromArgb(95,188,225,255),.25),new GradientStop(Colors.Transparent,1)}));
            glint=Freeze(new SolidColorBrush(Color.FromArgb(27,235,246,255)));rim=new Pen(Freeze(new SolidColorBrush(Color.FromArgb(94,219,231,242))),.65);rim.Freeze();
            Loaded+=delegate{UpdateClock();};Unloaded+=delegate{Detach();};IsVisibleChanged+=delegate{UpdateClock();};
        }
        private static Brush Freeze(Brush brush){brush.Freeze();return brush;}
        internal void Set(AssistantStage stage)
        {
            if(disposed)return;if(Stage!=stage){Stage=stage;elapsed.Restart();lastFrame=-1;}UpdateClock();InvalidateVisual();
        }
        private void UpdateClock()
        {
            bool needs=!disposed&&IsLoaded&&IsVisible&&Stage!=AssistantStage.Hidden&&Motion.Enabled&&(Stage!=AssistantStage.Answer||elapsed.Elapsed.TotalSeconds<2.2);
            if(needs&&!attached){attached=true;CompositionTarget.Rendering+=Frame;}else if(!needs)Detach();
        }
        private void Detach(){if(attached){CompositionTarget.Rendering-=Frame;attached=false;}}
        private void Frame(object sender,EventArgs args)
        {
            if(disposed||!IsVisible||!Motion.Enabled){Detach();return;}double time=elapsed.Elapsed.TotalSeconds;if(time-lastFrame<1.0/62)return;lastFrame=time;InvalidateVisual();if(Stage==AssistantStage.Answer&&time>2.2)Detach();
        }
        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);if(Stage==AssistantStage.Hidden||ActualWidth<=0||ActualHeight<=0)return;double time=Motion.Enabled?elapsed.Elapsed.TotalSeconds:.3;
            dc.PushClip(new RectangleGeometry(new Rect(0,0,ActualWidth,ActualHeight),ActualHeight/2,ActualHeight/2));
            if(Stage==AssistantStage.Appearance)DrawLens(dc,new Point(ActualWidth/2,ActualHeight/2),Math.Min(ActualWidth,ActualHeight)*.455,time);
            else if(Stage==AssistantStage.Listening)DrawLens(dc,new Point(30,ActualHeight/2),22,time);
            else if(Stage==AssistantStage.Working)
            {
                var center=new Point(29,ActualHeight/2);for(int i=0;i<6;i++){double angle=time*1.45+i*Math.PI/3,breath=1+.08*Math.Sin(time*2.3+i),r=(9+2*Math.Sin(time+i*.7))*breath;var p=new Point(center.X+Math.Cos(angle)*r,center.Y+Math.Sin(angle)*r);double intensity=.57+.43*(.5+.5*Math.Sin(angle-time*.4));dc.PushOpacity(intensity);dc.DrawEllipse(halo,null,p,7,7);dc.DrawEllipse(Brushes.White,null,p,1.45,1.45);dc.Pop();}
            }
            else if(Stage==AssistantStage.Answer){double swell=Motion.Enabled?1+.07*Math.Sin(time*2):1;dc.DrawEllipse(horizon,null,new Point(ActualWidth*.5,ActualHeight+2),ActualWidth*.48,15*swell);}
            dc.Pop();
        }
        private void DrawLens(DrawingContext dc,Point center,double radius,double time)
        {
            double sway=Motion.Enabled?Math.Sin(time*1.9)*radius*.045:0;dc.DrawEllipse(lens,rim,center,radius,radius);
            dc.PushClip(new EllipseGeometry(center,radius-.8,radius-.8));
            dc.PushTransform(new RotateTransform(-5+Math.Sin(time*1.2)*3,center.X,center.Y));
            dc.DrawEllipse(horizon,null,new Point(center.X,center.Y+radius*.37+sway),radius*1.18,radius*.44);
            dc.DrawEllipse(reflection,null,new Point(center.X,center.Y+radius*.15+sway),radius*1.08,radius*.12);
            dc.DrawEllipse(glint,null,new Point(center.X-radius*.28,center.Y+radius*.62),radius*.36,radius*.13);
            dc.Pop();dc.Pop();
        }
        public void Dispose(){if(disposed)return;disposed=true;Detach();elapsed.Stop();}
    }
}

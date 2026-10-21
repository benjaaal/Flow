using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
namespace MacFlow
{
    internal enum AssistantStage { Hidden, Ready, Appearance, Listening, Working, Answer, Speaking }

    // Original vector rendering: a thin luminous rim and continuous flowing
    // ribbons. Speaking is supplied by real playback, never by a visual timer.
    // No video playback, bitmap sequence, screen capture or Apple resources.
    internal sealed class AssistantVisual : FrameworkElement, IDisposable
    {
        private readonly Stopwatch elapsed=new Stopwatch();
        private readonly Brush reflection,halo;
        private readonly Pen rim;
        private bool attached,disposed;private double lastFrame=-1;
        internal AssistantStage Stage{get;private set;}
        internal bool Rendering{get{return attached;}}
        internal AssistantVisual()
        {
            IsHitTestVisible=false;
            reflection=Freeze(new LinearGradientBrush(new GradientStopCollection{new GradientStop(Color.FromArgb(0,58,182,255),0),new GradientStop(Color.FromArgb(175,55,184,255),.2),new GradientStop(Color.FromArgb(245,188,238,255),.43),new GradientStop(Colors.White,.52),new GradientStop(Color.FromArgb(235,255,208,156),.63),new GradientStop(Color.FromArgb(105,255,153,91),.83),new GradientStop(Colors.Transparent,1)},new Point(0,.3),new Point(1,.7)));
            halo=Freeze(new RadialGradientBrush(new GradientStopCollection{new GradientStop(Color.FromArgb(225,242,250,255),0),new GradientStop(Color.FromArgb(95,188,225,255),.25),new GradientStop(Colors.Transparent,1)}));
            rim=new Pen(reflection,.7);rim.Freeze();
            Loaded+=delegate{UpdateClock();};Unloaded+=delegate{Detach();};IsVisibleChanged+=delegate{UpdateClock();};
        }
        private static Brush Freeze(Brush brush){brush.Freeze();return brush;}
        internal void Set(AssistantStage stage)
        {
            if(disposed)return;if(Stage!=stage){Stage=stage;elapsed.Restart();lastFrame=-1;}UpdateClock();InvalidateVisual();
        }
        private void UpdateClock()
        {
            bool needs=!disposed&&IsLoaded&&IsVisible&&Stage!=AssistantStage.Hidden&&Stage!=AssistantStage.Ready&&Motion.Enabled&&(Stage!=AssistantStage.Answer||elapsed.Elapsed.TotalSeconds<1.6)&&(Stage!=AssistantStage.Appearance||elapsed.Elapsed.TotalSeconds<.8);
            if(needs&&!attached){attached=true;CompositionTarget.Rendering+=Frame;}else if(!needs)Detach();
        }
        private void Detach(){if(attached){CompositionTarget.Rendering-=Frame;attached=false;}}
        private void Frame(object sender,EventArgs args)
        {
            if(disposed||!IsVisible||!Motion.Enabled){Detach();return;}double time=elapsed.Elapsed.TotalSeconds;if(time-lastFrame<1.0/60)return;lastFrame=time;InvalidateVisual();if(Stage==AssistantStage.Answer&&time>1.6||Stage==AssistantStage.Appearance&&time>.8)Detach();
        }
        protected override void OnRender(DrawingContext dc)
        {
            if(Stage==AssistantStage.Hidden||ActualWidth<1||ActualHeight<1)return;
            double size=Math.Min(ActualWidth,ActualHeight),r=size*.39;var center=new Point(ActualWidth/2,ActualHeight/2);
            bool active=Motion.Enabled&&(Stage==AssistantStage.Listening||Stage==AssistantStage.Working||Stage==AssistantStage.Speaking||Stage==AssistantStage.Appearance);
            double t=active?elapsed.Elapsed.TotalSeconds:0;
            dc.DrawEllipse(null,rim,center,r,r);
            var clip=new EllipseGeometry(center,r-.5,r-.5);dc.PushClip(clip);
            for(int band=0;band<3;band++)
            {
                var geometry=new StreamGeometry();double amplitude=size*(active?.105:.045),phase=t*(Stage==AssistantStage.Speaking?3.2:1.8)+band*1.2;
                using(var path=geometry.Open())
                {
                    for(int i=0;i<=32;i++)
                    {
                        double u=i/32.0,x=center.X-r+2*r*u,envelope=Math.Pow(Math.Sin(Math.PI*u),1.5);
                        double y=center.Y+envelope*amplitude*Math.Sin(u*Math.PI*2+phase);
                        if(i==0)path.BeginFigure(new Point(x,y),true,true);else path.LineTo(new Point(x,y),true,false);
                    }
                    for(int i=32;i>=0;i--)
                    {
                        double u=i/32.0,envelope=Math.Pow(Math.Sin(Math.PI*u),1.5);
                        path.LineTo(new Point(center.X-r+2*r*u,center.Y+envelope*(amplitude*Math.Sin(u*Math.PI*2+phase)+size*.065)),true,false);
                    }
                }
                geometry.Freeze();dc.PushOpacity(band==1?.85:.48);dc.DrawGeometry(reflection,null,geometry);dc.Pop();
            }
            if(Stage==AssistantStage.Appearance||Stage==AssistantStage.Working)
                for(int i=0;i<3;i++){double a=t*2.1+i*2*Math.PI/3;var point=new Point(center.X+Math.Cos(a)*r*.67,center.Y+Math.Sin(a)*r*.67);dc.DrawEllipse(halo,null,point,size*.085,size*.085);}
            dc.Pop();
        }
        public void Dispose(){if(disposed)return;disposed=true;Detach();elapsed.Stop();}
    }
}

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace MacFlow
{
    // A single, interruptible spring for the physical capsule. Repeated provider
    // updates do not restart it; retargeting retains both size and velocity.
    internal sealed class IslandMotion : IDisposable
    {
        private readonly Window window;private readonly Border plate;private readonly Action position;
        private double width,height,currentWidth,currentHeight,vw,vh,vt,top,currentTop,previous=-1;private bool attached,disposed;
        internal bool Running{get{return attached;}}
        // Exact solution retains velocity when an activity changes mid-flight.
        // This slight overshoot belongs only to the capsule, never Dock hiding.
        internal static void Step(ref double value,ref double velocity,double target,double seconds)
        {
            if(seconds<=0)return;
            const double omega=25,damping=.80;
            double decayRate=omega*damping,frequency=omega*Math.Sqrt(1-damping*damping);
            double offset=value-target,phase=(velocity+decayRate*offset)/frequency;
            double angle=frequency*seconds,decay=Math.Exp(-decayRate*seconds);
            double cosine=Math.Cos(angle),sine=Math.Sin(angle);
            value=target+decay*(offset*cosine+phase*sine);
            velocity=decay*((-decayRate*offset+frequency*phase)*cosine+(-decayRate*phase-frequency*offset)*sine);
        }
        internal IslandMotion(Window view,Border shell,Action center){window=view;plate=shell;position=center;width=currentWidth=view.Width;height=currentHeight=view.Height;top=currentTop=Double.IsNaN(view.Top)?0:view.Top;view.Closed+=delegate{Dispose();};}
        internal void To(double w,double h){To(w,h,Double.IsNaN(window.Top)?0:window.Top);}
        internal void To(double w,double h,double y)
        {
            if(disposed)return;width=w;height=h;top=y;if(!attached)currentTop=Double.IsNaN(window.Top)?y:window.Top;
            if(!Motion.Enabled||!window.IsVisible){currentWidth=w;currentHeight=h;currentTop=y;Apply(w,h);Stop();return;}
            if(Math.Abs(currentWidth-w)<.08&&Math.Abs(currentHeight-h)<.08&&Math.Abs(currentTop-y)<.08&&!attached)return;
            if(attached)return;previous=-1;attached=true;CompositionTarget.Rendering+=Frame;
        }
        private void Frame(object sender,EventArgs args)
        {
            if(disposed||!window.IsVisible){Stop();return;}
            if(!Motion.Enabled){currentWidth=width;currentHeight=height;currentTop=top;Apply(width,height);Stop();return;}
            double now=((RenderingEventArgs)args).RenderingTime.TotalSeconds,dt=previous<0?1.0/60:Math.Min(.05,now-previous);if(dt<=0)return;previous=now;
            double w=currentWidth,h=currentHeight,y=currentTop;
            Step(ref w,ref vw,width,dt);Step(ref h,ref vh,height,dt);
            Motion.Spring(ref y,ref vt,top,Motion.Enabled?dt:0,23,.98);bool settled=Math.Abs(w-width)<.12&&Math.Abs(h-height)<.12&&Math.Abs(y-top)<.12&&Math.Abs(vw)+Math.Abs(vh)+Math.Abs(vt)<1;
            currentTop=settled?top:y;currentWidth=settled?width:w;currentHeight=settled?height:h;Apply(currentWidth,currentHeight);if(settled)Stop();
        }
        private void Apply(double w,double h){double nw=Math.Max(32,Math.Round(w*2)/2),nh=Math.Max(24,Math.Round(h*2)/2);if(Math.Abs(window.Top-currentTop)>.1||(currentTop==top&&window.Top!=top))window.Top=currentTop;if(window.Width!=nw)window.Width=nw;if(window.Height!=nh)window.Height=nh;var radius=new CornerRadius(Math.Min(38,nh/2));if(plate.CornerRadius!=radius)plate.CornerRadius=radius;position();}
        internal void Stop(){if(attached)CompositionTarget.Rendering-=Frame;attached=false;previous=-1;vw=vh=vt=0;}
        public void Dispose(){disposed=true;Stop();}
    }
    // Playback indicator, not an audio meter: driven only by Playing and only
    // rendered while visible. Paused/idle sessions never keep a frame callback.
    internal sealed class ActivityWave : FrameworkElement,IDisposable
    {
        private bool playing,attached;private double time;
        private readonly Brush ink=Design.Brush("#7296A1");
        internal ActivityWave(){Width=18;Height=18;IsHitTestVisible=false;Loaded+=delegate{Update();};Unloaded+=delegate{Detach();};IsVisibleChanged+=delegate{Update();};}
        internal void Set(bool value){playing=value;Update();InvalidateVisual();}
        private void Update(){bool needed=playing&&IsVisible&&IsLoaded&&Motion.Enabled;if(needed&&!attached){attached=true;CompositionTarget.Rendering+=Frame;}else if(!needed)Detach();}
        private void Frame(object sender,EventArgs args){if(!Motion.Enabled||!IsVisible){Detach();return;}double now=((RenderingEventArgs)args).RenderingTime.TotalSeconds;if(now-time<1.0/31)return;time=now;InvalidateVisual();}
        private void Detach(){if(attached)CompositionTarget.Rendering-=Frame;attached=false;}
        protected override void OnRender(DrawingContext dc){for(int i=0;i<4;i++){double h=playing?4+9*(.5+.5*Math.Sin((Motion.Enabled?time:0)*4+i*1.7)):3;dc.DrawRoundedRectangle(ink,null,new Rect(i*4.2,(18-h)/2,2.4,h),1.2,1.2);}}
        public void Dispose(){playing=false;Detach();}
    }
}

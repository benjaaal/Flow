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
        private double width,height,currentWidth,currentHeight,vw,vh,previous=-1;private bool attached,disposed;
        internal bool Running{get{return attached;}}
        internal IslandMotion(Window view,Border shell,Action center){window=view;plate=shell;position=center;width=currentWidth=view.Width;height=currentHeight=view.Height;view.Closed+=delegate{Dispose();};}
        internal void To(double w,double h)
        {
            if(disposed)return;width=w;height=h;
            if(!Motion.Enabled||!window.IsVisible){currentWidth=w;currentHeight=h;Apply(w,h);Stop();return;}
            if(Math.Abs(window.Width-w)<.08&&Math.Abs(window.Height-h)<.08&&!attached)return;
            if(attached)return;previous=-1;attached=true;CompositionTarget.Rendering+=Frame;
        }
        private void Frame(object sender,EventArgs args)
        {
            if(disposed||!window.IsVisible){Stop();return;}
            double now=((RenderingEventArgs)args).RenderingTime.TotalSeconds,dt=previous<0?1.0/60:Math.Min(.05,now-previous);if(dt<=0)return;previous=now;
            double w=currentWidth,h=currentHeight;
            Motion.Spring(ref w,ref vw,width,Motion.Enabled?dt:0,23,.88);Motion.Spring(ref h,ref vh,height,Motion.Enabled?dt:0,23,.88);
            bool settled=Math.Abs(w-width)<.12&&Math.Abs(h-height)<.12&&Math.Abs(vw)+Math.Abs(vh)<1;
            currentWidth=settled?width:w;currentHeight=settled?height:h;Apply(currentWidth,currentHeight);if(settled)Stop();
        }
        private void Apply(double w,double h){window.Width=Math.Max(32,w);window.Height=Math.Max(24,h);plate.CornerRadius=new CornerRadius(Math.Min(30,h/2));position();}
        internal void Stop(){if(attached)CompositionTarget.Rendering-=Frame;attached=false;previous=-1;vw=vh=0;}
        public void Dispose(){disposed=true;Stop();}
    }
    // Playback indicator, not an audio meter: driven only by Playing and only
    // rendered while visible. Paused/idle sessions never keep a frame callback.
    internal sealed class ActivityWave : FrameworkElement,IDisposable
    {
        private bool playing,attached;private double time;
        private readonly Brush ink=Design.Brush("#C4F6D9");
        internal ActivityWave(){Width=18;Height=18;IsHitTestVisible=false;Loaded+=delegate{Update();};Unloaded+=delegate{Detach();};IsVisibleChanged+=delegate{Update();};}
        internal void Set(bool value){playing=value;Update();InvalidateVisual();}
        private void Update(){bool needed=playing&&IsVisible&&IsLoaded&&Motion.Enabled;if(needed&&!attached){attached=true;CompositionTarget.Rendering+=Frame;}else if(!needed)Detach();}
        private void Frame(object sender,EventArgs args){if(!Motion.Enabled||!IsVisible){Detach();return;}double now=((RenderingEventArgs)args).RenderingTime.TotalSeconds;if(now-time<1.0/31)return;time=now;InvalidateVisual();}
        private void Detach(){if(attached)CompositionTarget.Rendering-=Frame;attached=false;}
        protected override void OnRender(DrawingContext dc){for(int i=0;i<4;i++){double h=playing?4+9*(.5+.5*Math.Sin((Motion.Enabled?time:0)*4+i*1.7)):3;dc.DrawRoundedRectangle(ink,null,new Rect(i*4.2,(18-h)/2,2.4,h),1.2,1.2);}}
        public void Dispose(){playing=false;Detach();}
    }
}

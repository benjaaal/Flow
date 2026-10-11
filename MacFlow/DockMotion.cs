using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MacFlow
{
    // One time-based hover controller; mouse events only replace a target point.
    // No per-event animation clocks, allocations or layout changes.
    public sealed class DockMotion : IDisposable
    {
        private sealed class Item {public FrameworkElement Cell;public ScaleTransform Scale;public double Center,Velocity;}
        private readonly StackPanel panel;
        private readonly List<Item> items=new List<Item>();
        private readonly bool vertical;
        private readonly double reach;
        private readonly double magnification;
        internal static double PeakScale(double size){return 1.28-Math.Max(0,Math.Min(36,size-36))*.08/36;}
        private bool hovering,listening,disposed,centersReady;
        private double pointer,previous=-1;
        public DockMotion(StackPanel panel,bool vertical,double size)
        {
            this.panel=panel;this.vertical=vertical;reach=size*2.0;magnification=PeakScale(size)-1;
            foreach(var cell in panel.Children.OfType<StackPanel>())
            {
                var icon=cell.Tag as FrameworkElement;if(icon==null)continue;var group=icon.RenderTransform as TransformGroup;var scale=icon.RenderTransform as ScaleTransform;
                if(scale==null&&group!=null)scale=group.Children.OfType<ScaleTransform>().FirstOrDefault();if(scale!=null)items.Add(new Item{Cell=cell,Scale=scale});
            }
            panel.SizeChanged+=SizeChanged;
        }
        private void SizeChanged(object sender,SizeChangedEventArgs e){centersReady=false;}
        public void Move(Point point){if(disposed)return;pointer=vertical?point.Y:point.X;hovering=true;Start();}
        public void Probe(double fraction){Move(new Point(panel.ActualWidth*fraction,panel.ActualHeight*fraction));}
        public void Leave(){if(disposed)return;hovering=false;Start();}
        private void Start()
        {
            if(!Design.Animations){Step(0);Stop();return;}
            if(listening)return;previous=-1;listening=true;CompositionTarget.Rendering+=Render;
        }
        private void Render(object sender,EventArgs args)
        {
            double now=((RenderingEventArgs)args).RenderingTime.TotalSeconds;
            if(previous>=0&&now<=previous)return;
            double dt=previous<0?1.0/60:Math.Min(.05,now-previous);previous=now;
            if(!Step(Design.Animations?dt:0))Stop();
        }
        private bool Step(double seconds)
        {
            if(!centersReady)
            {
                foreach(var item in items){var center=item.Cell.TranslatePoint(new Point(item.Cell.ActualWidth/2,item.Cell.ActualHeight/2),panel);item.Center=vertical?center.Y:center.X;}centersReady=true;
            }
            bool changing=false;
            foreach(var item in items)
            {
                double distance=hovering?Math.Abs(pointer-item.Center):reach;
                double target=distance>=reach?1:1+magnification*(1+Math.Cos(Math.PI*distance/reach))/2;
                double current=item.Scale.ScaleX,next=target;
                if(seconds>0){double velocity=item.Velocity;SpringStep(ref current,ref velocity,target,seconds);next=Math.Max(1,Math.Min(1+magnification,current));item.Velocity=next!=current?0:velocity;}else item.Velocity=0;
                current=item.Scale.ScaleX;
                if(Math.Abs(target-next)<.0005&&Math.Abs(item.Velocity)<.01){next=target;item.Velocity=0;}else changing=true;
                if(Math.Abs(current-next)>.0001)item.Scale.ScaleX=item.Scale.ScaleY=next;
            }
            return changing;
        }
        // Exact solution of a damped spring, retaining velocity across retargets.
        // Independent from layout and frame rate; no new clocks on mouse movement.
        internal static void SpringStep(ref double value,ref double velocity,double target,double seconds)
        {
            const double omega=25,damping=.88;double rate=omega*damping,frequency=omega*Math.Sqrt(1-damping*damping);
            double offset=value-target,b=(velocity+rate*offset)/frequency,t=Math.Min(.05,Math.Max(0,seconds));
            double decay=Math.Exp(-rate*t),cos=Math.Cos(frequency*t),sin=Math.Sin(frequency*t),wave=offset*cos+b*sin;
            value=target+decay*wave;velocity=decay*(-rate*wave-offset*frequency*sin+b*frequency*cos);
        }
        private void Stop(){if(!listening)return;CompositionTarget.Rendering-=Render;listening=false;previous=-1;}
        public void Dispose(){disposed=true;Stop();panel.SizeChanged-=SizeChanged;}
    }
}

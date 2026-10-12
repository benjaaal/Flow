using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;

namespace MacFlow
{
    internal static class DockMetrics
    {
        internal const double Padding=6,Indicator=6,LaunchTravel=4;
        internal static double Slot(double size){return size+12;}
        internal static double Row(double size){double peak=DockMotion.PeakScale(size);return Math.Ceiling(size*peak*1.035+LaunchTravel*peak)+2;}
        internal static double Thickness(double size){return Row(size)+Indicator+Padding*2;}
        internal static double Reserve(double size){return Math.Ceiling(size*.32)+2;}
    }
    // One time-based hover controller; mouse events only replace a target point.
    // No per-event animation clocks, allocations or layout changes.
    public sealed class DockMotion : IDisposable
    {
        private sealed class Item {public FrameworkElement Cell;public ScaleTransform Scale;public TranslateTransform Shift;public double Center,Velocity,ShiftVelocity,Extra;}
        private readonly StackPanel panel;
        private readonly List<Item> items=new List<Item>();
        private FrameworkElement separator;private TranslateTransform separatorShift;private double separatorCenter,separatorVelocity;
        private readonly bool vertical;
        private readonly double reach;
        private readonly double magnification;
        internal static double PeakScale(double size){return 1.28-Math.Max(0,Math.Min(36,size-36))*.08/36;}
        private bool hovering,listening,disposed,centersReady,probing;
        private double pointer,previous=-1,probeFraction;
        public DockMotion(StackPanel panel,bool vertical,double size,DockMotion prior=null)
        {
            this.panel=panel;this.vertical=vertical;reach=size*2.0;magnification=PeakScale(size)-1;
            foreach(var cell in panel.Children.OfType<StackPanel>())
            {
                var icon=cell.Tag as FrameworkElement;if(icon==null)continue;var group=icon.RenderTransform as TransformGroup;var scale=icon.RenderTransform as ScaleTransform;
                if(scale==null&&group!=null)scale=group.Children.OfType<ScaleTransform>().FirstOrDefault();if(scale!=null){var shift=new TranslateTransform();cell.RenderTransform=shift;items.Add(new Item{Cell=cell,Scale=scale,Shift=shift});}
            }
            panel.SizeChanged+=SizeChanged;
            separator=panel.Children.OfType<Border>().FirstOrDefault();if(separator!=null){separatorShift=new TranslateTransform();separator.RenderTransform=separatorShift;}
            if(prior!=null)
            {
                hovering=prior.hovering;probing=prior.probing;probeFraction=prior.probeFraction;
                foreach(var item in items){var key=((FrameworkElement)item.Cell.Tag).Tag;var old=prior.items.FirstOrDefault(value=>Object.Equals(((FrameworkElement)value.Cell.Tag).Tag,key));if(old==null)continue;item.Scale.ScaleX=item.Scale.ScaleY=old.Scale.ScaleX;item.Velocity=old.Velocity;item.Shift.X=old.Shift.X;item.Shift.Y=old.Shift.Y;item.ShiftVelocity=old.ShiftVelocity;}
                panel.Loaded+=Loaded;
            }
        }
        private void Loaded(object sender,RoutedEventArgs e){panel.Loaded-=Loaded;if(disposed)return;pointer=probing?(vertical?panel.ActualHeight:panel.ActualWidth)*probeFraction:vertical?Mouse.GetPosition(panel).Y:Mouse.GetPosition(panel).X;Start();}
        private void SizeChanged(object sender,SizeChangedEventArgs e){centersReady=false;}
        public void Move(Point point){if(disposed||probing)return;pointer=vertical?point.Y:point.X;hovering=true;Start();}
        public void Probe(double fraction){if(disposed)return;probing=true;probeFraction=fraction;pointer=(vertical?panel.ActualHeight:panel.ActualWidth)*fraction;hovering=true;Start();}
        public void EndProbe(){probing=false;Leave();}
        public void Leave(){if(disposed||probing)return;hovering=false;Start();}
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
                foreach(var item in items){var center=item.Cell.TranslatePoint(new Point(item.Cell.ActualWidth/2,item.Cell.ActualHeight/2),panel);item.Center=vertical?center.Y-item.Shift.Y:center.X-item.Shift.X;}if(separator!=null){var point=separator.TranslatePoint(new Point(separator.ActualWidth/2,separator.ActualHeight/2),panel);separatorCenter=vertical?point.Y-separatorShift.Y:point.X-separatorShift.X;}centersReady=true;
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
                // Expand visual spacing without measuring or reflowing the row.
                item.Extra=(next-1)*(item.Cell.Width-12);
            }
            double total=items.Sum(item=>item.Extra),prefix=0;
            foreach(var item in items)
            {
                double destination=prefix+item.Extra*.5-total*.5;prefix+=item.Extra;double position=vertical?item.Shift.Y:item.Shift.X,velocity=item.ShiftVelocity;
                Motion.Spring(ref position,ref velocity,destination,seconds,28,.97);
                if(Math.Abs(destination-position)<.015&&Math.Abs(velocity)<.05){position=destination;velocity=0;}else changing=true;
                item.ShiftVelocity=velocity;if(vertical)item.Shift.Y=position;else item.Shift.X=position;
            }
            if(separator!=null){double target=items.Where(item=>item.Center<separatorCenter).Sum(item=>item.Extra)-total*.5,position=vertical?separatorShift.Y:separatorShift.X;Motion.Spring(ref position,ref separatorVelocity,target,seconds,28,.97);if(Math.Abs(target-position)<.015&&Math.Abs(separatorVelocity)<.05){position=target;separatorVelocity=0;}else changing=true;if(vertical)separatorShift.Y=position;else separatorShift.X=position;}
            return changing;
        }
        // Exact solution of a damped spring, retaining velocity across retargets.
        // Independent from layout and frame rate; no new clocks on mouse movement.
        internal static void SpringStep(ref double value,ref double velocity,double target,double seconds)
        {
            Motion.Spring(ref value,ref velocity,target,seconds);
        }
        private void Stop(){if(!listening)return;CompositionTarget.Rendering-=Render;listening=false;previous=-1;}
        public void Dispose(){disposed=true;Stop();panel.SizeChanged-=SizeChanged;panel.Loaded-=Loaded;}
    }
}

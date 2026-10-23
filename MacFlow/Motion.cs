using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media.Animation;
namespace MacFlow
{
    // Exact, time-based springs shared by gestures, controls and window clocks.
    internal static class Motion
    {
        internal const int Hover=210,Press=120,Menu=210,Panel=380,WindowClose=240,Content=240;
        internal const double IslandFrequency=22,IslandDamping=.82;
        internal static bool? TestClientAreaAnimations;
        internal static bool Enabled {get{return Design.Animations&&(TestClientAreaAnimations??SystemParameters.ClientAreaAnimation)&&!SystemParameters.HighContrast;}}
        private sealed class Track {internal double Start,End,Velocity,Omega,Seconds;internal long Timestamp;}
        private static readonly ConditionalWeakTable<DependencyObject,Dictionary<DependencyProperty,Track>> tracks=new ConditionalWeakTable<DependencyObject,Dictionary<DependencyProperty,Track>>();
        internal static IEasingFunction Ease(bool exit=false){return new MotionEase{Exit=exit,EasingMode=EasingMode.EaseIn};}
        internal static double Smooth(double t){t=Math.Max(0,Math.Min(1,t));return t*t*t*(t*(t*6-15)+10);}
        internal static double Arrival(double time,double damping=.94)
        {
            if(time<=0)return 0;if(time>=1)return 1;
            double value=0,velocity=0,end=0,endVelocity=0;
            Spring(ref value,ref velocity,1,time,10,damping);Spring(ref end,ref endVelocity,1,1,10,damping);return value/end;
        }
        private static void Begin(DependencyObject target,DependencyProperty property,AnimationTimeline animation)
        {var value=target as Animatable;if(value!=null)value.BeginAnimation(property,animation);else ((UIElement)target).BeginAnimation(property,animation);}
        internal static void To(DependencyObject target,DependencyProperty property,double value,int duration,bool exit=false,Action completed=null,double? initialVelocity=null)
        {
            double current=(double)target.GetValue(property),velocity=0;var map=tracks.GetOrCreateValue(target);Track prior;
            if(map.TryGetValue(property,out prior))
            {
                double elapsed=(Stopwatch.GetTimestamp()-prior.Timestamp)/(double)Stopwatch.Frequency;
                if(elapsed<prior.Seconds){double pose=prior.Start;velocity=prior.Velocity;Spring(ref pose,ref velocity,prior.End,elapsed,prior.Omega,1);}
            }
            if(initialVelocity.HasValue)velocity=initialVelocity.Value;
            Begin(target,property,null);target.SetValue(property,current);
            if(!Enabled||duration<=0){target.SetValue(property,value);map.Remove(property);if(completed!=null)completed();return;}
            var track=new Track{Start=current,End=value,Velocity=velocity,Seconds=duration/1000.0,Omega=10.5/(duration/1000.0),Timestamp=Stopwatch.GetTimestamp()};map[property]=track;
            var animation=new SpringTrack(current,value,velocity,track.Omega,track.Seconds){FillBehavior=FillBehavior.Stop};Timeline.SetDesiredFrameRate(animation,60);
            animation.Completed+=delegate{Track latest;if(map.TryGetValue(property,out latest)&&Object.ReferenceEquals(latest,track)){target.SetValue(property,value);Begin(target,property,null);map.Remove(property);if(completed!=null)completed();}};Begin(target,property,animation);
        }
        internal static void Fade(UIElement target,double from,double to,int duration)
        {
            target.BeginAnimation(UIElement.OpacityProperty,null);target.Opacity=to;if(!Enabled)return;
            var animation=new DoubleAnimation(from,to,TimeSpan.FromMilliseconds(duration)){EasingFunction=new CubicEase{EasingMode=EasingMode.EaseOut},FillBehavior=FillBehavior.Stop};Timeline.SetDesiredFrameRate(animation,60);target.BeginAnimation(UIElement.OpacityProperty,animation);
        }
        internal static void Spring(ref double value,ref double velocity,double target,double seconds,double omega=25,double damping=.94)
        {
            if(seconds<=0){value=target;velocity=0;return;}
            if(Double.IsNaN(seconds)||Double.IsInfinity(seconds)||omega<=0||damping<0)throw new ArgumentOutOfRangeException("seconds");
            double offset=value-target,t=seconds;
            if(Math.Abs(damping-1)<.000001){double u=velocity+omega*offset,decay=Math.Exp(-omega*t);value=target+(offset+u*t)*decay;velocity=(velocity-omega*u*t)*decay;return;}
            if(damping<1)
            {
                double rate=omega*damping,f=omega*Math.Sqrt(1-damping*damping),phase=(velocity+rate*offset)/f;
                double c=Math.Cos(f*t),s=Math.Sin(f*t),decay=Math.Exp(-rate*t);
                value=target+decay*(offset*c+phase*s);velocity=decay*((-rate*offset+f*phase)*c+(-rate*phase-f*offset)*s);return;
            }
            double root=Math.Sqrt(damping*damping-1),a=-omega*(damping-root),b=-omega*(damping+root),first=(velocity-b*offset)/(a-b),second=offset-first;
            value=target+first*Math.Exp(a*t)+second*Math.Exp(b*t);velocity=a*first*Math.Exp(a*t)+b*second*Math.Exp(b*t);
        }
    }
    internal sealed class MotionEase : EasingFunctionBase
    {
        internal bool Exit;
        protected override double EaseInCore(double time){return Exit?Motion.Smooth(time):Motion.Arrival(time);}
        protected override Freezable CreateInstanceCore(){return new MotionEase{Exit=Exit};}
    }
    internal sealed class SpringTrack : DoubleAnimationBase
    {
        private readonly double start,end,velocity,omega,seconds;
        internal SpringTrack(double start,double end,double velocity,double omega,double seconds){this.start=start;this.end=end;this.velocity=velocity;this.omega=omega;this.seconds=seconds;Duration=TimeSpan.FromSeconds(seconds);}
        protected override double GetCurrentValueCore(double origin,double destination,AnimationClock clock)
        {double p=clock.CurrentProgress??0;if(p<=0)return start;if(p>=1)return end;double x=start,v=velocity;Motion.Spring(ref x,ref v,end,p*seconds,omega,1);return x;}
        protected override Freezable CreateInstanceCore(){return new SpringTrack(start,end,velocity,omega,seconds);}
    }
}

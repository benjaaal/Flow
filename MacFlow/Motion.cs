using System;
using System.Windows;
using System.Windows.Media.Animation;
namespace MacFlow
{
    // Shared motion vocabulary. Continuous interaction retains velocity;
    // discrete navigation has finite clocks and exact, cancellable endpoints.
    internal static class Motion
    {
        internal const int Hover=160,Press=100,Menu=220,Panel=340,WindowClose=300,Content=240;
        // Isolated fixture processes can exercise both policies without changing
        // Windows. Normal application and installer sessions leave this unset.
        internal static bool? TestClientAreaAnimations;
        internal static bool Enabled {get{return Design.Animations&&(TestClientAreaAnimations??SystemParameters.ClientAreaAnimation)&&!SystemParameters.HighContrast;}}
        internal static IEasingFunction Ease(bool exit=false){return new CubicEase{EasingMode=exit?EasingMode.EaseIn:EasingMode.EaseOut};}
        internal static void To(Animatable target,DependencyProperty property,double value,int duration,bool exit=false,Action completed=null)
        {
            double current=(double)target.GetValue(property);target.BeginAnimation(property,null);target.SetValue(property,value);
            if(!Enabled){if(completed!=null)completed();return;}
            var animation=new DoubleAnimation(current,value,TimeSpan.FromMilliseconds(duration)){EasingFunction=Ease(exit),FillBehavior=FillBehavior.Stop};Timeline.SetDesiredFrameRate(animation,60);
            animation.Completed+=delegate{if(completed!=null)completed();};target.BeginAnimation(property,animation);
        }
        internal static void Fade(UIElement target,double from,double to,int duration)
        {
            target.BeginAnimation(UIElement.OpacityProperty,null);target.Opacity=to;if(!Enabled)return;
            var animation=new DoubleAnimation(from,to,TimeSpan.FromMilliseconds(duration)){EasingFunction=Ease(),FillBehavior=FillBehavior.Stop};Timeline.SetDesiredFrameRate(animation,60);target.BeginAnimation(UIElement.OpacityProperty,animation);
        }
        internal static void Spring(ref double value,ref double velocity,double target,double seconds,double omega=25,double damping=.94)
        {
            if(seconds<=0){value=target;velocity=0;return;}
            double rate=omega*damping,frequency=omega*Math.Sqrt(1-damping*damping),offset=value-target,b=(velocity+rate*offset)/frequency,t=Math.Min(.05,seconds);
            double decay=Math.Exp(-rate*t),cos=Math.Cos(frequency*t),sin=Math.Sin(frequency*t),wave=offset*cos+b*sin;
            value=target+decay*wave;velocity=decay*(-rate*wave-offset*frequency*sin+b*frequency*cos);
        }
    }
}

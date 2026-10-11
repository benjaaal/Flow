using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
namespace MacFlow
{
    internal static class DockAppMotion
    {
        // Separate from the hover scale: launching never fights magnification.
        internal static Border Host(UIElement content)
        {
            var group=new TransformGroup();group.Children.Add(new ScaleTransform(1,1));group.Children.Add(new TranslateTransform());
            return new Border{Child=content,RenderTransform=group,RenderTransformOrigin=new Point(.5,1)};
        }
        internal static void Play(FrameworkElement icon,bool closing,bool remove=false)
        {
            var outer=icon as Border;var host=outer==null?null:outer.Child as Border;if(host==null)return;
            var group=host.RenderTransform as TransformGroup;if(group==null||group.Children.Count!=2)return;
            var scale=(ScaleTransform)group.Children[0];var shift=(TranslateTransform)group.Children[1];
            scale.BeginAnimation(ScaleTransform.ScaleXProperty,null);scale.BeginAnimation(ScaleTransform.ScaleYProperty,null);shift.BeginAnimation(TranslateTransform.YProperty,null);host.BeginAnimation(UIElement.OpacityProperty,null);
            scale.ScaleX=scale.ScaleY=1;shift.Y=0;host.Opacity=1;if(!Design.Animations)return;
            if(closing&&remove)
            {
                var curve=new CubicEase{EasingMode=EasingMode.EaseInOut};var shrink=new DoubleAnimation(.86,TimeSpan.FromMilliseconds(220)){EasingFunction=curve};Timeline.SetDesiredFrameRate(shrink,60);scale.BeginAnimation(ScaleTransform.ScaleXProperty,shrink);scale.BeginAnimation(ScaleTransform.ScaleYProperty,shrink);shift.BeginAnimation(TranslateTransform.YProperty,new DoubleAnimation(4,TimeSpan.FromMilliseconds(220)){EasingFunction=curve});host.BeginAnimation(UIElement.OpacityProperty,new DoubleAnimation(0,TimeSpan.FromMilliseconds(220)){EasingFunction=curve});return;
            }
            int length=closing?260:400,peak=closing?90:150;
            scale.BeginAnimation(ScaleTransform.ScaleXProperty,Curve(closing?.975:1.02,peak,length));
            scale.BeginAnimation(ScaleTransform.ScaleYProperty,Curve(closing?.975:1.035,peak,length));
            shift.BeginAnimation(TranslateTransform.YProperty,Curve(closing?0:-4,peak,length,0));
            host.BeginAnimation(UIElement.OpacityProperty,Curve(closing?.76:.82,closing?90:70,length));
        }
        private static DoubleAnimationUsingKeyFrames Curve(double peak,int at,int length,double rest=1)
        {
            var motion=new DoubleAnimationUsingKeyFrames{FillBehavior=FillBehavior.Stop};
            motion.KeyFrames.Add(new EasingDoubleKeyFrame(rest,KeyTime.FromTimeSpan(TimeSpan.Zero)));
            motion.KeyFrames.Add(new EasingDoubleKeyFrame(peak,KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(at)),new CubicEase{EasingMode=EasingMode.EaseOut}));
            motion.KeyFrames.Add(new EasingDoubleKeyFrame(rest,KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(length)),new CubicEase{EasingMode=EasingMode.EaseInOut}));
            Timeline.SetDesiredFrameRate(motion,60);return motion;
        }
    }
}

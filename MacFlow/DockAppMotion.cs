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
            double sx=scale.ScaleX,sy=scale.ScaleY,y=shift.Y,opacity=host.Opacity;
            scale.BeginAnimation(ScaleTransform.ScaleXProperty,null);scale.BeginAnimation(ScaleTransform.ScaleYProperty,null);shift.BeginAnimation(TranslateTransform.YProperty,null);host.BeginAnimation(UIElement.OpacityProperty,null);
            scale.ScaleX=scale.ScaleY=1;shift.Y=0;host.Opacity=closing&&remove&&!Motion.Enabled?0:1;if(!Motion.Enabled)return;
            if(closing&&remove)
            {
                var curve=Motion.Ease(true);var shrink=new DoubleAnimation(sx,.94,TimeSpan.FromMilliseconds(220)){EasingFunction=curve};Timeline.SetDesiredFrameRate(shrink,60);scale.BeginAnimation(ScaleTransform.ScaleXProperty,shrink);scale.BeginAnimation(ScaleTransform.ScaleYProperty,new DoubleAnimation(sy,.94,TimeSpan.FromMilliseconds(220)){EasingFunction=curve});shift.BeginAnimation(TranslateTransform.YProperty,new DoubleAnimation(y,3,TimeSpan.FromMilliseconds(220)){EasingFunction=curve});host.BeginAnimation(UIElement.OpacityProperty,new DoubleAnimation(opacity,0,TimeSpan.FromMilliseconds(220)){EasingFunction=curve});return;
            }
            int length=closing?280:400,peak=closing?100:145;
            scale.BeginAnimation(ScaleTransform.ScaleXProperty,Curve(closing?1.008:1.018,peak,length,1,sx));
            scale.BeginAnimation(ScaleTransform.ScaleYProperty,Curve(closing?1.008:1.018,peak,length,1,sy));
            shift.BeginAnimation(TranslateTransform.YProperty,Curve(closing?1.5:-4,peak,length,0,y));
            host.BeginAnimation(UIElement.OpacityProperty,Curve(closing?.94:1,closing?90:70,length,1,opacity));
        }
        private static DoubleAnimationUsingKeyFrames Curve(double peak,int at,int length,double rest=1,double start=1)
        {
            var motion=new DoubleAnimationUsingKeyFrames{FillBehavior=FillBehavior.Stop};
            motion.KeyFrames.Add(new EasingDoubleKeyFrame(start,KeyTime.FromTimeSpan(TimeSpan.Zero)));
            motion.KeyFrames.Add(new EasingDoubleKeyFrame(peak,KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(at)),Motion.Ease()));
            motion.KeyFrames.Add(new EasingDoubleKeyFrame(rest,KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(length)),Motion.Ease()));
            Timeline.SetDesiredFrameRate(motion,60);return motion;
        }
    }
}

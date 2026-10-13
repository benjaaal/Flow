using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
namespace MacFlow
{
    internal static class ContentTransition
    {
        // Animate the incoming controls, never a screenshot of UI.
        // Only one page is visible, including during rapid navigation.
        internal static void Show(Grid host,FrameworkElement previous,FrameworkElement incoming,int direction)
        {
            host.Children.Clear();host.ClipToBounds=true;incoming.IsHitTestVisible=true;
            if(previous==null||!Motion.Enabled){host.Children.Add(incoming);return;}
            previous.IsHitTestVisible=false;
            host.Children.Add(incoming);
            var shift=new TranslateTransform(direction*10,0);var scale=new ScaleTransform(.985,.985);var transforms=new TransformGroup();transforms.Children.Add(scale);transforms.Children.Add(shift);incoming.RenderTransform=transforms;incoming.RenderTransformOrigin=new Point(.5,.35);
            Motion.To(scale,ScaleTransform.ScaleXProperty,1,Motion.Content);Motion.To(scale,ScaleTransform.ScaleYProperty,1,Motion.Content);
            var movement=new DoubleAnimation(direction*10,0,TimeSpan.FromMilliseconds(Motion.Content)){EasingFunction=Motion.Ease(),FillBehavior=FillBehavior.Stop};Timeline.SetDesiredFrameRate(movement,60);shift.BeginAnimation(TranslateTransform.XProperty,movement);
            Motion.Fade(incoming,.18,1,Motion.Content);
        }
    }
}

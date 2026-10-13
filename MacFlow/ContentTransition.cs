using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
namespace MacFlow
{
    internal static class ContentTransition
    {
        // Animate actual outgoing/incoming controls, never a screenshot of UI.
        // At most two pages exist, even during rapid navigation.
        internal static void Show(Grid host,FrameworkElement previous,FrameworkElement incoming,int direction)
        {
            host.Children.Clear();host.ClipToBounds=true;incoming.IsHitTestVisible=true;
            if(previous==null||!Motion.Enabled){host.Children.Add(incoming);return;}
            previous.IsHitTestVisible=false;previous.VerticalAlignment=VerticalAlignment.Top;
            incoming.Measure(new Size(Math.Max(1,host.ActualWidth),Double.PositiveInfinity));
            previous.MaxHeight=Math.Max(1,incoming.DesiredSize.Height);previous.ClipToBounds=true;
            host.Children.Add(previous);host.Children.Add(incoming);
            var shift=new TranslateTransform(direction*10,0);var scale=new ScaleTransform(.985,.985);var transforms=new TransformGroup();transforms.Children.Add(scale);transforms.Children.Add(shift);incoming.RenderTransform=transforms;incoming.RenderTransformOrigin=new Point(.5,.35);
            Motion.To(scale,ScaleTransform.ScaleXProperty,1,Motion.Content);Motion.To(scale,ScaleTransform.ScaleYProperty,1,Motion.Content);
            var movement=new DoubleAnimation(direction*10,0,TimeSpan.FromMilliseconds(Motion.Content)){EasingFunction=Motion.Ease(),FillBehavior=FillBehavior.Stop};Timeline.SetDesiredFrameRate(movement,60);shift.BeginAnimation(TranslateTransform.XProperty,movement);
            Motion.Fade(incoming,.65,1,Motion.Content);
            var fade=new DoubleAnimation(previous.Opacity,0,TimeSpan.FromMilliseconds(Motion.Content*.65)){EasingFunction=Motion.Ease(true)};Timeline.SetDesiredFrameRate(fade,60);fade.Completed+=delegate{host.Children.Remove(previous);};previous.BeginAnimation(UIElement.OpacityProperty,fade);
        }
    }
}

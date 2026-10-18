using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
namespace MacFlow
{
    // Original typography, stretched as one geometry; no Apple font or artwork.
    internal sealed class PrivacyClock : FrameworkElement
    {
        private string text="";
        internal void Set(string value){if(text==value)return;text=value;InvalidateVisual();}
        protected override void OnRender(DrawingContext dc)
        {
            if(ActualWidth<1||ActualHeight<1)return;
            var formatted=new FormattedText(text,CultureInfo.InvariantCulture,FlowDirection.LeftToRight,new Typeface(new FontFamily("Segoe UI Variable Display, Segoe UI"),FontStyles.Normal,FontWeights.SemiBold,FontStretches.Condensed),140,Brushes.White,VisualTreeHelper.GetDpi(this).PixelsPerDip);
            var shape=formatted.BuildGeometry(new Point());var bounds=shape.Bounds;if(bounds.IsEmpty)return;
            var transform=new TransformGroup();transform.Children.Add(new TranslateTransform(-bounds.Left,-bounds.Top));transform.Children.Add(new ScaleTransform((ActualWidth-4)/bounds.Width,(ActualHeight-4)/bounds.Height));transform.Children.Add(new TranslateTransform(2,2));shape.Transform=transform;
            var glass=new LinearGradientBrush{StartPoint=new Point(0,0),EndPoint=new Point(0,1)};glass.GradientStops.Add(new GradientStop(Color.FromArgb(225,245,250,255),0));glass.GradientStops.Add(new GradientStop(Color.FromArgb(164,237,245,255),.6));glass.GradientStops.Add(new GradientStop(Color.FromArgb(104,236,242,248),1));dc.DrawGeometry(glass,new Pen(Design.Brush("#58FFFFFF"),.8),shape);
        }
    }
}

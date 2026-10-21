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
        private FontWeight weight=FontWeights.SemiBold;private Brush ink=Brushes.White;
        internal void Appearance(string style,string color){weight=style=="Regular"?FontWeights.Normal:style=="Medium"?FontWeights.Medium:style=="Bold"?FontWeights.Bold:FontWeights.SemiBold;ink=Design.Brush(color??"#FFFFFF");InvalidateVisual();}
        internal void Set(string value){if(text==value)return;text=value;InvalidateVisual();}
        protected override void OnRender(DrawingContext dc)
        {
            if(ActualWidth<1||ActualHeight<1)return;
            var formatted=new FormattedText(text,CultureInfo.InvariantCulture,FlowDirection.LeftToRight,new Typeface(new FontFamily("Segoe UI Variable Display, Segoe UI"),FontStyles.Normal,weight,FontStretches.Normal),140,ink,VisualTreeHelper.GetDpi(this).PixelsPerDip);
            var shape=formatted.BuildGeometry(new Point());var bounds=shape.Bounds;if(bounds.IsEmpty)return;
            double scale=Math.Min((ActualWidth-4)/bounds.Width,(ActualHeight-4)/bounds.Height);var transform=new TransformGroup();transform.Children.Add(new TranslateTransform(-bounds.Left,-bounds.Top));transform.Children.Add(new ScaleTransform(scale,scale));transform.Children.Add(new TranslateTransform((ActualWidth-bounds.Width*scale)/2,(ActualHeight-bounds.Height*scale)/2));shape.Transform=transform;
            dc.DrawGeometry(ink,null,shape);
        }
    }
}

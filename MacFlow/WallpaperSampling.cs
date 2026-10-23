using System;
using System.Windows;
using System.Windows.Controls;
namespace MacFlow
{
    internal static class WallpaperSampling
    {
        internal static Image Photo(FrameworkElement source)
        {var image=source as Image;if(image!=null)return image;var panel=source as Panel;return panel!=null&&panel.Children.Count==1?panel.Children[0] as Image:null;}
        internal static Rect Coordinates(FrameworkElement target,Image image)
        {
            var bounds=target.TransformToVisual(image).TransformBounds(new Rect(0,0,target.ActualWidth,target.ActualHeight));
            double width=image.Source.Width,height=image.Source.Height;
            double scale=Math.Max(image.ActualWidth/width,image.ActualHeight/height);
            double x=(width-image.ActualWidth/scale)/2,y=(height-image.ActualHeight/scale)/2;
            return new Rect((x+bounds.Left/scale)/width,(y+bounds.Top/scale)/height,bounds.Width/scale/width,bounds.Height/scale/height);
        }
    }
}

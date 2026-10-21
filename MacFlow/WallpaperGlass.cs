using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
namespace MacFlow
{
    // This material samples only our already displayed wallpaper visual. It
    // does not capture the screen or blur foreground icons/text. No outline.
    internal sealed class WallpaperGlass : Grid
    {
        private readonly FrameworkElement wallpaper;private readonly double radius;
        private readonly VisualBrush backdrop;
        internal WallpaperGlass(FrameworkElement source,UIElement content,double corner,Thickness padding,bool dark)
        {
            wallpaper=source;radius=corner;backdrop=new VisualBrush(source){ViewboxUnits=BrushMappingMode.Absolute,Stretch=Stretch.Fill,AutoLayoutContent=false};
            Children.Add(new Border{Background=backdrop,Effect=new BlurEffect{Radius=12,RenderingBias=RenderingBias.Performance},IsHitTestVisible=false,CacheMode=new BitmapCache()});
            Children.Add(new Border{Background=Design.Brush(dark?"#50162230":"#54DAE2EA"),IsHitTestVisible=false});
            Children.Add(new Border{Padding=padding,Child=content});Loaded+=delegate{Update();};SizeChanged+=delegate{Update();};
        }
        private void Update(){if(!IsLoaded||ActualWidth<=0||ActualHeight<=0)return;Clip=new RectangleGeometry(new Rect(0,0,ActualWidth,ActualHeight),radius,radius);try{backdrop.Viewbox=TransformToVisual(wallpaper).TransformBounds(new Rect(0,0,ActualWidth,ActualHeight));}catch(InvalidOperationException){}}
    }
}

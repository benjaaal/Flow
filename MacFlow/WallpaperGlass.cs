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
        private readonly VisualBrush backdrop;private readonly Border sample;private ImageBrush photoBrush;private LiquidLens lens;
        internal WallpaperGlass(FrameworkElement source,UIElement content,double corner,Thickness padding,bool dark)
        {
            wallpaper=source;radius=corner;backdrop=new VisualBrush(source){ViewboxUnits=BrushMappingMode.Absolute,Stretch=Stretch.Fill,AutoLayoutContent=false};
            var optical=new Grid{IsHitTestVisible=false};sample=new Border{Background=backdrop,Effect=new BlurEffect{Radius=12,RenderingBias=RenderingBias.Performance},IsHitTestVisible=false,CacheMode=new BitmapCache(.75)};optical.Children.Add(sample);if(!SystemParameters.HighContrast&&RenderCapability.IsPixelShaderVersionSupported(3,0)){lens=new LiquidLens{Radius=corner,Strength=1.4,Tint=Color.FromArgb(8,255,255,255)};optical.Effect=lens;}Children.Add(optical);
            Children.Add(new Border{Background=Design.Brush(dark?"#4214161A":"#38DAE2EA"),IsHitTestVisible=false});

            Children.Add(new Border{Padding=padding,Child=content});Loaded+=delegate{Update();};SizeChanged+=delegate{Update();};
        }
        internal void Update()
        {
            if(!IsLoaded||ActualWidth<=0||ActualHeight<=0)return;if(lens!=null)lens.Size=new Point(ActualWidth,ActualHeight);
            var shape=Clip as RectangleGeometry;if(shape==null||shape.Rect.Width!=ActualWidth||shape.Rect.Height!=ActualHeight)Clip=new RectangleGeometry(new Rect(0,0,ActualWidth,ActualHeight),radius,radius);
            try
            {
                var image=WallpaperSampling.Photo(wallpaper);
                if(image!=null&&image.Source!=null&&image.ActualWidth>0&&image.ActualHeight>0)
                {
                    if(photoBrush==null||photoBrush.ImageSource!=image.Source){photoBrush=new ImageBrush(image.Source){ViewboxUnits=BrushMappingMode.RelativeToBoundingBox,Stretch=Stretch.Fill};sample.Background=photoBrush;}
                    var crop=WallpaperSampling.Coordinates(this,image);if(photoBrush.Viewbox!=crop)photoBrush.Viewbox=crop;
                }
                else backdrop.Viewbox=TransformToVisual(wallpaper).TransformBounds(new Rect(0,0,ActualWidth,ActualHeight));
            }
            catch(InvalidOperationException){}
        }
    }
}

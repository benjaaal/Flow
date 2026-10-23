using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
namespace MacFlow
{
    // The clock refracts only its own wallpaper. Geometry is cached until the
    // minute, size or chosen weight changes; there is no idle render callback.
    internal sealed class PrivacyClock : FrameworkElement
    {
        private string text="";private FontWeight weight=FontWeights.Normal;
        private Color color=Colors.White;private Geometry shape;private FrameworkElement wallpaper;
        private LockGlass glass;private ImageBrush photoBrush;
        internal void AttachWallpaper(FrameworkElement source)
        {
            wallpaper=source;
            if(!SystemParameters.HighContrast&&RenderCapability.IsPixelShaderVersionSupported(3,0))
            {glass=new LockGlass{Backdrop=new VisualBrush(source){Stretch=Stretch.Fill,AutoLayoutContent=false},Tint=color};Effect=glass;}
            Loaded+=delegate{UpdateMaterial();};SizeChanged+=delegate{shape=null;UpdateMaterial();InvalidateVisual();};
        }
        internal bool Refractive{get{return glass!=null;}}
        internal void UpdateMaterial()
        {
            if(glass==null||wallpaper==null||wallpaper.ActualWidth<1||wallpaper.ActualHeight<1)return;
            try{var image=WallpaperSampling.Photo(wallpaper);if(image!=null&&image.Source!=null&&image.ActualWidth>0&&image.ActualHeight>0){if(photoBrush==null||photoBrush.ImageSource!=image.Source){photoBrush=new ImageBrush(image.Source){Stretch=Stretch.Fill};glass.Backdrop=photoBrush;}var crop=WallpaperSampling.Coordinates(this,image);glass.Offset=new Point(crop.Left,crop.Top);glass.Extent=new Point(crop.Width,crop.Height);glass.Size=new Point(ActualWidth,ActualHeight);return;}var bounds=TransformToVisual(wallpaper).TransformBounds(new Rect(0,0,ActualWidth,ActualHeight));glass.Offset=new Point(bounds.Left/wallpaper.ActualWidth,bounds.Top/wallpaper.ActualHeight);glass.Extent=new Point(bounds.Width/wallpaper.ActualWidth,bounds.Height/wallpaper.ActualHeight);glass.Size=new Point(ActualWidth,ActualHeight);}catch(InvalidOperationException){}
        }
        internal void Appearance(string style,string ink)
        {weight=style=="Regular"?FontWeights.Normal:style=="Medium"?FontWeights.Medium:style=="Bold"?FontWeights.Bold:FontWeights.SemiBold;color=(Color)ColorConverter.ConvertFromString(ink??"#FFFFFF");if(glass!=null)glass.Tint=color;shape=null;InvalidateVisual();}
        internal void Set(string value){if(text==value)return;text=value;shape=null;InvalidateVisual();}
        protected override void OnRender(DrawingContext dc)
        {
            if(ActualWidth<8||ActualHeight<8)return;
            if(shape==null)
            {
                var formatted=new FormattedText(text,CultureInfo.InvariantCulture,FlowDirection.LeftToRight,new Typeface(new FontFamily("Segoe UI Variable Display, Segoe UI"),FontStyles.Normal,weight,FontStretches.Normal),180,Brushes.White,VisualTreeHelper.GetDpi(this).PixelsPerDip);
                shape=formatted.BuildGeometry(new Point());var b=shape.Bounds;if(b.IsEmpty)return;
                double s=Math.Min((ActualWidth-8)/b.Width,(ActualHeight-8)/b.Height);shape.Transform=new MatrixTransform(s,0,0,s,(ActualWidth-b.Width*s)/2-b.Left*s,(ActualHeight-b.Height*s)/2-b.Top*s);shape.Freeze();
            }
            dc.DrawRectangle(Brushes.Transparent,null,new Rect(0,0,ActualWidth,ActualHeight));
            if(glass!=null){UpdateMaterial();dc.DrawGeometry(Brushes.White,null,shape);}
            else if(SystemParameters.HighContrast)dc.DrawGeometry(SystemColors.WindowTextBrush,null,shape);
            else{var fill=new LinearGradientBrush(Color.FromArgb(85,color.R,color.G,color.B),Color.FromArgb(145,color.R,color.G,color.B),90);dc.DrawGeometry(fill,null,shape);}
        }
    }
}

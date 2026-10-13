using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
namespace MacFlow
{
    // Individual glass pieces reuse the window's captured texture. They never
    // capture the desktop independently, and their content stays outside effects.
    public sealed class GlassPiece : Grid
    {
        private readonly Border plate;
        private readonly Border lensLayer;
        private readonly ImageBrush backdrop=new ImageBrush {ViewboxUnits=BrushMappingMode.Absolute,Stretch=Stretch.Fill};
        private LiquidLens lens;
        private readonly double radius;
        private LiquidMaterial material;
        private Window owner;
        private Rect previous;
        public GlassPiece(UIElement child,double corner,Thickness padding)
        {
            radius=corner;plate=new Border {Background=backdrop,IsHitTestVisible=false};
            lensLayer=new Border {Child=plate,IsHitTestVisible=false,Visibility=Visibility.Collapsed};
            Children.Add(lensLayer);
            var face=new Border{Child=child,CornerRadius=new CornerRadius(corner),Padding=padding,BorderThickness=new Thickness(0)};face.SetResourceReference(Border.BackgroundProperty,"GlassTileSurface");Children.Add(face);
            Children.Add(new CrystalLight(corner,.6));
            Loaded+=delegate {owner=Window.GetWindow(this);material=LiquidMaterial.ForWindow(owner);if(material!=null)material.BackdropChanged+=Bind;Bind();};
            Unloaded+=delegate {if(material!=null)material.BackdropChanged-=Bind;material=null;owner=null;backdrop.ImageSource=null;};
            SizeChanged+=delegate {lensLayer.Clip=new RectangleGeometry(new Rect(0,0,Math.Max(1,ActualWidth),Math.Max(1,ActualHeight)),radius,radius);if(lens!=null)lens.Size=new Point(Math.Max(1,ActualWidth),Math.Max(1,ActualHeight));Bind();};
            // Hover light lives in the button overlay. Keep the optical texture
            // cached instead of invalidating its shader on every mouse event.
        }
        private void Bind()
        {
            // Unloaded can arrive after reparenting during a panel rebuild.
            // Skip that frame so a detached piece cannot disable its owner's glass.
            if(owner==null||!owner.IsAncestorOf(this))return;if(material==null)material=LiquidMaterial.ForWindow(owner);
            if(material==null||material.BackdropTexture==null){lensLayer.Visibility=Visibility.Collapsed;return;}
            if(lensLayer.CacheMode==null)
            {plate.Effect=new BlurEffect{Radius=3,RenderingBias=RenderingBias.Performance};lensLayer.CacheMode=new BitmapCache(.75);if(RenderCapability.IsPixelShaderVersionSupported(3,0)){lens=new LiquidLens{Radius=radius,Strength=3.5,Tint=Color.FromArgb(5,255,255,255),Size=new Point(Math.Max(1,ActualWidth),Math.Max(1,ActualHeight))};lensLayer.Effect=lens;}}
            if(backdrop.ImageSource!=material.BackdropTexture)backdrop.ImageSource=material.BackdropTexture;
            if(lens!=null&&lens.Readability!=material.Readability)lens.Readability=material.Readability;
            var source=PresentationSource.FromVisual(owner);double scale=(source==null?1:source.CompositionTarget.TransformToDevice.M11)*material.TextureScale;
            var p=TransformToAncestor(owner).Transform(new Point(0,0));
            var crop=new Rect((p.X-material.LeftInset)*scale,(p.Y-material.UpperInset)*scale,Math.Max(1,ActualWidth*scale),Math.Max(1,ActualHeight*scale));
            if(crop!=previous){previous=crop;backdrop.Viewbox=crop;}lensLayer.Visibility=Visibility.Visible;
        }
    }
}

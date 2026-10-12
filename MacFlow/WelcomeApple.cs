using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
namespace MacFlow
{
    internal sealed class WelcomeApple : ShaderEffect
    {
        private static readonly PixelShader shader=Load();
        private static PixelShader Load(){var result=new PixelShader();using(var stream=typeof(WelcomeApple).Assembly.GetManifestResourceStream("MacFlow.WelcomeApple.ps"))result.SetStreamSource(stream);result.Freeze();return result;}
        public static readonly DependencyProperty InputProperty=RegisterPixelShaderSamplerProperty("Input",typeof(WelcomeApple),0);
        public Brush Input{get{return (Brush)GetValue(InputProperty);}set{SetValue(InputProperty,value);}}
        public static readonly DependencyProperty DarkInkProperty=DependencyProperty.Register("DarkInk",typeof(double),typeof(WelcomeApple),new UIPropertyMetadata(0.0,PixelShaderConstantCallback(0)));
        public double DarkInk{get{return (double)GetValue(DarkInkProperty);}set{SetValue(DarkInkProperty,value);}}
        public static readonly DependencyProperty BackdropProperty=RegisterPixelShaderSamplerProperty("Backdrop",typeof(WelcomeApple),1);
        public Brush Backdrop{get{return (Brush)GetValue(BackdropProperty);}set{SetValue(BackdropProperty,value);}}
        public static readonly DependencyProperty OffsetProperty=DependencyProperty.Register("Offset",typeof(Point),typeof(WelcomeApple),new UIPropertyMetadata(new Point(),PixelShaderConstantCallback(1)));
        public Point Offset{get{return (Point)GetValue(OffsetProperty);}set{SetValue(OffsetProperty,value);}}
        public static readonly DependencyProperty ExtentProperty=DependencyProperty.Register("Extent",typeof(Point),typeof(WelcomeApple),new UIPropertyMetadata(new Point(1,1),PixelShaderConstantCallback(2)));
        public Point Extent{get{return (Point)GetValue(ExtentProperty);}set{SetValue(ExtentProperty,value);}}
        public static readonly DependencyProperty WallpaperProperty=DependencyProperty.Register("Wallpaper",typeof(double),typeof(WelcomeApple),new UIPropertyMetadata(0.0,PixelShaderConstantCallback(3)));
        public double Wallpaper{get{return (double)GetValue(WallpaperProperty);}set{SetValue(WallpaperProperty,value);}}
        public static readonly DependencyProperty SizeProperty=DependencyProperty.Register("Size",typeof(Point),typeof(WelcomeApple),new UIPropertyMetadata(new Point(180,180),PixelShaderConstantCallback(4)));
        public Point Size{get{return (Point)GetValue(SizeProperty);}set{SetValue(SizeProperty,value);}}
        public static readonly DependencyProperty FrostProperty=DependencyProperty.Register("Frost",typeof(double),typeof(WelcomeApple),new UIPropertyMetadata(.14,PixelShaderConstantCallback(5)));
        public double Frost{get{return (double)GetValue(FrostProperty);}set{SetValue(FrostProperty,value);}}
        internal WelcomeApple(){PixelShader=shader;UpdateShaderValue(InputProperty);UpdateShaderValue(DarkInkProperty);UpdateShaderValue(BackdropProperty);UpdateShaderValue(OffsetProperty);UpdateShaderValue(ExtentProperty);UpdateShaderValue(WallpaperProperty);UpdateShaderValue(SizeProperty);UpdateShaderValue(FrostProperty);}
    }
}

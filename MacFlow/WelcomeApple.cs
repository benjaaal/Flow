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
        internal WelcomeApple(){PixelShader=shader;UpdateShaderValue(InputProperty);UpdateShaderValue(DarkInkProperty);}
    }
}

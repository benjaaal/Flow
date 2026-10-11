using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
namespace MacFlow
{
    // An isolated backdrop fixture for comparable native-window captures.
    // This never changes the user's wallpaper or supplies fake UI.
    public sealed class VisualTestBackground : Window
    {
        public VisualTestBackground()
        {
            Title="MacFlow · Fondo de prueba";WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;ShowInTaskbar=false;WindowState=WindowState.Maximized;
            var canvas=new Grid {Background=new LinearGradientBrush(Design.Brush("#0C1529").Color,Design.Brush("#26364B").Color,55)};
            canvas.Children.Add(new Ellipse {Width=1300,Height=1200,Margin=new Thickness(-420,-460,0,0),HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top,Fill=new RadialGradientBrush(Design.Brush("#555597BD").Color,Colors.Transparent)});
            canvas.Children.Add(new Ellipse {Width=1200,Height=1200,Margin=new Thickness(0,0,-600,-850),HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Bottom,Fill=new RadialGradientBrush(Design.Brush("#6077DCC8").Color,Colors.Transparent)});
            var ribbon=new System.Windows.Shapes.Path {Data=Geometry.Parse("M-180,810 C400,940 700,550 430,170 C290,-90 430,-180 850,120 C1300,550 1440,820 1710,840"),Stroke=new LinearGradientBrush(Design.Brush("#616BF2").Color,Design.Brush("#FFB9D4").Color,35),StrokeThickness=40,Effect=new BlurEffect {Radius=25},Opacity=.72,Stretch=Stretch.Fill,Margin=new Thickness(-200,100,-200,-150)};
            canvas.Children.Add(ribbon);canvas.Children.Add(new System.Windows.Shapes.Path {Data=Geometry.Parse("M-150,160 C260,300 430,-60 810,115 C1140,290 1420,100 1890,10"),Stroke=new LinearGradientBrush(Design.Brush("#7570DD").Color,Design.Brush("#DD9BC9").Color,15),StrokeThickness=18,Effect=new BlurEffect {Radius=10},Opacity=.7,Height=250,Stretch=Stretch.Fill,VerticalAlignment=VerticalAlignment.Bottom,Margin=new Thickness(-150,0,-150,-20)});Content=canvas;
        }
    }
}

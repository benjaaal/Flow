using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace MacFlow
{
    // Familiar traffic lights with a generous hit area around each small lens.
    public sealed class WindowControls : StackPanel
    {
        public Button CloseButton { get; private set; }
        public Button MinimizeButton { get; private set; }
        public Button ZoomButton { get; private set; }
        public WindowControls(Window window,Action close):this(close,delegate{Design.Minimize(window);},delegate{window.WindowState=window.WindowState==WindowState.Maximized?WindowState.Normal:WindowState.Maximized;}){}
        public WindowControls(Action close,Action minimize,Action zoom)
        {
            Orientation=Orientation.Horizontal;
            CloseButton=Light("#FF6058","×","Cerrar · Esc",close);
            MinimizeButton=Light("#FFBD2E","−","Minimizar",minimize);
            ZoomButton=Light("#28C840","↗","Ampliar o restaurar",zoom);
            Children.Add(CloseButton);Children.Add(MinimizeButton);Children.Add(ZoomButton);
        }
        private Button Light(string color,string symbol,string label,Action action)
        {return MakeLight(color,symbol,label,action,this);}
        private static Button MakeLight(string color,string symbol,string label,Action action,UIElement hoverSurface)
        {
            var glyph=new TextBlock{Text=symbol,FontFamily=new FontFamily("Segoe UI Symbol"),FontSize=11,FontWeight=FontWeights.SemiBold,Foreground=Design.Brush("#B0182227"),HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,Opacity=0,IsHitTestVisible=false};
            var lens=new Grid{Width=13,Height=13};lens.Children.Add(new Ellipse{Fill=Design.Brush(color),Stroke=Design.Brush("#241B202A"),StrokeThickness=.65});lens.Children.Add(glyph);
            var button=Design.Button("",action,false);button.Content=lens;button.Width=25;button.Height=28;button.Padding=new Thickness(0);button.Margin=new Thickness(0);button.Background=Brushes.Transparent;button.BorderThickness=new Thickness(0);button.ToolTip=label;
            System.Windows.Automation.AutomationProperties.SetName(button,label);
            var hover=hoverSurface??button;hover.MouseEnter+=delegate{Fade(glyph,1);};hover.MouseLeave+=delegate{Fade(glyph,0);};return button;
        }
        public static Button MinimizeControl(Action action)
        {
            return MakeLight("#FFBD2E","−","Minimizar esta ventana",action,null);
        }
        private static void Fade(UIElement glyph,double value){if(!Design.Animations)glyph.Opacity=value;else glyph.BeginAnimation(UIElement.OpacityProperty,new DoubleAnimation(value,TimeSpan.FromMilliseconds(120)));}
    }
}

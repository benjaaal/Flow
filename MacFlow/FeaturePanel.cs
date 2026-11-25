using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
namespace MacFlow
{
    public abstract class FeaturePanel : Window
    {
        protected readonly DockPanel Body = new DockPanel();
        public Button CloseButton { get; private set; }
        public Button MinimizeButton { get; private set; }
        protected FeaturePanel(string name, double width, double height)
        {
            Title = "MacFlow · " + name; Width = width; Height = height; MinWidth = 340; MinHeight = 300;
            WindowStyle = WindowStyle.None; AllowsTransparency = false; ResizeMode = ResizeMode.CanResizeWithGrip;
            ShowInTaskbar = true; Topmost = false; Background = Brushes.Transparent;
            var root = new DockPanel();var header=new MacWindowChrome(this,name,delegate{if(CanCloseWindow())Design.Dismiss(this);});
            CloseButton=header.Controls.CloseButton;MinimizeButton=header.Controls.MinimizeButton;
            DockPanel.SetDock(header, System.Windows.Controls.Dock.Top); root.Children.Add(header); root.Children.Add(Body);
            Content = Design.Glass(root, Design.PanelRadius, false); SourceInitialized += delegate { Native.Glass(this, Design.Dark); };
            PreviewKeyDown += delegate(object sender, KeyEventArgs e) { if (e.Key == Key.Escape) { e.Handled = true; if(CanCloseWindow())Design.Dismiss(this); } };
        }
        protected virtual bool CanCloseWindow(){return true;}
        protected static TextBlock Caption(string text)
        { var value = Design.Text(text, 11, false); value.SetResourceReference(TextBlock.ForegroundProperty, "GlassMuted"); value.TextWrapping = TextWrapping.Wrap; return value; }
    }
}

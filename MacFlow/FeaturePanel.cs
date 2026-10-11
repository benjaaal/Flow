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
            ShowInTaskbar = true; Topmost = true; Background = Brushes.Transparent;
            var root = new DockPanel(); var header = new DockPanel { Margin = new Thickness(0, 0, 0, 18) };
            var lights = new WindowControls(this, delegate { Design.Dismiss(this); }); CloseButton = lights.CloseButton; MinimizeButton = lights.MinimizeButton;
            DockPanel.SetDock(lights, System.Windows.Controls.Dock.Left); header.Children.Add(lights);
            var label = Design.Text(name, 13, true); label.Margin = new Thickness(12, 0, 0, 0); label.VerticalAlignment = VerticalAlignment.Center; header.Children.Add(label);
            DockPanel.SetDock(header, System.Windows.Controls.Dock.Top); root.Children.Add(header); root.Children.Add(Body);
            Content = Design.Glass(root, 24, false); SourceInitialized += delegate { Native.Glass(this, Design.Dark); };
            PreviewKeyDown += delegate(object sender, KeyEventArgs e) { if (e.Key == Key.Escape) { e.Handled = true; Design.Dismiss(this); } };
        }
        protected static TextBlock Caption(string text)
        { var value = Design.Text(text, 11, false); value.SetResourceReference(TextBlock.ForegroundProperty, "GlassMuted"); value.TextWrapping = TextWrapping.Wrap; return value; }
    }
}

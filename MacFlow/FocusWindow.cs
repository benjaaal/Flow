using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
namespace MacFlow
{
    public sealed class FocusWindow : FeaturePanel
    {
        private readonly FocusController focus;
        private readonly TextBlock time, state, task;
        private readonly TextBox label;
        private readonly Button primary, reset;
        private readonly System.Windows.Shapes.Path progress;
        private readonly Ellipse full;
        private readonly StackPanel presets;
        private int minutes = 25;
        public FocusWindow(FocusController value) : base("Concentración", 420, 545)
        {
            focus = value; var content = new StackPanel { HorizontalAlignment = HorizontalAlignment.Stretch };
            task = Design.Text("Un momento para vos", 20, true); task.TextAlignment = TextAlignment.Center; task.TextTrimming = TextTrimming.CharacterEllipsis; content.Children.Add(task);
            state = Caption(""); state.TextAlignment = TextAlignment.Center; state.Margin = new Thickness(0, 6, 0, 10); content.Children.Add(state);
            var dial = new Grid { Width = 164, Height = 164, Margin = new Thickness(0, 6, 0, 15) };
            dial.Children.Add(new Ellipse { Width = 150, Height = 150, Stroke = Design.Brush(Design.Dark ? "#20FFFFFF" : "#20000000"), StrokeThickness = 4 });
            progress = new System.Windows.Shapes.Path { Stroke = Design.Brush(Design.Accent), StrokeThickness = 4, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round };
            full = new Ellipse { Width = 150, Height = 150, Stroke = Design.Brush(Design.Accent), StrokeThickness = 4, Visibility = Visibility.Collapsed }; dial.Children.Add(progress); dial.Children.Add(full);
            time = Design.Text("25:00", 42, false); time.HorizontalAlignment = HorizontalAlignment.Center; time.VerticalAlignment = VerticalAlignment.Center; time.FontFamily = new FontFamily("Segoe UI Variable Display, Segoe UI"); dial.Children.Add(time); content.Children.Add(dial);
            presets = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            foreach (int choice in new[] { 25, 45, 60 }) { int duration = choice; var button = Design.Button(choice + " min", delegate { minutes = duration; Update(); }, false); presets.Children.Add(button); } content.Children.Add(presets);
            var prompt = Caption("¿En qué te vas a concentrar?"); prompt.Margin = new Thickness(2, 12, 0, 7); content.Children.Add(prompt);
            label = new TextBox { Text = "", MaxLength = 80, FontSize = 13, Padding = new Thickness(0), Background = Brushes.Transparent, BorderThickness = new Thickness(0) }; label.SetResourceReference(Control.ForegroundProperty, "GlassInk"); label.Background = Brushes.Transparent; label.ToolTip = "¿En qué te vas a concentrar?";
            System.Windows.Automation.AutomationProperties.SetName(label, "Nombre de la sesión de concentración"); content.Children.Add(new GlassPiece(label, 12, new Thickness(12, 11, 12, 11)) { Margin = new Thickness(0, 0, 0, 14) });
            var row = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            primary = Design.Button("Comenzar", delegate { if (focus.Session.State == "Idle" || focus.Session.State == "Complete") focus.Start(minutes, label.Text); else focus.Toggle(); }, true);
            reset = Design.Button("Finalizar", focus.Reset, false); row.Children.Add(primary); row.Children.Add(reset); content.Children.Add(row);
            var foot = Caption("Tu sesión continúa al cerrar este panel. Pausá para descansar."); foot.TextAlignment = TextAlignment.Center; foot.Margin = new Thickness(0, 17, 0, 0); content.Children.Add(foot);
            Body.Children.Add(new ScrollViewer { Content = content, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled });
            focus.Changed += Update; Closed += delegate { focus.Changed -= Update; }; Update();
        }
        private void Update()
        {
            string phase = focus.Session.State; bool ready = phase == "Idle" || phase == "Complete";
            time.Text = phase == "Idle" ? minutes.ToString("00") + ":00" : focus.TimeText;
            task.Text = ready ? phase == "Complete" ? "Bien hecho." : "Un momento para vos" : focus.Session.Label;
            state.Text = phase == "Running" ? "Concentración en curso" : phase == "Paused" ? "Pausado · retomá a tu ritmo" : phase == "Complete" ? "Tu sesión terminó. Es momento de descansar." : "Elegí tu tiempo y empezá.";
            primary.Content = ready ? phase == "Complete" ? "Otra sesión" : "Comenzar" : phase == "Running" ? "Pausar" : "Continuar";
            reset.Visibility = ready ? Visibility.Collapsed : Visibility.Visible; presets.IsEnabled = ready; label.IsEnabled = ready;
            double ratio = ready && phase == "Idle" ? 0 : focus.Session.Progress(DateTime.UtcNow);
            full.Visibility = ratio >= .999 ? Visibility.Visible : Visibility.Collapsed; progress.Visibility = ratio > 0 && ratio < .999 ? Visibility.Visible : Visibility.Collapsed;
            if (ratio > 0 && ratio < .999) { double angle = ratio * 2 * Math.PI; var figure = new PathFigure { StartPoint = new Point(82, 7), IsClosed = false }; figure.Segments.Add(new ArcSegment(new Point(82 + 75 * Math.Sin(angle), 82 - 75 * Math.Cos(angle)), new Size(75, 75), 0, ratio > .5, SweepDirection.Clockwise, true)); progress.Data = new PathGeometry(new[] { figure }); }
            foreach (Button preset in presets.Children) preset.Opacity = (string)preset.Content == minutes + " min" ? 1 : .65;
        }
    }
}

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace MacFlow
{
    public static class Design
    {
        public static bool Dark = false, Animations = true;
        public static double GlassOpacity = .18;
        public static string Accent = "#8DAEFF";
        private static bool loaded;
        private static readonly DependencyProperty ScaleTargetProperty=DependencyProperty.RegisterAttached("ScaleTarget",typeof(double),typeof(Design),new PropertyMetadata(1.0));
        private static readonly DependencyProperty DismissingProperty=DependencyProperty.RegisterAttached("Dismissing",typeof(bool),typeof(Design),new PropertyMetadata(false));
        private static readonly DependencyProperty MinimizingProperty=DependencyProperty.RegisterAttached("Minimizing",typeof(bool),typeof(Design),new PropertyMetadata(false));
        private static readonly DependencyProperty RestoreHookProperty=DependencyProperty.RegisterAttached("RestoreHook",typeof(bool),typeof(Design),new PropertyMetadata(false));
        public static void InstallTheme()
        {
            var resources = Application.Current.Resources;
            resources["GlassInk"]=Ink;resources["GlassMuted"]=Muted;
            resources["TextBrush"] = Ink; resources["AccentBrush"] = Brush(Accent);
            resources["FieldBrush"] = Brush(Dark?"#553D3D40":"#14000000");
            resources["EdgeBrush"] = Brush(Dark?"#28FFFFFF":"#20000000");
            resources["PanelBrush"] = Brush(Dark ? "#F028282A" : "#FAF7F7F8");
            resources["SelectionBrush"] = Brush(Dark ? "#556D9DEF" : "#306D9DEF");
            resources["GlassCardSurface"]=CardSurface;resources["GlassTileSurface"]=Brush(Dark?"#22313942":"#40FFFFFF");
            if (!loaded)
            {
                using (var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("MacFlow.Theme.xaml"))
                { resources.MergedDictionaries.Add((ResourceDictionary)System.Windows.Markup.XamlReader.Load(stream)); }
                loaded = true;
            }
        }
        public static SolidColorBrush Brush(string hex) { var b = (SolidColorBrush)new BrushConverter().ConvertFromString(hex); b.Freeze(); return b; }
        public static Brush Ink { get { return Brush(Dark ? "#F5F5F7" : "#202023"); } }
        public static Brush Muted { get { return Brush(Dark ? "#C5C5CC" : "#5D5D64"); } }
        public static Brush ContentSurface {get{return Brush(Dark?"#18161A22":"#20FFFFFF");}}
        public static Brush SidebarSurface {get{return Brush(Dark?"#26171B23":"#30FFFFFF");}}
        public static Brush CardSurface {get{return Brush(Dark?"#22313942":"#40FFFFFF");}}
        public static Border Glass(UIElement child, int radius, bool dock, bool contain=false)
        {
            var background = MaterialTint(dock,Dark);
            // Keep one continuous material. A second inset highlight rectangle
            // created the unwanted double rim even when its outline was removed.
            var layer = new Grid();background.Freeze();
            layer.Children.Add(child);
            var outer = new Border { CornerRadius = new CornerRadius(radius), BorderThickness = new Thickness(0), BorderBrush = Brushes.Transparent, Background = background, Padding = new Thickness(dock ? 5 : 22), Child = layer };
            if(!dock||contain){Action clip=delegate{double r=Math.Max(0,radius-outer.Padding.Left-outer.BorderThickness.Left);layer.Clip=new RectangleGeometry(new Rect(0,0,Math.Max(0,layer.ActualWidth),Math.Max(0,layer.ActualHeight)),r,r);};layer.SizeChanged+=delegate{clip();};outer.Loaded+=delegate{clip();};}
            return outer;
        }
        internal static LinearGradientBrush MaterialTint(bool dock,bool dark)
        {
            var colors=dock?dark?new[]{"#081E2630","#071A222C","#0C18202A"}:new[]{"#22FFFFFF","#12FFFFFF","#1AFFFFFF"}
                :dark?new[]{"#38232B38","#34202735","#38202835"}:new[]{"#60FFFFFF","#50F7FAFF","#55FFFFFF"};
            var brush=new LinearGradientBrush{StartPoint=new Point(0,0),EndPoint=new Point(.8,1)};
            for(int i=0;i<3;i++){var color=(Color)ColorConverter.ConvertFromString(colors[i]);color.A=(byte)Math.Min(220,color.A*Math.Max(.75,GlassOpacity/.18));brush.GradientStops.Add(new GradientStop(color,i==0?0:i==1?.48:1));}brush.Freeze();return brush;
        }
        public static TextBlock Text(string text, double size, bool bold)
        {
            var label=new TextBlock { Text = text, FontFamily = new FontFamily("Segoe UI Variable Text, Segoe UI"), FontSize = size, FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis,SnapsToDevicePixels=true };TextOptions.SetTextFormattingMode(label,TextFormattingMode.Display);label.SetResourceReference(TextBlock.ForegroundProperty,"GlassInk");return label;
        }
        public static Button Button(string text, Action action, bool prominent)
        {
            var b = new Button { Content = text, HorizontalContentAlignment=HorizontalAlignment.Center,VerticalContentAlignment=VerticalAlignment.Center, Foreground = prominent ? Brushes.White : Ink, Background = prominent ? Brush("#007AFF") : Brush(Dark ? "#403D4655" : "#60FFFFFF"), BorderBrush = Brushes.Transparent, BorderThickness = new Thickness(0), Padding = new Thickness(12, 6, 12, 6), Margin = new Thickness(0, 3, 6, 3), Cursor = Cursors.Hand, FontSize = 12 };
            var template = new ControlTemplate(typeof(Button));
            var disabled=new Trigger{Property=UIElement.IsEnabledProperty,Value=false};disabled.Setters.Add(new Setter(UIElement.OpacityProperty,.42));template.Triggers.Add(disabled);
            var border = new FrameworkElementFactory(typeof(Border));border.Name="AnimatedFace";border.SetValue(UIElement.RenderTransformProperty,new ScaleTransform(1,1));border.SetValue(UIElement.RenderTransformOriginProperty,new Point(.5,.5));border.SetValue(Border.CornerRadiusProperty, new CornerRadius(8)); border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty)); border.SetValue(Border.BorderBrushProperty,new TemplateBindingExtension(Control.BorderBrushProperty)); border.SetValue(Border.BorderThicknessProperty,new TemplateBindingExtension(Control.BorderThicknessProperty));
            var cp = new FrameworkElementFactory(typeof(ContentPresenter)); cp.SetValue(FrameworkElement.MarginProperty, new TemplateBindingExtension(Control.PaddingProperty)); cp.SetValue(FrameworkElement.HorizontalAlignmentProperty, new TemplateBindingExtension(Control.HorizontalContentAlignmentProperty)); cp.SetValue(FrameworkElement.VerticalAlignmentProperty, new TemplateBindingExtension(Control.VerticalContentAlignmentProperty));
            var layers=new FrameworkElementFactory(typeof(Grid));var glint=new FrameworkElementFactory(typeof(Border));glint.Name="Glint";glint.SetValue(Border.CornerRadiusProperty,new CornerRadius(14));glint.SetValue(Border.BackgroundProperty,new LinearGradientBrush(Colors.White,Colors.Transparent,70));glint.SetValue(UIElement.OpacityProperty,0.0);glint.SetValue(UIElement.IsHitTestVisibleProperty,false);layers.AppendChild(glint);layers.AppendChild(cp);border.AppendChild(layers);var hitArea=new FrameworkElementFactory(typeof(Grid));hitArea.SetValue(System.Windows.Controls.Panel.BackgroundProperty,Brushes.Transparent);hitArea.AppendChild(border);template.VisualTree = hitArea; b.Template = template;
            if(!prominent)b.SetResourceReference(Control.ForegroundProperty,"GlassInk");
            b.RenderTransformOrigin = new Point(.5, .5); b.RenderTransform = new ScaleTransform(1, 1);
            b.MouseEnter += delegate { Scale(b, 1.025); AnimateGlint(b,.19); }; b.MouseLeave += delegate { Scale(b, 1); AnimateGlint(b,0); };
            b.PreviewMouseLeftButtonDown+=delegate {Scale(b,.975);};b.PreviewMouseLeftButtonUp+=delegate {Scale(b,b.IsMouseOver?1.025:1);};
            b.Click += delegate { action(); }; return b;
        }
        private static void AnimateGlint(Button button,double value)
        {
            var solid=button.Background as SolidColorBrush;if(solid!=null&&solid.Color.A==0&&!(button.Content is GlassPiece))return;
            var layer=button.Template.FindName("Glint",button) as Border;if(layer==null)return;
            if(!Animations)layer.Opacity=value;else layer.BeginAnimation(UIElement.OpacityProperty,new DoubleAnimation(value,TimeSpan.FromMilliseconds(180)){EasingFunction=new CubicEase{EasingMode=EasingMode.EaseOut}});
        }
        public static void Scale(FrameworkElement element, double value)
        {
            var button=element as Button;if(button!=null){if(button.RenderTransform==null)return;button.ApplyTemplate();var face=button.Template.FindName("AnimatedFace",button) as FrameworkElement;if(face==null)return;element=face;}
            if(element.RenderTransform!=null&&element.RenderTransform.IsFrozen)element.RenderTransform=element.RenderTransform.CloneCurrentValue();
            var transform = element.RenderTransform as ScaleTransform;var group=element.RenderTransform as TransformGroup;if(transform==null&&group!=null)transform=group.Children.OfType<ScaleTransform>().FirstOrDefault();if(transform==null)return;
            double previous=(double)element.GetValue(ScaleTargetProperty);if(Math.Abs(previous-value)<.015)return;element.SetValue(ScaleTargetProperty,value);
            if (!Animations) { transform.BeginAnimation(ScaleTransform.ScaleXProperty, null); transform.BeginAnimation(ScaleTransform.ScaleYProperty, null); transform.ScaleX = transform.ScaleY = value; return; }
            var a = new DoubleAnimation(value, TimeSpan.FromMilliseconds(240)) { EasingFunction = new SpringEase { EasingMode = EasingMode.EaseIn },FillBehavior=FillBehavior.Stop };
            double x=transform.ScaleX,y=transform.ScaleY;transform.ScaleX=transform.ScaleY=value;
            var ay=a.Clone();a.From=x;ay.From=y;
            Timeline.SetDesiredFrameRate(a,60);Timeline.SetDesiredFrameRate(ay,60);transform.BeginAnimation(ScaleTransform.ScaleXProperty, a); transform.BeginAnimation(ScaleTransform.ScaleYProperty, ay);
        }
        public static void Reveal(Window window)
        {
            if((bool)window.GetValue(DismissingProperty))return;
            if (!Animations) return;
            var entrance = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
            entrance.Completed += delegate { if(window.IsVisible&&!(bool)window.GetValue(DismissingProperty)&&!(bool)window.GetValue(MinimizingProperty)){window.BeginAnimation(UIElement.OpacityProperty,null); Native.Glass(window,Dark);} };
            window.BeginAnimation(UIElement.OpacityProperty, entrance);
            PanelMotion.Open(window);
        }
        public static void Dismiss(Window window,Action after=null)
        {
            if((bool)window.GetValue(DismissingProperty))return;window.SetValue(DismissingProperty,true);window.IsHitTestVisible=false;
            if(!Animations){window.Close();if(after!=null)after();return;}
            bool finished=false;var fallback=new System.Windows.Threading.DispatcherTimer(System.Windows.Threading.DispatcherPriority.Background){Interval=TimeSpan.FromMilliseconds(320)};
            Action finish=delegate{if(finished)return;finished=true;fallback.Stop();window.Close();if(after!=null)after();};
            fallback.Tick+=delegate{finish();};window.Closed+=delegate{finished=true;fallback.Stop();};fallback.Start();
            PanelMotion.Close(window);
            var animation=new DoubleAnimation(0,TimeSpan.FromMilliseconds(250)){EasingFunction=new CubicEase{EasingMode=EasingMode.EaseInOut}};Timeline.SetDesiredFrameRate(animation,60);animation.Completed+=delegate{finish();};window.BeginAnimation(UIElement.OpacityProperty,animation);
        }
        public static void Minimize(Window window)
        {
            if(window.WindowState==WindowState.Minimized||(bool)window.GetValue(MinimizingProperty)||(bool)window.GetValue(DismissingProperty))return;
            if(!(bool)window.GetValue(RestoreHookProperty)){window.SetValue(RestoreHookProperty,true);bool minimized=false;window.StateChanged+=delegate{if(window.WindowState==WindowState.Minimized)minimized=true;else if(minimized){minimized=false;Reveal(window);}};}
            if(!Animations){window.WindowState=WindowState.Minimized;return;}window.SetValue(MinimizingProperty,true);window.IsHitTestVisible=false;
            PanelMotion.Close(window);
            bool finished=false;var fallback=new System.Windows.Threading.DispatcherTimer{Interval=TimeSpan.FromMilliseconds(280)};EventHandler closed=null;closed=delegate{finished=true;fallback.Stop();window.Closed-=closed;};Action finish=delegate{if(finished)return;finished=true;fallback.Stop();window.Closed-=closed;PanelMotion.Reset(window);window.BeginAnimation(UIElement.OpacityProperty,null);window.Opacity=1;window.IsHitTestVisible=true;window.SetValue(MinimizingProperty,false);if(window.IsVisible)window.WindowState=WindowState.Minimized;};fallback.Tick+=delegate{finish();};window.Closed+=closed;fallback.Start();
            var fade=new DoubleAnimation(0,TimeSpan.FromMilliseconds(180)){EasingFunction=new CubicEase{EasingMode=EasingMode.EaseIn}};fade.Completed+=delegate{finish();};window.BeginAnimation(UIElement.OpacityProperty,fade);
        }
        public static Window Panel(string title, double width, double height)
        {
            var w = new Window { Title = title, Width = width, Height = height, WindowStyle = WindowStyle.None, AllowsTransparency = false, ResizeMode = ResizeMode.NoResize, ShowInTaskbar = false, Topmost = true, Background = Brush(Dark ? "#202938" : "#ECF2FA"), FontFamily = new FontFamily("Segoe UI") };
            w.SourceInitialized += delegate { Native.Glass(w, Dark); };
            w.KeyDown += delegate(object sender, KeyEventArgs e) { if (e.Key == Key.Escape) {e.Handled=true;Dismiss(w);} };
            return w;
        }
        public static UIElement AppIcon(AppEntry app, int size)
        {
            if(System.IO.Directory.Exists(app.Target))return new Image{Source=FeatureIcons.Get("folder"),Width=size,Height=size,Stretch=Stretch.Uniform};
            var system=SystemAppIcon(app);if(system!=null)return new Image{Source=system,Width=size,Height=size,Stretch=Stretch.Uniform};
            var iconPath=!String.IsNullOrEmpty(app.IconPath)?app.IconPath:!String.IsNullOrEmpty(app.Target)&&app.Target.StartsWith("shell:AppsFolder\\",StringComparison.OrdinalIgnoreCase)?app.Target:!String.IsNullOrEmpty(app.Executable)?app.Executable:app.Target;
            var source = Icons.Get(iconPath);
            if (source != null) return new Image { Source = source, Width = size, Height = size, Stretch = Stretch.Uniform };
            var host=new Grid {Width=size,Height=size};var placeholder=new Border {CornerRadius=new CornerRadius(size*.24),Background=Brush("#158DAEFF"),Child=Symbol("windows",size*.56,Ink)};var actual=new Image {Stretch=Stretch.Uniform};host.Children.Add(placeholder);host.Children.Add(actual);
            Icons.Request(iconPath,delegate(ImageSource icon){if(icon!=null){actual.Source=icon;placeholder.Visibility=Visibility.Collapsed;}});return host;
        }
        private static readonly System.Collections.Generic.Dictionary<string,ImageSource> systemIcons=new System.Collections.Generic.Dictionary<string,ImageSource>();
        private static ImageSource SystemAppIcon(AppEntry app)
        {
            string target=app.Executable??app.Target??"",file=System.IO.Path.GetFileName(target),kind=null;
            if(String.Equals(file,"explorer.exe",StringComparison.OrdinalIgnoreCase))kind="files";
            else if(String.Equals(file,"SystemSettings.exe",StringComparison.OrdinalIgnoreCase)||String.Equals(target,"ms-settings:",StringComparison.OrdinalIgnoreCase))kind="settings";
            else if(String.Equals(file,"MacFlow.exe",StringComparison.OrdinalIgnoreCase))kind="macflow";
            if(kind=="macflow")return FeatureIcons.Get("macflow");
            if(kind==null)return null;ImageSource cached;if(systemIcons.TryGetValue(kind,out cached))return cached;
            var drawing=new DrawingGroup();
            using(var dc=drawing.Open())
            {
                // Original vector artwork. Other applications retain their brands.
                var fill=new LinearGradientBrush(kind=="files"?Color.FromRgb(51,166,255):kind=="settings"?Color.FromRgb(236,241,248):Color.FromRgb(172,143,255),kind=="files"?Color.FromRgb(0,103,226):kind=="settings"?Color.FromRgb(152,165,184):Color.FromRgb(62,91,235),90);
                dc.DrawRoundedRectangle(fill,null,new Rect(2,2,60,60),14,14);
                if(kind=="files")
                {
                    dc.PushClip(new RectangleGeometry(new Rect(2,2,60,60),14,14));dc.DrawRectangle(Brush("#CFEAFF"),null,new Rect(32,2,30,60));dc.Pop();
                    var pen=new Pen(Brush("#173961"),2.4){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round};
                    dc.DrawLine(pen,new Point(20,20),new Point(20,25));dc.DrawLine(pen,new Point(44,20),new Point(44,25));
                    dc.DrawGeometry(null,pen,Geometry.Parse("M33,14 L29,35 35,35 M18,42 C24,49 40,49 46,41"));
                }
                else if(kind=="settings")
                {
                    var pen=new Pen(Brush("#43536B"),4){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round};dc.DrawEllipse(null,pen,new Point(32,32),14,14);dc.DrawEllipse(null,new Pen(Brush("#F9FBFF"),3),new Point(32,32),6,6);
                    for(int i=0;i<12;i++){double a=i*Math.PI/6;dc.DrawLine(pen,new Point(32+18*Math.Cos(a),32+18*Math.Sin(a)),new Point(32+23*Math.Cos(a),32+23*Math.Sin(a)));}
                }
                else dc.DrawGeometry(null,new Pen(Brushes.White,4){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round,LineJoin=PenLineJoin.Round},Geometry.Parse("M13,40 C20,40 18,23 25,23 C32,23 29,42 36,42 C43,42 42,22 51,22"));
            }
            drawing.Freeze();cached=new DrawingImage(drawing);cached.Freeze();systemIcons[kind]=cached;return cached;
        }
        public static CheckBox Check(string text, bool value, Action<bool> change)
        {
            var c = new CheckBox { Content = text, IsChecked = value, Foreground = Ink, FontSize = 14, Margin = new Thickness(0, 10, 0, 10) };
            Action move=delegate {var t=c.Template.FindName("KnobMotion",c) as TranslateTransform;if(t==null)return;double x=c.IsChecked==true?17:0;if(!Animations){t.BeginAnimation(TranslateTransform.XProperty,null);t.X=x;}else t.BeginAnimation(TranslateTransform.XProperty,new DoubleAnimation(x,TimeSpan.FromMilliseconds(190)){EasingFunction=new CubicEase{EasingMode=EasingMode.EaseInOut}});};c.Loaded+=delegate {move();};c.Checked+=delegate {move();};c.Unchecked+=delegate {move();};
            c.SetResourceReference(Control.ForegroundProperty,"GlassInk");c.Checked += delegate { change(true); }; c.Unchecked += delegate { change(false); }; return c;
        }
        public static UIElement AppleMark(double size)
        {
            // F8FF is a private-use glyph, absent from normal Windows fonts.
            // Draw a vector fallback so this menu never becomes a missing box.
            var mark=new System.Windows.Shapes.Path{Width=size,Height=size,Stretch=Stretch.Uniform,
                Data=Geometry.Parse("M15.3,1 C15.4,3.1 14,4.9 12,5.3 C11.8,3.6 13.2,1.5 15.3,1 Z M12,7 C13.5,7 14.6,5.8 16.3,6 C18,6.1 19.3,7 20,8.1 C16.5,10.1 16.7,14.5 20.6,16.1 C20,17.9 18.6,20.1 17.2,21.2 C15.8,22.4 14.4,21 12.6,21 C10.9,21 9.8,22.3 8.4,21.6 C5.2,20 2.4,14.7 3.3,10.5 C3.9,7.6 6.2,5.8 8.6,6 C10.1,6.1 10.9,7 12,7 Z")};
            mark.SetResourceReference(System.Windows.Shapes.Path.FillProperty,"GlassInk");return mark;
        }
        public static UIElement Symbol(string key, double size, Brush color)
        {
            string data;
            switch(key)
            {
                case "search": data="M10,3 A7,7 0 1 1 9.99,3 M15,15 L21,21"; break;
                case "grid": data="M3,3 L9,3 9,9 3,9 Z M15,3 L21,3 21,9 15,9 Z M3,15 L9,15 9,21 3,21 Z M15,15 L21,15 21,21 15,21 Z"; break;
                case "windows": data="M3,3 L17,3 17,15 3,15 Z M7,19 L21,19 21,7"; break;
                case "settings": data="M12,7 A5,5 0 1 1 11.99,7 M12,1 L12,4 M12,20 L12,23 M1,12 L4,12 M20,12 L23,12 M4,4 L6,6 M18,18 L20,20 M4,20 L6,18 M18,6 L20,4"; break;
                case "control": data="M3,6 L8,6 M13,6 L21,6 M3,18 L14,18 M19,18 L21,18 M10.5,3 A3,3 0 1 1 10.49,3 M16.5,15 A3,3 0 1 1 16.49,15"; break;
                case "wifi": data="M2,8 Q12,-1 22,8 M5,12 Q12,5 19,12 M8,16 Q12,12 16,16 M12,20 L12.01,20"; break;
                case "bluetooth": data="M12,2 L19,8 5,18 M12,22 L19,16 5,6 M12,2 L12,22"; break;
                case "sun": data="M12,7 A5,5 0 1 1 11.99,7 M12,1 L12,3 M12,21 L12,23 M1,12 L3,12 M21,12 L23,12 M4,4 L5,5 M19,19 L20,20 M4,20 L5,19 M19,5 L20,4"; break;
                case "moon": data="M20,15 A9,9 0 1 1 9,3 A8,8 0 0 0 20,15 Z"; break;
                case "volume": data="M3,9 L7,9 12,4 12,20 7,15 3,15 Z M16,8 Q20,12 16,16 M19,5 Q25,12 19,19"; break;
                case "display": data="M3,4 L21,4 21,17 3,17 Z M12,17 L12,21 M7,21 L17,21"; break;
                case "power": data="M12,2 L12,12 M6,5 A9,9 0 1 0 18,5"; break;
                case "play": data="M8,5 L19,12 8,19 Z"; break;
                case "folder": data="M2,6 L9,6 11,8 22,8 22,20 2,20 Z"; break;
                case "check": data="M4,12 L9,17 20,6"; break;
                case "close": data="M6,6 L18,18 M18,6 L6,18"; break;
                default: data="M12,2 L22,12 12,22 2,12 Z M8,15 L11,9 14,15 17,8"; break;
            }
            var path = new System.Windows.Shapes.Path { Data=Geometry.Parse(data), Stroke=color, StrokeThickness=1.7, StrokeStartLineCap=PenLineCap.Round, StrokeEndLineCap=PenLineCap.Round, StrokeLineJoin=PenLineJoin.Round, Stretch=Stretch.Uniform, Margin=new Thickness(2) };
            var solid=color as SolidColorBrush;if(solid!=null&&solid.Color==((SolidColorBrush)Ink).Color)path.SetResourceReference(Shape.StrokeProperty,"GlassInk");
            else if(solid!=null&&solid.Color==((SolidColorBrush)Muted).Color)path.SetResourceReference(Shape.StrokeProperty,"GlassMuted");
            return new Viewbox { Width=size, Height=size, Child=new Border { Width=24, Height=24, Child=path } };
        }
        public static Border Card(UIElement content)
        {
            var card=new Border{Margin=new Thickness(0,0,0,16),Padding=new Thickness(14),CornerRadius=new CornerRadius(14),BorderThickness=new Thickness(0),Child=content};card.SetResourceReference(Border.BackgroundProperty,"GlassCardSurface");return card;
        }
        public static Button Circle(string symbol, Action action, string tooltip)
        {
            var button=Button("",action,false); button.Content=Symbol(symbol,20,Ink); button.Width=38; button.Height=38; button.Padding=new Thickness(7); button.ToolTip=tooltip; return button;
        }
    }
}

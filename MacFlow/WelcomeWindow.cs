using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
namespace MacFlow
{
    // Bézier lettering adapted from kumailnanji/letters (MIT).
    // THIRD-PARTY-NOTICES.md includes the full license. No Apple assets.
    internal sealed class HelloLettering : FrameworkElement
    {
        private sealed class Stroke {internal Point[] Points;internal double[] Distances;internal double Length;}
        private readonly List<Stroke> strokes=new List<Stroke>();
        private double total;
        private double wordWidth=49;
        private Rect wordBounds=new Rect(0,-12,49,22);
        internal bool LightInk=true;
        internal bool SetWord(string text)
        {
            var normalized=text.ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormD);double x=0,last=0;var marks=new List<KeyValuePair<char,double>>();
            foreach(char ch in normalized){if(ch==' '||System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch)==System.Globalization.UnicodeCategory.NonSpacingMark)continue;if(!WelcomeScriptGlyphs.Paths.ContainsKey(ch))return false;}
            BeginAnimation(ProgressProperty,null);strokes.Clear();total=0;
            foreach(char ch in normalized)
            {
                if(ch==' '){x+=9;continue;}
                if(System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch)==System.Globalization.UnicodeCategory.NonSpacingMark){marks.Add(new KeyValuePair<char,double>(ch,last));continue;}
                last=x;foreach(var path in WelcomeScriptGlyphs.Paths[ch])Add(path,x);x+=WelcomeScriptGlyphs.Advance[ch];
            }
            foreach(var mark in marks)
            {
                string path=mark.Key=='\u0300'?"M4,-6 L1,-9":mark.Key=='\u0301'?"M2,-6 L5,-9":mark.Key=='\u0303'?"M0,-7 C2,-10 4,-4 6,-7":mark.Key=='\u0308'?"M1,-7 L1,-7.05 M5,-7 L5,-7.05":mark.Key=='\u0327'?"M4,9 C7,12 1,13 2,11":mark.Key=='\u030C'?"M0,-9 L3,-6 L6,-9":"M0,-8 L6,-8";
                Add(path,mark.Value);
            }
            wordWidth=Math.Max(1,x);wordBounds=Rect.Empty;foreach(var stroke in strokes)foreach(var p in stroke.Points)wordBounds.Union(p);
            System.Windows.Automation.AutomationProperties.SetName(this,text);Progress=1;InvalidateVisual();return true;
        }
        internal static readonly DependencyProperty ProgressProperty=DependencyProperty.Register("Progress",typeof(double),typeof(HelloLettering),new FrameworkPropertyMetadata(0.0,FrameworkPropertyMetadataOptions.AffectsRender));
        internal double Progress {get{return (double)GetValue(ProgressProperty);}set{SetValue(ProgressProperty,value);}}
        internal HelloLettering()
        {
            Width=300;Height=150;System.Windows.Automation.AutomationProperties.SetName(this,"Hola");IsHitTestVisible=false;
            Add("M 0 4 C 0.5 3.25 0.75 3 2 1 C 2.6014 0.0377 10.5999 -12 6 -12 C 2.8115 -12 2.4422 -7.2109 2 -5 C 1.5 -2.5 1.5 -2.5 1 1 C 0.5 4.5 0.25 7 0 9",0);
            Add("M 0 8.9999 C 0.25 8.2499 0.5 7.2499 1 5.9999 C 1.6941 4.2646 2.6744 2.3255 4 0.9999 C 4.9852 0.0147 6.6474 -0.4509 8 -0.0001 C 9.3237 0.4412 9.233 1.835 9 2.9999 C 8.666 4.67 7.3333 5.9999 8 7.9999 C 8.3018 8.9054 9.4082 9.8752 10.5341 9.6294 C 13.3199 9.0212 14.0878 6.3773 15 3.9999",0);
            Add("M 6.1786 0.2242 C -2.4358 -2.9299 -2.2245 11.7285 6.1786 8.8452 C 10.8917 7.228 10.0536 2.7604 6.5038 0.3938",12.7);
            Add("M 4.8615 0.4579 C 3.5824 7.1609 10.6724 7.7432 13.9656 3.9828",12.7);
            Add("M 0 4 C 2.1153 0.827 4.5767 -2.4419 6 -6 C 6.3998 -6.9996 8.4714 -12 6 -12 C 0.862 -12 1.2427 -1.1552 1 2 C 0.9082 3.1936 -0.0933 9.6036 2.5407 9.194 C 4.4865 8.8914 7.5818 5.5097 8 4",26.3);
            Add("M 9.0039 2.9991 C 8.2089 1.4091 7.8711 0.4659 6.0039 -0.0009 C 0.5166 -1.3727 -2.6187 7.5935 3.0039 8.9991 C 5.6538 9.6616 7.1841 8.1853 8.0039 5.9991 C 8.7539 3.9991 9.7539 0.2491 10.0039 -0.0009 C 10.2539 -0.2509 9.2539 2.9991 9.0039 4.9991 C 8.8263 6.4198 8.7357 9.7552 11.0039 8.9991 C 13.5611 8.1467 14.5827 6.1309 16.0039 3.9991",32.8);
            Unloaded+=delegate{BeginAnimation(ProgressProperty,null);};
        }
        private void Add(string path,double x)
        {
            var geometry=Geometry.Parse(path).GetFlattenedPathGeometry(.025,ToleranceType.Absolute);
            foreach(var figure in geometry.Figures)
            {
                var points=new List<Point>{new Point(figure.StartPoint.X+x,figure.StartPoint.Y)};
                foreach(var segment in figure.Segments){var poly=segment as PolyLineSegment;var line=segment as LineSegment;if(poly!=null)foreach(var p in poly.Points)points.Add(new Point(p.X+x,p.Y));else if(line!=null)points.Add(new Point(line.Point.X+x,line.Point.Y));}
                var stroke=new Stroke{Points=points.ToArray(),Distances=new double[points.Count]};for(int i=1;i<points.Count;i++)stroke.Distances[i]=stroke.Distances[i-1]+(points[i]-points[i-1]).Length;
                stroke.Length=stroke.Distances[points.Count-1];total+=stroke.Length;strokes.Add(stroke);
            }
        }
        internal void Play(double delay=200)
        {
            BeginAnimation(ProgressProperty,null);Progress=Motion.Enabled?0:1;if(!Motion.Enabled)return;
            var animation=new DoubleAnimationUsingKeyFrames{BeginTime=TimeSpan.FromMilliseconds(delay)};animation.KeyFrames.Add(new LinearDoubleKeyFrame(0,KeyTime.FromTimeSpan(TimeSpan.Zero)));double written=0;foreach(var stroke in strokes){written+=stroke.Length;animation.KeyFrames.Add(new EasingDoubleKeyFrame(written/Math.Max(1,total),KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(1550*written/Math.Max(1,total))),new SineEase{EasingMode=EasingMode.EaseInOut}));}
            Timeline.SetDesiredFrameRate(animation,60);animation.Completed+=delegate{Progress=1;BeginAnimation(ProgressProperty,null);};BeginAnimation(ProgressProperty,animation);
        }
        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);bool imageGlass=Effect is WelcomeApple&&((WelcomeApple)Effect).Wallpaper>0;double budget=total*Math.Max(0,Math.Min(1,Progress));double scale=Math.Min(ActualWidth/(wordWidth+5),ActualHeight/(wordBounds.Height+6));
            dc.PushTransform(new MatrixTransform(scale,0,0,scale,(ActualWidth-wordWidth*scale)/2,(ActualHeight-wordBounds.Height*scale)/2-wordBounds.Top*scale));
            var brush=imageGlass?Brushes.White:SystemParameters.HighContrast?(Brush)SystemColors.WindowTextBrush:LightInk?WelcomeGlass.Brush("#EFFFFFFF","#35FFFFFF","#D9FFFFFF"):WelcomeGlass.Brush("#BD181818","#40202020","#B0181818");var pen=new Pen(brush,1.55){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round,LineJoin=PenLineJoin.Round};pen.Freeze();
            var edge=new Pen(SystemParameters.HighContrast?(Brush)SystemColors.WindowTextBrush:WelcomeGlass.Brush("#FFFFFFFF","#25FFFFFF","#80FFFFFF"),.12){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round,LineJoin=PenLineJoin.Round};edge.Freeze();
            foreach(var stroke in strokes)
            {
                if(budget<=0)break;double visible=Math.Min(stroke.Length,budget);var path=new StreamGeometry();using(var context=path.Open()){context.BeginFigure(stroke.Points[0],false,false);for(int i=1;i<stroke.Points.Length;i++){if(stroke.Distances[i]<=visible)context.LineTo(stroke.Points[i],true,false);else{double span=stroke.Distances[i]-stroke.Distances[i-1];double t=span==0?0:(visible-stroke.Distances[i-1])/span;context.LineTo(stroke.Points[i-1]+(stroke.Points[i]-stroke.Points[i-1])*t,true,false);break;}}}path.Freeze();dc.DrawGeometry(null,pen,path);if(!imageGlass){dc.PushTransform(new TranslateTransform(0,-.63));dc.DrawGeometry(null,edge,path);dc.Pop();}budget-=stroke.Length;
            }dc.Pop();
        }
    }
    internal static class WelcomeGlass
    {
        internal static LinearGradientBrush Brush(string top,string middle,string bottom)
        {
            var brush=new LinearGradientBrush{StartPoint=new Point(.15,0),EndPoint=new Point(.8,1)};
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(top),0));
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(middle),.52));
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(bottom),1));brush.Freeze();return brush;
        }
        internal static Grid Apple()
        {
            // Brand silhouette from Simple Icons (CC0); preserve its proportions.
            var geometry=Geometry.Parse("M12.152 6.896c-.948 0-2.415-1.078-3.96-1.04-2.04.027-3.91 1.183-4.961 3.014-2.117 3.675-.546 9.103 1.519 12.09 1.013 1.454 2.208 3.09 3.792 3.039 1.52-.065 2.09-.987 3.935-.987 1.831 0 2.35.987 3.96.948 1.637-.026 2.676-1.48 3.676-2.948 1.156-1.688 1.636-3.325 1.662-3.415-.039-.013-3.182-1.221-3.22-4.857-.026-3.04 2.48-4.494 2.597-4.559-1.429-2.09-3.623-2.324-4.39-2.376-2-.156-3.675 1.09-4.61 1.09zM15.53 3.83c.843-1.012 1.4-2.427 1.245-3.83-1.207.052-2.662.805-3.532 1.818-.78.896-1.454 2.338-1.273 3.714 1.338.104 2.715-.688 3.559-1.701").Clone();var rect=geometry.Bounds;double scale=176/Math.Max(rect.Width,rect.Height);
            geometry.Transform=new MatrixTransform(scale,0,0,scale,(180-rect.Width*scale)/2-rect.Left*scale,2-rect.Top*scale);geometry.Freeze();
            var grid=new Grid{Width=180,Height=180,IsHitTestVisible=false};
            // Vector layers remain translucent over the live compositor. These
            // reflections do not pretend to be optical refraction of the desktop.
            if(SystemParameters.HighContrast)grid.Children.Add(new System.Windows.Shapes.Path{Data=geometry,Fill=SystemColors.WindowTextBrush});
            else if(RenderCapability.IsPixelShaderVersionSupported(3,0))
            {
                var mask=new Grid{Width=180,Height=180,Effect=new WelcomeApple(),CacheMode=new BitmapCache()};mask.Children.Add(new System.Windows.Shapes.Path{Data=geometry,Fill=Brushes.White});grid.Children.Add(mask);
            }
            else
            {
                // Low-spec fallback uses a quiet surface and directional rim.
                grid.Children.Add(new System.Windows.Shapes.Path{Data=geometry,Fill=Brush("#24FFFFFF","#0DFFFFFF","#15FFFFFF"),Stroke=Brush("#B0FFFFFF","#12FFFFFF","#50FFFFFF"),StrokeThickness=.8});
            }
            System.Windows.Automation.AutomationProperties.SetName(grid,"Manzanita de cristal");return grid;
        }
    }
    internal class WelcomeWindow : Window
    {
        internal readonly HelloLettering Lettering=new HelloLettering();
        internal readonly Button StartButton;
        private readonly System.Drawing.Rectangle bounds;
        private readonly Grid surface,scene,apple;
        private readonly StackPanel actions;
        private readonly Viewbox content;
        private readonly Image wallpaperImage=new Image{Stretch=Stretch.UniformToFill,IsHitTestVisible=false};
        private ImageBrush wallpaperSampler;private bool wallpaperRendering;private DateTime wallpaperDeadline;
        internal bool WallpaperVisible{get{return wallpaperImage.Source!=null;}}
        internal BitmapSource WallpaperImageSource{get{return wallpaperImage.Source as BitmapSource;}}
        private readonly Grid greetings;
        private readonly TextBlock translatedGreeting;
        private TextBlock subtitleLabel,startLabel;
        private readonly TranslateTransform greetingPosition=new TranslateTransform();
        private readonly System.Windows.Threading.DispatcherTimer languageTimer=new System.Windows.Threading.DispatcherTimer(System.Windows.Threading.DispatcherPriority.Background);
        internal static readonly string[] Greetings={"Hola","Hello","Bonjour","Ciao","Olá","Hallo","Hej","こんにちは","你好","안녕하세요","مرحبًا","שלום","नमस्ते","হ্যালো","สวัสดี","Xin chào","Halo","Jambo","Kumusta","Γεια σου","Привет","Привіт","Cześć","Merhaba","Salut","Hei","Tere","Sveiki","Ahoj","Sawubona"};
        private int greetingIndex;
        private int greetingGeneration;
        private readonly ScaleTransform appleScale=new ScaleTransform(.86,.86);
        private readonly TranslateTransform applePosition=new TranslateTransform(0,0);
        private readonly TranslateTransform actionsPosition=new TranslateTransform(0,14);
        private readonly List<Window> companions=new List<Window>();
        private readonly List<Action> clearAnimations=new List<Action>();
        private bool dismissing;
        private int activeAnimations;
        private readonly bool animated;
        private readonly bool showNews;internal bool NewsVisible{get;private set;}internal Button ContinueButton{get;private set;}
        private readonly bool finalGreeting;
        internal Action EnterDesktop;
        internal bool Advancing{get;private set;}
        internal string PreferredWallpaper;
        private readonly Action afterHello;
        internal WelcomeWindow(System.Drawing.Rectangle? testBounds=null,bool showNews=false,BitmapSource wallpaper=null,Action afterHello=null,bool finalGreeting=false)
        {
            this.showNews=showNews;this.finalGreeting=finalGreeting;this.afterHello=afterHello;
            bounds=testBounds??System.Windows.Forms.Screen.PrimaryScreen.Bounds;
            animated=Motion.Enabled;
            Title="MacFlow · Hola";Width=bounds.Width;Height=bounds.Height;Left=bounds.Left;Top=bounds.Top;WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;
            AllowsTransparency=false;Background=Brushes.Black;ShowInTaskbar=false;Topmost=true;
            surface=new Grid{Opacity=animated?0:1};surface.Children.Add(wallpaperImage);surface.Children.Add(new Border{Background=SceneBackdrop(),IsHitTestVisible=false});
            if(wallpaper!=null)SetWallpaper(wallpaper);
            scene=new Grid{Width=580,Height=450};
            apple=WelcomeGlass.Apple();apple.HorizontalAlignment=HorizontalAlignment.Center;apple.VerticalAlignment=VerticalAlignment.Center;apple.Opacity=animated?0:1;
            var transform=new TransformGroup();transform.Children.Add(appleScale);transform.Children.Add(applePosition);apple.RenderTransform=transform;apple.RenderTransformOrigin=new Point(.5,.5);
            scene.Children.Add(apple);
            Lettering.Width=470;Lettering.Height=215;Lettering.HorizontalAlignment=HorizontalAlignment.Center;Lettering.VerticalAlignment=VerticalAlignment.Center;
            Lettering.Margin=new Thickness(0,10,0,-10);Lettering.Opacity=animated?0:1;
            greetings=new Grid{Width=470,Height=215,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,RenderTransform=greetingPosition};greetings.Children.Add(Lettering);
            translatedGreeting=new TextBlock{FontFamily=new FontFamily("Segoe UI Variable Display, Segoe UI, Segoe UI Symbol"),FontSize=100,FontWeight=FontWeights.Light,TextAlignment=TextAlignment.Center,Foreground=WelcomeGlass.Brush("#FFFFFFFF","#55FFFFFF","#A0FFFFFF"),Text="Hello",Visibility=Visibility.Collapsed};
            System.Windows.Automation.AutomationProperties.SetLiveSetting(translatedGreeting,System.Windows.Automation.AutomationLiveSetting.Off);
            greetings.Children.Add(new Viewbox{Stretch=Stretch.Uniform,StretchDirection=StretchDirection.DownOnly,Margin=new Thickness(8,20,8,20),Child=translatedGreeting});scene.Children.Add(greetings);
            actions=new StackPanel{HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Bottom,Margin=new Thickness(0,0,0,18),Opacity=animated?0:1,RenderTransform=actionsPosition};
            var subtitle=Design.Text("Un nuevo comienzo.",14,false);subtitleLabel=subtitle;subtitle.Foreground=SystemParameters.HighContrast?SystemColors.WindowTextBrush:Design.Brush("#E5F0F6FF");subtitle.HorizontalAlignment=HorizontalAlignment.Center;subtitle.Margin=new Thickness(0,0,0,17);actions.Children.Add(subtitle);
            StartButton=Design.Button("Iniciar",ContinueWelcome,false);StartButton.Width=168;StartButton.Height=48;StartButton.Margin=new Thickness(0);StartButton.HorizontalAlignment=HorizontalAlignment.Center;StartButton.IsDefault=true;StartButton.FontSize=14;
            StartButton.BorderThickness=new Thickness(0);StartButton.Background=Brushes.Transparent;StartButton.Padding=new Thickness(0);StartButton.IsEnabled=!animated;
            var buttonFace=new Grid{Width=168,Height=48,Background=WelcomeGlass.Brush("#32FFFFFF","#14FFFFFF","#25FFFFFF")};
            buttonFace.Children.Add(new Border{BorderBrush=WelcomeGlass.Brush("#8AFFFFFF","#0BFFFFFF","#50FFFFFF"),BorderThickness=new Thickness(.75),CornerRadius=new CornerRadius(24)});
            var label=Design.Text("Iniciar  →",14,true);startLabel=label;label.Foreground=SystemParameters.HighContrast?SystemColors.WindowTextBrush:Brushes.White;label.HorizontalAlignment=HorizontalAlignment.Center;buttonFace.Children.Add(label);
            buttonFace.Clip=new RectangleGeometry(new Rect(0,0,168,48),24,24);StartButton.Content=buttonFace;actions.Children.Add(StartButton);scene.Children.Add(actions);
            if(finalGreeting){subtitleLabel.Text="Tu escritorio está listo.";if(!Lettering.SetWord("Bienvenido")){Lettering.Visibility=Visibility.Collapsed;translatedGreeting.Visibility=Visibility.Visible;translatedGreeting.Text="Bienvenido";}}
            if(afterHello!=null){startLabel.Text="Continuar  →";subtitleLabel.Text="Descubrí las novedades a tu ritmo.";System.Windows.Automation.AutomationProperties.SetName(StartButton,"Continuar a la demostración");}
            content=new Viewbox{Stretch=Stretch.Uniform,StretchDirection=StretchDirection.DownOnly,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(24),Child=scene};surface.Children.Add(content);Content=surface;
            SourceInitialized+=delegate{Native.OpaquePresentation(this);RemoveFrame(this);if(testBounds.HasValue)Native.NoActivate(this);};
            if(testBounds.HasValue){ShowActivated=false;IsHitTestVisible=false;}
            Loaded+=delegate
            {
                if(!testBounds.HasValue)foreach(var screen in System.Windows.Forms.Screen.AllScreens)
                {
                    if(screen.Primary)continue;var rect=screen.Bounds;
                    var companion=new Window{Title="MacFlow · Hola",Width=rect.Width,Height=rect.Height,Left=rect.Left,Top=rect.Top,Background=Brushes.Transparent,Content=new Grid{Background=SceneBackdrop()},WindowStyle=WindowStyle.None,ResizeMode=ResizeMode.NoResize,ShowInTaskbar=false,Topmost=true,ShowActivated=false};
                    companion.SourceInitialized+=delegate{Native.OpaquePresentation(companion);RemoveFrame(companion);Native.NoActivate(companion);};companion.Loaded+=delegate{Cover(companion,rect);};if(WallpaperVisible)((Grid)companion.Content).Children.Insert(0,new Image{Source=wallpaperImage.Source,Stretch=Stretch.UniformToFill,IsHitTestVisible=false});companions.Add(companion);companion.Show();
                }
                Cover(this,bounds);Play();if(finalGreeting)languageTimer.Stop();RefreshInk();if(WallpaperVisible){UpdateWallpaperMapping();if(animated)StartWallpaperMapping();}else if(!testBounds.HasValue)LoadWallpaper();
            };
            Closed+=delegate{StopScene();foreach(var window in companions)window.Close();companions.Clear();};
            languageTimer.Interval=TimeSpan.FromMilliseconds(4800);languageTimer.Tick+=delegate{if(!dismissing&&IsVisible)ChangeGreeting((greetingIndex+1)%Greetings.Length);};
            KeyDown+=delegate(object sender,KeyEventArgs e){if(e.Key==Key.Escape){e.Handled=true;DismissWelcome();}};
        }
        private async void LoadWallpaper()
        {
            var image=await WelcomeWallpaper.Load(PreferredWallpaper);if(dismissing||!IsVisible||image==null)return;SetWallpaper(image);UpdateWallpaperMapping();if(activeAnimations>0)StartWallpaperMapping();RefreshInk();
        }
        private void SetWallpaper(BitmapSource image)
        {
            wallpaperImage.Source=image;wallpaperSampler=new ImageBrush(image){Stretch=Stretch.Fill};
            Tag="Welcome wallpaper · recordable";foreach(var companion in companions){var grid=companion.Content as Grid;if(grid!=null){var background=new Image{Source=image,Stretch=Stretch.UniformToFill,IsHitTestVisible=false};grid.Children.Insert(0,background);}}
        }
        private void UpdateWallpaperMapping()
        {
            if(apple==null||wallpaperSampler==null||!IsLoaded||ActualWidth<=0||ActualHeight<=0)return;
            var image=(BitmapSource)wallpaperImage.Source;double imageAspect=(double)image.PixelWidth/image.PixelHeight,screenAspect=ActualWidth/ActualHeight;
            double width=Math.Min(1,screenAspect/imageAspect),height=Math.Min(1,imageAspect/screenAspect);wallpaperSampler.ViewboxUnits=BrushMappingMode.RelativeToBoundingBox;wallpaperSampler.Viewbox=new Rect((1-width)/2,(1-height)/2,width,height);
            foreach(UIElement child in apple.Children){var layer=child as Grid;var effect=layer==null?null:layer.Effect as WelcomeApple;if(effect!=null)MapWallpaper(apple,effect);}
            if(RenderCapability.IsPixelShaderVersionSupported(3,0)&&!SystemParameters.HighContrast)
            {
                if(Lettering.Effect==null){Lettering.Effect=new WelcomeApple{Frost=.18};Lettering.InvalidateVisual();}MapWallpaper(Lettering,(WelcomeApple)Lettering.Effect);
                if(translatedGreeting.Effect==null)translatedGreeting.Effect=new WelcomeApple{Frost=.18};MapWallpaper(translatedGreeting,(WelcomeApple)translatedGreeting.Effect);
            }
        }
        private void MapWallpaper(FrameworkElement element,WelcomeApple effect)
        {if(element.ActualWidth<=0||element.ActualHeight<=0)return;var rect=element.TransformToAncestor(this).TransformBounds(new Rect(0,0,element.ActualWidth,element.ActualHeight));effect.Backdrop=wallpaperSampler;effect.Offset=new Point(rect.Left/ActualWidth,rect.Top/ActualHeight);effect.Extent=new Point(rect.Width/ActualWidth,rect.Height/ActualHeight);effect.Size=new Point(element.ActualWidth,element.ActualHeight);effect.DarkInk=Lettering.LightInk?0:1;effect.Wallpaper=1;}
        private void StartWallpaperMapping(){if(wallpaperRendering)return;wallpaperRendering=true;CompositionTarget.Rendering+=WallpaperFrame;}
        private void StopWallpaperMapping(){if(!wallpaperRendering)return;CompositionTarget.Rendering-=WallpaperFrame;wallpaperRendering=false;}
        private void WallpaperFrame(object sender,EventArgs e){if(dismissing||!IsVisible){StopWallpaperMapping();return;}UpdateWallpaperMapping();if(activeAnimations==0&&DateTime.UtcNow>=wallpaperDeadline)StopWallpaperMapping();}
        private async void RefreshInk()
        {
            if(SystemParameters.HighContrast)return;
            // Loaded can precede the first compositor frame. Give it one brief
            // settling interval, then retry only an occluded/unavailable sample.
            await System.Threading.Tasks.Task.Delay(120);bool? tone=null;
            for(int attempt=0;attempt<3;attempt++){if(dismissing||!IsVisible)return;tone=await LiquidMaterial.WelcomeTone(this);if(tone.HasValue)break;await System.Threading.Tasks.Task.Delay(100);}
            if(dismissing||!IsVisible||!tone.HasValue)return;
            bool light=tone.Value;Lettering.LightInk=light;Lettering.InvalidateVisual();
            Resources["GlassInk"]=light?Brushes.White:Design.Brush("#181818");Resources["GlassMuted"]=Design.Brush(light?"#D9E4F1":"#343434");Resources["GlassTileSurface"]=Design.Brush(light?"#35131A23":"#58FFFFFF");
            translatedGreeting.Foreground=light?WelcomeGlass.Brush("#FFFFFFFF","#55FFFFFF","#A0FFFFFF"):WelcomeGlass.Brush("#FF181818","#80181818","#FF181818");
            subtitleLabel.Foreground=Design.Brush(light?"#E5F0F6FF":"#F0181818");startLabel.Foreground=light?Brushes.White:Design.Brush("#181818");
            foreach(UIElement child in apple.Children){var layer=child as Grid;var effect=layer==null?null:layer.Effect as WelcomeApple;if(effect!=null)effect.DarkInk=light?0:1;}
        }
        private static Brush SceneBackdrop()
        {
            if(SystemParameters.HighContrast)return SystemColors.WindowBrush;
            var brush=new RadialGradientBrush{Center=new Point(.5,.46),GradientOrigin=new Point(.5,.46),RadiusX=.8,RadiusY=.85};
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(6,0,0,0),0));brush.GradientStops.Add(new GradientStop(Color.FromArgb(14,0,0,0),1));brush.Freeze();return brush;
        }
        private void Play()
        {
            if(!animated){appleScale.ScaleX=appleScale.ScaleY=.42;applePosition.Y=-150;Lettering.Progress=1;actionsPosition.Y=0;StartButton.IsEnabled=true;return;}
            Animate(surface,OpacityProperty,0,1,450,0);
            Animate(apple,OpacityProperty,0,1,700,0);
            // A finite timeline, with no perpetual render handler or polling timer.
            ScaleSequence(ScaleTransform.ScaleXProperty);ScaleSequence(ScaleTransform.ScaleYProperty);
            Animate(applePosition,TranslateTransform.YProperty,0,-150,850,950);
            Animate(Lettering,OpacityProperty,0,1,500,950);Lettering.Play(1050);
            Animate(actions,OpacityProperty,0,1,600,2400,delegate{StartButton.IsEnabled=true;});
            Animate(actionsPosition,TranslateTransform.YProperty,14,0,650,2350);
            languageTimer.Start();
        }
        internal int GreetingIndex{get{return greetingIndex;}}
        internal bool LanguageTimerRunning{get{return languageTimer.IsEnabled;}}
        internal FlowDirection GreetingDirection{get{return translatedGreeting.FlowDirection;}}
        internal void ChangeGreeting(int index)
        {
            if(dismissing||!animated||index<0||index>=Greetings.Length)return;
            languageTimer.Stop();languageTimer.Start();int generation=++greetingGeneration;
            // Local text and font fallback, no translation service or network.
            var fade=new DoubleAnimation(greetings.Opacity,0,TimeSpan.FromMilliseconds(340)){EasingFunction=new CubicEase{EasingMode=EasingMode.EaseInOut}};
            fade.Completed+=delegate
            {
                if(dismissing||generation!=greetingGeneration)return;greetingIndex=index;translatedGreeting.Text=Greetings[index];translatedGreeting.FlowDirection=index==10||index==11?FlowDirection.RightToLeft:FlowDirection.LeftToRight;
                bool script=Lettering.SetWord(Greetings[index]);Lettering.Visibility=script?Visibility.Visible:Visibility.Collapsed;translatedGreeting.Visibility=script?Visibility.Collapsed:Visibility.Visible;if(script)Lettering.Play(60);
                greetings.Opacity=1;greetings.BeginAnimation(OpacityProperty,new DoubleAnimation(0,1,TimeSpan.FromMilliseconds(560)){EasingFunction=new CubicEase{EasingMode=EasingMode.EaseOut},FillBehavior=FillBehavior.Stop});if(WallpaperVisible){wallpaperDeadline=DateTime.UtcNow.AddMilliseconds(650);StartWallpaperMapping();}
                greetingPosition.BeginAnimation(TranslateTransform.YProperty,new DoubleAnimation(8,0,TimeSpan.FromMilliseconds(560)){EasingFunction=new CubicEase{EasingMode=EasingMode.EaseOut},FillBehavior=FillBehavior.Stop});
            };greetings.BeginAnimation(OpacityProperty,fade);
        }
        private void ScaleSequence(DependencyProperty property)
        {
            var animation=new DoubleAnimationUsingKeyFrames();var ease=new CubicEase{EasingMode=EasingMode.EaseInOut};
            animation.KeyFrames.Add(new DiscreteDoubleKeyFrame(.86,KeyTime.FromTimeSpan(TimeSpan.Zero)));
            animation.KeyFrames.Add(new EasingDoubleKeyFrame(1,KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(950)),ease));
            animation.KeyFrames.Add(new EasingDoubleKeyFrame(1,KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(1100)),ease));
            animation.KeyFrames.Add(new EasingDoubleKeyFrame(.42,KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(1800)),ease));
            Timeline.SetDesiredFrameRate(animation,60);activeAnimations++;
            animation.Completed+=delegate{if(dismissing)return;appleScale.SetValue(property,.42);appleScale.BeginAnimation(property,null);activeAnimations--;};
            clearAnimations.Add(delegate{var current=appleScale.GetValue(property);appleScale.BeginAnimation(property,null);appleScale.SetValue(property,current);});appleScale.BeginAnimation(property,animation);
        }
        private void Animate(Animatable target,DependencyProperty property,double from,double to,int duration,int delay,Action done=null)
        {
            var animation=new DoubleAnimation(from,to,TimeSpan.FromMilliseconds(duration)){BeginTime=TimeSpan.FromMilliseconds(delay),EasingFunction=new CubicEase{EasingMode=EasingMode.EaseInOut}};
            Timeline.SetDesiredFrameRate(animation,60);activeAnimations++;
            animation.Completed+=delegate{if(dismissing)return;target.SetValue(property,to);target.BeginAnimation(property,null);activeAnimations--;if(done!=null)done();};
            clearAnimations.Add(delegate{var current=target.GetValue(property);target.BeginAnimation(property,null);target.SetValue(property,current);});target.BeginAnimation(property,animation);
        }
        private void Animate(UIElement target,DependencyProperty property,double from,double to,int duration,int delay,Action done=null)
        {
            var animation=new DoubleAnimation(from,to,TimeSpan.FromMilliseconds(duration)){BeginTime=TimeSpan.FromMilliseconds(delay),EasingFunction=new CubicEase{EasingMode=EasingMode.EaseInOut}};
            Timeline.SetDesiredFrameRate(animation,60);activeAnimations++;
            animation.Completed+=delegate{if(dismissing)return;target.SetValue(property,to);target.BeginAnimation(property,null);activeAnimations--;if(done!=null)done();};
            clearAnimations.Add(delegate{var current=target.GetValue(property);target.BeginAnimation(property,null);target.SetValue(property,current);});target.BeginAnimation(property,animation);
        }
        private void StopScene(){languageTimer.Stop();StopWallpaperMapping();greetings.BeginAnimation(OpacityProperty,null);greetingPosition.BeginAnimation(TranslateTransform.YProperty,null);foreach(var clear in clearAnimations)clear();clearAnimations.Clear();double current=Lettering.Progress;Lettering.BeginAnimation(HelloLettering.ProgressProperty,null);Lettering.Progress=current;activeAnimations=0;}
        private static void RemoveFrame(Window window){var h=new System.Windows.Interop.WindowInteropHelper(window).Handle;int disabled=1;Native.DwmSetWindowAttribute(h,33,ref disabled,4);Native.DwmSetWindowAttribute(h,2,ref disabled,4);int noBorder=unchecked((int)0xFFFFFFFE);Native.DwmSetWindowAttribute(h,34,ref noBorder,4);}
        private static void Cover(Window window,System.Drawing.Rectangle rect){Native.SetWindowPos(new System.Windows.Interop.WindowInteropHelper(window).Handle,new IntPtr(-1),rect.Left,rect.Top,rect.Width,rect.Height,0x50);}
        internal double ContentOpacity{get{return surface.Opacity;}}
        internal bool ContentFits{get{var current=(FrameworkElement)content.Child;var rect=current.TransformToAncestor(this).TransformBounds(new Rect(current.RenderSize));return rect.Top>=0&&rect.Left>=0&&rect.Bottom<=ActualHeight+.5&&rect.Right<=ActualWidth+.5;}}
        internal bool LogoVisible{get{return apple.Opacity>.5;}}
        internal bool Ready{get{return StartButton.IsEnabled&&Lettering.Progress==1&&activeAnimations==0;}}
        private void ContinueWelcome()
        {
            if(dismissing)return;
            Advancing=true;
            if(finalGreeting){var enter=EnterDesktop;EnterDesktop=null;if(enter!=null)Closed+=delegate{enter();};DismissWelcome();return;}
            if(afterHello!=null){Closed+=delegate{afterHello();};DismissWelcome();return;}
            if(!showNews||NewsVisible){DismissWelcome();return;}
            StopScene();NewsVisible=true;StartButton.IsDefault=false;
            var news=new Grid{Width=580,Height=450};var stack=new StackPanel{Margin=new Thickness(24,10,24,8)};news.Children.Add(stack);
            var version=Design.Text("MACFLOW "+Product.VersionText,11,true);version.Margin=new Thickness(0,0,0,8);stack.Children.Add(version);
            var heading=Design.Text("Un escritorio, en armonía.",28,true);heading.Margin=new Thickness(0,0,0,8);stack.Children.Add(heading);
            var description=Design.Text("Estas son las novedades de tu actualización.",13,false);description.SetResourceReference(TextBlock.ForegroundProperty,"GlassMuted");description.Margin=new Thickness(0,0,0,22);stack.Children.Add(description);
            string[] icons={"grid","wifi","windows"};string[] names={"Un Dock coordinado","Iconos claros, estados reales","Movimientos con continuidad"};string[] details={"Ampliación suave y espacio para los iconos vecinos.","Wi-Fi, sonido y controles con una geometría coherente.","Paneles hacia el Dock y cambios de sección sin cortes."};
            for(int i=0;i<names.Length;i++)
            {
                var row=new DockPanel();var glyph=new Border{Child=Design.Symbol(icons[i],24,Design.Ink),Margin=new Thickness(0,0,14,0),VerticalAlignment=VerticalAlignment.Center};DockPanel.SetDock(glyph,Dock.Left);row.Children.Add(glyph);
                var labels=new StackPanel();labels.Children.Add(Design.Text(names[i],14,true));var detail=Design.Text(details[i],12,false);detail.SetResourceReference(TextBlock.ForegroundProperty,"GlassMuted");detail.TextWrapping=TextWrapping.Wrap;detail.Margin=new Thickness(0,5,0,0);labels.Children.Add(detail);row.Children.Add(labels);
                var card=new GlassPiece(row,16,new Thickness(16,12,16,12)){Margin=new Thickness(0,0,0,10)};stack.Children.Add(card);
                if(animated){card.Opacity=0;Animate(card,OpacityProperty,0,1,240,80+i*65);}
            }
            ContinueButton=Design.Button("Comenzar",DismissWelcome,true);ContinueButton.Width=168;ContinueButton.Height=44;ContinueButton.IsDefault=true;ContinueButton.Margin=new Thickness(0,16,0,0);ContinueButton.HorizontalAlignment=HorizontalAlignment.Center;stack.Children.Add(ContinueButton);
            var host=new Grid{Width=580,Height=450};var previous=(FrameworkElement)content.Child;content.Child=null;content.Child=host;ContentTransition.Show(host,previous,news,0);
        }
        internal void DismissWelcome()
        {
            if(dismissing)return;dismissing=true;double opacity=surface.Opacity;StopScene();surface.Opacity=opacity;surface.IsHitTestVisible=false;
            if(!animated){Close();return;}
            bool finished=false;var fallback=new System.Windows.Threading.DispatcherTimer{Interval=TimeSpan.FromMilliseconds(400)};
            Action finish=delegate{if(finished)return;finished=true;fallback.Stop();Close();};fallback.Tick+=delegate{finish();};Closed+=delegate{finished=true;fallback.Stop();};fallback.Start();
            var animation=new DoubleAnimation(opacity,0,TimeSpan.FromMilliseconds(320)){EasingFunction=new CubicEase{EasingMode=EasingMode.EaseInOut}};animation.Completed+=delegate{finish();};surface.BeginAnimation(OpacityProperty,animation);
        }
    }
}

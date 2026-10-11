using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
namespace MacFlow
{
    // Bézier lettering adapted from kumailnanji/letters (MIT).
    // THIRD-PARTY-NOTICES.md includes the full license. No Apple assets.
    internal sealed class HelloLettering : FrameworkElement
    {
        private sealed class Stroke {internal Point[] Points;internal double[] Distances;internal double Length;}
        private readonly List<Stroke> strokes=new List<Stroke>();
        private double total;
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
        internal void Play()
        {
            BeginAnimation(ProgressProperty,null);Progress=Design.Animations?0:1;if(!Design.Animations)return;
            var animation=new DoubleAnimation(0,1,TimeSpan.FromMilliseconds(1800)){BeginTime=TimeSpan.FromMilliseconds(200),EasingFunction=new CubicEase{EasingMode=EasingMode.EaseInOut}};
            Timeline.SetDesiredFrameRate(animation,60);animation.Completed+=delegate{Progress=1;BeginAnimation(ProgressProperty,null);};BeginAnimation(ProgressProperty,animation);
        }
        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);double budget=total*Math.Max(0,Math.Min(1,Progress));double scale=Math.Min(ActualWidth/54,ActualHeight/28);
            dc.PushTransform(new MatrixTransform(scale,0,0,scale,(ActualWidth-49*scale)/2,(ActualHeight-22*scale)/2+12*scale));
            var brush=new LinearGradientBrush(Design.Brush(Design.Dark?"#FFFFFF":"#202A35").Color,Design.Brush(Design.Dark?"#D9E9FF":"#344E75").Color,20);brush.Freeze();var pen=new Pen(brush,1.35){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round,LineJoin=PenLineJoin.Round};pen.Freeze();
            foreach(var stroke in strokes)
            {
                if(budget<=0)break;double visible=Math.Min(stroke.Length,budget);var path=new StreamGeometry();using(var context=path.Open()){context.BeginFigure(stroke.Points[0],false,false);for(int i=1;i<stroke.Points.Length;i++){if(stroke.Distances[i]<=visible)context.LineTo(stroke.Points[i],true,false);else{double span=stroke.Distances[i]-stroke.Distances[i-1];double t=span==0?0:(visible-stroke.Distances[i-1])/span;context.LineTo(stroke.Points[i-1]+(stroke.Points[i]-stroke.Points[i-1])*t,true,false);break;}}}path.Freeze();dc.DrawGeometry(null,pen,path);budget-=stroke.Length;
            }dc.Pop();
        }
    }
    internal sealed class WelcomeWindow : Window
    {
        internal readonly HelloLettering Lettering=new HelloLettering();
        internal readonly Button StartButton;
        private readonly System.Drawing.Rectangle bounds;
        private readonly Grid surface;
        private readonly List<Window> companions=new List<Window>();
        private bool dismissing;
        internal WelcomeWindow(System.Drawing.Rectangle? testBounds=null)
        {
            var primary=System.Windows.Forms.Screen.PrimaryScreen;bounds=testBounds??primary.Bounds;
            Title="MacFlow · Hola";Width=bounds.Width;Height=bounds.Height;Left=bounds.Left;Top=bounds.Top;WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;AllowsTransparency=false;Background=Backdrop();ShowInTaskbar=false;Topmost=true;
            surface=new Grid{Background=Brushes.Transparent,Opacity=Design.Animations?0:1};
            var content=new StackPanel{HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center};
            var brand=Design.Text("MacFlow",14,true);brand.HorizontalAlignment=HorizontalAlignment.Center;brand.Margin=new Thickness(0,0,0,24);content.Children.Add(brand);
            Lettering.Width=Math.Max(300,Math.Min(520,bounds.Width*.27));Lettering.Height=Lettering.Width*.5;Lettering.HorizontalAlignment=HorizontalAlignment.Center;Lettering.Margin=new Thickness(0,0,0,20);content.Children.Add(Lettering);
            var subtitle=Design.Text("Tu escritorio, a tu manera.",15,false);subtitle.HorizontalAlignment=HorizontalAlignment.Center;subtitle.Margin=new Thickness(0,0,0,28);content.Children.Add(subtitle);
            StartButton=Design.Button("Iniciar",DismissWelcome,false);StartButton.Width=170;StartButton.Height=46;StartButton.HorizontalAlignment=HorizontalAlignment.Center;StartButton.IsDefault=true;StartButton.FontSize=14;StartButton.BorderThickness=new Thickness(0);StartButton.Background=Brushes.Transparent;StartButton.Content=new GlassPiece(Design.Text("Iniciar",14,true),22,new Thickness(12,8,12,8));StartButton.Padding=new Thickness(0);content.Children.Add(StartButton);
            surface.Children.Add(new Viewbox{Stretch=Stretch.Uniform,StretchDirection=StretchDirection.DownOnly,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(24),Child=content});Content=surface;
            SizeChanged+=delegate{Lettering.Width=Math.Max(180,Math.Min(520,Math.Min(ActualWidth*.34,Math.Max(80,ActualHeight-215)*2)));Lettering.Height=Lettering.Width*.5;};
            SourceInitialized+=delegate{RemoveFrame(this);};
            Loaded+=delegate
            {
                if(!testBounds.HasValue)foreach(var screen in System.Windows.Forms.Screen.AllScreens)
                {
                    if(screen.Primary)continue;var rect=screen.Bounds;
                    var companion=new Window{Title="MacFlow · Hola",Width=rect.Width,Height=rect.Height,Left=rect.Left,Top=rect.Top,Background=Backdrop(),WindowStyle=WindowStyle.None,ResizeMode=ResizeMode.NoResize,ShowInTaskbar=false,Topmost=true,ShowActivated=false};
                    companion.SourceInitialized+=delegate{RemoveFrame(companion);Native.NoActivate(companion);};companion.Loaded+=delegate{Cover(companion,rect);};companions.Add(companion);companion.Show();
                }
                Cover(this,bounds);Lettering.Play();if(Design.Animations)surface.BeginAnimation(OpacityProperty,new DoubleAnimation(0,1,TimeSpan.FromMilliseconds(360)){EasingFunction=new CubicEase{EasingMode=EasingMode.EaseOut}});
            };
            Closed+=delegate{Lettering.BeginAnimation(HelloLettering.ProgressProperty,null);surface.BeginAnimation(OpacityProperty,null);foreach(var window in companions)window.Close();companions.Clear();};
            KeyDown+=delegate(object sender,KeyEventArgs e){if(e.Key==Key.Escape){e.Handled=true;DismissWelcome();}};
        }
        private static Brush Backdrop(){var brush=new LinearGradientBrush(Design.Brush(Design.Dark?"#111722":"#F5F7FC").Color,Design.Brush(Design.Dark?"#1A2434":"#E5EBF6").Color,90);brush.Freeze();return brush;}
        private static void RemoveFrame(Window window){var h=new System.Windows.Interop.WindowInteropHelper(window).Handle;int disabled=1;Native.DwmSetWindowAttribute(h,33,ref disabled,4);Native.DwmSetWindowAttribute(h,2,ref disabled,4);int noBorder=unchecked((int)0xFFFFFFFE);Native.DwmSetWindowAttribute(h,34,ref noBorder,4);}
        private static void Cover(Window window,System.Drawing.Rectangle rect){Native.SetWindowPos(new System.Windows.Interop.WindowInteropHelper(window).Handle,new IntPtr(-1),rect.Left,rect.Top,rect.Width,rect.Height,0x50);}
        internal double ContentOpacity{get{return surface.Opacity;}}
        internal bool ContentFits{get{var content=((Viewbox)surface.Children[0]).Child as FrameworkElement;var rect=content.TransformToAncestor(this).TransformBounds(new Rect(content.RenderSize));return rect.Top>=0&&rect.Left>=0&&rect.Bottom<=ActualHeight+.5&&rect.Right<=ActualWidth+.5;}}
        internal void DismissWelcome()
        {
            if(dismissing)return;dismissing=true;surface.IsHitTestVisible=false;
            if(!Design.Animations){Close();return;}
            bool finished=false;var fallback=new System.Windows.Threading.DispatcherTimer{Interval=TimeSpan.FromMilliseconds(320)};
            Action finish=delegate{if(finished)return;finished=true;fallback.Stop();Close();};fallback.Tick+=delegate{finish();};Closed+=delegate{finished=true;fallback.Stop();};fallback.Start();
            var animation=new DoubleAnimation(surface.Opacity,0,TimeSpan.FromMilliseconds(240)){EasingFunction=new CubicEase{EasingMode=EasingMode.EaseInOut}};animation.Completed+=delegate{finish();};surface.BeginAnimation(OpacityProperty,animation);
        }
    }
}

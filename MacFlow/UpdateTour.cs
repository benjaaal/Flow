using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
namespace MacFlow
{
    // Full-screen presentation with real demo controls; no screenshot UI.
    internal sealed class UpdateTour : Window
    {
        private readonly Desktop desktop;
        private readonly TextBlock title,description,step;
        private readonly Button forward,back;
        private readonly Image wallpaper=new Image{Stretch=Stretch.UniformToFill};
        private readonly Grid preview=new Grid{Height=400};
        private readonly List<Window> companions=new List<Window>();
        private readonly System.Drawing.Rectangle bounds;
        private Window example,cursor;
        private StackPanel dockItems;
        private DockMotion dockMotion;
        private ActivityIsland islandPreview;
        private int index;
        private bool rendering,closed;
        private readonly Stopwatch elapsed=new Stopwatch();
        internal int Step{get{return index;}}
        internal bool FrameRunning{get{return rendering;}}
        internal bool Completed{get;private set;}
        internal bool PreviewVisible{get{return example!=null&&example.IsVisible;}}
        internal bool WallpaperVisible{get{return wallpaper.Source!=null;}}
        internal string CurrentTitle{get{return title.Text;}}
        internal Window Example{get{return example;}}
        internal bool PreviewContained
        {
            get{if(example==null||!example.IsVisible)return false;var area=preview.TransformToAncestor(this).TransformBounds(new Rect(preview.RenderSize));var a=PointToScreen(area.TopLeft);var b=PointToScreen(area.BottomRight);Native.RECT actual;return Native.GetWindowRect(new WindowInteropHelper(example).Handle,out actual)&&actual.Top>=a.Y-2&&actual.Bottom<=b.Y+2&&actual.Left>=a.X-2&&actual.Right<=b.X+2;}
        }
        internal UpdateTour(Desktop desktop)
        {
            this.desktop=desktop;desktop.BeginPresentation();wallpaper.Source=desktop.PresentationWallpaper;
            bounds=desktop.IsTesting?new System.Drawing.Rectangle(100,50,1000,850):System.Windows.Forms.Screen.PrimaryScreen.Bounds;
            Title="MacFlow · Descubrí las novedades";Width=bounds.Width;Height=bounds.Height;Left=bounds.Left;Top=bounds.Top;
            WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;ShowInTaskbar=false;Topmost=true;Background=Brushes.Black;
            var root=new Grid{Background=Brushes.Black};root.Children.Add(wallpaper);root.Children.Add(new Border{Background=Design.Brush("#46000000"),IsHitTestVisible=false});
            var scene=new Grid{Width=900,Height=660};scene.RowDefinitions.Add(new RowDefinition{Height=new GridLength(150)});scene.RowDefinitions.Add(new RowDefinition{Height=new GridLength(400)});scene.RowDefinitions.Add(new RowDefinition{Height=new GridLength(110)});
            var header=new StackPanel{HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center};
            step=Design.Text("",12,true);step.Foreground=Design.Brush("#BBFFFFFF");step.HorizontalAlignment=HorizontalAlignment.Center;header.Children.Add(step);
            title=Design.Text("",32,true);title.Foreground=WelcomeGlass.Brush("#FAFFFFFF","#AEFFFFFF","#F5FFFFFF");title.Effect=new System.Windows.Media.Effects.DropShadowEffect{BlurRadius=8,ShadowDepth=1,Opacity=.28,Color=Colors.Black};title.Margin=new Thickness(0,12,0,10);title.HorizontalAlignment=HorizontalAlignment.Center;header.Children.Add(title);
            description=Design.Text("",15,true);description.Foreground=Design.Brush("#F0FFFFFF");description.Effect=new System.Windows.Media.Effects.DropShadowEffect{BlurRadius=6,ShadowDepth=1,Opacity=.4,Color=Colors.Black};description.TextWrapping=TextWrapping.Wrap;description.TextAlignment=TextAlignment.Center;description.MaxWidth=700;header.Children.Add(description);scene.Children.Add(header);
            Grid.SetRow(preview,1);scene.Children.Add(preview);
            var buttons=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center};
            buttons.Children.Add(TourButton("Omitir",Close));back=TourButton("Atrás",Back);buttons.Children.Add(back);forward=Design.Button("Siguiente",Advance,true);buttons.Children.Add(forward);buttons.Children.Add(TourButton("Ver de nuevo",Repeat));
            foreach(Button button in buttons.Children){button.MinWidth=104;button.Height=44;button.FontSize=14;if(button!=forward)button.Foreground=Brushes.White;}
            Grid.SetRow(buttons,2);scene.Children.Add(buttons);root.Children.Add(new Viewbox{Child=scene,Stretch=Stretch.Uniform,StretchDirection=StretchDirection.DownOnly,Margin=new Thickness(24)});Content=root;
            SourceInitialized+=delegate{Native.OpaquePresentation(this);};
            Loaded+=delegate{Cover(this,bounds);if(!desktop.IsTesting)CreateCompanions();ShowStep();LoadWallpaper();};
            Closed+=delegate{closed=true;ClearExample();foreach(var companion in companions)companion.Close();companions.Clear();};
            KeyDown+=delegate(object s,KeyEventArgs e){if(e.Key==Key.Escape){e.Handled=true;Close();}};
            if(desktop.IsTesting){IsHitTestVisible=false;ShowActivated=false;SourceInitialized+=delegate{Native.NoActivate(this);};}
        }
        private async void LoadWallpaper()
        {if(wallpaper.Source!=null)return;var image=await WelcomeWallpaper.Load(desktop.Preferences.WallpaperPath);if(closed||image==null)return;wallpaper.Source=image;foreach(var companion in companions)((Image)companion.Content).Source=image;}
        private static Button TourButton(string text,Action action)
        {var label=Design.Text(text,14,true);label.Foreground=Brushes.White;var button=Design.Button("",action,false);button.Content=new GlassPiece(label,22,new Thickness(18,10,18,10));button.Background=Brushes.Transparent;button.Padding=new Thickness(0);return button;}
        private void CreateCompanions()
        {
            foreach(var screen in System.Windows.Forms.Screen.AllScreens)
            {
                if(screen.Primary)continue;var rect=screen.Bounds;var image=new Image{Source=wallpaper.Source,Stretch=Stretch.UniformToFill};
                var companion=new Window{Title="MacFlow · Presentación",Background=Brushes.Black,Content=image,WindowStyle=WindowStyle.None,ResizeMode=ResizeMode.NoResize,ShowInTaskbar=false,Topmost=true,ShowActivated=false};
                companion.SourceInitialized+=delegate{Native.OpaquePresentation(companion);Native.NoActivate(companion);};companion.Loaded+=delegate{Cover(companion,rect);};companions.Add(companion);companion.Show();
            }
        }
        private static void Cover(Window window,System.Drawing.Rectangle rect){Native.SetWindowPos(new WindowInteropHelper(window).Handle,new IntPtr(-1),rect.Left,rect.Top,rect.Width,rect.Height,0x50);}
        internal void Advance(){if(closed)return;ClearExample();if(++index>=6){Completed=true;Close();return;}ShowStep();}
        internal void Back(){if(closed||index==0)return;ClearExample();index--;ShowStep();}
        internal void Repeat(){if(closed)return;ClearExample();ShowStep();}
        private void ShowStep()
        {
            step.Text="MACFLOW "+Product.VersionText+"  ·  "+(index+1)+" / 6";back.IsEnabled=index>0;
            switch(index)
            {
                case 0:
                    title.Text="Todo empieza en el Dock.";description.Text="Acercá el cursor y los iconos crecen dentro del cristal. Tus aplicaciones, siempre a mano.";CreateDockPreview();break;
                case 1:
                    title.Text="Deslizá. Y tomá el control.";description.Text="Arrastrá hacia abajo desde la batería o los indicadores con clic izquierdo o derecho. Música, brillo y sonido comparten una distribución compacta; los accesos quedan debajo.";
                    example=new ControlCenter(desktop);var controlBody=(UIElement)example.Content;example.Content=null;example.Content=new Viewbox{Child=controlBody,Stretch=Stretch.Uniform};ShowExample();SlidePreview(80);CreatePointer(example.Width-34,28,160);break;
                case 2:
                    title.Text="Un momento para vos.";description.Text="Ctrl + Windows abre Privacidad: reloj, música y avisos, mientras tus tareas siguen. Creá tu PIN y registrá tu rostro localmente. Ctrl + Windows + Alt recupera el escritorio.";
                    example=new PrivacyScreen(desktop,true){Width=560,Height=400};ShowExample();SlidePreview(18);break;                case 3:
                    title.Text="Pequeños espacios, más tuyos.";description.Text="Galería en dos columnas, datos grandes y materiales consistentes: transparente, oscuro o claro. Usá su menú para elegir tamaño y acomodalos arrastrando el fondo.";
                    example=new DesktopWidget(desktop.Widgets,new WidgetSpec{Kind="Calendar",Size="S",Surface="Dark"}){Width=164,Height=164};ShowExample();SlidePreview(18);break;
                case 4:
                    title.Text="Lo que pasa, siempre a la vista.";description.Text="La cápsula negra reúne música, temporizadores y acciones recientes. En reposo no reacciona al clic. Con actividad, se despliega debajo de la barra sin cubrirla. Los avisos de MacFlow bajan desde la manzana con un rebote suave.";
                    var provider=new MediaFixture();provider.State.Playing=true;provider.State.Title="Música · Demostración";provider.State.Artist="Controles conectados al reproductor de Windows";islandPreview=new ActivityIsland(desktop,provider,true);example=islandPreview.View;ShowExample();SlidePreview(18);break;
                case 5:
                    title.Text="Preguntá con tus palabras.";description.Text="El asistente distingue acciones locales y preguntas libres. Las preguntas se envían a Gemini en Chrome; la luz acompaña el trabajo y se apaga al terminar. Incluye reconocimiento local en español; Hey Siri activa la escucha.";
                    // Read-only instance of the actual menu: a tour must never
                    // execute a power action or change the user's preferences.
                    example=new AssistantWindow(desktop,null);ShowExample();SlidePreview(24);break;
            }
            Motion.Fade(title,0,1,300);Motion.Fade(description,0,1,380);
            forward.Content=index==5?"Continuar":"Siguiente";
        }
        private void SlidePreview(double distance)
        {
            if(example==null||!Motion.Enabled)return;double top=example.Top;
            var slide=new DoubleAnimation(top-distance,top,TimeSpan.FromMilliseconds(600)){EasingFunction=Motion.Ease(),FillBehavior=FillBehavior.Stop};Timeline.SetDesiredFrameRate(slide,60);example.BeginAnimation(Window.TopProperty,slide);Motion.Fade(example,.15,1,400);
        }
        private void CreatePointer(double x,double y,double travel)
        {
            CreateCursor();cursor.Left=example.Left+x;double top=(double)example.GetAnimationBaseValue(Window.TopProperty)+y;cursor.Top=top+travel;
            if(!Motion.Enabled)return;
            var slide=new DoubleAnimation(top,top+travel,TimeSpan.FromMilliseconds(850)){EasingFunction=Motion.Ease(),BeginTime=TimeSpan.FromMilliseconds(150),FillBehavior=FillBehavior.Stop};Timeline.SetDesiredFrameRate(slide,60);cursor.BeginAnimation(Window.TopProperty,slide);
        }
        private void ShowExample()
        {
            if(example==null)return;preview.UpdateLayout();var rect=preview.TransformToAncestor(this).TransformBounds(new Rect(preview.RenderSize));var corner=PointToScreen(rect.TopLeft);var bottom=PointToScreen(rect.BottomRight);var parentSource=PresentationSource.FromVisual(this);double scale=parentSource.CompositionTarget.TransformToDevice.M11;
            double maxHeight=(bottom.Y-corner.Y-20)/scale;if(example.Height>maxHeight){double ratio=maxHeight/example.Height;example.Height=maxHeight;if(example.Content is Viewbox)example.Width*=ratio;}
            example.Owner=this;example.ShowActivated=false;example.IsHitTestVisible=index==0&&!desktop.IsTesting;example.Show();
            var center=PointToScreen(new Point(rect.Left+rect.Width/2,rect.Top+rect.Height/2));var source=PresentationSource.FromVisual(example);scale=source.CompositionTarget.TransformToDevice.M11;
            Native.SetWindowPos(new WindowInteropHelper(example).Handle,IntPtr.Zero,(int)(center.X-example.Width*scale/2),(int)(center.Y-example.Height*scale/2),0,0,0x15);
        }
        private void CreateDockPreview()
        {
            const int size=44;dockItems=new StackPanel{Orientation=Orientation.Horizontal,VerticalAlignment=VerticalAlignment.Center,HorizontalAlignment=HorizontalAlignment.Center};
            var apps=desktop.Preferences.Pins.Take(6).ToList();if(apps.Count==0)apps=desktop.Apps.Take(6).ToList();if(apps.Count==0)apps.Add(new AppEntry{Name="MacFlow",Target="MacFlow.exe",Executable="MacFlow.exe"});
            foreach(var app in apps)
            {
                var icon=DockAppMotion.Host(DockAppMotion.Host(Design.AppIcon(app,size)));icon.RenderTransform=new ScaleTransform(1,1);icon.RenderTransformOrigin=new Point(.5,.5);icon.Tag=app.Key;
                var cell=new StackPanel{Width=DockMetrics.Slot(size),Tag=icon,VerticalAlignment=VerticalAlignment.Center};var button=Design.Button("",delegate{DockAppMotion.Play(icon,false);},false);button.Content=icon;button.Background=Brushes.Transparent;button.RenderTransform=null;button.Padding=new Thickness(0);button.Margin=new Thickness(0);button.Width=DockMetrics.Slot(size);button.Height=DockMetrics.Row(size);button.ToolTip=app.Name;cell.Children.Add(button);dockItems.Children.Add(cell);
            }
            dockMotion=new DockMotion(dockItems,false,size);dockItems.MouseMove+=delegate(object s,MouseEventArgs e){dockMotion.Move(e.GetPosition(dockItems));};dockItems.MouseLeave+=delegate{dockMotion.Leave();};
            example=Design.Panel("MacFlow Dock · Demostración",apps.Count*DockMetrics.Slot(size)+DockMetrics.Reserve(size)*2+12,DockMetrics.Thickness(size));var glass=Design.Glass(dockItems,18,true,true);glass.Padding=new Thickness(DockMetrics.Padding);example.Content=glass;ShowExample();
            CreateCursor();
            if(Motion.Enabled){elapsed.Restart();rendering=true;CompositionTarget.Rendering+=Frame;}else MovePointer(.5);
        }
        private void CreateCursor()
        {
            cursor=new Window{Width=28,Height=36,AllowsTransparency=true,Background=Brushes.Transparent,WindowStyle=WindowStyle.None,ResizeMode=ResizeMode.NoResize,Topmost=true,ShowInTaskbar=false,ShowActivated=false,IsHitTestVisible=false,Owner=this};
            cursor.Content=new Path{Data=Geometry.Parse("M3,2 L3,26 L9,20 L14,31 L19,29 L14,18 L23,18 Z"),Fill=Brushes.White,Stroke=Design.Brush("#233142"),StrokeThickness=1.4};cursor.SourceInitialized+=delegate{var h=new WindowInteropHelper(cursor).Handle;Native.SetWindowLong(h,-20,Native.GetWindowLong(h,-20)|0x20|0x80|0x08000000);};cursor.Show();
        }
        private void Frame(object sender,EventArgs e)
        {if(closed||cursor==null){StopFrames();return;}double progress=Math.Min(1,elapsed.Elapsed.TotalSeconds/4.8);double fraction=.12+.76*(.5-.5*Math.Cos(Math.PI*progress));MovePointer(fraction);if(progress>=1){StopFrames();if(dockMotion!=null)dockMotion.EndProbe();if(cursor!=null)cursor.Close();cursor=null;}}
        private void MovePointer(double fraction)
        {if(cursor==null||dockItems==null||example==null||!example.IsVisible)return;var p=dockItems.PointToScreen(new Point(dockItems.ActualWidth*fraction,dockItems.ActualHeight*.55));var source=PresentationSource.FromVisual(cursor);if(source!=null)p=source.CompositionTarget.TransformFromDevice.Transform(p);cursor.Left=p.X;cursor.Top=p.Y;if(dockMotion!=null)dockMotion.Probe(fraction);}
        private void StopFrames(){if(rendering){CompositionTarget.Rendering-=Frame;rendering=false;}elapsed.Stop();}
        private void ClearExample(){StopFrames();if(cursor!=null){cursor.Close();cursor=null;}if(dockMotion!=null){dockMotion.Dispose();dockMotion=null;}dockItems=null;if(islandPreview!=null){islandPreview.Dispose();islandPreview=null;example=null;}if(example!=null){example.Close();example=null;}}
    }
}

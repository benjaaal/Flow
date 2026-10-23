using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MacFlow
{
    public sealed class Launchpad : Window
    {
        private readonly WrapPanel grid;
        private readonly TextBox search;
        private readonly Desktop desktop;
        private readonly TextBlock count;
        private bool pinnedOnly;
        private readonly Button allApps,pinnedApps;
        internal Button CloseButton { get; private set; }
        internal Button MinimizeButton { get; private set; }
        internal Button ZoomButton { get; private set; }
        internal int VisibleAppCount {get{return grid.Children.Count;}}
        internal string SearchQuery {get{return search.Text;}set{search.Text=value;}}
        public Launchpad(Desktop controller)
        {
            desktop=controller;Title="MacOS · Aplicaciones";Width=770;Height=540;WindowStyle=WindowStyle.None;AllowsTransparency=false;ResizeMode=ResizeMode.NoResize;Topmost=true;ShowInTaskbar=true;Background=Brushes.Transparent;
            var root=new DockPanel();var header=new DockPanel{Margin=new Thickness(0,0,0,17)};
            var controls=new WindowControls(this,delegate{Design.Dismiss(this);}){Margin=new Thickness(0,0,17,0)};DockPanel.SetDock(controls,System.Windows.Controls.Dock.Left);header.Children.Add(controls);CloseButton=controls.CloseButton;MinimizeButton=controls.MinimizeButton;ZoomButton=controls.ZoomButton;
            var name=new StackPanel();name.Children.Add(Design.Text("Aplicaciones",20,true));count=Design.Text("",11,false);count.SetResourceReference(TextBlock.ForegroundProperty,"GlassMuted");count.Margin=new Thickness(0,4,0,0);name.Children.Add(count);header.Children.Add(name);DockPanel.SetDock(header,System.Windows.Controls.Dock.Top);root.Children.Add(header);header.MouseLeftButtonDown+=delegate(object sender,MouseButtonEventArgs e){if(e.OriginalSource is TextBlock)DragMove();};
            var toolbar=new Grid{Margin=new Thickness(0,0,0,17)};toolbar.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});toolbar.ColumnDefinitions.Add(new ColumnDefinition());
            var filters=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(0,0,18,0)};
            allApps=Design.Button("Todas las apps",delegate{pinnedOnly=false;Refresh();},false);allApps.Margin=new Thickness(0,0,5,0);filters.Children.Add(allApps);
            pinnedApps=Design.Button("En el Dock",delegate{pinnedOnly=true;Refresh();},false);pinnedApps.Margin=new Thickness(0);filters.Children.Add(pinnedApps);toolbar.Children.Add(filters);
            var field=new DockPanel();field.Children.Add(new Border{Child=Design.Symbol("search",16,Design.Muted),Margin=new Thickness(10,0,8,0),VerticalAlignment=VerticalAlignment.Center});search=new TextBox{FontSize=13,Padding=new Thickness(0,8,8,8),Background=Brushes.Transparent,BorderThickness=new Thickness(0)};search.SetResourceReference(Control.ForegroundProperty,"GlassInk");search.SetResourceReference(TextBox.CaretBrushProperty,"GlassInk");System.Windows.Automation.AutomationProperties.SetName(search,"Buscar aplicaciones");field.Children.Add(search);
            var searchPlate=new Border{Child=new GlassPiece(field,12,new Thickness(0)),CornerRadius=new CornerRadius(12)};Grid.SetColumn(searchPlate,1);toolbar.Children.Add(searchPlate);DockPanel.SetDock(toolbar,System.Windows.Controls.Dock.Top);root.Children.Add(toolbar);
            grid=new WrapPanel{HorizontalAlignment=HorizontalAlignment.Center};var browser=new ScrollViewer{Content=grid,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,Padding=new Thickness(8,8,8,8)};
            root.Children.Add(browser);Content=Design.Glass(root,22,false);
            search.TextChanged+=delegate{Refresh();};PreviewKeyDown+=delegate(object sender,KeyEventArgs e){if(e.Key==Key.Escape){e.Handled=true;Design.Dismiss(this);}};SourceInitialized+=delegate{Native.Glass(this,Design.Dark);};Loaded+=delegate{if(!desktop.IsTesting)search.Focus();};Deactivated+=delegate{if(!desktop.IsTesting)Dispatcher.BeginInvoke(new Action(delegate{if(IsVisible&&WindowState!=WindowState.Minimized&&!IsActive&&!(GlassMenu.Current!=null&&GlassMenu.Current.Owner==this))Design.Dismiss(this);}));};Refresh();
        }
        public void Refresh()
        {
            allApps.Background=pinnedOnly?Brushes.Transparent:Design.Brush("#007AFF");pinnedApps.Background=pinnedOnly?Design.Brush("#007AFF"):Brushes.Transparent;
            allApps.Resources["GlassInk"]=pinnedOnly?Design.Ink:Brushes.White;pinnedApps.Resources["GlassInk"]=pinnedOnly?Brushes.White:Design.Ink;
            grid.Children.Clear();var apps=desktop.Apps.Where(p=>(!pinnedOnly||desktop.Preferences.Pins.Any(pin=>pin.Key==p.Key))&&System.Globalization.CultureInfo.CurrentCulture.CompareInfo.IndexOf(p.Name,search.Text,System.Globalization.CompareOptions.IgnoreCase|System.Globalization.CompareOptions.IgnoreNonSpace)>=0).ToList();count.Text=desktop.AppCount==0?"Preparando tus aplicaciones…":apps.Count+" aplicaciones";
            foreach(var app in apps.Take(180))
            {
                var captured=app;var content=new StackPanel();content.Children.Add(new Border {Child=Design.AppIcon(app,48),HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(0,0,0,9)});var label=Design.Text(app.Name,11,false);label.TextAlignment=TextAlignment.Center;label.TextWrapping=TextWrapping.Wrap;label.MaxHeight=30;content.Children.Add(label);
                var b=Design.Button("",delegate{Design.Dismiss(this,delegate{desktop.Launch(captured);});},false);b.Content=content;b.Width=98;b.Height=99;b.Padding=new Thickness(7);b.Background=Brushes.Transparent;b.BorderThickness=new Thickness(0);b.Margin=new Thickness(3,2,3,2);b.ToolTip=app.Name;b.PreviewMouseRightButtonUp+=delegate(object sender,MouseButtonEventArgs e){GlassMenu.Open(this,app.Name,new List<GlassCommand> {new GlassCommand(desktop.Preferences.Pins.Any(p=>p.Key==captured.Key)?"Quitar del Dock":"Fijar en el Dock",delegate {desktop.Pin(captured);Refresh();})});e.Handled=true;};grid.Children.Add(b);
            }
        }
    }
    public sealed class WindowOverview : Window
    {
        private readonly List<Thumbnail> thumbnails=new List<Thumbnail>();
        internal readonly string AppKey;
        public WindowOverview(Desktop desktop,string appKey=null,string appName=null)
        {
            AppKey=appKey;var windows=desktop.Windows.Where(w=>appKey==null||w.Key==appKey).Take(60).ToList();
            Title="MacOS · Ventanas abiertas";Width=920;Height=610;WindowStyle=WindowStyle.None;AllowsTransparency=false;ResizeMode=ResizeMode.CanResizeWithGrip;MinWidth=620;MinHeight=430;Topmost=true;ShowInTaskbar=false;Background=Design.Brush(Design.Dark?"#242A36":"#EEF1F6");
            var root=new DockPanel();var header=new DockPanel {Margin=new Thickness(4,0,4,22)};var close=new WindowControls(this,delegate{Design.Dismiss(this);});DockPanel.SetDock(close,System.Windows.Controls.Dock.Left);header.Children.Add(close);var title=Design.Text(String.IsNullOrEmpty(appName)?"Tus ventanas":appName,25,true);title.Margin=new Thickness(12,0,0,0);header.Children.Add(title);DockPanel.SetDock(header,System.Windows.Controls.Dock.Top);root.Children.Add(header);
            var grid=new WrapPanel {HorizontalAlignment=HorizontalAlignment.Center};
            foreach(var entry in windows)
            {
                var captured=entry;var preview=new Thumbnail(entry.Handle) {Width=246,Height=138};thumbnails.Add(preview);
                var card=new StackPanel();var bar=new DockPanel {Margin=new Thickness(4,0,4,8)};
                WindowControls lights=null;
                lights=new WindowControls(delegate{
                    if(!desktop.CloseWindow(captured))return;
                    var wait=new System.Windows.Threading.DispatcherTimer{Interval=TimeSpan.FromMilliseconds(150)};int ticks=0;bool done=false;
                    wait.Tick+=delegate{if(!Native.Matches(captured)){wait.Stop();if(done)return;done=true;card.IsHitTestVisible=false;preview.Dispose();
                        if(Design.Animations){var fade=new System.Windows.Media.Animation.DoubleAnimation(0,TimeSpan.FromMilliseconds(180)){EasingFunction=new System.Windows.Media.Animation.CubicEase{EasingMode=System.Windows.Media.Animation.EasingMode.EaseIn}};System.Windows.Media.Animation.Timeline.SetDesiredFrameRate(fade,60);fade.Completed+=delegate{grid.Children.Remove((UIElement)card.Parent);};card.BeginAnimation(UIElement.OpacityProperty,fade);}else grid.Children.Remove((UIElement)card.Parent);
                    }else if(++ticks>=30)wait.Stop();};Closed+=delegate{wait.Stop();};wait.Start();
                },delegate{desktop.MinimizeWindow(captured);},delegate{desktop.ZoomWindow(captured);});
                System.Windows.Automation.AutomationProperties.SetName(lights.CloseButton,"Cerrar "+entry.Title);System.Windows.Automation.AutomationProperties.SetName(lights.MinimizeButton,"Minimizar "+entry.Title);System.Windows.Automation.AutomationProperties.SetName(lights.ZoomButton,"Ampliar o restaurar "+entry.Title);DockPanel.SetDock(lights,System.Windows.Controls.Dock.Top);bar.Children.Add(lights);bar.Children.Add(Design.Text(entry.Title,11,true));card.Children.Add(bar);
                var activate=Design.Button("",delegate{if(!Native.Activate(captured))MessageBox.Show("Windows no permitió activar la ventana. Usá Alt+Tab.","MacOS");Design.Dismiss(this);},false);activate.Content=preview;activate.Background=Brushes.Transparent;activate.BorderThickness=new Thickness(0);activate.Padding=new Thickness(0);activate.Margin=new Thickness(0);activate.ToolTip=entry.Title;card.Children.Add(activate);var glassCard=Design.Card(card);glassCard.Width=282;glassCard.Height=220;glassCard.Margin=new Thickness(7);grid.Children.Add(glassCard);
            }
            if(windows.Count==0)grid.Children.Add(Design.Text("No hay ventanas abiertas.",15,false));root.Children.Add(new ScrollViewer {Content=grid,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled});Content=Design.Glass(root,20,false);SourceInitialized+=delegate{Native.Glass(this,Design.Dark);};KeyDown+=delegate(object sender,KeyEventArgs e){if(e.Key==Key.Escape)Design.Dismiss(this);};Closed+=delegate{foreach(var t in thumbnails)t.Dispose();};
        }
    }
    public sealed class Thumbnail : FrameworkElement, IDisposable
    {
        private readonly IntPtr source;
        private IntPtr thumbnail;
        private Native.RECT last;
        private byte lastOpacity;
        public Thumbnail(IntPtr hwnd)
        {
            source=hwnd;Loaded+=delegate{var window=Window.GetWindow(this);if(window!=null)Native.DwmRegisterThumbnail(new System.Windows.Interop.WindowInteropHelper(window).Handle,source,out thumbnail);PanelMotion.Frame-=MotionChanged;PanelMotion.Frame+=MotionChanged;Update();};LayoutUpdated+=delegate{Update();};Unloaded+=delegate{Dispose();};
        }
        protected override void OnRender(DrawingContext dc){dc.DrawRoundedRectangle(Design.Brush(Design.Dark?"#20121925":"#306A7F96"),null,new Rect(0,0,ActualWidth,ActualHeight),12,12);}
        private void Update()
        {
            if(thumbnail==IntPtr.Zero||!IsVisible)return;var window=Window.GetWindow(this);if(window==null||!window.IsAncestorOf(this))return;var src=PresentationSource.FromVisual(window);if(src==null)return;var bounds=TransformToAncestor(window).TransformBounds(new Rect(0,0,ActualWidth,ActualHeight));var m=src.CompositionTarget.TransformToDevice;var pos=m.Transform(bounds.TopLeft);var size=m.Transform(new Point(bounds.Width,bounds.Height));var rect=new Native.RECT {Left=(int)pos.X,Top=(int)pos.Y,Right=(int)(pos.X+size.X),Bottom=(int)(pos.Y+size.Y)};byte opacity=(byte)(Math.Max(0,Math.Min(1,window.Opacity))*255);
            if(rect.Left==last.Left&&rect.Top==last.Top&&rect.Right==last.Right&&rect.Bottom==last.Bottom&&opacity==lastOpacity)return;last=rect;lastOpacity=opacity;Native.RECT sourceRect;Native.GetWindowRect(source,out sourceRect);double ratio=(double)(sourceRect.Right-sourceRect.Left)/Math.Max(1,sourceRect.Bottom-sourceRect.Top);int width=rect.Right-rect.Left,height=rect.Bottom-rect.Top;if(ratio>0){int fittedW=Math.Min(width,(int)(height*ratio));int fittedH=Math.Min(height,(int)(width/ratio));rect.Left+=(width-fittedW)/2;rect.Top+=(height-fittedH)/2;rect.Right=rect.Left+fittedW;rect.Bottom=rect.Top+fittedH;}
            var prop=new Native.THUMBNAIL {Flags=1|4|8|16,Destination=rect,Opacity=opacity,Visible=true,ClientOnly=false};Native.DwmUpdateThumbnailProperties(thumbnail,ref prop);
        }
        private void MotionChanged(Window window){if(Window.GetWindow(this)==window)Update();}
        public void Dispose(){PanelMotion.Frame-=MotionChanged;if(thumbnail!=IntPtr.Zero){Native.DwmUnregisterThumbnail(thumbnail);thumbnail=IntPtr.Zero;}}
    }
}

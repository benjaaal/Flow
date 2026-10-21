using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace MacFlow
{
    [DataContract] internal sealed class TabletPreferences
    {
        [DataMember] public bool Calendar=true, Battery=true, Clock=true;
        [DataMember] public string Display="";
    }
    // Gestures are confined to our own strip, never a global input hook.
    internal static class TabletGesture
    {
        internal static int Resolve(double travel,double heldMilliseconds,double height)
        {return travel<Math.Min(70,height*.12)?0:heldMilliseconds>=450?2:1;}
        internal static Rect Floating(DisplayArea area)
        {double width=Math.Min(980,area.Width*.82),height=Math.Min(760,area.Height*.78);return new Rect((area.Width-width)/2,Math.Max(44,(area.Height-height)/2),width,height);}
    }
    internal sealed class TabletController : IDisposable
    {
        private readonly Desktop desktop;
        private readonly TabletPreferences preferences;
        private readonly string file;
        private DisplayArea area;
        private Window edge;
        private TabletHome home;
        private Window switcher;
        private bool disposed,enabled,dragging;
        private Point origin;
        private DateTime movedAt;
        private double travel;
        private readonly List<Thumbnail> previews=new List<Thumbnail>();
        private readonly Dictionary<IntPtr,SavedWindow> placements=new Dictionary<IntPtr,SavedWindow>();
        private AppEntry pending;
        private DateTime pendingUntil;
        internal bool Enabled{get{return enabled;}} internal bool Covers(DisplayArea target){return enabled&&area!=null&&target.Device==area.Device;}
        internal Window HomeView{get{return home;}}
        internal Window SwitcherView{get{return switcher;}}
        internal DisplayArea Area{get{return area;}}
        internal TabletPreferences Preferences{get{return preferences;}}
        internal TabletController(Desktop owner,DisplayArea preview=null){desktop=owner;area=preview;file=Path.Combine(owner.Store.DirectoryPath,"tablet.json");preferences=ModuleStore.Read<TabletPreferences>(file);}
        internal void Save(){ModuleStore.Write(file,preferences);}
        internal void Toggle(){if(enabled)Exit();else Enter(DisplaySpace.Current);}
        internal void Enter(DisplayArea target)
        {
            if(disposed)return;if(enabled){Home();return;}area=target;enabled=true;preferences.Display=target.StableId;Save();
            edge=new Window{Title="MacFlow · Gesto de Inicio",WindowStyle=WindowStyle.None,ResizeMode=ResizeMode.NoResize,ShowInTaskbar=false,ShowActivated=false,Topmost=true,AllowsTransparency=true,Background=Brushes.Transparent,Width=target.Width,Height=24};
            edge.Content=new Border{Background=Design.Brush("#01000000"),Child=new Border{Width=126,Height=5,CornerRadius=new CornerRadius(3),Background=Design.Brush("#D9FFFFFF"),VerticalAlignment=VerticalAlignment.Bottom,Margin=new Thickness(0,0,0,5)}};
            edge.MouseEnter+=delegate{desktop.MaintainTabletTaskbar();PositionEdge();};WindowLife.Track(edge);edge.SourceInitialized+=delegate{Native.NoActivate(edge);};
            edge.PreviewMouseLeftButtonDown+=delegate(object s,MouseButtonEventArgs e){Start(e.GetPosition(edge));edge.CaptureMouse();e.Handled=true;};
            edge.PreviewMouseMove+=delegate(object s,MouseEventArgs e){if(dragging&&edge.IsMouseCaptured)Move(e.GetPosition(edge));};
            edge.PreviewMouseLeftButtonUp+=delegate(object s,MouseButtonEventArgs e){if(dragging){Move(e.GetPosition(edge));Complete();edge.ReleaseMouseCapture();e.Handled=true;}};
            edge.LostMouseCapture+=delegate{if(dragging)Cancel();};
            edge.PreviewTouchDown+=delegate(object s,TouchEventArgs e){if(dragging)return;Start(e.GetTouchPoint(edge).Position);edge.CaptureTouch(e.TouchDevice);e.Handled=true;};
            edge.PreviewTouchMove+=delegate(object s,TouchEventArgs e){if(dragging){Move(e.GetTouchPoint(edge).Position);e.Handled=true;}};
            edge.PreviewTouchUp+=delegate(object s,TouchEventArgs e){if(dragging){Move(e.GetTouchPoint(edge).Position);Complete();edge.ReleaseTouchCapture(e.TouchDevice);e.Handled=true;}};
            edge.LostTouchCapture+=delegate{if(dragging)Cancel();};
            edge.Show();PositionEdge();Home();
        }
        private void PositionEdge(){if(WindowLife.Alive(edge)){DisplaySpace.Place(edge,area,0,area.Height-24);if(edge.IsVisible)Native.SetWindowPos(new WindowInteropHelper(edge).Handle,new IntPtr(-1),0,0,0,0,0x13);}}
        private void Start(Point point){if(!enabled||!desktop.ShellVisible)return;origin=point;dragging=true;travel=0;movedAt=DateTime.UtcNow;}
        private void Move(Point point){double next=Math.Max(0,origin.Y-point.Y);if(Math.Abs(next-travel)>5)movedAt=DateTime.UtcNow;travel=next;}
        private void Complete(){int action=TabletGesture.Resolve(travel,(DateTime.UtcNow-movedAt).TotalMilliseconds,area.Height);dragging=false;if(action==2)ShowSwitcher();else if(action==1)Home();}
        private void Cancel(){dragging=false;travel=0;}
        internal void Home()
        {
            if(!enabled||disposed)return;CloseSwitcher();
            // Show our Home above other windows; do not minimize the desktop.
            if(!WindowLife.Alive(home)){home=new TabletHome(this,desktop);home.Closed+=delegate{home=null;};home.IsVisibleChanged+=delegate{desktop.RefreshTabletSurfaces();};}
            if(!home.IsVisible){home.Show();DisplaySpace.Place(home,area,0,0);home.Entrance();}else home.Refresh();
            if(!desktop.IsTesting)home.Activate();PositionEdge();
        }
        internal void Launch(AppEntry entry)
        {
            if(!enabled||disposed)return;CloseSwitcher();if(WindowLife.Alive(home))home.Hide();desktop.MaintainTabletTaskbar();PositionEdge();
            var current=desktop.Windows.FirstOrDefault(w=>w.Key==entry.Key&&Native.Matches(w));
            if(current!=null){Float(current);if(!desktop.IsTesting)Native.Activate(current);return;}
            if(!desktop.IsTesting){pending=entry;pendingUntil=DateTime.UtcNow.AddSeconds(12);desktop.Launch(entry);}
        }
        internal void Float(WindowEntry entry){Arrange(entry,TabletGesture.Floating(area));}
        private void Arrange(WindowEntry entry,Rect rect)
        {
            if(disposed||!Native.Matches(entry))return;
            SavedWindow saved;if(!placements.TryGetValue(entry.Handle,out saved)){var placement=new Placement{Length=Marshal.SizeOf(typeof(Placement))};if(!GetWindowPlacement(entry.Handle,ref placement))return;placements.Add(entry.Handle,new SavedWindow{Entry=entry,Placement=placement});}
            Native.ShowWindowAsync(entry.Handle,9);
            Native.SetWindowPos(entry.Handle,IntPtr.Zero,(int)(area.Pixels.Left+rect.Left*area.Scale),(int)(area.Pixels.Top+rect.Top*area.Scale),(int)(rect.Width*area.Scale),(int)(rect.Height*area.Scale),0x14);
        }
        private void Activate(WindowEntry entry){CloseSwitcher();if(WindowLife.Alive(home))home.Hide();desktop.MaintainTabletTaskbar();PositionEdge();if(!desktop.IsTesting)Native.Activate(entry);}
        internal void ShowSwitcher()
        {
            if(!enabled||disposed)return;CloseSwitcher();if(WindowLife.Alive(home))home.Hide();
            switcher=new Window{Title="MacFlow · Multitarea iPad",Width=area.Width,Height=area.Height,WindowStyle=WindowStyle.None,ResizeMode=ResizeMode.NoResize,Topmost=true,ShowInTaskbar=false,Background=Design.Brush("#171B26"),ShowActivated=!desktop.IsTesting};WindowLife.Track(switcher);
            var root=new Grid();root.Children.Add(new Image{Source=desktop.PresentationWallpaper,Stretch=Stretch.UniformToFill,Opacity=.75,Effect=new System.Windows.Media.Effects.BlurEffect{Radius=16,RenderingBias=System.Windows.Media.Effects.RenderingBias.Performance}});
            var panel=new DockPanel{Margin=new Thickness(44,40,44,64)};var header=new DockPanel{Margin=new Thickness(0,0,0,26)};var close=Design.Button("Inicio",Home,false);DockPanel.SetDock(close,Dock.Right);header.Children.Add(close);var text=Design.Text("Tus ventanas",28,true);text.Foreground=Brushes.White;header.Children.Add(text);DockPanel.SetDock(header,Dock.Top);panel.Children.Add(header);
            var list=new WrapPanel{HorizontalAlignment=HorizontalAlignment.Center};var entries=desktop.Windows.Where(OnDisplay).Take(24).ToArray();
            foreach(var entry in entries)
            {
                var selected=entry;var content=new StackPanel();var caption=new DockPanel();var kill=Design.Button("×",delegate{if(Native.RequestClose(selected))Activate(selected);},false);kill.ToolTip="Cerrar esta ventana; la aplicación puede pedir guardar";DockPanel.SetDock(kill,Dock.Right);caption.Children.Add(kill);var name=Design.Text(entry.Title,12,true);name.MaxWidth=210;caption.Children.Add(name);content.Children.Add(caption);
                var preview=new Thumbnail(entry.Handle){Width=266,Height=158,Margin=new Thickness(0,10,0,12)};previews.Add(preview);content.Children.Add(preview);
                var show=Design.Button("Volver a la app",delegate{Activate(selected);},false);content.Children.Add(show);var actions=new UniformGrid{Columns=3};actions.Children.Add(Design.Button("Flotante",delegate{Float(selected);Activate(selected);},false));actions.Children.Add(Design.Button("Izquierda",delegate{Arrange(selected,new Rect(12,48,area.Width/2-18,area.Height-96));Activate(selected);},false));actions.Children.Add(Design.Button("Derecha",delegate{Arrange(selected,new Rect(area.Width/2+6,48,area.Width/2-18,area.Height-96));Activate(selected);},false));content.Children.Add(actions);
                list.Children.Add(new Border{Width=306,Background=Design.ContentSurface,CornerRadius=new CornerRadius(24),Padding=new Thickness(16),Margin=new Thickness(12),Child=content});
            }
            if(entries.Length==0){var empty=Design.Text("No hay ventanas en esta pantalla",20,false);empty.Foreground=Brushes.White;list.Children.Add(empty);}
            panel.Children.Add(new ScrollViewer{Content=list,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled});root.Children.Add(panel);switcher.Content=root;
            switcher.IsVisibleChanged+=delegate{desktop.RefreshTabletSurfaces();};switcher.KeyDown+=delegate(object s,KeyEventArgs e){if(e.Key==Key.Escape){Home();e.Handled=true;}};switcher.Closed+=delegate{foreach(var preview in previews)preview.Dispose();previews.Clear();switcher=null;};if(desktop.IsTesting)switcher.SourceInitialized+=delegate{Native.NoActivate(switcher);};switcher.Show();DisplaySpace.Place(switcher,area,0,0);Motion.Fade(switcher,0,1,220);PositionEdge();
        }
        private bool OnDisplay(WindowEntry entry){Native.RECT r;return Native.Matches(entry)&&Native.GetWindowRect(entry.Handle,out r)&&area.Pixels.IntersectsWith(new Rect(r.Left,r.Top,Math.Max(0,r.Right-r.Left),Math.Max(0,r.Bottom-r.Top)));}
        private void CloseSwitcher(){if(WindowLife.Alive(switcher))WindowLife.Close(switcher);}
        internal void Tick()
        {
            if(!enabled||disposed)return;
            if(!desktop.ShellVisible){if(WindowLife.Alive(home))home.Hide();if(WindowLife.Alive(edge))edge.Hide();CloseSwitcher();return;}
            if(!desktop.IsTesting){var screen=System.Windows.Forms.Screen.AllScreens.FirstOrDefault(s=>s.DeviceName==area.Device);var next=screen==null?DisplaySpace.Current:DisplaySpace.Of(screen);if(next.Pixels!=area.Pixels||next.Scale!=area.Scale){area=next;WindowLife.Close(home);CloseSwitcher();PositionEdge();}}
            if(WindowLife.Alive(home)&&home.IsVisible){home.UpdateClock();home.Refresh();}
            if(WindowLife.Alive(edge)&&!edge.IsVisible)edge.Show();PositionEdge();
            if(pending!=null){var found=desktop.Windows.FirstOrDefault(w=>w.Key==pending.Key&&Native.Matches(w));if(found!=null){Float(found);pending=null;}else if(DateTime.UtcNow>pendingUntil)pending=null;}
        }
        internal void Exit()
        {
            if(!enabled)return;enabled=false;Cancel();pending=null;CloseSwitcher();WindowLife.Close(home);WindowLife.Close(edge);edge=null;
            foreach(var saved in placements.Values)if(Native.Matches(saved.Entry)){var placement=saved.Placement;SetWindowPlacement(saved.Entry.Handle,ref placement);}placements.Clear();
        }
        public void Dispose(){if(disposed)return;Exit();disposed=true;}
        private sealed class SavedWindow{internal WindowEntry Entry;internal Placement Placement;}
        [StructLayout(LayoutKind.Sequential)]private struct XY{internal int X,Y;}
        [StructLayout(LayoutKind.Sequential)]private struct Placement{internal int Length,Flags,Show;internal XY Min,Max;internal Native.RECT Rect;}
        [DllImport("user32.dll")]private static extern bool GetWindowPlacement(IntPtr hwnd,ref Placement placement);
        [DllImport("user32.dll")]private static extern bool SetWindowPlacement(IntPtr hwnd,ref Placement placement);
    }

    internal sealed class TabletHome : Window
    {
        private readonly TabletController owner;private readonly Desktop desktop;
        private readonly Grid root=new Grid();private readonly WrapPanel apps=new WrapPanel{HorizontalAlignment=HorizontalAlignment.Center};
        private readonly StackPanel widgets=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center};
        private readonly TextBlock clock,date,pageInfo;private readonly Image wallpaper=new Image{Stretch=Stretch.UniformToFill};
        private readonly TextBox query;private readonly ScrollViewer appBrowser;private readonly DispatcherTimer clockTimer=new DispatcherTimer{Interval=TimeSpan.FromMinutes(1)};
        private int page;private string signature="",clockSignature="";private bool closed;private Point? down;
        internal TabletHome(TabletController controller,Desktop system)
        {
            owner=controller;desktop=system;Title="MacFlow · Inicio iPad";Width=owner.Area.Width;Height=owner.Area.Height;WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;ShowInTaskbar=false;Topmost=true;ShowActivated=!desktop.IsTesting;Background=Design.Brush("#25374F");WindowLife.Track(this);
            root.ClipToBounds=true;wallpaper.Source=desktop.PresentationWallpaper;root.Children.Add(wallpaper);root.Children.Add(new Border{Background=Design.Brush("#25000000"),IsHitTestVisible=false});
            var layout=new DockPanel{Margin=new Thickness(32,22,32,34)};var top=new DockPanel{Margin=new Thickness(0,0,0,12)};var commands=new StackPanel{Orientation=Orientation.Horizontal};commands.Children.Add(Design.Button("Modo Mac",owner.Exit,false));commands.Children.Add(Design.Button("Widgets",EditWidgets,false));commands.Children.Add(Design.Button("Ventanas",owner.ShowSwitcher,false));commands.Children.Add(Design.Button("Controles",delegate{Hide();desktop.OpenControl();},false));DockPanel.SetDock(commands,Dock.Right);top.Children.Add(commands);
            clock=White("",16,true);date=White("",12,false);var status=new StackPanel();status.Children.Add(clock);status.Children.Add(date);top.Children.Add(status);DockPanel.SetDock(top,Dock.Top);layout.Children.Add(top);
            var bottom=new StackPanel{HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(0,6,0,8)};var pages=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center};pages.Children.Add(Design.Button("‹",delegate{ChangePage(-1);},false));pageInfo=White("",12,true);pageInfo.Margin=new Thickness(12,0,12,0);pages.Children.Add(pageInfo);pages.Children.Add(Design.Button("›",delegate{ChangePage(1);},false));bottom.Children.Add(pages);
            var dockRow=new StackPanel{Orientation=Orientation.Horizontal};foreach(var entry in desktop.Preferences.Pins.Take(8)){var app=entry;dockRow.Children.Add(App(app,true));}var launcher=Design.Button("",owner.ShowSwitcher,false);launcher.Content=Design.Symbol("windows",28,Design.Ink);launcher.Width=58;launcher.Height=58;launcher.Background=Brushes.Transparent;launcher.ToolTip="Multitarea";dockRow.Children.Add(launcher);var dock=new WallpaperGlass(wallpaper,dockRow,28,new Thickness(12,8,12,8),false);bottom.Children.Add(dock);DockPanel.SetDock(bottom,Dock.Bottom);layout.Children.Add(bottom);
            var main=new DockPanel();var hero=new StackPanel();hero.Children.Add(widgets);query=new TextBox{Width=240,Height=34,FontSize=14,Padding=new Thickness(12,5,12,5),BorderThickness=new Thickness(0),Background=Brushes.Transparent,Foreground=Brushes.White,CaretBrush=Brushes.White};System.Windows.Automation.AutomationProperties.SetName(query,"Buscar aplicaciones en Inicio");query.TextChanged+=delegate{page=0;Refresh();};var search=new Grid();search.Children.Add(query);var placeholder=White("Buscar",13,false);placeholder.IsHitTestVisible=false;placeholder.HorizontalAlignment=HorizontalAlignment.Center;search.Children.Add(placeholder);query.TextChanged+=delegate{placeholder.Visibility=query.Text.Length==0?Visibility.Visible:Visibility.Collapsed;};hero.Children.Add(new WallpaperGlass(wallpaper,search,17,new Thickness(0),false){Width=260,Height=34,Margin=new Thickness(0,10,0,12)});DockPanel.SetDock(hero,Dock.Top);main.Children.Add(hero);appBrowser=new ScrollViewer{Content=apps,VerticalScrollBarVisibility=ScrollBarVisibility.Disabled,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};appBrowser.SizeChanged+=delegate{signature="";Refresh();};main.Children.Add(appBrowser);layout.Children.Add(main);root.Children.Add(layout);Content=root;
            SourceInitialized+=delegate{Native.OpaquePresentation(this);if(desktop.IsTesting)Native.NoActivate(this);};Loaded+=async delegate{UpdateClock();Refresh();clockTimer.Start();if(wallpaper.Source==null){var loaded=await WelcomeWallpaper.Load(desktop.Preferences.WallpaperPath);if(!closed){wallpaper.Source=loaded;desktop.PresentationWallpaper=loaded;}}};Closed+=delegate{closed=true;clockTimer.Stop();};clockTimer.Tick+=delegate{UpdateClock();};
            KeyDown+=delegate(object s,KeyEventArgs e){if(e.Key==Key.Escape){owner.Exit();e.Handled=true;}else if(e.Key==Key.Right&&!(e.OriginalSource is TextBox))ChangePage(1);else if(e.Key==Key.Left&&!(e.OriginalSource is TextBox))ChangePage(-1);else if(e.Key==Key.F3)owner.ShowSwitcher();};
            PreviewMouseLeftButtonDown+=delegate(object s,MouseButtonEventArgs e){if(e.OriginalSource==root||e.OriginalSource is Image)down=e.GetPosition(this);};PreviewMouseLeftButtonUp+=delegate(object s,MouseButtonEventArgs e){if(down.HasValue){double delta=e.GetPosition(this).X-down.Value.X;down=null;if(Math.Abs(delta)>80)ChangePage(delta<0?1:-1);}};
            IsManipulationEnabled=true;ManipulationStarting+=delegate(object s,ManipulationStartingEventArgs e){e.ManipulationContainer=this;e.Mode=ManipulationModes.TranslateX;};ManipulationCompleted+=delegate(object s,ManipulationCompletedEventArgs e){if(Math.Abs(e.TotalManipulation.Translation.X)>80){ChangePage(e.TotalManipulation.Translation.X<0?1:-1);e.Handled=true;}};
        }
        private TextBlock White(string text,double size,bool bold){var label=Design.Text(text,size,bold);label.Foreground=Brushes.White;return label;}
        private Button App(AppEntry app,bool dock)
        {
            var content=new StackPanel{HorizontalAlignment=HorizontalAlignment.Center};content.Children.Add(Design.AppIcon(app,dock?48:58));if(!dock){var name=White(app.Name,12,false);name.Width=100;name.TextAlignment=TextAlignment.Center;name.Margin=new Thickness(0,8,0,0);content.Children.Add(name);}var button=Design.Button("",delegate{owner.Launch(app);},false);button.Content=content;button.Width=dock?62:124;button.Height=dock?62:102;button.Background=Brushes.Transparent;button.Padding=new Thickness(4);button.Margin=new Thickness(dock?2:9);button.ToolTip=app.Name;System.Windows.Automation.AutomationProperties.SetName(button,"Abrir "+app.Name);return button;
        }
        internal void UpdateClock(){string next=DateTime.Now.ToString("yyyyMMddHHmm");if(clockSignature==next)return;clockSignature=next;clock.Text=DateTime.Now.ToString("HH:mm");date.Text=DateTime.Now.ToString("dddd d 'de' MMMM",CultureInfo.GetCultureInfo("es-AR"));BuildWidgets();}
        internal void Refresh()
        {
            var found=desktop.Apps.Where(a=>a.Name.IndexOf(query.Text,StringComparison.CurrentCultureIgnoreCase)>=0).OrderBy(a=>desktop.Preferences.Pins.Any(p=>p.Key==a.Key)?0:desktop.Windows.Any(w=>w.Key==a.Key)?1:2).ThenBy(a=>a.Name).ToArray();int columns=Math.Max(1,(int)(Math.Max(142,appBrowser.ActualWidth)/142));int rows=Math.Max(1,(int)(Math.Max(120,appBrowser.ActualHeight)/120));int count=columns*rows;int total=Math.Max(1,(found.Length+count-1)/count);page=Math.Max(0,Math.Min(page,total-1));pageInfo.Text=(page+1)+" / "+total;string next=page+":"+query.Text+":"+found.Length+":"+columns+":"+rows;
            if(signature==next)return;signature=next;apps.Children.Clear();foreach(var app in found.Skip(page*count).Take(count))apps.Children.Add(App(app,false));Motion.Fade(apps,0,1,180);
        }
        private void ChangePage(int change){page+=change;signature="";Refresh();}
        private void BuildWidgets()
        {
            widgets.Children.Clear();if(owner.Preferences.Calendar){var content=new StackPanel();var month=White(DateTime.Now.ToString("dddd",CultureInfo.GetCultureInfo("es-AR")).ToUpperInvariant(),12,true);month.Foreground=Design.Brush("#FF827A");content.Children.Add(month);content.Children.Add(White(DateTime.Now.Day.ToString(),48,false));content.Children.Add(White(DateTime.Now.ToString("MMMM yyyy",CultureInfo.GetCultureInfo("es-AR")),12,false));widgets.Children.Add(Widget(content));}
            if(owner.Preferences.Clock){var content=new StackPanel();content.Children.Add(White(DateTime.Now.ToString("HH:mm"),42,false));content.Children.Add(White("Tu hora local",12,false));widgets.Children.Add(Widget(content));}
            if(owner.Preferences.Battery){Native.POWER power;Native.GetSystemPowerStatus(out power);var content=new StackPanel();var glyph=new BatteryGlyph{Width=56,Height=30,Margin=new Thickness(0,0,0,13)};glyph.Update(power);content.Children.Add(glyph);content.Children.Add(White(power.BatteryLifePercent>100?"Con corriente":power.BatteryLifePercent+"%",32,false));content.Children.Add(White(power.ACLineStatus==1?"Cargando":"Batería",12,false));widgets.Children.Add(Widget(content));}
        }
        private FrameworkElement Widget(UIElement child){return new WallpaperGlass(wallpaper,child,26,new Thickness(18),true){Width=Math.Max(132,Math.Min(190,(Math.Max(500,Width)-140)/3)),Height=142,Margin=new Thickness(7)};}
        private void EditWidgets(){GlassMenu.Open(this,"Widgets en Inicio",new List<GlassCommand>{new GlassCommand("Calendario",delegate{owner.Preferences.Calendar=!owner.Preferences.Calendar;owner.Save();BuildWidgets();},"calendar"),new GlassCommand("Reloj",delegate{owner.Preferences.Clock=!owner.Preferences.Clock;owner.Save();BuildWidgets();},"timer"),new GlassCommand("Batería",delegate{owner.Preferences.Battery=!owner.Preferences.Battery;owner.Save();BuildWidgets();},"battery")});}
        internal void Entrance(){var scale=new ScaleTransform(.96,.96);root.RenderTransformOrigin=new Point(.5,.5);root.RenderTransform=scale;Motion.To(scale,ScaleTransform.ScaleXProperty,1,320);Motion.To(scale,ScaleTransform.ScaleYProperty,1,320);Motion.Fade(root,0,1,240);}
    }
}

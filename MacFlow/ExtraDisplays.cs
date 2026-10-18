using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using Forms=System.Windows.Forms;
namespace MacFlow
{
    // Each additional display owns its HWNDs, scale, appbar and motion clock.
    // Application state/commands are shared; no second application instance.
    internal sealed class ExtraDisplays : IDisposable
    {
        private readonly Desktop desktop;private readonly Dictionary<string,Surface> surfaces=new Dictionary<string,Surface>();private bool disposed;private DateTime nextScan;private readonly bool fixture;
        internal ExtraDisplays(Desktop desktop):this(desktop,null){}
        internal ExtraDisplays(Desktop desktop,IEnumerable<DisplayArea> areas){this.desktop=desktop;fixture=areas!=null;if(areas!=null)foreach(var area in areas)surfaces.Add(area.Device,new Surface(desktop,area));Refresh();}
        internal Window[] Bars{get{return surfaces.Values.Select(s=>s.Bar).ToArray();}}internal Window[] Docks{get{return surfaces.Values.Select(s=>s.Dock).ToArray();}}
        internal Window BarAtCursor(){Surface item;return surfaces.TryGetValue(DisplaySpace.Current.Device,out item)?item.Bar:null;}
        internal Point? DockDestination(Window source){var device=DisplaySpace.For(source).Device;Surface item;if(!surfaces.TryGetValue(device,out item))return null;var rect=item.Visibility.RestingBounds();return PanelMotion.IconDestination(source,item.Dock,rect)??new Point((rect.Left+rect.Right)/2.0,(rect.Top+rect.Bottom)/2.0);}
        internal void Rebuild(){foreach(var surface in surfaces.Values){surface.Signature=null;surface.DrawBar();surface.Layout();}Refresh();}
        internal void Refresh()
        {
            if(disposed)return;if(!fixture&&DateTime.UtcNow>=nextScan){nextScan=DateTime.UtcNow.AddSeconds(2);var screens=Forms.Screen.AllScreens.Where(s=>!s.Primary).ToList();foreach(var key in surfaces.Keys.Where(k=>!screens.Any(s=>s.DeviceName==k)).ToArray()){surfaces[key].Dispose();surfaces.Remove(key);}foreach(var screen in screens){Surface surface;if(!surfaces.TryGetValue(screen.DeviceName,out surface)){surface=new Surface(desktop,DisplaySpace.Of(screen));surfaces.Add(screen.DeviceName,surface);}else{var area=DisplaySpace.Of(screen);if(area.Pixels!=surface.Area.Pixels||Math.Abs(area.Scale-surface.Area.Scale)>.001){surface.Area=area;surface.Signature=null;surface.Layout();}}}}
            foreach(var item in surfaces.Values)item.Refresh();
        }
        internal void DisplayChanged(){nextScan=DateTime.MinValue;Refresh();}
        internal void Tick(){if(!disposed)foreach(var surface in surfaces.Values)surface.VisibilityTick();}
        internal void Hide(){foreach(var surface in surfaces.Values)surface.Hide();}
        public void Dispose(){if(disposed)return;disposed=true;foreach(var surface in surfaces.Values)surface.Dispose();surfaces.Clear();}
        private sealed class Surface : IDisposable
        {
            private readonly Desktop desktop;internal DisplayArea Area;internal readonly Window Bar,Dock;internal readonly DockVisibility Visibility;internal string Signature;
            private DockMotion motion;private ControlPullGesture gesture;private AppleClickAction apple;private TextBlock clock,active,batteryText;private BatteryGlyph battery;private Button assistantButton;private bool registered,disposed;private DateTime leftAt;private ActivityIsland capsule;
            internal Surface(Desktop owner,DisplayArea area)
            {
                desktop=owner;Area=area;Bar=Design.Panel("MacFlow barra superior · "+area.Device,area.Width-12,Desktop.TopBarHeight);Dock=Design.Panel("MacFlow Dock · "+area.Device,300,80);Bar.ShowActivated=Dock.ShowActivated=false;if(desktop.IsTesting)Bar.IsHitTestVisible=Dock.IsHitTestVisible=false;Bar.SourceInitialized+=delegate{Native.NoActivate(Bar);};Dock.SourceInitialized+=delegate{Native.NoActivate(Dock);};Visibility=new DockVisibility(Dock);DrawBar();DrawDock();Layout();if(desktop.Preferences.ShowTopBar)Bar.Show();Dock.Show();Layout();Register();if(!desktop.IsTesting)capsule=new ActivityIsland(desktop,new WindowsMediaSource(),false,Bar);
            }
            internal void DrawBar()
            {
                if(apple!=null)apple.Dispose();if(gesture!=null)gesture.Dispose();
                double gap=desktop.Preferences.ShowActivityIsland==false?0:220;Bar.SetValue(Native.BarGap,gap);
                var grid=new Grid();grid.ColumnDefinitions.Add(new ColumnDefinition());grid.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(gap==0?16:gap)});grid.ColumnDefinitions.Add(new ColumnDefinition());var left=new DockPanel{ClipToBounds=true};
                apple=new AppleClickAction(delegate{desktop.OpenAppleMenuAt(Bar);},desktop.OpenSettings,Forms.SystemInformation.DoubleClickTime);var mark=Button("",apple.Click,"Manzana · menú y Configuración");mark.Content=Design.AppleMark(16);DockPanel.SetDock(mark,System.Windows.Controls.Dock.Left);left.Children.Add(mark);var identity=Design.Text("MacFlow",12,true);identity.Margin=new Thickness(9,0,12,0);DockPanel.SetDock(identity,System.Windows.Controls.Dock.Left);left.Children.Add(identity);active=Design.Text("Escritorio",11,true);left.Children.Add(active);grid.Children.Add(left);
                var right=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};assistantButton=Button("assistant",desktop.OpenAssistant,"Activar asistente con micrófono");desktop.DecorateAssistantButton(assistantButton);right.Children.Add(assistantButton);right.Children.Add(Button("wifi",desktop.OpenWifi,"Redes Wi-Fi"));battery=new BatteryGlyph{VerticalAlignment=VerticalAlignment.Center};batteryText=Design.Text("",11,false);var batteryRow=new StackPanel{Orientation=Orientation.Horizontal};batteryRow.Children.Add(battery);batteryText.Margin=new Thickness(4,0,0,0);batteryRow.Children.Add(batteryText);var power=Button("battery",desktop.OpenControl,"Batería · Centro de control");power.Content=batteryRow;right.Children.Add(power);right.Children.Add(Button("volume",desktop.OpenControl,"Sonido"));right.Children.Add(Button("control",desktop.OpenControl,"Centro de control"));right.Children.Add(Button("bell",desktop.OpenActivity,"Notificaciones de MacFlow"));clock=Design.Text("",11,true);clock.Margin=new Thickness(8,0,3,0);right.Children.Add(clock);Grid.SetColumn(right,2);grid.Children.Add(right);var shell=Design.Glass(grid,12,true);shell.Padding=new Thickness(10,0,10,0);Bar.Content=shell;
                gesture=new ControlPullGesture(Bar,delegate{return desktop.ShellVisible&&desktop.Preferences.ControlCenterGesture!=false;},desktop.CreateGestureControl);if(Bar.IsLoaded){Native.Glass(Bar,Design.Dark);Native.SetGlassClip(Bar,1);}
            }
            private static Button Button(string symbol,Action action,string label){var button=Design.Button("",action,false);button.Content=Design.Symbol(symbol,17,Design.Ink);button.ToolTip=label;System.Windows.Automation.AutomationProperties.SetName(button,label);button.Padding=new Thickness(7,2,7,2);button.Margin=new Thickness(0);button.Height=Desktop.TopBarHeight-2;button.Background=Brushes.Transparent;return button;}
            private void DrawDock()
            {
                if(motion!=null)motion.Dispose();var entries=AppIdentity.DockEntries(desktop.Preferences.Pins,desktop.Windows);
                double size=desktop.Preferences.IconSize,reserve=DockMetrics.Reserve(size);bool vertical=desktop.Preferences.DockPosition!="Bottom";int max=Math.Max(3,(int)((vertical?Area.Height:Area.Width)/(size+20))-6);var row=new StackPanel{Orientation=vertical?Orientation.Vertical:Orientation.Horizontal,Margin=vertical?new Thickness(0,reserve,0,reserve):new Thickness(reserve,0,reserve,0)};foreach(var entry in entries.Take(max))row.Children.Add(desktop.DockItem(entry,false));
                motion=new DockMotion(row,vertical,size);var own=motion;row.MouseMove+=delegate(object s,System.Windows.Input.MouseEventArgs e){own.Move(e.GetPosition(row));};row.MouseLeave+=delegate{own.Leave();};var glass=Design.Glass(row,20,true,true);glass.Padding=new Thickness(DockMetrics.Padding);Dock.Content=glass;double length=Math.Min(entries.Count,max)*(vertical?DockMetrics.Row(size)+DockMetrics.Indicator:DockMetrics.Slot(size))+reserve*2+DockMetrics.Padding*2;Dock.Width=vertical?DockMetrics.Thickness(size):Math.Min(Area.Width-20,length);Dock.Height=vertical?Math.Min(Area.Height-65,length):DockMetrics.Thickness(size);Layout();if(Dock.IsLoaded)Native.Glass(Dock,Design.Dark);
            }
            internal void Layout()
            {
                if(disposed)return;Visibility.PositionChanging();Bar.Width=Area.Width-12;DisplaySpace.Place(Bar,Area,6,3);bool left=desktop.Preferences.DockPosition=="Left",right=desktop.Preferences.DockPosition=="Right";DisplaySpace.Place(Dock,Area,left?10:right?Area.Width-Dock.Width-10:(Area.Width-Dock.Width)/2,left||right?(Area.Height-Dock.Height)/2:Area.Height-Dock.Height-(desktop.Preferences.AutoHideTaskbar?5:54));if(registered)Appbar();
            }
            internal void Refresh()
            {
                desktop.DecorateAssistantButton(assistantButton);string signature=desktop.Preferences.IconSize+":"+desktop.Preferences.DockPosition+":"+Design.Dark+":"+Design.IconAppearance+":"+String.Join(";",desktop.Preferences.Pins.Select(p=>p.Key))+":"+String.Join(";",desktop.Windows.Select(w=>w.Key).Distinct());if(signature!=Signature){Signature=signature;DrawDock();}
                string time=DateTime.Now.ToString(Area.Width<1300?"HH:mm":"ddd d · HH:mm",CultureInfo.GetCultureInfo("es-AR"));if(clock.Text!=time)clock.Text=time;var foreground=Native.GetForegroundWindow();string app=Native.IsDesktopContext(foreground)?"Escritorio":Native.Title(foreground);if(active.Text!=app)active.Text=app;Native.POWER power;if(Native.GetSystemPowerStatus(out power)){battery.Update(power);string text=power.BatteryLifePercent>100?"":power.BatteryLifePercent+"%";if(batteryText.Text!=text)batteryText.Text=text;}VisibilityTick();if(capsule!=null)capsule.Refresh();
            }
            internal void VisibilityTick()
            {
                if(!desktop.ShellVisible){Hide();return;}if(desktop.FullscreenOn(Area)){Visibility.HideImmediately();Bar.Hide();if(capsule!=null&&!desktop.AssistantOn(Bar))capsule.Hide();return;}var p=Forms.Cursor.Position;bool withinX=p.X>=Area.Pixels.Left&&p.X<Area.Pixels.Right,withinY=p.Y>=Area.Pixels.Top&&p.Y<Area.Pixels.Bottom;bool edge=desktop.Preferences.DockPosition=="Left"?withinY&&p.X>=Area.Pixels.Left&&p.X<Area.Pixels.Left+8:desktop.Preferences.DockPosition=="Right"?withinY&&p.X>=Area.Pixels.Right-8&&p.X<Area.Pixels.Right:withinX&&p.Y>=Area.Pixels.Bottom-8&&p.Y<Area.Pixels.Bottom;Native.RECT bounds;bool inside=Dock.IsVisible&&Native.GetWindowRect(new WindowInteropHelper(Dock).Handle,out bounds)&&Desktop.CursorInside(bounds,p.X,p.Y);bool menu=GlassMenu.Current!=null&&GlassMenu.Current.Anchor==Dock;
                bool occupied=desktop.OccupiedOn(Area);bool automatic=desktop.Preferences.DockAutoHide||desktop.Preferences.HideDockInApps||desktop.Preferences.HideDockInCodex&&desktop.CodexActive;
                if(!occupied){Visibility.ShowImmediately();leftAt=DateTime.UtcNow;}else if(!automatic||edge||inside||menu){Visibility.Show();leftAt=DateTime.UtcNow;}else if(DateTime.UtcNow-leftAt>TimeSpan.FromMilliseconds(300))Visibility.Hide();if(desktop.Preferences.ShowTopBar){if(!Bar.IsVisible)Bar.Show();Register();}else{Bar.Hide();Unregister();}
            }
            private bool Intersects(IntPtr h){Native.RECT r;if(!Native.GetWindowRect(h,out r))return false;return Area.Pixels.IntersectsWith(new Rect(r.Left,r.Top,Math.Max(0,r.Right-r.Left),Math.Max(0,r.Bottom-r.Top)));}
            private static uint Pid(IntPtr h){uint pid;Native.GetWindowThreadProcessId(h,out pid);return pid;}
            internal void Hide(){Visibility.HideImmediately();Bar.Hide();if(capsule!=null)capsule.Hide();Unregister();}
            private void Register(){if(registered||!desktop.Preferences.ShowTopBar||desktop.IsTesting)return;var data=new Native.APPBARDATA{cbSize=Marshal.SizeOf(typeof(Native.APPBARDATA)),hWnd=new WindowInteropHelper(Bar).Handle};registered=Native.SHAppBarMessage(0,ref data).ToUInt64()!=0;if(registered)Appbar();}
            private void Appbar(){var rect=Area.Pixels;int height=(int)Math.Ceiling(Desktop.TopBarReserve*Area.Scale);var data=new Native.APPBARDATA{cbSize=Marshal.SizeOf(typeof(Native.APPBARDATA)),hWnd=new WindowInteropHelper(Bar).Handle,uEdge=1,rc=new Native.RECT{Left=(int)rect.Left,Right=(int)rect.Right,Top=(int)rect.Top,Bottom=(int)rect.Top+height}};Native.SHAppBarMessage(2,ref data);data.rc.Bottom=data.rc.Top+height;Native.SHAppBarMessage(3,ref data);Native.SetWindowPos(data.hWnd,IntPtr.Zero,data.rc.Left+(int)Math.Round(6*Area.Scale),data.rc.Top+(int)Math.Round(3*Area.Scale),(int)Math.Round(Bar.Width*Area.Scale),(int)Math.Round(Desktop.TopBarHeight*Area.Scale),0x14);}
            private void Unregister(){if(!registered)return;var data=new Native.APPBARDATA{cbSize=Marshal.SizeOf(typeof(Native.APPBARDATA)),hWnd=new WindowInteropHelper(Bar).Handle};Native.SHAppBarMessage(1,ref data);registered=false;}
            public void Dispose(){if(disposed)return;disposed=true;Unregister();if(capsule!=null)capsule.Dispose();if(motion!=null)motion.Dispose();if(gesture!=null)gesture.Dispose();if(apple!=null)apple.Dispose();Dock.Close();Bar.Close();}
        }
    }
}

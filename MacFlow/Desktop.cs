using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace MacFlow
{
    public sealed class Desktop : IDisposable
    {
        public readonly SettingsStore Store;
        public Preferences Preferences;
        public List<AppEntry> Apps = new List<AppEntry>();
        public List<WindowEntry> Windows = new List<WindowEntry>();
        public List<string> Files = new List<string>();
        public Window Dock, TopBar, Search, Control, Settings;
        public Window Launcher, Overview;
        private FolderStack stacks;
        private FilePreviewWindow filePreview;
        private FocusWindow focusWindow;
        public readonly FocusController Focus;
        private Button focusIndicator;
        private ConnectivityPanel connections;
        private WelcomeWindow welcome;
        private UpdateWindow updates;
        private DispatcherTimer updatesTimer;
        private System.IO.FileSystemWatcher updatesWatcher;private DispatcherTimer updateDebounce;
        private readonly CancellationTokenSource updatesStop=new CancellationTokenSource();
        private bool updatesBusy;
        private string notifiedUpdate;
        internal const double TopBarHeight=40,TopBarReserve=46;
        private ClockWidget widget;
        internal WidgetManager Widgets;
        internal StageController Stage;
        private WidgetGallery widgetGallery;
        private UpdateTour tour;
        internal bool PresentationActive{get;private set;}
        internal System.Windows.Media.Imaging.BitmapSource PresentationWallpaper;
        private DispatcherTimer edgeTimer;
        private ForegroundEvents foregroundEvents;
        private IntPtr materialForeground;
        private bool fullscreenNow, dockHidden;
        private DateTime dockLeaveAt=DateTime.UtcNow;
        private readonly TaskbarLease lease;
        private readonly ShellRecovery shellRecovery=new ShellRecovery();
        private readonly int taskbarCreated=Native.RegisterWindowMessage("TaskbarCreated");
        private DateTime startupDockUntil;
        private Forms.NotifyIcon tray;
        private DispatcherTimer timer;
        private TextBlock active, clock;
        private WindowControls activeControls;
        private WindowEntry activeTarget,pressedTarget;
        private LidTransition lidTransition;
        private Button network, battery, volume;
        private BatteryGlyph batteryIcon;private TextBlock batteryText;
        private int tick;
        private string signature = "";
        private readonly Dictionary<string,FrameworkElement> dockIcons=new Dictionary<string,FrameworkElement>();
        private readonly HashSet<string> pendingLaunches=new HashSet<string>();
        private DockMotion dockMotion;
        internal StackPanel DockItems;
        internal Border DockGlass;
        private bool dockRebuildPending;
        private DispatcherTimer dockRedraw;
        private readonly HashSet<string> enteringApps=new HashSet<string>();
        private readonly HashSet<string> closeFeedback=new HashSet<string>();
        private WindowsMenuKey windowsKey;
        private DockEdge dockEdge;
        private CaptionButtons captionButtons;
        private DockVisibility dockVisibility;
        private bool codexForeground;
        private bool desktopForeground;
        private uint contextProcess;
        public Window PowerConfirmation;
        private bool disposed, suspended, appbarRegistered,dockPainted;
        private bool indicatorsBusy;
        private readonly bool smoke;
        private readonly bool nonInteractiveTest;
        public bool HotkeyAvailable;
        public int AppCount { get { return Apps.Count; } }
        public bool IsTesting {get{return nonInteractiveTest;}}
        private readonly Func<Window,Point?> panelDestination;
        private Point? PanelDestination(Window window)
        {
            if(disposed||suspended||Dock==null||dockVisibility==null)return null;
            var rect=dockVisibility.RestingBounds();if(rect.Right<=rect.Left||rect.Bottom<=rect.Top)return null;
            return new Point((rect.Left+rect.Right)/2.0,(rect.Top+rect.Bottom)/2.0);
        }
        private void PrepareTestWindow(Window window)
        {
            if(!nonInteractiveTest)return;window.ShowActivated=false;window.IsHitTestVisible=false;window.SourceInitialized+=delegate {Native.NoActivate(window);};
        }
        public Desktop(string directory, bool smokeMode,bool nonInteractive=false,bool backgroundStart=false)
        {
            smoke = smokeMode;nonInteractiveTest=nonInteractive;Store = new SettingsStore(directory); Preferences = Store.Load(); lease = new TaskbarLease(directory);
            Focus = new FocusController(directory); Focus.Changed += UpdateFocusIndicator;
            Focus.Completed += delegate { if (!smoke) Notify("Terminaste tu sesión de concentración. Tomate un descanso."); };
            startupDockUntil=backgroundStart?DateTime.UtcNow.AddSeconds(1):DateTime.MinValue;
            bool dockUpgraded=Preferences.UpgradeContainedDock();
            if (Preferences.Pins.Count == 0)
            {
                Preferences.Pins.Add(new AppEntry { Name = "Explorador", Target = "explorer.exe", Executable = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe") });
                Preferences.Pins.Add(new AppEntry { Name = "Configuración", Target = "ms-settings:", Executable=System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),"ImmersiveControlPanel","SystemSettings.exe") });
            }
            ApplyDesign(); CreateDock();dockVisibility=new DockVisibility(Dock);panelDestination=PanelDestination;PanelMotion.DockDestination=panelDestination;CreateTopBar();
            if(dockUpgraded&&!smoke)Save();
            if (!smoke) { CreateTray(); lease.Restore(); if (Preferences.AutoHideTaskbar) TryAcquire(); ResumeWallpaper();lidTransition=new LidTransition(Dock,delegate{return !suspended&&Preferences.LidAnimation&&Preferences.Animations;}); }
            timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromSeconds(Preferences.LowPower?2:1) };
            Widgets=new WidgetManager(this);Stage=new StageController(this);
            timer.Tick += delegate { Refresh(); }; timer.Start(); Refresh(); RebuildWidget();
            edgeTimer=new DispatcherTimer(DispatcherPriority.Background){Interval=TimeSpan.FromMilliseconds(120)};
            edgeTimer.Tick+=delegate {UpdateDockVisibility();};edgeTimer.Start();
            if(!smoke)foregroundEvents=new ForegroundEvents(Application.Current.Dispatcher,UpdateDockVisibility);
            var thread = new Thread(delegate()
            {
                var found = Catalog.Scan(); Icons.Warm(found);
                Application.Current.Dispatcher.BeginInvoke(new Action(delegate { if (disposed) return; Apps = found;if(widgetGallery!=null)widgetGallery.RefreshApps();
                    foreach(var pin in Preferences.Pins){var installed=found.FirstOrDefault(a=>a.Key==pin.Key||String.Equals(a.Target,pin.Target,StringComparison.OrdinalIgnoreCase));if(installed!=null){pin.IconPath=installed.IconPath;pin.Executable=installed.Executable;}}
                    if(!File.Exists(Store.PathName)&&Preferences.Pins.Count<=2){string[] favorites={"Edge","Chrome","Bloc","Paint","WhatsApp","Spotify"};foreach(var name in favorites){var app=found.FirstOrDefault(a=>a.Name.IndexOf(name,StringComparison.OrdinalIgnoreCase)>=0);if(app!=null&&!Preferences.Pins.Any(p=>p.Key==app.Key)&&Preferences.Pins.Count<8)Preferences.Pins.Add(app);}DrawDock();}
                    DrawDock();if (Search != null) RefreshSearch(); if(Launcher!=null)((Launchpad)Launcher).Refresh(); }));
            }); thread.SetApartmentState(ApartmentState.STA); thread.IsBackground = true; thread.Start();
            System.Threading.Tasks.Task.Run(delegate {var files=LocalFiles.Scan();Application.Current.Dispatcher.BeginInvoke(new Action(delegate{if(!disposed)Files=files;}));});
            ConfigureWindowsKey();
            UpdateDockEdge();
            if(!smoke){updatesTimer=new DispatcherTimer{Interval=TimeSpan.FromSeconds(25)};updatesTimer.Tick+=delegate{updatesTimer.Interval=TimeSpan.FromHours(12);CheckUpdatesInBackground();};updatesTimer.Start();WatchLocalUpdates();}
        }
        private void ApplyDesign() { Design.Dark = Preferences.Theme == "Dark";Design.IconAppearance=Preferences.IconAppearance; Design.Accent = Preferences.Accent; Design.Animations = Preferences.Animations; Design.GlassOpacity=Preferences.GlassOpacity; LiquidMaterial.LowPower=Preferences.LowPower;LiquidMaterial.LiveBackdrop=Preferences.LiveGlass;Design.InstallTheme(); }
        private void TryAcquire()
        {
            if(smoke||disposed||suspended)return;
            var handle=Dock==null?IntPtr.Zero:new WindowInteropHelper(Dock).Handle;
            bool ready=dockPainted&&Dock.IsLoaded&&handle!=IntPtr.Zero&&Native.IsWindow(handle)&&Dock.ActualWidth>0&&Dock.ActualHeight>0;
            bool wasAcquired=shellRecovery.Acquired;bool acquired=shellRecovery.Attempt(Preferences.AutoHideTaskbar,ready,Native.FindWindow("Shell_TrayWnd",null)!=IntPtr.Zero,DateTime.UtcNow,lease.Acquire,lease.Restore);
            if(acquired){if(!wasAcquired)Position();UpdateDockEdge();}
        }
        public void Save() { try { Store.Save(Preferences); } catch (Exception ex) { Notify("No se pudieron guardar los cambios: " + ex.Message); } }
        private void Notify(string value) { Log.Write(value); if (tray != null) { tray.BalloonTipTitle = "MacFlow"; tray.BalloonTipText = value; tray.ShowBalloonTip(4000); } else MessageBox.Show(value, "MacFlow"); }
        private void CreateTray()
        {
            var menu = new Forms.ContextMenuStrip();
            menu.Items.Add("Buscar aplicaciones · Alt+Espacio", null, delegate { Application.Current.Dispatcher.Invoke(new Action(OpenSearch)); });
            menu.Items.Add("Centro de control", null, delegate { Application.Current.Dispatcher.Invoke(new Action(OpenControl)); });
            menu.Items.Add("Configuración de MacFlow", null, delegate { Application.Current.Dispatcher.Invoke(new Action(OpenSettings)); });
            menu.Items.Add("Actualizaciones",null,delegate{Application.Current.Dispatcher.Invoke(new Action(delegate{OpenUpdates(Preferences.UpdateRepository);}));});
            menu.Items.Add("Energía", null, delegate { Application.Current.Dispatcher.Invoke(new Action(OpenPowerMenu)); });
            menu.Items.Add("Pausar / reanudar MacFlow", null, delegate { Application.Current.Dispatcher.Invoke(new Action(ToggleSuspend)); });
            menu.Items.Add(new Forms.ToolStripSeparator());
            menu.Items.Add("Restaurar Windows y salir", null, delegate { Application.Current.Dispatcher.Invoke(new Action(delegate { Application.Current.Shutdown(); })); });
            var iconFile = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MacFlow.ico");
            tray = new Forms.NotifyIcon { Text = "MacFlow · Alt+Espacio", Icon = File.Exists(iconFile) ? new System.Drawing.Icon(iconFile) : System.Drawing.SystemIcons.Application, ContextMenuStrip = menu, Visible = true };
            tray.DoubleClick += delegate { Application.Current.Dispatcher.Invoke(new Action(OpenSettings)); };
        }
        public void ToggleSuspend()
        {
            suspended = !suspended;
            if (suspended) { if(!smoke)Wallpapers.Restore(Store.DirectoryPath);if(lidTransition!=null)lidTransition.Cancel();shellRecovery.Reset(lease.Restore); UnregisterAppbar(); dockVisibility.HideImmediately(); TopBar.Hide(); if(widget!=null)widget.Hide();if(Widgets!=null)Widgets.SetVisible(false); ClosePanels(); }
            else { ResumeWallpaper();dockHidden=false;dockVisibility.Show(); if(widget!=null)widget.Show(); if (Preferences.ShowTopBar) { TopBar.Show(); RegisterAppbar(); } if (Preferences.AutoHideTaskbar) TryAcquire(); Position(); }
            ConfigureWindowsKey();
        }
        private void ClosePanels() { if(tour!=null)tour.Close();if(widgetGallery!=null)widgetGallery.Close(); if(stacks!=null)stacks.Close();if(filePreview!=null)filePreview.Close();if(focusWindow!=null)focusWindow.Close();if(updates!=null)updates.Close();if(welcome!=null)welcome.Close();if(connections!=null)connections.Close();if(GlassMenu.Current!=null)GlassMenu.Current.Close(); if(PowerConfirmation!=null)PowerConfirmation.Close();if (Search != null) Search.Close(); if (Control != null) Control.Close(); if (Settings != null) Settings.Close();if(Launcher!=null)Launcher.Close();if(Overview!=null)Overview.Close(); }
        private void CreateDock()
        {
            Dock = Design.Panel("MacFlow Dock", 420, 100); Dock.ShowActivated = false;
            Dock.SourceInitialized += delegate
            {
                Native.NoActivate(Dock); var source = (HwndSource)PresentationSource.FromVisual(Dock);
                source.AddHook(Hook);
                if (!smoke) RegisterHotkey();
            };
            Dock.ContentRendered+=delegate{dockPainted=true;TryAcquire();};
            Dock.Loaded += delegate { Position(); }; Dock.Show(); DrawDock();
        }
        private IntPtr Hook(IntPtr h, int message, IntPtr w, IntPtr l, ref bool handled)
        {
            if(taskbarCreated!=0&&message==taskbarCreated)Application.Current.Dispatcher.BeginInvoke(new Action(delegate{if(disposed)return;appbarRegistered=false;shellRecovery.Reset(lease.Restore);startupDockUntil=DateTime.UtcNow.AddSeconds(3);Position();RegisterAppbar();TryAcquire();Refresh();}));
            if (message == 0x312 && w.ToInt32() == 1) { OpenSearch(); handled = true; }
            if (message == 0x312 && w.ToInt32() == 2) { OpenWindowsMenu(); handled = true; }
            if (message == 0x312 && w.ToInt32() == 3) { OpenDesktopMenu(); handled = true; }
            if (message == 0x7E || message == 0x2E0 || (message == 0x8001 && w.ToInt32() == 1)) Application.Current.Dispatcher.BeginInvoke(new Action(Position));
            return IntPtr.Zero;
        }
        private void CreateTopBar()
        {
            TopBar = Design.Panel("MacFlow barra superior", ScreenSize().Width-12, TopBarHeight); TopBar.ShowActivated = false;
            TopBar.SourceInitialized += delegate { Native.NoActivate(TopBar); ((HwndSource)PresentationSource.FromVisual(TopBar)).AddHook(Hook); };
            TopBar.Loaded += delegate { Position(); RegisterAppbar(); };
            DrawTopBar(); if (Preferences.ShowTopBar) TopBar.Show();
        }
        private void DrawTopBar()
        {
            var grid = new Grid(); grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); grid.ColumnDefinitions.Add(new ColumnDefinition()); grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var left = new StackPanel { Orientation = Orientation.Horizontal };
            var apple=Design.Button("",OpenMenu,false);apple.Content=Design.AppleMark(16);apple.ToolTip="Menú de MacFlow";System.Windows.Automation.AutomationProperties.SetName(apple,"Menú de MacFlow");left.Children.Add(apple);
            var identity=Design.Button("IOS",OpenSettings,false);identity.ToolTip="Configuración de MacFlow";left.Children.Add(identity);
            activeControls=new WindowControls(delegate{ControlActiveWindow(0);},delegate{ControlActiveWindow(1);},delegate{ControlActiveWindow(2);}){Margin=new Thickness(10,0,2,0),VerticalAlignment=VerticalAlignment.Center};
            activeControls.PreviewMouseLeftButtonDown+=delegate{pressedTarget=activeTarget;};left.Children.Add(activeControls);
            active = Design.Text("Escritorio", 11, true); active.Margin = new Thickness(9, 0, 9, 0);active.TextTrimming=TextTrimming.CharacterEllipsis;active.VerticalAlignment=VerticalAlignment.Center;
            var right = new StackPanel { Orientation = Orientation.Horizontal };
            focusIndicator = Design.Button("", OpenFocus, false); focusIndicator.ToolTip = "Concentración"; right.Children.Add(focusIndicator); UpdateFocusIndicator();
            network = Design.Button("", OpenWifi, false);network.Content=Design.Symbol("wifi",18,Design.Ink);network.ToolTip = "Redes Wi-Fi"; network.Visibility=Preferences.ShowNetwork?Visibility.Visible:Visibility.Collapsed;
            battery = Design.Button("Batería", delegate { OpenUri("ms-settings:batterysaver"); }, false);
            batteryIcon=new BatteryGlyph {VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(0,0,5,0)};batteryText=Design.Text("—",11,false);batteryText.VerticalAlignment=VerticalAlignment.Center;var batteryRow=new StackPanel {Orientation=Orientation.Horizontal};batteryRow.Children.Add(batteryIcon);batteryRow.Children.Add(batteryText);battery.Content=batteryRow;
            battery.Visibility=Preferences.ShowBattery?Visibility.Visible:Visibility.Collapsed;
            volume = Design.Button("", OpenControl, false);volume.Content=Design.Symbol("volume",18,Design.Ink);volume.ToolTip="Sonido";volume.Visibility=Preferences.ShowVolume?Visibility.Visible:Visibility.Collapsed;
            right.Children.Add(network); right.Children.Add(battery); right.Children.Add(volume); var controls=Design.Button("",OpenControl,false);controls.Content=Design.Symbol("control",17,Design.Ink);right.Children.Add(controls);
            clock=Design.Text("",12,true);clock.Margin=new Thickness(12,0,5,0);right.Children.Add(clock);
            foreach (var b in left.Children.OfType<Button>().Concat(right.Children.OfType<Button>())) { b.Padding = new Thickness(7, 2, 7, 2); b.Margin = new Thickness(0); b.Background = Brushes.Transparent; b.BorderThickness=new Thickness(0); b.FontSize = 11;b.Height=36; }
            Grid.SetColumn(active,1);Grid.SetColumn(right,2);grid.Children.Add(left);grid.Children.Add(active);grid.Children.Add(right);
            var glass=Design.Glass(grid,12,true);glass.Padding=new Thickness(10,0,10,0);TopBar.Content=glass;UpdateClock();
        }
        private void UpdateClock(){if(clock!=null)clock.Text=DateTime.Now.ToString("ddd d MMM · HH:mm",CultureInfo.GetCultureInfo("es-AR"));}
        private void UpdateActiveWindow(IntPtr handle)
        {
            uint pid;Native.GetWindowThreadProcessId(handle,out pid);
            bool external=pid!=0&&pid!=(uint)Process.GetCurrentProcess().Id&&!Native.IsDesktopContext(handle)&&Native.IsWindowVisible(handle)&&!Native.IsIconic(handle)&&Native.GetWindowTextLength(handle)>0;
            activeTarget=external?new WindowEntry{Handle=handle,ProcessId=pid,Title=Native.Title(handle)}:null;
            if(activeControls==null)return;activeControls.Visibility=external?Visibility.Visible:Visibility.Collapsed;
            if(external){int style=Native.GetWindowLong(handle,-16);activeControls.MinimizeButton.IsEnabled=(style&0x20000)!=0;activeControls.ZoomButton.IsEnabled=(style&0x10000)!=0;activeControls.CloseButton.ToolTip="Cerrar · "+activeTarget.Title;active.Text=activeTarget.Title;}else if(Native.IsDesktopContext(handle))active.Text="Escritorio";
        }
        private void ControlActiveWindow(int action)
        {
            var target=pressedTarget??activeTarget;pressedTarget=null;
            if(target==null||activeTarget==null||target.Handle!=activeTarget.Handle||target.ProcessId!=activeTarget.ProcessId||Native.GetForegroundWindow()!=target.Handle||!Native.Matches(target))return;
            bool result=action==0?CloseWindow(target):action==1?Native.RequestMinimize(target):Native.RequestZoom(target);
            if(!result)Notify("Windows no permitió la acción sobre esa ventana.");
        }
        public void PreviewLidAnimation(){if(lidTransition!=null)lidTransition.Preview();}
        public string LidStatus{get{return lidTransition==null?"Disponible en la sesión normal de MacFlow.":lidTransition.Status;}}
        private void OpenMenu()
        {
            GlassMenu.Open(TopBar,"MacFlow",new List<GlassCommand> {
                new GlassCommand("Buscar aplicaciones",OpenSearch),new GlassCommand("Aplicaciones",OpenLaunchpad),new GlassCommand("Ventanas abiertas",OpenOverview),new GlassCommand("Carpetas",OpenPlaces),new GlassCommand("Tu escritorio · Ctrl+Alt+D",OpenDesktopMenu),new GlassCommand("Concentración",OpenFocus),new GlassCommand("Centro de control",OpenControl),new GlassCommand("Configurar MacFlow",OpenSettings),new GlassCommand("Actualizaciones",delegate{OpenUpdates(Preferences.UpdateRepository);}),new GlassCommand("Energía · Apagar, reiniciar…",OpenPowerMenu),new GlassCommand("Pausar y restaurar Windows",ToggleSuspend),new GlassCommand("Salir",delegate {Application.Current.Shutdown();})
            });
        }
        public void DrawDock()
        {
            if(DockItems!=null&&Dock.IsMouseOver&&Mouse.LeftButton==MouseButtonState.Pressed){dockRebuildPending=true;return;}dockRebuildPending=false;
            var previousMotion=dockMotion;if(previousMotion!=null)previousMotion.Dispose();
            dockIcons.Clear();
            bool vertical=Preferences.DockPosition!="Bottom";
            double reserve=DockMetrics.Reserve(Preferences.IconSize);
            var items = new StackPanel { Orientation = vertical?Orientation.Vertical:Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Bottom,Margin=vertical?new Thickness(0,reserve,0,reserve):new Thickness(reserve,0,reserve,0) };

            var entries = new List<AppEntry>(Preferences.Pins);
            var running = Windows.Where(p => !String.IsNullOrEmpty(p.Executable)).GroupBy(p => p.Key).Select(p => p.First());
            foreach (var w in running)
            {
                if (!entries.Any(p => p.Key == w.Key)) entries.Add(new AppEntry { Name = System.IO.Path.GetFileNameWithoutExtension(w.Executable), Target = w.Executable, Executable = w.Executable });
            }
            // Keep the Dock usable on small screens; overflow is exposed through a real window menu.
            int max = Math.Max(3, (int)((vertical?ScreenSize().Height:ScreenSize().Width) / (Preferences.IconSize + 20)) - 6);
            foreach (var entry in entries.Take(max)) items.Children.Add(DockItem(entry));
            if (entries.Count > max) items.Children.Add(DockUtility("windows", "Más aplicaciones y ventanas", OpenWindowMenu));
            var glass = Design.Glass(items, 20, true, true);glass.Padding=new Thickness(DockMetrics.Padding);glass.VerticalAlignment = VerticalAlignment.Stretch;
            DockItems=items;DockGlass=glass;
            var movement=new DockMotion(items,vertical,Preferences.IconSize,previousMotion);dockMotion=movement;
            items.MouseMove+=delegate(object sender,MouseEventArgs e){movement.Move(e.GetPosition(items));};items.MouseLeave+=delegate{movement.Leave();};
            double slot=vertical?DockMetrics.Row(Preferences.IconSize)+DockMetrics.Indicator:DockMetrics.Slot(Preferences.IconSize);
            double length=(Math.Min(entries.Count,max)+(entries.Count>max?1:0))*slot+reserve*2+DockMetrics.Padding*2,thickness=DockMetrics.Thickness(Preferences.IconSize);
            Dock.Content = glass; Dock.Width = vertical?thickness:Math.Min(ScreenSize().Width-20,length); Dock.Height=vertical?Math.Min(ScreenSize().Height-65,length):thickness; Position();
        }
        private UIElement DockUtility(string symbol, string tooltip, Action action)
        {
            var size=Preferences.IconSize;var group=new TransformGroup();group.Children.Add(new ScaleTransform(1,1));group.Children.Add(new TranslateTransform());
            var icon=new Border{Width=size,Height=size,Tag="utility:"+symbol,Child=DockAppMotion.Host(new Image{Source=FeatureIcons.Get(symbol),Width=size,Height=size,Stretch=Stretch.Uniform}),RenderTransform=group,RenderTransformOrigin=new Point(.5,1),CacheMode=new BitmapCache(1.5)};
            var button=Design.Button("",action,false);button.Content=icon;button.Width=DockMetrics.Slot(size);button.Height=DockMetrics.Row(size);button.VerticalContentAlignment=VerticalAlignment.Bottom;button.Padding=new Thickness(0);button.Margin=new Thickness(0);button.ToolTip=tooltip;button.Background=Brushes.Transparent;button.BorderThickness=new Thickness(0);button.RenderTransform=null;
            var cell=new StackPanel{Width=DockMetrics.Slot(size),VerticalAlignment=VerticalAlignment.Bottom,Tag=icon};cell.Children.Add(button);cell.Children.Add(new Border{Height=DockMetrics.Indicator});return cell;
        }
        private UIElement DockItem(AppEntry entry)
        {
            var matching = Windows.Where(w => w.Key == entry.Key).ToList();
            var cell = new StackPanel { Width = Preferences.IconSize + 12, VerticalAlignment = VerticalAlignment.Bottom };
            var transform = new ScaleTransform(1, 1);
            var motion=new TransformGroup();motion.Children.Add(transform);motion.Children.Add(new TranslateTransform());
            var icon = new Border { Child = DockAppMotion.Host(Design.AppIcon(entry, Preferences.IconSize)), Width = Preferences.IconSize, Height = Preferences.IconSize, RenderTransformOrigin = new Point(.5, 1), RenderTransform = motion,CacheMode=new BitmapCache(1.5) };dockIcons[entry.Key]=icon;
            var button = Design.Button("", delegate {if(Directory.Exists(entry.Target)){OpenStack(entry.Target);return;}if(String.Equals(entry.Executable??entry.Target,System.Reflection.Assembly.GetExecutingAssembly().Location,StringComparison.OrdinalIgnoreCase)){OpenSettings();return;}var current=Windows.Where(w=>w.Key==entry.Key&&Native.Matches(w)).ToList();if(current.Count==1){if(!Native.Activate(current[0]))Notify("Windows no permitió activar esta ventana. Podés usar Alt+Tab.");}else if(current.Count>1)WindowMenu(entry,current);else Launch(entry);}, false);
            button.Content = icon; button.Background = Brushes.Transparent; button.BorderThickness=new Thickness(0); button.RenderTransform=null; button.Padding = new Thickness(0); button.Margin = new Thickness(0); button.Width = DockMetrics.Slot(Preferences.IconSize);button.Height = DockMetrics.Row(Preferences.IconSize);button.VerticalContentAlignment=VerticalAlignment.Bottom;button.ToolTip = entry.Name + (matching.Count > 0 ? " · " + matching.Count + " ventana(s)" : "");
            icon.Tag=entry.Key;cell.Tag=icon;
            Point dragStart=new Point(); bool canDrag=false;
            button.PreviewMouseLeftButtonDown+=delegate(object sender,MouseButtonEventArgs e){dragStart=e.GetPosition(button);canDrag=Preferences.Pins.Any(p=>p.Key==entry.Key);};
            button.PreviewMouseLeftButtonUp+=delegate{canDrag=false;};
            button.PreviewMouseMove+=delegate(object sender,MouseEventArgs e){if(!canDrag||e.LeftButton!=MouseButtonState.Pressed)return;var pos=e.GetPosition(button);if(Math.Abs(pos.X-dragStart.X)<SystemParameters.MinimumHorizontalDragDistance && Math.Abs(pos.Y-dragStart.Y)<SystemParameters.MinimumVerticalDragDistance)return;canDrag=false;DragDrop.DoDragDrop(button,new DataObject("MacFlow.Pin",entry.Key),DragDropEffects.Move);e.Handled=true;};
            button.AllowDrop=true;
            button.DragOver+=delegate(object sender,DragEventArgs e){e.Effects=e.Data.GetDataPresent("MacFlow.Pin")?DragDropEffects.Move:DragDropEffects.None;e.Handled=true;};
            button.Drop+=delegate(object sender,DragEventArgs e){if(!e.Data.GetDataPresent("MacFlow.Pin"))return;if(Preferences.MovePin((string)e.Data.GetData("MacFlow.Pin"),entry.Key)){Save();Application.Current.Dispatcher.BeginInvoke(new Action(DrawDock));}e.Handled=true;};
            button.PreviewMouseRightButtonUp+=delegate(object sender,MouseButtonEventArgs e)
            {
                var commands=new List<GlassCommand>();commands.Add(new GlassCommand("Abrir una nueva instancia",delegate {Launch(entry);}));
                if(matching.Count>0)commands.Add(new GlassCommand("Ver sus ventanas",delegate{OpenWindowMenu(entry.Key,entry.Name);}));
                foreach(var w in Windows.Where(w=>w.Key==entry.Key)){var captured=w;commands.Add(new GlassCommand(w.Title,delegate {Native.Activate(captured);}));}
                var open=Windows.Where(w=>w.Key==entry.Key).ToList();
                commands.AddRange(MinimizeCommands(open));
                if(open.Count==1){var captured=open[0];commands.Add(new GlassCommand("Cerrar ventana",delegate {CloseWindow(captured);}));}
                else if(open.Count>1){foreach(var w in open){var captured=w;commands.Add(new GlassCommand("Cerrar: "+w.Title,delegate {CloseWindow(captured);}));}commands.Add(new GlassCommand("Cerrar todas sus ventanas",delegate {foreach(var w in open)CloseWindow(w);}));}
                commands.Add(new GlassCommand(Preferences.Pins.Any(p=>p.Key==entry.Key)?"Quitar del Dock":"Fijar en el Dock",delegate {Pin(entry);}));GlassMenu.Open(Dock,entry.Name,commands);e.Handled=true;
            };
            cell.Children.Add(button);cell.Children.Add(new Border{Height=DockMetrics.Indicator,Child=new Ellipse{Width=3,Height=3,Fill=matching.Count>0?Design.Ink:Brushes.Transparent,VerticalAlignment=VerticalAlignment.Center}});return cell;
        }
        private void Bounce(FrameworkElement icon)
        {
            DockAppMotion.Play(icon,false);
        }
        public void Launch(AppEntry app)
        {
            if(!pendingLaunches.Add(app.Key))return;FrameworkElement icon;if(dockIcons.TryGetValue(app.Key,out icon))Bounce(icon);
            // Shell activation can be slow. Keep all launch/COM work off the UI thread.
            var worker=new Thread(delegate()
            {
                Exception failure=null;try{Catalog.Launch(app);}catch(Exception ex){failure=ex;}
                if(!Application.Current.Dispatcher.HasShutdownStarted)Application.Current.Dispatcher.BeginInvoke(new Action(delegate{pendingLaunches.Remove(app.Key);if(!disposed&&failure!=null)Notify("No se pudo abrir "+app.Name+": "+failure.Message);}));
            });worker.IsBackground=true;worker.SetApartmentState(ApartmentState.STA);worker.Start();
        }
        public bool CloseWindow(WindowEntry window)
        {
            if(!WindowCloseTransition.Request(window,delegate{if(!disposed)Notify("Windows no permitió cerrar esta ventana. Podés usar su botón de cierre o Alt+F4.");})){Notify("Windows no permitió cerrar esta ventana. Podés usar su botón de cierre o Alt+F4.");return false;}
            var confirmation=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(150)};int checks=0;
            confirmation.Tick+=delegate{if(disposed||!Native.Matches(window)){confirmation.Stop();if(!disposed){FrameworkElement icon;if(Preferences.Animations&&Dock.IsVisible&&Windows.Any(w=>w.Key==window.Key)&&dockIcons.TryGetValue(window.Key,out icon)){DockAppMotion.Play(icon,true,!Preferences.Pins.Any(p=>p.Key==window.Key)&&!Windows.Any(w=>w.Key==window.Key&&Native.Matches(w)));closeFeedback.Add(window.Key);}Refresh();}}else if(++checks>=20)confirmation.Stop();};confirmation.Start();return true;
        }
        private void AnimateWindowChanges(List<WindowEntry> previous,bool initial)
        {
            var before=new HashSet<string>(previous.Select(w=>w.Key));var after=new HashSet<string>(Windows.Select(w=>w.Key));
            if(!initial)foreach(var key in after.Except(before))enteringApps.Add(key);
            bool closing=closeFeedback.Count>0;
            if(!initial&&Preferences.Animations&&Dock.IsVisible)foreach(var key in before.Except(after)){FrameworkElement icon;if(!closeFeedback.Contains(key)&&dockIcons.TryGetValue(key,out icon)){DockAppMotion.Play(icon,true,!Preferences.Pins.Any(p=>p.Key==key));closing=true;}}closeFeedback.Clear();
            if(dockRedraw==null){dockRedraw=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(280)};dockRedraw.Tick+=delegate{dockRedraw.Stop();FinishWindowChanges();};}
            if(closing){dockRedraw.Stop();dockRedraw.Start();}else if(!dockRedraw.IsEnabled)FinishWindowChanges();
        }
        private void FinishWindowChanges()
        {
            if(disposed)return;DrawDock();if(Preferences.Animations)foreach(var key in enteringApps){FrameworkElement icon;if(dockIcons.TryGetValue(key,out icon))DockAppMotion.Play(icon,false);}enteringApps.Clear();
        }
        public bool MinimizeWindow(WindowEntry window)
        {
            if(!Native.RequestMinimize(window)){Notify("Windows no permitió minimizar esta ventana. Podés usar su botón de minimizar.");return false;}return true;
        }
        public bool ZoomWindow(WindowEntry window)
        {
            if(!Native.RequestZoom(window)){Notify("Esta ventana no permite ampliar o restaurar desde MacFlow.");return false;}return true;
        }
        internal List<GlassCommand> MinimizeCommands(IList<WindowEntry> windows)
        {
            var commands=new List<GlassCommand>();
            foreach(var window in windows){var captured=window;commands.Add(new GlassCommand(windows.Count==1?"Minimizar ventana":"Minimizar: "+window.Title,delegate{MinimizeWindow(captured);}));}
            if(windows.Count>1)commands.Add(new GlassCommand("Minimizar todas sus ventanas",delegate{foreach(var window in windows)MinimizeWindow(window);}));
            foreach(var window in windows){var captured=window;commands.Add(new GlassCommand(windows.Count==1?"Ampliar o restaurar":"Ampliar o restaurar: "+window.Title,delegate{ZoomWindow(captured);}));}
            return commands;
        }
        internal void ProbeDockMotion(double fraction){if(dockMotion!=null)dockMotion.Probe(fraction);}
        internal void StopDockProbe(){if(dockMotion!=null)dockMotion.EndProbe();}
        public void Pin(AppEntry entry)
        {
            var found = Preferences.Pins.FirstOrDefault(p => p.Key == entry.Key);
            if (found != null) Preferences.Pins.Remove(found); else if (Preferences.Pins.Count < 24) Preferences.Pins.Add(entry); else { Notify("El Dock admite hasta 24 aplicaciones fijadas."); return; }
            Save(); DrawDock(); if (Search != null) RefreshSearch();
        }
        private void WindowMenu(AppEntry entry, List<WindowEntry> windows)
        {
            var commands=new List<GlassCommand>();foreach(var w in windows){var captured=w;commands.Add(new GlassCommand(w.Title,delegate {Native.Activate(captured);}));}
            commands.AddRange(MinimizeCommands(windows));
            foreach(var w in windows){var captured=w;commands.Add(new GlassCommand("Cerrar: "+w.Title,delegate {CloseWindow(captured);}));}
            commands.Add(new GlassCommand("Abrir "+entry.Name,delegate {Launch(entry);}));GlassMenu.Open(Dock,entry.Name,commands);
        }
        private void OpenWindowMenu()
        {OpenWindowMenu(null,null);}
        private void OpenWindowMenu(string key,string name)
        {
            if(Overview!=null){var current=(WindowOverview)Overview;if(current.AppKey!=key){Design.Dismiss(current,delegate{OpenWindowMenu(key,name);});return;}if(current.WindowState==WindowState.Minimized)current.WindowState=WindowState.Normal;if(!nonInteractiveTest)current.Activate();return;}Overview=new WindowOverview(this,key,name);PrepareTestWindow(Overview);var size=ScreenSize();Overview.Width=Math.Min(Overview.Width,size.Width-40);Overview.Height=Math.Min(Overview.Height,size.Height-65);PlaceCenter(Overview,.12);Overview.Closed+=delegate{Overview=null;};Overview.Show();Design.Reveal(Overview);
        }
        public void OpenOverview() {OpenWindowMenu();}
        private void OpenPlaces()
        {
            var commands=new List<GlassCommand>();
            foreach(var place in new[]{Environment.SpecialFolder.UserProfile,Environment.SpecialFolder.DesktopDirectory,Environment.SpecialFolder.MyDocuments,Environment.SpecialFolder.MyPictures,Environment.SpecialFolder.MyMusic,Environment.SpecialFolder.MyVideos})
            {string path=Environment.GetFolderPath(place);if(String.IsNullOrEmpty(path)||!Directory.Exists(path))continue;string label=place==Environment.SpecialFolder.UserProfile?"Inicio":place==Environment.SpecialFolder.DesktopDirectory?"Escritorio":place==Environment.SpecialFolder.MyDocuments?"Documentos":place==Environment.SpecialFolder.MyPictures?"Imágenes":place==Environment.SpecialFolder.MyMusic?"Música":"Videos";commands.Add(new GlassCommand(label,delegate{Launch(new AppEntry{Name=label,Target=path});}));}
            GlassMenu.Open(TopBar,"Carpetas",commands);
        }
        public void OpenLaunchpad()
        {
            if(Launcher!=null){if(Launcher.WindowState==WindowState.Minimized){Launcher.WindowState=WindowState.Normal;if(!nonInteractiveTest)Launcher.Activate();}else Design.Dismiss(Launcher);return;}Launcher=new Launchpad(this);PrepareTestWindow(Launcher);var size=ScreenSize();Launcher.Width=Math.Min(Launcher.Width,size.Width-40);Launcher.Height=Math.Min(Launcher.Height,size.Height-65);PlaceCenter(Launcher,.13);Launcher.Closed+=delegate{Launcher=null;};Launcher.Show();Design.Reveal(Launcher);
        }
        private static void AddMenu(ContextMenu menu, string label, Action action) { var item = new MenuItem { Header = label }; item.Click += delegate { action(); }; menu.Items.Add(item); }
        private static void StyleMenu(ContextMenu menu)
        {
            menu.Background = Design.Brush(Design.Dark ? "#F020293B" : "#F2F3F7FC"); menu.Foreground = Design.Ink; menu.BorderBrush = Design.Brush("#558FA5C7"); menu.Padding = new Thickness(7); menu.FontSize = 13;
            if (Design.Animations) menu.Opened += delegate { menu.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(160))); };
        }
        private Size ScreenSize()
        {
            var s = Forms.Screen.PrimaryScreen.Bounds; var source = Dock == null ? null : PresentationSource.FromVisual(Dock); var m = source == null ? Matrix.Identity : source.CompositionTarget.TransformFromDevice;
            var p = m.Transform(new Point(s.Width, s.Height)); return new Size(p.X, p.Y);
        }
        private void Position()
        {
            if (disposed || Dock == null || TopBar == null) return;
            if(dockVisibility!=null)dockVisibility.PositionChanging();
            var bounds = Forms.Screen.PrimaryScreen.Bounds; var src = PresentationSource.FromVisual(Dock); if (src == null) return;
            var m = src.CompositionTarget.TransformFromDevice; var origin = m.Transform(new Point(bounds.Left, bounds.Top)); var size = ScreenSize();
            Dock.Left = Preferences.DockPosition=="Left"?origin.X+10:Preferences.DockPosition=="Right"?origin.X+size.Width-Dock.Width-10:origin.X+(size.Width-Dock.Width)/2;
            Dock.Top = Preferences.DockPosition!="Bottom"?origin.Y+(size.Height-Dock.Height)/2:origin.Y+size.Height-Dock.Height-(!smoke&&Preferences.AutoHideTaskbar&&!shellRecovery.Acquired?58:Preferences.AutoHideTaskbar?5:58);
            TopBar.Width=size.Width-12;TopBar.Left=origin.X+6;TopBar.Top=origin.Y+3;
            if(dockEdge!=null)dockEdge.Position(Dock);
            if (appbarRegistered) AppbarPosition();
        }
        private void RegisterAppbar()
        {
            if (smoke || suspended || !Preferences.ShowTopBar || appbarRegistered) return;
            var h = new WindowInteropHelper(TopBar).Handle; if (h == IntPtr.Zero) return;
            var data = new Native.APPBARDATA { cbSize = Marshal.SizeOf(typeof(Native.APPBARDATA)), hWnd = h, uCallbackMessage = 0x8001 };
            appbarRegistered = Native.SHAppBarMessage(0, ref data).ToUInt64() != 0; if (appbarRegistered) AppbarPosition();
        }
        private void AppbarPosition()
        {
            var bounds = Forms.Screen.PrimaryScreen.Bounds; var source = PresentationSource.FromVisual(TopBar); if (source == null) return;
            int height = (int)Math.Ceiling(source.CompositionTarget.TransformToDevice.Transform(new Point(0, TopBarReserve)).Y);
            var data = new Native.APPBARDATA { cbSize = Marshal.SizeOf(typeof(Native.APPBARDATA)), hWnd = new WindowInteropHelper(TopBar).Handle, uEdge = 1, rc = new Native.RECT { Left = bounds.Left, Right = bounds.Right, Top = bounds.Top, Bottom = bounds.Top + height } };
            Native.SHAppBarMessage(2, ref data); data.rc.Bottom = data.rc.Top + height; Native.SHAppBarMessage(3, ref data);
            // Apply the negotiated rectangle in physical pixels (handles pre-existing appbars).
            var scale=source.CompositionTarget.TransformToDevice;int width=(int)Math.Round(scale.Transform(new Point(TopBar.Width,0)).X),visibleHeight=(int)Math.Round(scale.Transform(new Point(0,TopBarHeight)).Y),offset=(int)Math.Round(scale.Transform(new Point(0,3)).Y);
            Native.SetWindowPos(data.hWnd,IntPtr.Zero,data.rc.Left+(data.rc.Right-data.rc.Left-width)/2,data.rc.Top+offset,width,visibleHeight,0x14);
        }
        private void UnregisterAppbar()
        {
            if (!appbarRegistered) return; var data = new Native.APPBARDATA { cbSize = Marshal.SizeOf(typeof(Native.APPBARDATA)), hWnd = new WindowInteropHelper(TopBar).Handle }; Native.SHAppBarMessage(1, ref data); appbarRegistered = false;
        }
        private void Refresh()
        {
            if (disposed) return;
            try
            {
                var previous=Windows;Windows = Native.Windows(); string next = String.Join("|", Windows.OrderBy(p=>p.Handle.ToInt64()).Select(p => p.Handle.ToString() + ":" + p.Key));
                if (next != signature || closeFeedback.Count>0) { bool initial=signature==""&&previous.Count==0;signature = next;AnimateWindowChanges(previous,initial); }
                var foreground = Native.GetForegroundWindow(); uint pid; Native.GetWindowThreadProcessId(foreground, out pid);
                if(pid!=(uint)Process.GetCurrentProcess().Id&&pid!=contextProcess){contextProcess=pid;string executable=Native.ProcessPath(pid);bool nextCodex=IsCodex(executable);if(nextCodex!=codexForeground){codexForeground=nextCodex;dockLeaveAt=DateTime.UtcNow;}}
                if (pid != (uint)Process.GetCurrentProcess().Id) active.Text = Native.Title(foreground);
                UpdateClock();
                if (tick++ % 5 == 0) RefreshIndicators();
                bool fullscreen = false;
                if (Preferences.HideInFullscreen && pid != (uint)Process.GetCurrentProcess().Id && !Native.IsDesktopContext(foreground))
                {
                    Native.RECT rect; var b = Forms.Screen.PrimaryScreen.Bounds;
                    int style=Native.GetWindowLong(foreground,-16);
                    // A maximized desktop window can include invisible resize
                    // margins beyond the screen; it is not borderless full screen.
                    if(foreground!=Native.FindWindow("Progman",null)&&Native.GetWindowRect(foreground,out rect))fullscreen=(style&0x00C00000)==0&&(style&0x01000000)==0&&rect.Left<=b.Left&&rect.Top<=b.Top&&rect.Right>=b.Right&&rect.Bottom>=b.Bottom;
                }
                fullscreenNow=fullscreen && !Native.IsDesktopContext(foreground) && Windows.Any(w=>w.Handle==foreground);
                if (!suspended&&!PresentationActive) { if(fullscreenNow){dockVisibility.HideImmediately();dockHidden=true;TopBar.Hide();if(widget!=null)widget.Hide();}else{if(Preferences.ShowTopBar&&!TopBar.IsVisible)TopBar.Show();if(widget!=null&&!widget.IsVisible)widget.Show();UpdateDockVisibility();} }
            }
            catch (Exception ex) { Log.Write("Actualización: " + ex.Message); }
        }
        private async void RefreshIndicators()
        {
            if(indicatorsBusy||disposed)return;indicatorsBusy=true;
            try
            {
                var snapshot=await System.Threading.Tasks.Task.Run(delegate
                {
                    Native.POWER power;Native.GetSystemPowerStatus(out power);
                    bool wifi=NetworkInterface.GetAllNetworkInterfaces().Any(n=>n.NetworkInterfaceType==NetworkInterfaceType.Wireless80211&&n.OperationalStatus==OperationalStatus.Up);
                    string connection=wifi?"Wi-Fi conectado":NetworkInterface.GetIsNetworkAvailable()?"Red conectada":"Sin conexión";
                    int level;bool muted;using(var device=new Audio()){level=device.Available?(int)Math.Round(device.Volume*100):-1;muted=device.Muted;}
                    return Tuple.Create(power,connection,level,muted);
                });
                if(disposed)return;var powerState=snapshot.Item1;batteryIcon.Update(powerState);batteryText.Text=powerState.BatteryFlag==128?"AC":powerState.BatteryLifePercent>100?"—":powerState.BatteryLifePercent+"%";battery.ToolTip=powerState.BatteryFlag==128?"Equipo sin batería":powerState.ACLineStatus==1?"Conectada a corriente · Configuración de energía":"Batería · Configuración de energía";network.Content=Design.Symbol(snapshot.Item2=="Sin conexión"?"wifi-off":"wifi",18,Design.Ink);network.ToolTip=snapshot.Item2;volume.Content=Design.Symbol(snapshot.Item3<0||snapshot.Item4?"volume-off":snapshot.Item3<35?"volume-low":"volume",18,Design.Ink);volume.ToolTip=snapshot.Item3<0?"Sin dispositivo de salida":snapshot.Item4?"Silenciado":"Volumen · "+snapshot.Item3+"%";
            }
            catch(Exception ex){if(!disposed)Log.Write("Indicadores: "+ex.Message);}finally{indicatorsBusy=false;}
        }
        public void OpenSearch()
        {
            if(PresentationActive)return;
            if(suspended)ToggleSuspend();if(Search!=null){Search.Activate();return;}Search=new Spotlight(this);PrepareTestWindow(Search);var size=ScreenSize();Search.Width=Math.Min(Search.Width,size.Width-40);Search.Height=Math.Min(Search.Height,size.Height-65);PlaceCenter(Search,.23);Search.Closed+=delegate{Search=null;};Search.Show();Design.Reveal(Search);
        }
        public void RefreshSearch() { if(Search!=null)((Spotlight)Search).Refresh(); }
        public void OpenControl()
        {
            if(PresentationActive)return;
            if(Control!=null){if(Control.WindowState==WindowState.Minimized)Control.WindowState=WindowState.Normal;if(!Control.IsVisible)Control.Show();Control.Activate();return;}
            Control=new ControlCenter(this);PrepareTestWindow(Control);var size=ScreenSize();Control.Height=Math.Min(Control.Height,size.Height-65);Control.Left=size.Width-Control.Width-16;Control.Top=46;Control.Closed+=delegate {Control=null;};Control.Show();Design.Reveal(Control);
        }
        public void OpenWifi(){OpenConnections(true);}
        private void UpdateFocusIndicator()
        {
            if (focusIndicator == null || Focus == null) return;
            if (Focus.Session.State == "Running" || Focus.Session.State == "Paused") focusIndicator.Content = "◷ " + Focus.TimeText;
            else focusIndicator.Content = new Image { Source = FeatureIcons.Get("focus"), Width = 20, Height = 20 };
            System.Windows.Automation.AutomationProperties.SetName(focusIndicator, "Concentración · " + Focus.TimeText);
        }
        private void ShowFeature(FeaturePanel window, double y)
        { PrepareTestWindow(window); var size = ScreenSize(); window.Width = Math.Min(window.Width, size.Width - 32); window.Height = Math.Min(window.Height, size.Height - 70); PlaceCenter(window, y); window.Show(); Design.Reveal(window); }
        internal void ShowModule(FeaturePanel window){ShowFeature(window,.08);}
        public void OpenWidgets(string selection)
        {if(widgetGallery!=null){widgetGallery.SelectWidget(selection);RestoreFeature(widgetGallery);return;}widgetGallery=new WidgetGallery(Widgets,selection);widgetGallery.Closed+=delegate{widgetGallery=null;};ShowModule(widgetGallery);}
        public void OpenStageManager(){Stage.OpenSettings();}
        private void RestoreFeature(Window window)
        { if (window.WindowState == WindowState.Minimized) window.WindowState = WindowState.Normal; if (!window.IsVisible) window.Show(); if (!nonInteractiveTest) window.Activate(); }
        public void OpenFocus()
        { if (focusWindow != null) { RestoreFeature(focusWindow); return; } focusWindow = new FocusWindow(Focus); focusWindow.Closed += delegate { focusWindow = null; }; ShowFeature(focusWindow, .12); }
        public void OpenStack(string path)
        { if (stacks != null) { stacks.Navigate(path); RestoreFeature(stacks); return; } stacks = new FolderStack(this, path); stacks.Closed += delegate { stacks = null; }; ShowFeature(stacks, .12); }
        public void OpenPreview(string file)
        { if (filePreview != null) filePreview.Close(); filePreview = new FilePreviewWindow(file); filePreview.Closed += delegate { filePreview = null; }; ShowFeature(filePreview, .12); }
        public void OpenDesktopMenu()
        {
            GlassMenu.Open(TopBar, "Tu escritorio", new List<GlassCommand> {
                new GlassCommand("Pilas de carpetas", delegate { OpenStack(FolderStack.Downloads()); }, "folder"), new GlassCommand("Concentración", OpenFocus, "moon"),
                new GlassCommand("Widgets",delegate{OpenWidgets(null);},"grid"),new GlassCommand("Stage Manager",OpenStageManager,"windows"),
                new GlassCommand("Ventanas abiertas", OpenOverview, "windows"), new GlassCommand("Fondos de escritorio", delegate { OpenSettings(); ((SettingsWindow)Settings).SelectPage(1); }, "display"),
                new GlassCommand("Configuración de pantalla", delegate { OpenUri("ms-settings:display"); }, "display"), new GlassCommand("Personalizar MacFlow", OpenSettings, "settings")
            }, nonInteractiveTest);
        }
        public void OpenBluetooth(){OpenConnections(false);}
        private void OpenConnections(bool wifi)
        {
            if(connections!=null){if(connections.WifiMode==wifi)connections.Refresh();else connections.SwitchMode(wifi);if(connections.WindowState==WindowState.Minimized)connections.WindowState=WindowState.Normal;connections.Activate();return;}
            connections=new ConnectivityPanel(wifi);PrepareTestWindow(connections);var size=ScreenSize();connections.Height=Math.Min(connections.Height,size.Height-65);connections.Left=size.Width-connections.Width-16;connections.Top=46;connections.Closed+=delegate{connections=null;};connections.Show();Design.Reveal(connections);
        }
        public void OpenSettings()
        {
            if(PresentationActive)return;
            if (Settings != null) { if(Settings.WindowState==WindowState.Minimized)Settings.WindowState=WindowState.Normal;if(!Settings.IsVisible)Settings.Show();Settings.Activate(); return; }
            Settings = new SettingsWindow(this, smoke);
            PrepareTestWindow(Settings);
            var size = ScreenSize(); Settings.Width = Math.Min(Settings.Width, size.Width - 40); Settings.Height = Math.Min(Settings.Height, size.Height - 60);
            Settings.Closed += delegate { Settings = null; }; PlaceCenter(Settings, .08); Settings.Show(); Design.Reveal(Settings);
        }
        private readonly List<Window> presentationCovers=new List<Window>();
        internal int PresentationCoverCount{get{return presentationCovers.Count;}}
        internal void BeginPresentation()
        {
            if(PresentationActive)return;PresentationActive=true;
            var screens=smoke?new[]{new System.Drawing.Rectangle(100,50,1000,850)}:Forms.Screen.AllScreens.Select(s=>s.Bounds).ToArray();
            foreach(var rect in screens)
            {
                var image=new Image{Source=PresentationWallpaper,Stretch=Stretch.UniformToFill};
                var cover=new Window{Title="MacFlow · Fondo de presentación",WindowStyle=WindowStyle.None,ResizeMode=ResizeMode.NoResize,Background=Brushes.Black,Content=image,ShowInTaskbar=false,Topmost=true,ShowActivated=false,IsHitTestVisible=false};
                cover.SourceInitialized+=delegate{Native.OpaquePresentation(cover);Native.NoActivate(cover);};cover.Loaded+=delegate{Native.SetWindowPos(new WindowInteropHelper(cover).Handle,new IntPtr(-1),rect.Left,rect.Top,rect.Width,rect.Height,0x50);};presentationCovers.Add(cover);cover.Show();
            }
            ClosePanels();HidePresentationSurfaces();LoadPresentationWallpaper();
        }
        private async void LoadPresentationWallpaper()
        {
            var image=await WelcomeWallpaper.Load(Preferences.WallpaperPath);if(disposed||!PresentationActive||image==null)return;
            PresentationWallpaper=image;foreach(var cover in presentationCovers)((Image)cover.Content).Source=image;
        }
        private void ClosePresentationCovers(){foreach(var cover in presentationCovers)cover.Close();presentationCovers.Clear();}
        private void HidePresentationSurfaces(){dockVisibility.HideImmediately();dockHidden=true;TopBar.Hide();if(widget!=null)widget.Hide();if(Widgets!=null)Widgets.SetVisible(false);if(Stage!=null)Stage.Update(Windows,Native.GetForegroundWindow(),true);if(dockEdge!=null)dockEdge.Hide();}
        internal void EndPresentation(){if(!PresentationActive)return;PresentationActive=false;ClosePresentationCovers();if(disposed||suspended)return;if(Preferences.ShowTopBar)TopBar.Show();UpdateDockVisibility();}
        public void ReplayWelcome(){OpenWelcome(true);}
        public void OpenWelcome()
        {OpenWelcome(false);}
        private void OpenWelcome(bool afterUpdate)
        {
            if(disposed||suspended)return;
            if(welcome!=null){welcome.Activate();return;}BeginPresentation();
            welcome=new WelcomeWindow(smoke?(System.Drawing.Rectangle?)new System.Drawing.Rectangle(40,40,600,420):null,afterUpdate,PresentationWallpaper,afterUpdate?(Action)OpenUpdateTour:null){PreferredWallpaper=Preferences.WallpaperPath};var window=welcome;window.Closed+=delegate{if(window.Advancing)PresentationWallpaper=window.WallpaperImageSource;if(welcome==window)welcome=null;if(!afterUpdate||!window.Advancing)EndPresentation();if(afterUpdate){Preferences.LastWelcomedVersion=Product.VersionText;if(!smoke)Save();}};PrepareTestWindow(window);window.Show();
        }
        internal void OpenUpdateTour()
        {if(disposed||suspended||tour!=null)return;tour=new UpdateTour(this);var presentation=tour;tour.Closed+=delegate{tour=null;if(presentation.Completed)OpenCompletedWelcome();else EndPresentation();};tour.Loaded+=delegate{Preferences.LastWelcomedVersion=Product.VersionText;if(!smoke)Save();};tour.Show();}
        internal void OpenCompletedWelcome(){if(disposed||suspended||welcome!=null)return;welcome=new WelcomeWindow(smoke?(System.Drawing.Rectangle?)new System.Drawing.Rectangle(40,40,600,420):null,false,PresentationWallpaper,null,true){PreferredWallpaper=Preferences.WallpaperPath,EnterDesktop=EnterUpdatedDesktop};var final=welcome;final.Closed+=delegate{if(welcome==final)welcome=null;if(!final.Advancing)EndPresentation();};PrepareTestWindow(final);final.Show();}
        internal bool EnteredUpdatedDesktop{get;private set;}
        private void EnterUpdatedDesktop()
        {
            if(disposed||suspended)return;EnteredUpdatedDesktop=true;startupDockUntil=DateTime.UtcNow.AddSeconds(3);if(smoke){EndPresentation();return;}
            var worker=new Thread(delegate()
            {
                object shell=null;try{shell=Activator.CreateInstance(Type.GetTypeFromProgID("Shell.Application"));((dynamic)shell).MinimizeAll();}
                catch(Exception ex){Log.Write("Mostrar escritorio: "+ex.Message);foreach(var w in Native.Windows())if(Native.Matches(w)&&!Native.IsIconic(w.Handle))Native.RequestMinimize(w);}
                finally{if(shell!=null&&Marshal.IsComObject(shell))Marshal.ReleaseComObject(shell);}
                if(!Application.Current.Dispatcher.HasShutdownStarted)Application.Current.Dispatcher.BeginInvoke(new Action(delegate{if(disposed||suspended)return;EndPresentation();var h=Native.GetShellWindow();if(h!=IntPtr.Zero&&Native.IsDesktopWindow(h))Native.SetForegroundWindow(h);dockVisibility.ShowImmediately();dockHidden=false;Widgets.SetVisible(true);if(Preferences.ShowTopBar&&!TopBar.IsVisible)TopBar.Show();}));
            });worker.IsBackground=true;worker.SetApartmentState(ApartmentState.STA);worker.Start();
        }
        internal void TourDock(bool show){StopDockProbe();}
        internal bool WelcomeForNewVersion()
        {
            bool show=Preferences.NeedsUpdateWelcome(Product.Version);if(show)OpenWelcome(true);else if(Preferences.LastWelcomedVersion!=Product.VersionText){Preferences.LastWelcomedVersion=Product.VersionText;if(!smoke)Save();}return show;
        }
        public void ApplyPreferences(Preferences value, bool? startup)
        {
            value.Validate(); Preferences = value;
            if (!smoke) { if (startup.HasValue) Startup.Enabled = startup.Value; if (value.AutoHideTaskbar && !suspended) TryAcquire(); else shellRecovery.Reset(lease.Restore); }
            Save(); Rebuild();if(!smoke)WatchLocalUpdates();
        }
        private void ConfigureWindowsKey()
        {
            if(smoke||suspended||!Preferences.WindowsMenu){if(windowsKey!=null){windowsKey.Dispose();windowsKey=null;}return;}
            if(windowsKey==null)windowsKey=new WindowsMenuKey(Application.Current.Dispatcher,OpenWindowsMenu);
        }
        public bool WindowsKeyAvailable {get{return windowsKey!=null&&windowsKey.Available;}}
        private List<GlassCommand> PowerCommands()
        {
            var commands=new List<GlassCommand>();foreach(PowerAction action in Enum.GetValues(typeof(PowerAction))){var choice=action;commands.Add(new GlassCommand(PowerActions.Label(choice)+"…",delegate{ConfirmPower(choice);}));}return commands;
        }
        public void OpenPowerMenu(){GlassMenu.Open(Dock,"Energía",PowerCommands(),nonInteractiveTest);}
        public void OpenWindowsMenu()
        {
            if(PresentationActive)return;
            if(disposed)return;if(GlassMenu.Current!=null&&GlassMenu.Current.Title=="MacFlow · Inicio"){GlassMenu.Current.Close();return;}
            var commands=new List<GlassCommand>{new GlassCommand("Aplicaciones",OpenLaunchpad),new GlassCommand("Buscar",OpenSearch),new GlassCommand("Configuración de MacFlow",OpenSettings)};commands.AddRange(PowerCommands());
            GlassMenu.Open(Dock,"Tu Windows · MacFlow",commands,nonInteractiveTest);if(GlassMenu.Current!=null){var menu=GlassMenu.Current;menu.Title="MacFlow · Inicio";var source=PresentationSource.FromVisual(Dock);var matrix=source==null?Matrix.Identity:source.CompositionTarget.TransformFromDevice;var screen=Forms.Screen.PrimaryScreen.WorkingArea;var top=matrix.Transform(new Point(screen.Left,screen.Top));var bottom=matrix.Transform(new Point(screen.Right,screen.Bottom));menu.Left=top.X+18;menu.Top=Math.Max(top.Y+8,bottom.Y-menu.Height-18);}
        }
        public void ConfirmPower(PowerAction action)
        {
            if(PowerConfirmation!=null){PowerConfirmation.Activate();return;}
            var prompt=new PowerPrompt(action,nonInteractiveTest,async delegate{
                if(smoke)return;
                try{await System.Threading.Tasks.Task.Run(delegate{PowerActions.Execute(action);});}catch(Exception ex){Notify("Windows no permitió "+PowerActions.Label(action).ToLowerInvariant()+": "+ex.Message);}
            });PowerConfirmation=prompt;PrepareTestWindow(prompt);prompt.Closed+=delegate{PowerConfirmation=null;};PlaceCenter(prompt,.29);prompt.Show();Design.Reveal(prompt);
        }
        public void ChooseWallpaper(Window owner)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "Imágenes|*.jpg;*.jpeg;*.png;*.bmp" };
            if (dialog.ShowDialog(owner) == true) { try { if (smoke) { MessageBox.Show(owner,"El modo de pruebas no cambia el fondo de Windows.","MacFlow"); return; } Wallpapers.Apply(Store.DirectoryPath, dialog.FileName); } catch(Exception ex) { Notify(ex.Message); } }
        }
        public void RestoreWallpaper() { if(smoke)return;Wallpapers.Restore(Store.DirectoryPath);Preferences.WallpaperPath="";Save(); }
        private void ResumeWallpaper(){if(smoke||String.IsNullOrEmpty(Preferences.WallpaperPath))return;try{Wallpapers.Apply(Store.DirectoryPath,Preferences.WallpaperPath);}catch(Exception ex){Notify("No se pudo recuperar tu fondo: "+ex.Message);}}
        public bool SetWallpaper(string file)
        {
            if(String.IsNullOrEmpty(file)||!File.Exists(file))throw new IOException("Elegí una imagen válida antes de aplicar.");
            if(smoke)return false;
            Wallpapers.Apply(Store.DirectoryPath,file);Preferences.WallpaperPath=System.IO.Path.GetFullPath(file);Save();LiquidMaterial.DesktopChanged();return true;
        }
        public void OpenUpdates(string repository,string feed=null)
        {
            if(disposed||suspended)return;if(updates!=null){updates.Activate();return;}updates=new UpdateWindow(repository,Preferences.LocalUpdateFolder,feed??Preferences.UpdateFeedUrl);var window=updates;window.Closed+=delegate{if(updates==window)updates=null;};PlaceCenter(window,.18);PrepareTestWindow(window);window.Show();Design.Reveal(window);
        }
        private void WatchLocalUpdates()
        {
            if(updatesWatcher!=null){updatesWatcher.Dispose();updatesWatcher=null;}if(updateDebounce!=null)updateDebounce.Stop();
            string folder=Preferences.LocalUpdateFolder;if(!Preferences.CheckUpdatesAutomatically||String.IsNullOrWhiteSpace(folder)||!System.IO.Directory.Exists(folder)||folder.StartsWith("\\\\",StringComparison.Ordinal))return;
            try{
                updateDebounce=new DispatcherTimer(DispatcherPriority.Background){Interval=TimeSpan.FromSeconds(1.2)};updateDebounce.Tick+=delegate{updateDebounce.Stop();CheckUpdatesInBackground();};
                updatesWatcher=new System.IO.FileSystemWatcher(folder,"MacFlow-update.json"){NotifyFilter=System.IO.NotifyFilters.LastWrite|System.IO.NotifyFilters.FileName|System.IO.NotifyFilters.Size};
                System.IO.FileSystemEventHandler changed=delegate{var dispatcher=Application.Current.Dispatcher;if(disposed||dispatcher.HasShutdownStarted)return;try{dispatcher.BeginInvoke(new Action(delegate{if(disposed)return;updateDebounce.Stop();updateDebounce.Start();}));}catch(InvalidOperationException){}};
                updatesWatcher.Changed+=changed;updatesWatcher.Created+=changed;updatesWatcher.Renamed+=delegate(object sender,System.IO.RenamedEventArgs e){changed(sender,e);};updatesWatcher.EnableRaisingEvents=true;
            }catch(Exception ex){Log.Write("Canal local: "+ex.Message);}
        }
        private async void CheckUpdatesInBackground()
        {
            if(disposed||suspended||updatesBusy||!Preferences.CheckUpdatesAutomatically||!UpdateService.HasChannel(Preferences.UpdateRepository,Preferences.LocalUpdateFolder,Preferences.UpdateFeedUrl))return;updatesBusy=true;
            try{var found=await UpdateService.Check(Preferences.UpdateRepository,Preferences.LocalUpdateFolder,Preferences.UpdateFeedUrl,updatesStop.Token);if(!disposed&&found!=null&&notifiedUpdate!=found.Version){notifiedUpdate=found.Version;Notify("Hay una nueva versión de MacFlow: "+found.Version+". Abrí Actualizaciones para instalarla.");}}
            catch(Exception ex){if(!disposed)Log.Write("Consulta de actualización: "+ex.Message);}finally{updatesBusy=false;}
        }
        private void Rebuild()
        {
            ApplyDesign(); DrawDock(); DrawTopBar(); Native.Glass(Dock, Design.Dark);
            RegisterHotkey();ConfigureWindowsKey(); if(timer!=null)timer.Interval=TimeSpan.FromSeconds(Preferences.LowPower?2:1);
            if (Preferences.ShowTopBar && !suspended&&!PresentationActive) { TopBar.Show(); Native.Glass(TopBar, Design.Dark); RegisterAppbar(); } else { UnregisterAppbar(); TopBar.Hide(); }
            RebuildWidget(); Position(); Refresh();
        }
        private void UpdateDockEdge()
        {
            bool enabled=!smoke&&!disposed&&!suspended&&!PresentationActive&&(!fullscreenNow||desktopForeground)&&shellRecovery.Acquired&&Preferences.AutoHideTaskbar&&Preferences.ProtectDockEdge&&Preferences.DockPosition=="Bottom";
            if(!enabled){if(dockEdge!=null&&dockEdge.IsVisible)dockEdge.Hide();return;}
            if(dockEdge==null){dockEdge=new DockEdge(delegate{if(disposed||suspended||fullscreenNow)return;dockLeaveAt=DateTime.UtcNow;dockVisibility.Show();dockHidden=false;});dockEdge.Position(Dock);}
            if(!dockEdge.IsVisible)dockEdge.Show();
            dockEdge.KeepEdgeAvailable();
        }
        private void RebuildWidget()
        {
            if(widget!=null){widget.Close();widget=null;}if(Widgets!=null){Widgets.Sync();Widgets.SetVisible(!suspended&&!PresentationActive&&!fullscreenNow&&desktopForeground);}
        }
        private void UpdateDockVisibility()
        {
            if(PresentationActive){HidePresentationSurfaces();return;}
            if(dockRebuildPending&&!disposed&&Mouse.LeftButton==MouseButtonState.Released)DrawDock();
            if(!smoke&&!disposed&&!suspended&&Preferences.AutoHideTaskbar){if(!shellRecovery.Acquired){TryAcquire();if(shellRecovery.Acquired)Position();}if(shellRecovery.Acquired)lease.Maintain();}
            var foreground=Native.GetForegroundWindow();desktopForeground=DesktopContextForForeground(foreground);if(foreground!=materialForeground){materialForeground=foreground;LiquidMaterial.DesktopChanged();CompositorGlass.Reorder();}
            UpdateActiveWindow(foreground);
            uint foregroundPid;Native.GetWindowThreadProcessId(foreground,out foregroundPid);
            bool ours=foregroundPid==(uint)Process.GetCurrentProcess().Id;
            if(Widgets!=null&&!ours)Widgets.SetVisible(!suspended&&!PresentationActive&&!fullscreenNow&&desktopForeground);
            if(Stage!=null)Stage.Update(Windows,foreground,suspended||fullscreenNow||desktopForeground);
            // External caption overlays were removed: never cover another app's
            // native controls. Traffic lights belong only to MacFlow windows.
            if(captionButtons!=null){captionButtons.Dispose();captionButtons=null;}
            UpdateDockEdge();
            if(disposed||suspended||fullscreenNow&&!desktopForeground)return;
            if(tour!=null){HidePresentationSurfaces();return;}
            if(desktopForeground){fullscreenNow=false;dockLeaveAt=DateTime.UtcNow;dockVisibility.ShowImmediately();dockHidden=false;if(Preferences.ShowTopBar&&!TopBar.IsVisible)TopBar.Show();if(widget!=null&&!widget.IsVisible)widget.Show();return;}
            if(DateTime.UtcNow<startupDockUntil){dockVisibility.ShowImmediately();dockHidden=false;return;}
            bool automatic=AutomaticDockPolicy(Preferences.DockAutoHide,Preferences.HideDockInApps,Preferences.HideDockInCodex,codexForeground,smoke);
            if(!automatic){dockVisibility.Show();dockHidden=false;return;}
            var cursor=Forms.Cursor.Position;var bounds=Forms.Screen.PrimaryScreen.Bounds;
            bool withinX=cursor.X>=bounds.Left&&cursor.X<bounds.Right,withinY=cursor.Y>=bounds.Top&&cursor.Y<bounds.Bottom;
            bool edge=Preferences.DockPosition=="Left"?withinY&&cursor.X>=bounds.Left&&cursor.X<=bounds.Left+8:Preferences.DockPosition=="Right"?withinY&&cursor.X<bounds.Right&&cursor.X>=bounds.Right-8:withinX&&cursor.Y<bounds.Bottom&&cursor.Y>=bounds.Bottom-8;
            bool inside=Dock.IsVisible&&Dock.IsMouseOver,menu=GlassMenu.Current!=null&&GlassMenu.Current.Anchor==Dock;
            if(!ShouldAutoHide(desktopForeground,automatic,inside||edge,menu)){dockLeaveAt=DateTime.UtcNow;dockVisibility.Show();dockHidden=false;return;}
            if(DateTime.UtcNow-dockLeaveAt>TimeSpan.FromMilliseconds(codexForeground?1200:650)){dockVisibility.Hide();dockHidden=true;}
        }
        public static bool IsCodex(string executable)
        {
            if(String.IsNullOrEmpty(executable))return false;string name=System.IO.Path.GetFileNameWithoutExtension(executable);return String.Equals(name,"Codex",StringComparison.OrdinalIgnoreCase)||String.Equals(name,"ChatGPT",StringComparison.OrdinalIgnoreCase)||executable.IndexOf("\\OpenAI.Codex_",StringComparison.OrdinalIgnoreCase)>=0;
        }
        internal static bool ShouldAutoHide(bool desktop,bool automatic,bool interacting,bool dockMenu)
        {return automatic&&!desktop&&!interacting&&!dockMenu;}
        internal static bool AutomaticDockPolicy(bool explicitAutoHide,bool allApps,bool codexOnly,bool codex,bool testing)
        { return explicitAutoHide || (!testing && (allApps || codexOnly && codex)); }
        private bool DesktopContextForForeground(IntPtr foreground)
        {
            if (foreground == IntPtr.Zero || !Native.IsWindow(foreground) || Native.IsIconic(foreground) || !Native.IsWindowVisible(foreground))
                return !Windows.Any(w => Native.IsWindow(w.Handle) && Native.IsWindowVisible(w.Handle) && !Native.IsIconic(w.Handle));
            var source=HwndSource.FromHwnd(foreground);if(source!=null&&source.RootVisual is DesktopWidget)return true;
            return Native.IsDesktopContext(foreground);
        }
        private void RegisterHotkey()
        {
            if(smoke)return;var h=new WindowInteropHelper(Dock).Handle;Native.UnregisterHotKey(h,1);uint mods=Preferences.Hotkey=="CtrlAltSpace"?3u:Preferences.Hotkey=="CtrlSpace"?2u:1u;HotkeyAvailable=Native.RegisterHotKey(h,1,0x4000|mods,0x20);
            Native.UnregisterHotKey(h,2);Native.RegisterHotKey(h,2,0x4003,0x4D);
            Native.UnregisterHotKey(h,3);Native.RegisterHotKey(h,3,0x4003,0x44);
        }
        private void PlaceCenter(Window window, double y)
        {
            var size = ScreenSize(); window.Left = Math.Max(0, (size.Width - window.Width) / 2); window.Top = Math.Max(42, size.Height * y);
        }
        private void OpenUri(string target) { try { Process.Start(new ProcessStartInfo(target) { UseShellExecute = true }); } catch (Exception ex) { Notify(ex.Message); } }
        public void Dispose()
        {
            if(PanelMotion.DockDestination==panelDestination)PanelMotion.DockDestination=null;
            if(updatesTimer!=null)updatesTimer.Stop();if(updateDebounce!=null)updateDebounce.Stop();if(updatesWatcher!=null)updatesWatcher.Dispose();updatesStop.Cancel();
            if(lidTransition!=null)lidTransition.Dispose();
            Focus.Dispose();
            if(foregroundEvents!=null)foregroundEvents.Dispose();
            if (disposed) return; disposed = true;ClosePresentationCovers();if(tour!=null)tour.Close();if(Widgets!=null)Widgets.Dispose();if(Stage!=null)Stage.Dispose();if(dockRedraw!=null)dockRedraw.Stop();if(captionButtons!=null)captionButtons.Dispose();if(dockEdge!=null)dockEdge.Close();if(windowsKey!=null)windowsKey.Dispose();if(dockMotion!=null)dockMotion.Dispose();if (timer != null) timer.Stop();if(edgeTimer!=null)edgeTimer.Stop();if(widget!=null)widget.Close();
            UnregisterAppbar(); if (!smoke) {lease.Restore();try{Wallpapers.Restore(Store.DirectoryPath);}catch(Exception ex){Log.Write(ex.Message);}}
            if (Dock != null) Native.UnregisterHotKey(new WindowInteropHelper(Dock).Handle, 1);
            if (Dock != null) Native.UnregisterHotKey(new WindowInteropHelper(Dock).Handle, 2);
            if (Dock != null) Native.UnregisterHotKey(new WindowInteropHelper(Dock).Handle, 3);
            if (tray != null) { tray.Visible = false; tray.Dispose(); } ClosePanels(); if (Dock != null) Dock.Close(); if (TopBar != null) TopBar.Close();
        }
    }
}


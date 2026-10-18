using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
namespace MacFlow
{
    internal sealed class WidgetManager : IDisposable
    {
        internal readonly WidgetState State;
        internal readonly Desktop Desktop;
        private readonly string file;
        private readonly Dictionary<string,DesktopWidget> windows=new Dictionary<string,DesktopWidget>();
        private readonly DispatcherTimer timer=new DispatcherTimer(DispatcherPriority.Background){Interval=TimeSpan.FromSeconds(1)};
        private bool disposed,sampling,visible,desktopRequested;private int ticks;
        internal DeviceSnapshot Metrics=new DeviceSnapshot();
        internal WidgetManager(Desktop desktop)
        {Desktop=desktop;file=System.IO.Path.Combine(desktop.Store.DirectoryPath,"widgets.json");bool legacy=!File.Exists(file);State=ModuleStore.Read<WidgetState>(file);State.Validate();if(legacy&&desktop.Preferences.ShowWidget){State.Items.Add(new WidgetSpec{Kind="Digital"});Save();}if(State.LayoutRevision<1){foreach(var item in State.Items.Where(w=>w.Size=="M"&&(w.Kind=="Digital"||w.Kind=="Battery"||w.Kind=="Calendar")))item.Size="S";State.LayoutRevision=1;Save();}timer.Tick+=Tick;timer.Start();}
        internal IEnumerable<DesktopWidget> Windows{get{return windows.Values;}}
        internal void Save(){if(disposed)return;try{ModuleStore.Write(file,State);}catch(Exception ex){Log.Write("Widgets: "+ex.Message);}}
        internal bool Add(string kind)
        {if(State.Items.Count>=16)return false;var current=DisplaySpace.Current;var x=new WidgetSpec{Kind=kind,Monitor=current.StableId};if(!WidgetCatalog.Place(x,State.Items,Available(x),0,0))return false;State.Items.Add(x);Desktop.Preferences.ShowWidget=true;Desktop.Save();Save();Sync();return true;}
        internal DisplayArea Area(WidgetSpec spec){return DisplaySpace.Of(System.Windows.Forms.Screen.AllScreens.FirstOrDefault(s=>DisplaySpace.Of(s).Matches(spec.Monitor))??System.Windows.Forms.Screen.PrimaryScreen);}
        internal Size Available(WidgetSpec spec=null){var area=spec==null?DisplaySpace.Of(System.Windows.Forms.Screen.PrimaryScreen):Area(spec);return new Size(area.Width,area.Height);}
        internal bool Resize(WidgetSpec x,string size)
        {string old=x.Size;int c=x.Column,r=x.Row;x.Size=size;if(!WidgetCatalog.Place(x,State.Items,Available(x),c,r)){x.Size=old;return false;}Save();Sync();foreach(var w in windows.Values.Where(w=>w.Spec.Id==x.Id))w.RefreshLayout();return true;}
        internal void Remove(WidgetSpec x){State.Items.Remove(x);Save();Sync();}
        internal void Sync()
        {
            if(disposed)return;
            foreach(var id in windows.Keys.Where(id=>!State.Items.Any(x=>x.Id==id)).ToList()){Design.Dismiss(windows[id]);windows.Remove(id);}
            foreach(var x in State.Items){WidgetCatalog.Place(x,State.Items,Available(x),x.Column,x.Row);DesktopWidget window;if(!windows.TryGetValue(x.Id,out window)){window=new DesktopWidget(this,x);WindowLife.Track(window);var owned=window;var id=x.Id;window.Closed+=delegate{DesktopWidget current;if(windows.TryGetValue(id,out current)&&current==owned)windows.Remove(id);};windows.Add(x.Id,window);}window.Place();window.RefreshAppearance();}
            SetVisible(desktopRequested);
        }
        internal void SetVisible(bool value)
        {
            if(disposed)return;desktopRequested=value;visible=false;var free=new Dictionary<string,bool>();
            foreach(var w in windows.Values.ToArray()){if(!WindowLife.Alive(w))continue;bool show=value;if(!show&&!Desktop.IsTesting&&Desktop.ShellVisible){string key=w.Spec.Monitor??"";if(!free.TryGetValue(key,out show)){var area=Area(w.Spec).Pixels;show=!Desktop.Windows.Any(app=>{Native.RECT r;return !Native.IsIconic(app.Handle)&&Native.GetWindowRect(app.Handle,out r)&&area.IntersectsWith(new Rect(r.Left,r.Top,Math.Max(0,r.Right-r.Left),Math.Max(0,r.Bottom-r.Top)));});free[key]=show;}}show=show&&Desktop.Preferences.ShowWidget&&Desktop.ShellVisible;if(show){visible=true;if(!w.IsVisible){w.Show();w.SetActive(true);}}else if(w.IsVisible&&!w.IsKeyboardFocusWithin){w.SetActive(false);w.Hide();}}
        }
        private async void Tick(object sender,EventArgs e)
        {
            if(disposed)return;var now=DateTime.UtcNow;
            foreach(var x in State.Items.Where(x=>x.Kind=="Timer"&&x.Running&&!x.Completed).ToList())if(x.Remaining(now)<=0){x.Running=false;x.Completed=true;x.Seconds=x.TimerMinutes*60;Save();if(!Desktop.IsTesting)System.Media.SystemSounds.Asterisk.Play();}
            foreach(var w in windows.Values.Where(w=>w.IsVisible))w.Refresh(now);
            Desktop.RefreshLiveActivity();if(!visible||sampling||++ticks%5!=1)return;sampling=true;
            try{var snapshot=await System.Threading.Tasks.Task.Run((Func<DeviceSnapshot>)DeviceMetrics.Read);if(!disposed)Metrics=snapshot;}catch(Exception ex){Log.Write(ex.Message);}finally{sampling=false;}
        }
        public void Dispose(){if(disposed)return;Save();disposed=true;timer.Stop();foreach(var w in windows.Values.ToArray())WindowLife.Close(w);windows.Clear();}
    }
    internal sealed class DesktopWidget : Window
    {
        internal readonly WidgetSpec Spec;
        private readonly WidgetManager owner;
        private readonly StackPanel body=new StackPanel();
        private readonly Border glassPlate;
        private WidgetBattery batteryArt;private WidgetActivity activityArt;
        private TextBlock value,detail;private Canvas clock;private DateTime calendarDay;
        private Line hour,minute,second;
        private MediaCard media;private bool closed,weatherBusy;private WeatherResult weather;private DateTime nextWeather;private int weatherGeneration;
        private readonly CancellationTokenSource stop=new CancellationTokenSource();
        private readonly DispatcherTimer saveDelay=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(450)};
        private bool dark=Design.Dark;
        private string placedMonitor;
        internal DesktopWidget(WidgetManager owner,WidgetSpec spec)
        {
            this.owner=owner;Spec=spec;Title="MacFlow · Widget · "+WidgetCatalog.Name(spec.Kind);WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;ShowInTaskbar=false;ShowActivated=false;Background=Brushes.Transparent;
            Resources["GlassWidgetSurface"]=spec.Surface??"Clear";
            var root=new Grid();root.Children.Add(new ScrollViewer{Content=body,VerticalScrollBarVisibility=ScrollBarVisibility.Hidden,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled});
            var options=Design.Button("⋯",Menu,false);options.Width=24;options.Height=20;options.Padding=new Thickness(0);options.Margin=new Thickness(0,-5,-6,0);options.Background=Brushes.Transparent;options.HorizontalAlignment=HorizontalAlignment.Right;options.VerticalAlignment=VerticalAlignment.Top;options.ToolTip="Tamaño, material y configuración";System.Windows.Automation.AutomationProperties.SetName(options,"Opciones del widget");options.Opacity=0;root.Children.Add(options);
            MouseEnter+=delegate{Motion.Fade(options,options.Opacity,1,Motion.Hover);};MouseLeave+=delegate{Motion.Fade(options,options.Opacity,0,Motion.Hover);};GotKeyboardFocus+=delegate{options.Opacity=1;};
            MouseLeftButtonDown+=delegate(object s,MouseButtonEventArgs e){if(e.ClickCount!=1)return;var node=e.OriginalSource as DependencyObject;while(node!=null&&node!=root){if(node is System.Windows.Controls.Primitives.ButtonBase||node is TextBox||node is Slider)return;node=node is Visual?VisualTreeHelper.GetParent(node):LogicalTreeHelper.GetParent(node);}try{DragMove();var area=DisplaySpace.For(this);Spec.Monitor=area.StableId;int c=(int)Math.Round((Left-area.Dips.Left-16)/88),r=(int)Math.Round((Top-area.Dips.Top-58)/88);WidgetCatalog.Place(Spec,owner.State.Items,owner.Available(Spec),c,r);Place();owner.Save();e.Handled=true;}catch(InvalidOperationException){}};
            glassPlate=Design.Glass(root,26,true);glassPlate.Padding=new Thickness(16);Content=glassPlate;
            SourceInitialized+=delegate{Native.Glass(this,Design.Dark);var h=new System.Windows.Interop.WindowInteropHelper(this).Handle;Native.SetWindowLong(h,-20,Native.GetWindowLong(h,-20)|0x80);};
            Closed+=delegate{closed=true;stop.Cancel();saveDelay.Stop();if(media!=null)media.Dispose();stop.Dispose();};saveDelay.Tick+=delegate{saveDelay.Stop();owner.Save();};Build();
            if(owner.Desktop.IsTesting){ShowActivated=false;IsHitTestVisible=false;}
        }
        internal void Place()
        {
            var r=WidgetCatalog.Bounds(Spec);var area=owner.Area(Spec);AnimatePlacement(WidthProperty,r.Width);AnimatePlacement(HeightProperty,r.Height);if(!IsLoaded||placedMonitor!=area.Device){DisplaySpace.Place(this,area,r.Left,r.Top);placedMonitor=area.Device;}else{AnimatePlacement(LeftProperty,area.Dips.Left+r.Left);AnimatePlacement(TopProperty,area.Dips.Top+r.Top);}
        }
        private void AnimatePlacement(DependencyProperty property,double value)
        {
            double previous=(double)GetValue(property);BeginAnimation(property,null);SetValue(property,value);if(!IsLoaded||!Motion.Enabled||Double.IsNaN(previous)||Math.Abs(previous-value)<.5)return;
            var animation=new DoubleAnimation(previous,value,TimeSpan.FromMilliseconds(240)){EasingFunction=Motion.Ease(),FillBehavior=FillBehavior.Stop};Timeline.SetDesiredFrameRate(animation,60);BeginAnimation(property,animation);
        }
        private void Menu()
        {
            var commands=new List<GlassCommand>();foreach(var size in new[]{"S","M","L"}){var s=size;commands.Add(new GlassCommand(s=="S"?"Pequeño":s=="M"?"Mediano":"Grande",delegate{if(!owner.Resize(Spec,s))MessageBox.Show("No hay espacio libre para ese tamaño.","MacFlow");}));}
            foreach(var mode in new[]{"Clear","Dark","Light"}){var m=mode;commands.Add(new GlassCommand(mode=="Clear"?"Cristal transparente":mode=="Dark"?"Material oscuro":"Material claro",delegate{SetSurface(m);}));}
            commands.Add(new GlassCommand("Configurar",delegate{owner.Desktop.OpenWidgets(Spec.Id);}));commands.Add(new GlassCommand("Quitar widget",delegate{owner.Remove(Spec);}));GlassMenu.Open(this,Title,commands,owner.Desktop.IsTesting);
        }
        private void Build()
        {
            if(media!=null){media.Dispose();media=null;}body.Children.Clear();clock=null;value=null;detail=null;
            value=Design.Text("",Spec.Size=="S"?29:36,false);value.FontFamily=new FontFamily("Segoe UI Variable Display, Segoe UI");value.FontWeight=FontWeights.Light;detail=Design.Text("",11,false);detail.Margin=new Thickness(0,5,0,0);detail.TextWrapping=TextWrapping.Wrap;detail.SetResourceReference(TextBlock.ForegroundProperty,"GlassMuted");batteryArt=null;activityArt=null;
            switch(Spec.Kind)
            {
                case "Calendar": BuildCalendar();break;
                case "Analog":
                    clock=new Canvas{Width=108,Height=108};clock.Children.Add(new Ellipse{Width=108,Height=108,Stroke=Design.Muted,StrokeThickness=.7});
                    for(int i=0;i<12;i++){double a=i*Math.PI/6;clock.Children.Add(new Line{X1=54+Math.Sin(a)*46,Y1=54-Math.Cos(a)*46,X2=54+Math.Sin(a)*50,Y2=54-Math.Cos(a)*50,Stroke=Design.Muted,StrokeThickness=1});}
                    hour=Hand(3);minute=Hand(2);second=Hand(1);second.Stroke=Design.Brush(Design.Accent);body.Children.Add(new Viewbox{Height=108,Child=clock});break;
                case "Battery":
                    batteryArt=new WidgetBattery();if(Spec.Size=="S"){batteryArt.Width=batteryArt.Height=54;batteryArt.HorizontalAlignment=HorizontalAlignment.Left;batteryArt.Margin=new Thickness(0,0,0,8);body.Children.Add(batteryArt);body.Children.Add(value);body.Children.Add(detail);}else{var batteryLayout=new DockPanel();DockPanel.SetDock(batteryArt,Dock.Left);batteryArt.Margin=new Thickness(0,3,16,0);batteryLayout.Children.Add(batteryArt);var batteryLabels=new StackPanel{VerticalAlignment=VerticalAlignment.Center};batteryLabels.Children.Add(value);batteryLabels.Children.Add(detail);batteryLayout.Children.Add(batteryLabels);body.Children.Add(batteryLayout);}break;
                case "Performance":
                    value.FontSize=Spec.Size=="S"?24:30;body.Children.Add(value);body.Children.Add(detail);activityArt=new WidgetActivity{Margin=new Thickness(0,9,0,0)};body.Children.Add(activityArt);break;
                case "Music":media=new MediaCard(owner.Desktop.IsTesting?(IMediaSource)new MediaFixture():new WindowsMediaSource());body.Children.Add(media);break;
                case "Notes":
                    var notesTitle=Design.Text("Notas",12,true);notesTitle.Foreground=Design.Brush("#E8B94E");notesTitle.Margin=new Thickness(0,0,0,8);body.Children.Add(notesTitle);var notes=ModuleUI.Input(Spec.Text,4000,true);notes.MinHeight=94;notes.Background=Brushes.Transparent;notes.BorderThickness=new Thickness(0);notes.Padding=new Thickness(0);notes.SetResourceReference(TextBox.ForegroundProperty,"GlassInk");notes.TextChanged+=delegate{Spec.Text=notes.Text;saveDelay.Stop();saveDelay.Start();};body.Children.Add(notes);break;
                case "Tasks":
                    foreach(var task in Spec.Tasks.ToList()){var t=task;var row=new DockPanel();var remove=Design.Button("×",delegate{Spec.Tasks.Remove(t);owner.Save();Build();},false);remove.Padding=new Thickness(2);remove.Margin=new Thickness(0);DockPanel.SetDock(remove,Dock.Right);row.Children.Add(remove);var check=Design.Check(t.Text,t.Done,delegate(bool on){t.Done=on;owner.Save();});check.Margin=new Thickness(0,2,0,2);row.Children.Add(check);body.Children.Add(row);}
                    var input=ModuleUI.Input("",160);input.MinWidth=80;input.ToolTip="Escribí una tarea y pulsá Enter";input.SetResourceReference(TextBox.ForegroundProperty,"GlassInk");input.KeyDown+=delegate(object s,KeyEventArgs e){if(e.Key!=Key.Enter||String.IsNullOrWhiteSpace(input.Text)||Spec.Tasks.Count>=32)return;Spec.Tasks.Add(new WidgetTask{Text=input.Text.Trim()});owner.Save();Build();};body.Children.Add(ModuleUI.Field(input));break;
                case "Shortcuts":
                    if(!String.IsNullOrEmpty(Spec.AppTarget)){var app=new AppEntry{Name=Spec.AppName,Target=Spec.AppTarget,Executable=Spec.AppExecutable,IconPath=Spec.AppIconPath};var button=ModuleUI.Button(Spec.AppName,delegate{owner.Desktop.Launch(app);},false);var icon=Design.AppIcon(app,44);body.Children.Add(icon);body.Children.Add(button);}else body.Children.Add(Design.Button("Elegir aplicación",delegate{owner.Desktop.OpenWidgets(Spec.Id);},false));break;
                case "Digital":body.Children.Add(value);body.Children.Add(detail);break;
                case "Weather":var city=Design.Text(String.IsNullOrEmpty(Spec.City)?"Clima":Spec.City,14,true);city.TextTrimming=TextTrimming.CharacterEllipsis;body.Children.Add(city);value.FontSize=48;body.Children.Add(value);body.Children.Add(detail);break;
                default:
                    body.Children.Add(value);body.Children.Add(detail);
                    if(Spec.Kind=="Timer"||Spec.Kind=="Stopwatch"){var buttons=new StackPanel{Orientation=Orientation.Horizontal};buttons.Children.Add(Design.Button("▶ / Ⅱ",delegate{Spec.Toggle(DateTime.UtcNow);owner.Save();Refresh(DateTime.UtcNow);},false));buttons.Children.Add(Design.Button("↺",delegate{Spec.Reset();owner.Save();Refresh(DateTime.UtcNow);},false));body.Children.Add(buttons);}break;
            }
            Refresh(DateTime.UtcNow);if(IsLoaded&&Motion.Enabled){var shift=new TranslateTransform(0,5);body.RenderTransform=shift;Motion.To(shift,TranslateTransform.YProperty,0,Motion.Content);Motion.Fade(body,.25,1,Motion.Content);}
        }
        private Line Hand(double thickness){var h=new Line{X1=54,Y1=54,Stroke=Design.Ink,StrokeThickness=thickness,StrokeStartLineCap=PenLineCap.Round,StrokeEndLineCap=PenLineCap.Round};clock.Children.Add(h);return h;}
        private void BuildCalendar()
        {
            if(Spec.Size=="S")
            {
                calendarDay=DateTime.Today;var today=calendarDay;var weekday=Design.Text(today.ToString("dddd",CultureInfo.GetCultureInfo("es-AR")).ToUpperInvariant(),11,true);weekday.Foreground=Design.Brush("#F26965");body.Children.Add(weekday);var number=Design.Text(today.Day.ToString(),48,false);number.FontWeight=FontWeights.Light;number.Margin=new Thickness(0,2,0,4);body.Children.Add(number);body.Children.Add(Design.Text(today.ToString("MMMM yyyy",CultureInfo.GetCultureInfo("es-AR")),11,false));var open=Design.Button("Ver mes",delegate{if(!owner.Resize(Spec,"L"))owner.Desktop.OpenWidgets(Spec.Id);},false);open.Padding=new Thickness(0,4,0,0);open.Margin=new Thickness(0);open.FontSize=10;open.Background=Brushes.Transparent;open.HorizontalAlignment=HorizontalAlignment.Left;body.Children.Add(open);return;
            }
            calendarDay=DateTime.Today;DateTime month=new DateTime(DateTime.Today.Year,DateTime.Today.Month,1).AddMonths(Spec.MonthOffset);var heading=new DockPanel();var back=Design.Button("‹",delegate{Spec.MonthOffset=Math.Max(-120,Spec.MonthOffset-1);owner.Save();Build();},false);var forward=Design.Button("›",delegate{Spec.MonthOffset=Math.Min(120,Spec.MonthOffset+1);owner.Save();Build();},false);
            back.Padding=forward.Padding=new Thickness(3,0,3,0);back.Margin=forward.Margin=new Thickness(0);DockPanel.SetDock(back,Dock.Left);DockPanel.SetDock(forward,Dock.Right);heading.Children.Add(back);heading.Children.Add(forward);heading.Children.Add(Design.Text(month.ToString(Spec.Size=="L"?"MMMM yyyy":"MMM yyyy",CultureInfo.GetCultureInfo("es-AR")),Spec.Size=="L"?17:10,true));body.Children.Add(heading);
            var days=new System.Windows.Controls.Primitives.UniformGrid{Columns=7,Rows=7};foreach(string name in new[]{"L","M","M","J","V","S","D"}){var weekday=Design.Text(name,Spec.Size=="L"?12:10,false);weekday.TextAlignment=TextAlignment.Center;days.Children.Add(weekday);}int offset=((int)month.DayOfWeek+6)%7;
            for(int cell=0;cell<42;cell++){int number=cell-offset+1;if(number<1||number>DateTime.DaysInMonth(month.Year,month.Month)){days.Children.Add(new Border());continue;}DateTime date=month.AddDays(number-1);var day=Design.Button(number.ToString(),delegate{Spec.Text=date.ToString("yyyy-MM-dd");owner.Save();Build();},false);day.ToolTip=date.ToLongDateString();day.Margin=new Thickness(0);day.Padding=new Thickness(0);day.FontSize=Spec.Size=="L"?14:11;day.MinHeight=0;day.Height=Spec.Size=="L"?31:15;day.Background=Brushes.Transparent;if(date==DateTime.Today||Spec.Text==date.ToString("yyyy-MM-dd")){day.Foreground=date==DateTime.Today?Brushes.White:Design.Brush(Design.Accent);day.FontWeight=FontWeights.Bold;day.Background=Design.Brush(date==DateTime.Today?"#EFED453A":"#286D9DEF");}days.Children.Add(day);}body.Children.Add(days);if(Spec.Size=="L"){DateTime selected;if(DateTime.TryParseExact(Spec.Text,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out selected))body.Children.Add(ModuleUI.Caption(selected.ToString("dddd d MMMM",CultureInfo.GetCultureInfo("es-AR"))));}
        }
        private static void Position(Line hand,double angle,double length){hand.X2=54+Math.Sin(angle)*length;hand.Y2=54-Math.Cos(angle)*length;}
        internal async void Refresh(DateTime now)
        {
            if(closed)return;DateTime local=now.ToLocalTime();var m=owner.Metrics;
            switch(Spec.Kind)
            {
                case "Calendar":if(calendarDay!=DateTime.Today)Build();break;
                case "Digital":value.FontSize=Spec.Size=="L"?68:Spec.Size=="M"?52:40;value.FontWeight=FontWeights.Normal;value.Text=local.ToString("HH:mm");detail.Text=local.ToString("dddd d MMMM",CultureInfo.GetCultureInfo("es-AR"));break;
                case "Analog":Position(hour,(local.Hour%12+local.Minute/60.0)*Math.PI/6,26);Position(minute,(local.Minute+local.Second/60.0)*Math.PI/30,39);Position(second,local.Second*Math.PI/30,43);break;
                case "Battery":value.FontSize=Spec.Size=="S"?(m.PowerKnown&&(m.Power.BatteryFlag==128||m.Power.BatteryLifePercent==255)?14:24):36;value.Text=!m.PowerKnown?"—":m.Power.BatteryFlag==128||m.Power.BatteryLifePercent==255?"Corriente":m.Power.BatteryLifePercent+"%";detail.Text=!m.PowerKnown?"No disponible":m.Power.ACLineStatus==1?"Con corriente":"Usando batería";if(batteryArt!=null)batteryArt.Update(m);break;
                case "Performance":value.Text=m.Cpu.HasValue?m.Cpu.Value.ToString("0")+"% CPU":"CPU · —";detail.Text=m.Ready?"RAM "+(m.TotalRam>0?Gb(m.UsedRam)+" / "+Gb(m.TotalRam)+" GB":"—")+"\nDisco libre "+(m.DiskTotal>0?Gb(m.DiskFree)+" GB":"—"):"Consultando RAM y almacenamiento…";if(activityArt!=null)activityArt.Update(m);break;
                case "Network":value.FontSize=Spec.Size=="S"?21:30;value.TextWrapping=TextWrapping.Wrap;value.Text=!m.Ready?"Consultando…":m.Internet.HasValue?m.Internet.Value?"Internet activo":"Sin Internet":m.Network?"Red disponible":"Sin conexión";detail.Text=m.Wifi+(m.Internet.HasValue?"":"\nInternet no verificado");break;
                case "Timer":case "Stopwatch":double secs=Spec.Kind=="Timer"?Spec.Remaining(now):Spec.Elapsed(now);var time=TimeSpan.FromSeconds(Math.Min(secs,TimeSpan.MaxValue.TotalSeconds-1));value.Text=secs>=3600?((long)time.TotalHours).ToString("00")+time.ToString(@"\:mm\:ss"):time.ToString(@"mm\:ss");detail.Text=Spec.Completed?"Tiempo cumplido":Spec.Running?"En curso":"En pausa";break;
                case "Weather":
                    if(!Spec.HasLocation){value.FontSize=Spec.Size=="S"?20:30;value.Text="Elegí ciudad";detail.Text="Configurar · Clima\nOpen-Meteo / GeoNames";return;}
                    if(weather!=null){value.FontSize=Spec.Size=="S"?44:58;value.Text=weather.current.temperature_2m.ToString("0")+"°";detail.Text=Spec.City+" · "+WidgetWeather.Description(weather.current.weather_code);if(Spec.Size!="S"&&weather.daily.time!=null)for(int i=0;i<Math.Min(3,weather.daily.time.Length);i++)if(weather.daily.temperature_2m_min!=null&&weather.daily.temperature_2m_max!=null&&i<weather.daily.temperature_2m_min.Length&&i<weather.daily.temperature_2m_max.Length)detail.Text+="\n"+weather.daily.time[i]+"  "+weather.daily.temperature_2m_min[i].ToString("0")+"° / "+weather.daily.temperature_2m_max[i].ToString("0")+"°";detail.Text+="\nOpen-Meteo / GeoNames";}
                    if(weatherBusy||now<nextWeather||!IsVisible)return;weatherBusy=true;nextWeather=now.AddMinutes(30);if(weather==null){value.FontSize=Spec.Size=="S"?16:24;value.Text="Consultando…";detail.Text=Spec.City;}
                    int generation=weatherGeneration;try{var result=await WidgetWeather.Forecast(Spec.Latitude,Spec.Longitude,stop.Token);if(!closed&&generation==weatherGeneration){weather=result;Refresh(DateTime.UtcNow);}}catch(Exception ex){if(!closed&&generation==weatherGeneration){detail.Text="Sin pronóstico · reintento en 5 min\n"+Spec.City;nextWeather=now.AddMinutes(5);Log.Write(ex.Message);}}finally{weatherBusy=false;if(!closed&&generation!=weatherGeneration)Refresh(DateTime.UtcNow);}break;
            }
        }
        private static string Gb(ulong bytes){return (bytes/1073741824.0).ToString("0.0");}
        internal void RefreshLayout(){Build();}
        internal void SetSurface(string mode){Spec.Surface=mode=="Dark"||mode=="Light"?mode:"Clear";Resources["GlassWidgetSurface"]=Spec.Surface;owner.Save();if(IsLoaded)Native.Glass(this,Design.Dark);}
        internal void RefreshAppearance(){if(dark==Design.Dark)return;dark=Design.Dark;glassPlate.Background=Design.MaterialTint(true,dark);Build();if(IsLoaded)Native.Glass(this,dark);}
        internal void SetActive(bool active){if(Spec.Kind!="Music")return;if(!active&&media!=null){media.Dispose();media=null;body.Children.Clear();}else if(active&&media==null)Build();}
        internal void Reconfigure(){weatherGeneration++;weather=null;nextWeather=DateTime.MinValue;Build();}
    }
    internal sealed class WidgetGallery : FeaturePanel
    {
        private readonly WidgetManager owner;private readonly StackPanel editor=new StackPanel();private readonly ComboBox installed=new ComboBox();private readonly CancellationTokenSource stop=new CancellationTokenSource();
        private ComboBox appChoice;
        internal WidgetGallery(WidgetManager manager,string select):base("Widgets",900,660)
        {
            owner=manager;var grid=new Grid();grid.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(440)});grid.ColumnDefinitions.Add(new ColumnDefinition());
            var library=new StackPanel();library.Children.Add(Design.Text("Tu escritorio, a tu medida.",19,true));library.Children.Add(Caption("Arrastrá el espacio libre del widget. Elegí su tamaño y material desde ⋯."));
            var query=ModuleUI.Input("",50);query.ToolTip="Buscar widgets";System.Windows.Automation.AutomationProperties.SetName(query,"Buscar widgets");var search=new Grid();search.Children.Add(ModuleUI.Field(query));var placeholder=Design.Text("Buscar widgets",13,false);placeholder.Foreground=Design.Muted;placeholder.Margin=new Thickness(14,0,14,0);placeholder.IsHitTestVisible=false;search.Children.Add(placeholder);library.Children.Add(search);var choices=new WrapPanel();library.Children.Add(choices);
            string[] glyphs={"performance","performance","grid","sun","battery","performance","play","folder","check","performance","performance","grid","network"};
            for(int i=0;i<WidgetCatalog.Kinds.Length;i++){string kind=WidgetCatalog.Kinds[i],name=WidgetCatalog.Names[i];var button=ModuleUI.Button("",delegate{if(!owner.Add(kind)){MessageBox.Show("No hay espacio libre. Quitá o reducí un widget.","MacFlow");return;}Populate(owner.State.Items.Last().Id);},false);System.Windows.Automation.AutomationProperties.SetName(button,"+ "+name);button.Tag=name;var card=new StackPanel{Width=174};card.Children.Add(Preview(kind,glyphs[i]));var labels=new StackPanel();labels.Children.Add(Design.Text(name,13,true));labels.Children.Add(Caption("+ Añadir al escritorio"));card.Children.Add(labels);button.Content=card;button.HorizontalContentAlignment=HorizontalAlignment.Stretch;button.Padding=new Thickness(12,9,12,9);button.Margin=new Thickness(0,4,10,6);choices.Children.Add(button);}
            query.TextChanged+=delegate{placeholder.Visibility=query.Text.Length==0?Visibility.Visible:Visibility.Collapsed;foreach(Button button in choices.Children)button.Visibility=SettingsWindow.SearchMatches((string)button.Tag,query.Text.Trim())?Visibility.Visible:Visibility.Collapsed;};
            grid.Children.Add(new ScrollViewer{Content=library,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});Grid.SetColumn(editor,1);editor.Margin=new Thickness(24,0,0,0);grid.Children.Add(new ScrollViewer{Content=editor,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});Grid.SetColumn(grid.Children[1],1);Body.Children.Add(grid);
            installed.SelectionChanged+=delegate{Edit(installed.SelectedItem as WidgetSpec);};Closed+=delegate{stop.Cancel();stop.Dispose();};Populate(select);
        }
        private UIElement Preview(string kind,string glyph)
        {
            var body=new StackPanel();string value=kind=="Digital"?DateTime.Now.ToString("HH:mm"):kind=="Calendar"?DateTime.Today.Day.ToString():kind=="Timer"?"05:00":kind=="Stopwatch"?"00:00":kind=="Battery"?owner.Metrics.PowerKnown&&owner.Metrics.Power.BatteryLifePercent<=100?owner.Metrics.Power.BatteryLifePercent+"%":"—":kind=="Performance"?owner.Metrics.Cpu.HasValue?owner.Metrics.Cpu.Value.ToString("0")+"%":"—":"";
            if(kind=="Calendar"){var month=Design.Text(DateTime.Today.ToString("MMMM",CultureInfo.GetCultureInfo("es-AR")).ToUpperInvariant(),10,true);month.Foreground=Design.Brush("#F06464");body.Children.Add(month);}
            else body.Children.Add(new Border{Child=Design.Symbol(glyph,20,Design.Ink),HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,0,0,6)});
            if(value.Length>0)body.Children.Add(Design.Text(value,kind=="Calendar"?38:32,false));
            else if(kind=="Notes"||kind=="Tasks"){for(int n=0;n<2;n++)body.Children.Add(new Border{Height=5,CornerRadius=new CornerRadius(2),Background=Design.Brush(Design.Dark?"#36FFFFFF":"#26000000"),Margin=new Thickness(0,9,15+n*18,0)});}
            else if(kind=="Analog"){var hands=new System.Windows.Shapes.Path{Data=Geometry.Parse("M40,7 A33,33 0 1 1 39.9,7 M40,18 L40,40 60,48"),Stroke=Design.Ink,StrokeThickness=2,Width=72,Height=72,Stretch=Stretch.Uniform};body.Children.Clear();body.Children.Add(hands);}
            else{var caption=Design.Text(kind=="Weather"?"Elegí una ciudad":kind=="Music"?"Tu música":kind=="Network"?"Conexión":"Tus aplicaciones",13,true);body.Children.Add(caption);}
            return new Border{Height=116,CornerRadius=new CornerRadius(20),Background=Design.Brush(Design.Dark?"#D52E2E33":"#C8FAFAFC"),Padding=new Thickness(15,12,15,10),Child=body,Margin=new Thickness(0,0,0,10)};
        }        private void Populate(string select){installed.ItemsSource=owner.State.Items.ToList();installed.SelectedItem=owner.State.Items.FirstOrDefault(x=>x.Id==select)??owner.State.Items.FirstOrDefault();Edit(installed.SelectedItem as WidgetSpec);}
        internal void SelectWidget(string id){if(id!=null)Populate(id);}
        internal void RefreshApps(){if(appChoice!=null)appChoice.ItemsSource=owner.Desktop.Apps;}
        private void Edit(WidgetSpec x)
        {
            appChoice=null;editor.Children.Clear();editor.Children.Add(Design.Text("Tus widgets",20,true));editor.Children.Add(installed);if(x==null){editor.Children.Add(Caption("Elegí un widget para añadirlo."));editor.Children.Add(new GlassPiece(new StackPanel{Children={Design.Text(DateTime.Today.ToString("dd"),64,true),Design.Text(DateTime.Today.ToString("MMMM",CultureInfo.GetCultureInfo("es-AR")),22,true),Caption("Calendario, notas y tus herramientas favoritas.\nTres tamaños. Tu distribución queda guardada.")}},24,new Thickness(22)){Margin=new Thickness(0,24,0,0)});return;}
            editor.Children.Add(Design.Text(WidgetCatalog.Name(x.Kind),17,true));var sizes=new StackPanel{Orientation=Orientation.Horizontal};foreach(var size in new[]{"S","M","L"}){string s=size;sizes.Children.Add(Design.Button(s,delegate{if(!owner.Resize(x,s))MessageBox.Show("Ese tamaño necesita más espacio libre.","MacFlow");},false));}editor.Children.Add(sizes);
            editor.Children.Add(Caption("Material del widget"));var materials=new StackPanel{Orientation=Orientation.Horizontal};foreach(var mode in new[]{"Clear","Dark","Light"}){var selected=mode;materials.Children.Add(Design.Button(mode=="Clear"?"Transparente":mode=="Dark"?"Oscuro":"Claro",delegate{x.Surface=selected;owner.Save();foreach(var w in owner.Windows.Where(w=>w.Spec.Id==x.Id))w.SetSurface(selected);Edit(x);},x.Surface==mode));}editor.Children.Add(materials);
            if(x.Kind=="Timer"){var duration=ModuleUI.Input(x.TimerMinutes.ToString(),4);editor.Children.Add(Caption("Duración en minutos (1 a 1440)"));editor.Children.Add(ModuleUI.Field(duration));editor.Children.Add(ModuleUI.Button("Guardar duración",delegate{int n;if(Int32.TryParse(duration.Text,out n)&&n>=1&&n<=1440){x.TimerMinutes=n;x.Reset();owner.Save();}else MessageBox.Show("Ingresá entre 1 y 1440 minutos.","MacFlow");},false));}
            if(x.Kind=="Shortcuts"){var apps=new ComboBox{ItemsSource=owner.Desktop.Apps};appChoice=apps;editor.Children.Add(apps);editor.Children.Add(ModuleUI.Button("Elegir",delegate{var app=apps.SelectedItem as AppEntry;if(app!=null){x.AppTarget=app.Target;x.AppName=app.Name;x.AppExecutable=app.Executable;x.AppIconPath=app.IconPath;owner.Save();foreach(var w in owner.Windows.Where(w=>w.Spec.Id==x.Id))w.Reconfigure();}},false));}
            if(x.Kind=="Weather")
            {
                var city=ModuleUI.Input(x.City,100);var results=new ComboBox();var status=Caption("Buscá y seleccioná tu ciudad. No usamos tu ubicación automática.");editor.Children.Add(ModuleUI.Field(city));editor.Children.Add(results);editor.Children.Add(status);
                var search=ModuleUI.Button("Buscar ciudad",null,false);search.Click+=async delegate{search.IsEnabled=false;try{var found=await WidgetWeather.Search(city.Text,stop.Token);if(stop.IsCancellationRequested)return;results.ItemsSource=found;status.Text=found.Length==0?"No se encontraron ciudades. Probá un nombre más completo.":"Elegí un resultado y guardá.";}catch(Exception ex){if(!stop.IsCancellationRequested)status.Text="No se pudo consultar. Revisá Internet.";Log.Write(ex.Message);}finally{if(!stop.IsCancellationRequested)search.IsEnabled=true;}};editor.Children.Add(search);
                editor.Children.Add(ModuleUI.Button("Guardar ciudad",delegate{var choice=results.SelectedItem as GeoCity;if(choice==null)return;x.City=choice.name;x.Latitude=choice.latitude;x.Longitude=choice.longitude;x.HasLocation=true;owner.Save();foreach(var w in owner.Windows.Where(w=>w.Spec.Id==x.Id))w.Reconfigure();},false));editor.Children.Add(Caption("Clima: Open-Meteo. Ciudades: GeoNames. Actualización cada 30 minutos."));
            }
            editor.Children.Add(Design.Check("Mostrar widgets en el escritorio",owner.Desktop.Preferences.ShowWidget,delegate(bool on){owner.Desktop.Preferences.ShowWidget=on;owner.Desktop.Save();owner.Sync();}));editor.Children.Add(ModuleUI.Button("Quitar",delegate{owner.Remove(x);Populate(null);},false));
        }
    }
}

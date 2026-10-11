using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MacFlow
{
    internal sealed class ConnectivityPanel : Window
    {
        private readonly Func<bool,ConnectionSnapshot> read;
        private readonly Action<bool> settings;
        private readonly Action<ConnectionItem> connect;
        private readonly Func<bool,Task<RadioSnapshot>> readRadio;
        private readonly Func<bool,bool,System.Threading.CancellationToken,Task<RadioChange>> setRadio;
        private readonly System.Threading.CancellationTokenSource radioCancellation=new System.Threading.CancellationTokenSource();
        private readonly CheckBox power;
        private readonly TextBlock radioState;
        private bool updatingRadio,radioBusy;
        private string radioFeedback="";
        private readonly StackPanel rows=new StackPanel();
        private readonly TextBlock status,title;
        private readonly Button refresh,wifi,bluetooth;
        private bool closed,busy,connecting;
        private int generation;
        internal bool WifiMode {get;private set;}
        internal bool Ready {get;private set;}
        internal int ItemCount {get;private set;}
        internal string FirstName {get;private set;}
        internal ConnectivityPanel(bool wifiMode):this(wifiMode,
            mode=>mode?Connectivity.ReadWifi():Connectivity.ReadBluetooth(),
            mode=>Process.Start(new ProcessStartInfo(mode?"ms-settings:network-wifi":"ms-settings:bluetooth"){UseShellExecute=true}),Connectivity.ConnectSaved,RadioControls.ReadAsync,RadioControls.SetAsync){}
        internal ConnectivityPanel(bool wifiMode,Func<bool,ConnectionSnapshot> provider,Action<bool> openSettings,Action<ConnectionItem> connectSaved,Func<bool,Task<RadioSnapshot>> radioRead=null,Func<bool,bool,System.Threading.CancellationToken,Task<RadioChange>> radioSet=null)
        {
            read=provider;settings=openSettings;connect=connectSaved;WifiMode=wifiMode;
            readRadio=radioRead??(mode=>Task.FromResult(new RadioSnapshot{Message="Datos de prueba"}));setRadio=radioSet??((mode,on,cancellation)=>Task.FromResult(new RadioChange{Message="Prueba aislada"}));
            Title="MacFlow · Conexiones";Width=390;Height=Math.Min(555,SystemParameters.WorkArea.Height-70);
            WindowStyle=WindowStyle.None;AllowsTransparency=false;ResizeMode=ResizeMode.NoResize;Topmost=true;ShowInTaskbar=true;Background=Brushes.Transparent;
            var root=new Grid();root.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});root.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});root.RowDefinitions.Add(new RowDefinition());root.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
            var head=new DockPanel{Margin=new Thickness(0,0,0,14)};
            var lights=new WindowControls(this,delegate{Design.Dismiss(this);});DockPanel.SetDock(lights,Dock.Left);head.Children.Add(lights);
            title=Design.Text("",17,true);title.VerticalAlignment=VerticalAlignment.Center;title.Margin=new Thickness(13,0,0,0);head.Children.Add(title);
            head.MouseLeftButtonDown+=delegate(object sender,MouseButtonEventArgs e){if(e.OriginalSource==title||e.OriginalSource==head){try{DragMove();}catch(InvalidOperationException){}}};root.Children.Add(head);
            var tabs=new Grid{Margin=new Thickness(0,0,0,14)};tabs.ColumnDefinitions.Add(new ColumnDefinition());tabs.ColumnDefinitions.Add(new ColumnDefinition());
            wifi=Design.Button("Wi-Fi",delegate{SwitchMode(true);},false);bluetooth=Design.Button("Bluetooth",delegate{SwitchMode(false);},false);
            wifi.Margin=new Thickness(0,0,4,0);bluetooth.Margin=new Thickness(4,0,0,0);wifi.Height=36;bluetooth.Height=36;Grid.SetColumn(bluetooth,1);tabs.Children.Add(wifi);tabs.Children.Add(bluetooth);Grid.SetRow(tabs,1);root.Children.Add(tabs);
            root.RowDefinitions.Insert(2,new RowDefinition{Height=GridLength.Auto});var radioRow=new DockPanel{Margin=new Thickness(3,0,3,12)};
            power=Design.Check("",false,delegate(bool value){if(!updatingRadio)ChangeRadio(value);});power.Margin=new Thickness(8,0,0,0);power.Width=45;power.IsEnabled=false;DockPanel.SetDock(power,Dock.Right);radioRow.Children.Add(power);radioState=Design.Text("Consultando radio…",11,true);radioState.VerticalAlignment=VerticalAlignment.Center;radioState.TextWrapping=TextWrapping.Wrap;radioRow.Children.Add(radioState);Grid.SetRow(radioRow,2);root.Children.Add(radioRow);
            var list=new ScrollViewer{Content=rows,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,Padding=new Thickness(10)};
            Grid.SetRow(list,3);root.Children.Add(list);
            var footer=new StackPanel{Margin=new Thickness(0,12,0,0)};status=Design.Text("",11,false);status.TextWrapping=TextWrapping.Wrap;status.Margin=new Thickness(2,0,2,10);footer.Children.Add(status);
            var buttons=new Grid();buttons.ColumnDefinitions.Add(new ColumnDefinition());buttons.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});
            var manage=Design.Button("Configuración de Windows",delegate{OpenSettings();},false);manage.HorizontalAlignment=HorizontalAlignment.Left;manage.FontSize=11;manage.Padding=new Thickness(10,8,10,8);buttons.Children.Add(manage);
            refresh=Design.Button("Actualizar",delegate{radioFeedback="";Refresh();},false);refresh.FontSize=11;refresh.Padding=new Thickness(12,8,12,8);Grid.SetColumn(refresh,1);buttons.Children.Add(refresh);footer.Children.Add(buttons);Grid.SetRow(footer,4);root.Children.Add(footer);
            Content=Design.Glass(root,22,false);
            // Labels never depend on the varying desktop luminance inside this dense panel.
            Resources["GlassInk"]=Design.Ink;Resources["GlassMuted"]=Design.Muted;
            SourceInitialized+=delegate{Native.Glass(this,Design.Dark);};Loaded+=delegate{UpdateMode();Refresh();};
            Closed+=delegate{closed=true;generation++;radioCancellation.Cancel();};KeyDown+=delegate(object sender,KeyEventArgs e){if(e.Key==Key.Escape)Design.Dismiss(this);};
        }
        internal void SwitchMode(bool mode)
        {
            if(closed||radioBusy||WifiMode==mode)return;WifiMode=mode;generation++;Ready=false;radioFeedback="";rows.Children.Clear();ItemCount=0;FirstName=null;UpdateMode();Refresh();
        }
        private void UpdateMode()
        {
            title.Text=WifiMode?"Wi-Fi":"Bluetooth";
            System.Windows.Automation.AutomationProperties.SetName(power,WifiMode?"Encender o apagar Wi-Fi":"Encender o apagar Bluetooth");
            wifi.Background=WifiMode?Design.Brush(Design.Dark?"#45578DCA":"#305C9FF1"):Brushes.Transparent;
            bluetooth.Background=!WifiMode?Design.Brush(Design.Dark?"#45578DCA":"#305C9FF1"):Brushes.Transparent;
            status.Text=WifiMode?"Consultando redes disponibles…":"Consultando dispositivos guardados…";
        }
        internal async void Refresh()
        {
            if(closed||busy||connecting||radioBusy)return;busy=true;Ready=false;refresh.IsEnabled=false;power.IsEnabled=false;rows.IsEnabled=false;int request=generation;bool mode=WifiMode;status.Text="Actualizando…";
            ConnectionSnapshot result;
            RadioSnapshot radio;
            try{var networks=Task.Run(()=>read(mode));var radioTask=readRadio(mode);result=await networks;radio=await radioTask;if(result==null||radio==null)throw new InvalidOperationException();}
            catch{result=new ConnectionSnapshot{Message="No se pudo actualizar. Probá nuevamente o abrí Configuración de Windows."};radio=new RadioSnapshot{Message="No disponible"};}
            busy=false;if(closed)return;
            if(request!=generation){Refresh();return;}
            updatingRadio=true;power.IsChecked=radio.On;updatingRadio=false;power.IsEnabled=radio.Available&&radio.CanChange;radioState.Text=radio.Available?(radio.On?"Encendido":"Apagado")+(radio.Message==""?"":" · "+radio.Message):radio.Message;
            if(radio.Available&&!radio.On){if(mode){result.Items.Clear();result.Message="Wi-Fi está apagado.";}else foreach(var device in result.Items){device.Connected=false;if(device.Detail=="Conectado")device.Detail="Guardado";}}
            refresh.IsEnabled=true;rows.IsEnabled=true;rows.Children.Clear();ItemCount=result.Items.Count;FirstName=ItemCount==0?null:result.Items[0].Name;
            foreach(var item in result.Items)AddRow(item,mode);
            if(ItemCount==0){var empty=Design.Text(mode?"Sin redes disponibles":"Sin dispositivos disponibles",13,true);empty.Margin=new Thickness(10,30,10,12);empty.TextWrapping=TextWrapping.Wrap;rows.Children.Add(empty);}
            status.Text=(radioFeedback==""?"":radioFeedback+" ")+(result.Message!=""?result.Message:mode?"Conectá redes guardadas. Para una red nueva, abrí Configuración.":"Dispositivos guardados. Agregá o administrá conexiones en Windows.");Ready=true;
        }
        internal async void ChangeRadio(bool value)
        {
            if(closed||busy||connecting||radioBusy)return;radioBusy=true;Ready=false;generation++;power.IsEnabled=false;refresh.IsEnabled=false;rows.IsEnabled=false;wifi.IsEnabled=bluetooth.IsEnabled=false;status.Text=value?"Encendiendo…":"Apagando…";
            RadioChange result;try{result=await setRadio(WifiMode,value,radioCancellation.Token);}catch{result=new RadioChange{Message="Windows no pudo cambiar el radio."};}
            radioBusy=false;if(closed)return;wifi.IsEnabled=bluetooth.IsEnabled=true;radioFeedback=result.Message;Refresh();
        }
        internal bool RadioOn {get{return power.IsChecked==true;}}
        internal bool RadioEnabled {get{return power.IsEnabled;}}
        private void AddRow(ConnectionItem item,bool mode)
        {
            var grid=new Grid();grid.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(35)});grid.ColumnDefinitions.Add(new ColumnDefinition());grid.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});
            var symbol=new Border{Child=Design.Symbol(mode?"wifi":"bluetooth",20,Design.Ink),VerticalAlignment=VerticalAlignment.Center};grid.Children.Add(symbol);
            var text=new StackPanel{Margin=new Thickness(3,0,10,0),VerticalAlignment=VerticalAlignment.Center};var name=Design.Text(item.Name,12,true);name.TextTrimming=TextTrimming.CharacterEllipsis;name.ToolTip=item.Name;text.Children.Add(name);
            string detail=mode?(item.Connected?"Conectada":(item.Secure?"Protegida":"Abierta"))+" · Señal "+item.Signal+"%":item.Detail;
            var sub=Design.Text(detail,10,false);sub.SetResourceReference(TextBlock.ForegroundProperty,"GlassMuted");sub.Margin=new Thickness(0,4,0,0);sub.TextTrimming=TextTrimming.CharacterEllipsis;text.Children.Add(sub);Grid.SetColumn(text,1);grid.Children.Add(text);
            if(item.Connected){var check=Design.Text("✓",16,true);check.Foreground=Design.Brush("#3288E8");check.VerticalAlignment=VerticalAlignment.Center;Grid.SetColumn(check,2);grid.Children.Add(check);}
            var button=Design.Button("",delegate{Choose(item);},false);button.Content=new GlassPiece(grid,14,new Thickness(10,12,10,12));button.HorizontalContentAlignment=HorizontalAlignment.Stretch;button.Padding=new Thickness(0);button.Margin=new Thickness(0,0,0,7);button.Background=Brushes.Transparent;button.BorderThickness=new Thickness(0);button.IsEnabled=!mode||!item.Connected;
            if(mode&&item.Connected)button.Opacity=1; // Read-only status still needs full contrast.
            button.ToolTip=mode?String.IsNullOrWhiteSpace(item.Profile)?"Configurar esta red en Windows":"Conectar a esta red guardada":"Administrar dispositivo en Windows";
            System.Windows.Automation.AutomationProperties.SetName(button,item.Name+", "+detail);rows.Children.Add(button);
        }
        internal async void Choose(ConnectionItem item)
        {
            if(closed||connecting||item==null||WifiMode&&item.Connected)return;
            if(!WifiMode||String.IsNullOrWhiteSpace(item.Profile)||!item.Connectable){OpenSettings();return;}
            int request=++generation;connecting=true;Ready=false;refresh.IsEnabled=false;rows.IsEnabled=false;status.Text="Solicitando conexión a "+item.Name+"…";
            string failure=null;try{await Task.Run(()=>connect(item));await Task.Delay(1200);}catch{failure="Windows no pudo iniciar la conexión. Revisá la red en Configuración.";}
            connecting=false;if(closed)return;rows.IsEnabled=true;refresh.IsEnabled=true;
            if(request!=generation){Refresh();return;}
            if(failure!=null){status.Text=failure;Ready=true;return;}
            // WlanConnect queues a request; only a fresh native snapshot can report success.
            Refresh();
        }
        private void OpenSettings(){try{settings(WifiMode);}catch{status.Text="No se pudo abrir Configuración de Windows.";}}
    }
}


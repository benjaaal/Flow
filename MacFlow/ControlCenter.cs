using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
namespace MacFlow
{
    public sealed class ControlCenter : Window
    {
        private readonly IControlBackend backend;private readonly Desktop desktop;private readonly CancellationTokenSource stop=new CancellationTokenSource();
        private readonly DispatcherTimer poll=new DispatcherTimer(DispatcherPriority.Background){Interval=TimeSpan.FromSeconds(2)};
        private readonly DispatcherTimer brightnessDelay=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(140)};
        private readonly DispatcherTimer volumeDelay=new DispatcherTimer(DispatcherPriority.Input){Interval=TimeSpan.FromMilliseconds(40)};
        private double? pendingVolume;
        private readonly CheckBox wifi,bluetooth,mute;private readonly TextBlock wifiText,bluetoothText,soundText,brightnessText,powerText,status;
        internal readonly Slider VolumeSlider,BrightnessSlider;
        private readonly MediaCard media;private bool updating,busy,closed,radioBusy,brightnessBusy;private int? pendingBrightness;
        private int radioGeneration;
        public ControlCenter(Desktop desktop):this(desktop,desktop.IsTesting?(IControlBackend)new ControlFixture():new ControlBackend()){}
        internal ControlCenter(Desktop desktop,IControlBackend provider)
        {
            this.desktop=desktop;backend=provider;Title="MacFlow · Centro de control";Width=382;Height=Math.Min(650,DisplaySpace.Current.Height-65);WindowStyle=WindowStyle.None;AllowsTransparency=false;ResizeMode=ResizeMode.NoResize;Topmost=true;ShowInTaskbar=false;Background=Brushes.Transparent;
            var root=new StackPanel();var header=new DockPanel{Margin=new Thickness(4,0,4,12)};var close=Design.Button("",delegate{Design.Dismiss(this);},false);close.Content=Design.Symbol("up",18,Design.Ink);close.ToolTip="Cerrar hacia arriba";System.Windows.Automation.AutomationProperties.SetName(close,"Cerrar Centro de control");close.Padding=new Thickness(6);close.Margin=new Thickness(0);close.Background=Brushes.Transparent;DockPanel.SetDock(close,Dock.Right);header.Children.Add(close);header.Children.Add(Design.Text("Centro de control",18,true));root.Children.Add(header);
            var top=new Grid{Margin=new Thickness(-4,0,-4,0)};top.ColumnDefinitions.Add(new ColumnDefinition());top.ColumnDefinitions.Add(new ColumnDefinition());
            var radios=new StackPanel();wifi=Design.Check("",false,delegate(bool on){if(!updating)ChangeRadio(true,on);});wifiText=Note("Consultando…");radios.Children.Add(RadioRow("Wi-Fi",wifi,wifiText,desktop.OpenWifi));bluetooth=Design.Check("",false,delegate(bool on){if(!updating)ChangeRadio(false,on);});bluetoothText=Note("Consultando…");radios.Children.Add(RadioRow("Bluetooth",bluetooth,bluetoothText,desktop.OpenBluetooth));top.Children.Add(Piece(radios));
            media=new MediaCard(desktop.IsTesting?(IMediaSource)new MediaFixture():new WindowsMediaSource());var music=Piece(media);Grid.SetColumn(music,1);top.Children.Add(music);root.Children.Add(top);
            var levels=new Grid();levels.ColumnDefinitions.Add(new ColumnDefinition());levels.ColumnDefinitions.Add(new ColumnDefinition());
            var sound=new StackPanel();var soundTitle=Design.Text("Sonido",12,true);soundTitle.HorizontalAlignment=HorizontalAlignment.Center;sound.Children.Add(soundTitle);VolumeSlider=new GlassLevelSlider("volume"){Margin=new Thickness(0,9,0,8),IsEnabled=false};sound.Children.Add(VolumeSlider);soundText=Note("Salida predeterminada");soundText.FontSize=10;soundText.TextAlignment=TextAlignment.Center;sound.Children.Add(soundText);mute=Design.Check("Silenciar",false,delegate(bool on){if(!updating)try{backend.Mute(on);}catch(Exception ex){status.Text=ex.Message;}});mute.HorizontalAlignment=HorizontalAlignment.Center;mute.FontSize=11;mute.Margin=new Thickness(0,8,0,0);sound.Children.Add(mute);levels.Children.Add(Piece(sound));
            var light=new StackPanel();var lightTitle=Design.Text("Pantalla",12,true);lightTitle.HorizontalAlignment=HorizontalAlignment.Center;light.Children.Add(lightTitle);BrightnessSlider=new GlassLevelSlider("sun"){Margin=new Thickness(0,9,0,8),IsEnabled=false};light.Children.Add(BrightnessSlider);brightnessText=Note("Consultando brillo…");brightnessText.FontSize=10;brightnessText.TextAlignment=TextAlignment.Center;brightnessText.MaxHeight=40;light.Children.Add(brightnessText);light.Children.Add(Link("Luz nocturna ↗",delegate{Settings("ms-settings:nightlight");}));var lightPiece=Piece(light);Grid.SetColumn(lightPiece,1);levels.Children.Add(lightPiece);root.Children.Add(levels);
            var tiles=new UniformGrid{Columns=2};tiles.Children.Add(Tile("moon","Concentración",delegate{Design.Dismiss(this,desktop.OpenFocus);}));tiles.Children.Add(Tile("battery","Energía",delegate{Settings("ms-settings:batterysaver");}));tiles.Children.Add(Tile("display","Proyectar",delegate{if(!desktop.IsTesting)Settings(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),"System32","DisplaySwitch.exe"));}));tiles.Children.Add(Tile("camera","Captura",delegate{Design.Dismiss(this,delegate{Settings("ms-screenclip:");});}));tiles.Children.Add(Tile("calendar","Widgets",delegate{Design.Dismiss(this,delegate{desktop.OpenWidgets(null);});}));tiles.Children.Add(Tile("windows","Stage Manager",delegate{Design.Dismiss(this,desktop.OpenStageManager);}));root.Children.Add(tiles);
            powerText=Note("");powerText.FontSize=10;powerText.Margin=new Thickness(6,6,0,0);root.Children.Add(powerText);var bottom=new DockPanel();bottom.Children.Add(Link("Ajustes de MacFlow",delegate{Design.Dismiss(this,desktop.OpenSettings);}));var theme=Link("Claro / oscuro",delegate{desktop.Preferences.Theme=Design.Dark?"Light":"Dark";Design.Dismiss(this,delegate{desktop.ApplyPreferences(desktop.Preferences,null);desktop.OpenControl();});});theme.HorizontalAlignment=HorizontalAlignment.Right;bottom.Children.Add(theme);root.Children.Add(bottom);
            status=Note("");status.MaxHeight=48;root.Children.Add(status);var shell=Design.Glass(new ScrollViewer{Content=root,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled},30,true);shell.Padding=new Thickness(14,16,14,12);Content=shell;
            VolumeSlider.ValueChanged+=delegate{if(updating||closed||!VolumeSlider.IsEnabled)return;pendingVolume=VolumeSlider.Value;soundText.Text="Salida predeterminada · "+(int)VolumeSlider.Value+"%";if(!volumeDelay.IsEnabled)volumeDelay.Start();};volumeDelay.Tick+=delegate{WriteVolume();};
            BrightnessSlider.ValueChanged+=delegate{if(updating||!BrightnessSlider.IsEnabled)return;pendingBrightness=(int)BrightnessSlider.Value;brightnessText.Text=pendingBrightness+"%";brightnessDelay.Stop();brightnessDelay.Start();};brightnessDelay.Tick+=delegate{brightnessDelay.Stop();WriteBrightness();};
            SourceInitialized+=delegate{Native.Glass(this,Design.Dark);};Loaded+=delegate{RefreshState();poll.Start();};poll.Tick+=delegate{if(IsVisible)RefreshState();};Closed+=delegate{WriteVolume();closed=true;poll.Stop();brightnessDelay.Stop();volumeDelay.Stop();stop.Cancel();media.Dispose();backend.Dispose();stop.Dispose();};KeyDown+=delegate(object s,KeyEventArgs e){if(e.Key==Key.Escape)Design.Dismiss(this);};
        }
        private static TextBlock Note(string text){var t=Design.Text(text,12,false);t.TextWrapping=TextWrapping.Wrap;t.SetResourceReference(TextBlock.ForegroundProperty,"GlassMuted");return t;}
        private static UIElement RadioRow(string name,CheckBox toggle,TextBlock status,Action open)
        {var row=new DockPanel{Margin=new Thickness(0,6,0,8)};toggle.Width=40;toggle.Margin=new Thickness(0,0,0,0);toggle.VerticalAlignment=VerticalAlignment.Center;System.Windows.Automation.AutomationProperties.SetName(toggle,name);DockPanel.SetDock(toggle,Dock.Right);row.Children.Add(toggle);var text=new StackPanel{Margin=new Thickness(0,0,6,0)};var link=Link(name+" ›",open);link.FontSize=12;link.Padding=new Thickness(0);link.Margin=new Thickness(0,0,0,5);text.Children.Add(link);status.FontSize=10;status.TextWrapping=TextWrapping.NoWrap;status.TextTrimming=TextTrimming.CharacterEllipsis;text.Children.Add(status);row.Children.Add(text);return row;}
        private static UIElement Heading(string icon,string text){var row=new DockPanel();row.Children.Add(new Border{Child=Design.Symbol(icon,17,Design.Ink),Margin=new Thickness(0,0,9,0)});row.Children.Add(Design.Text(text,13,true));return row;}
        private static GlassPiece Piece(UIElement child){return new GlassPiece(child,19,new Thickness(12,9,12,9)){Margin=new Thickness(4,3,4,5)};}
        private static Button Link(string label,Action action){var b=Design.Button(label,action,false);b.FontSize=11;b.Background=Brushes.Transparent;b.Padding=new Thickness(3,3,3,3);b.Margin=new Thickness(0,3,0,0);b.HorizontalContentAlignment=HorizontalAlignment.Left;return b;}
        private static Button Tile(string icon,string name,Action action){var b=Design.Button("",action,false);b.Background=Brushes.Transparent;b.Padding=new Thickness(0);b.Content=new GlassPiece(Heading(icon,name),17,new Thickness(10)){MinHeight=42};b.Margin=new Thickness(4);return b;}
        private void Settings(string uri){try{backend.Settings(uri);}catch(Exception ex){status.Text="Windows no pudo abrir esta opción: "+ex.Message;}}
        internal async void RefreshState()
        {
            if(busy||closed)return;busy=true;int generation=radioGeneration;
            try
            {
                var sound=backend.Sound();updating=true;VolumeSlider.IsEnabled=mute.IsEnabled=sound.Available;if(!pendingVolume.HasValue&&!VolumeSlider.IsMouseCaptureWithin&&!VolumeSlider.IsKeyboardFocusWithin)VolumeSlider.Value=sound.Volume;mute.IsChecked=sound.Muted;if(!pendingVolume.HasValue)soundText.Text=sound.Available?"Salida predeterminada · "+(int)sound.Volume+"%":"Sin dispositivo de audio";updating=false;
                var w=await backend.Radio(true);if(closed)return;var b=await backend.Radio(false);if(closed)return;var bright=await backend.ReadBrightness();if(closed)return;updating=true;
                if(!radioBusy&&generation==radioGeneration){wifi.IsChecked=w.On;bluetooth.IsChecked=b.On;wifi.IsEnabled=w.Available&&w.CanChange;bluetooth.IsEnabled=b.Available&&b.CanChange;wifiText.Text=!w.CanChange&&!String.IsNullOrEmpty(w.Message)?w.Message:w.Available?w.On?"Activado":"Desactivado":w.Message;bluetoothText.Text=!b.CanChange&&!String.IsNullOrEmpty(b.Message)?b.Message:b.Available?b.On?"Activado":"Desactivado":b.Message;}
                BrightnessSlider.IsEnabled=bright.HasValue;if(bright.HasValue&&!brightnessBusy&&!pendingBrightness.HasValue&&!BrightnessSlider.IsMouseCaptureWithin&&!BrightnessSlider.IsKeyboardFocusWithin)BrightnessSlider.Value=bright.Value;
                brightnessText.Text=bright.HasValue?(int)BrightnessSlider.Value+"%":"Este monitor controla el brillo desde sus botones o Windows.";var p=backend.Power();powerText.Text=(p.BatteryFlag==128||p.BatteryLifePercent==255?"Con corriente":"Batería · "+p.BatteryLifePercent+"%")+(p.SystemStatusFlag==1?" · Ahorro activo":"");
            }
            catch(Exception ex){if(!closed)status.Text="No se pudo consultar este estado.";Log.Write(ex.Message);}finally{updating=false;busy=false;if(!closed&&!radioBusy&&generation!=radioGeneration)RefreshState();}
        }
        private async void ChangeRadio(bool mode,bool on)
        {
            if(radioBusy||closed)return;radioBusy=true;radioGeneration++;wifi.IsEnabled=bluetooth.IsEnabled=false;
            try{var result=await backend.SwitchRadio(mode,on,stop.Token);if(!closed){status.Text=result.Success?"":result.Message;if(result.Success)desktop.ReportAction((mode?"Wi-Fi":"Bluetooth")+(on?" activado":" desactivado"),mode?"network":"bluetooth");}}catch(Exception ex){if(!closed)status.Text="El cambio no se completó.";Log.Write(ex.Message);}finally{radioBusy=false;if(!closed)RefreshState();}
        }
        private async void WriteBrightness()
        {
            if(brightnessBusy||closed)return;brightnessBusy=true;
            try{while(pendingBrightness.HasValue&&!closed){int value=pendingBrightness.Value;pendingBrightness=null;await backend.SetBrightness(value);if(!closed)desktop.ReportAction("Brillo · "+value+"%","sun");}}catch(Exception ex){pendingBrightness=null;if(!closed)status.Text="Windows no permitió cambiar el brillo.";Log.Write(ex.Message);}finally{brightnessBusy=false;}
        }
        private void WriteVolume()
        {volumeDelay.Stop();if(closed||!pendingVolume.HasValue)return;double value=pendingVolume.Value;pendingVolume=null;try{backend.Volume(value);desktop.ReportAction("Volumen · "+(int)value+"%","volume");}catch(Exception ex){status.Text=ex.Message;}}
    }
}

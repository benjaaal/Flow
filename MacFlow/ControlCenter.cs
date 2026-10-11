using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace MacFlow
{
    public sealed class ControlCenter : Window
    {
        private readonly Audio audio;
        private bool closed;
        public ControlCenter(Desktop desktop)
        {
            Title="MacFlow · Centro de control";Width=350;Height=Math.Min(608,SystemParameters.WorkArea.Height-70);WindowStyle=WindowStyle.None;AllowsTransparency=false;ResizeMode=ResizeMode.NoResize;Topmost=true;ShowInTaskbar=false;Background=Brushes.Transparent;
            var root=new StackPanel();var head=new DockPanel {Margin=new Thickness(0,0,0,16)};var lights=new WindowControls(this,delegate{Design.Dismiss(this);});DockPanel.SetDock(lights,System.Windows.Controls.Dock.Left);head.Children.Add(lights);var title=Design.Text("Centro de control",13,true);title.Margin=new Thickness(11,0,0,0);head.Children.Add(title);root.Children.Add(head);
            var upper=new Grid{Margin=new Thickness(-4,0,-4,8)};upper.ColumnDefinitions.Add(new ColumnDefinition());upper.ColumnDefinitions.Add(new ColumnDefinition());
            var connectivity=new StackPanel();connectivity.Children.Add(Tile("wifi","Wi-Fi","Redes y conexión",true,null,delegate{Design.Dismiss(this,desktop.OpenWifi);},true));connectivity.Children.Add(Tile("bluetooth","Bluetooth","Dispositivos",true,null,delegate{Design.Dismiss(this,desktop.OpenBluetooth);},true));upper.Children.Add(connectivity);
            var media=new MediaCard(desktop.IsTesting?(IMediaSource)new MediaFixture():new WindowsMediaSource());
            var mediaGlass=new GlassPiece(media,18,new Thickness(14,10,12,10)){Margin=new Thickness(4)};Grid.SetColumn(mediaGlass,1);upper.Children.Add(mediaGlass);root.Children.Add(upper);
            var grid=new UniformGrid {Columns=2,Margin=new Thickness(-4,0,-4,12)};grid.Children.Add(Tile("moon","Concentración","Sin distracciones",false,"ms-settings:quiethours"));grid.Children.Add(Tile("display","Pantalla","Escala y resolución",false,"ms-settings:display"));root.Children.Add(grid);
            audio=new Audio();var sound=new StackPanel();var soundHeader=new DockPanel();soundHeader.Children.Add(new Border {Child=Design.Symbol("volume",17,Design.Ink),Margin=new Thickness(0,0,9,0)});soundHeader.Children.Add(Design.Text("Sonido",13,true));sound.Children.Add(soundHeader);
            var slider=new Slider {Minimum=0,Maximum=100,Value=audio.Available?audio.Volume*100:0,IsEnabled=audio.Available,Margin=new Thickness(0,10,0,7)};
            var volume=Design.Text(audio.Available?"Salida predeterminada · "+(int)slider.Value+"%":"Sin dispositivo de salida",11,false);volume.SetResourceReference(TextBlock.ForegroundProperty,"GlassMuted");sound.Children.Add(slider);sound.Children.Add(volume);
            slider.ValueChanged+=delegate {try{audio.Volume=(float)(slider.Value/100);volume.Text="Salida predeterminada · "+(int)slider.Value+"%";}catch(Exception ex){Log.Write(ex.Message);}};
            var mute=Design.Check("Silenciar",audio.Muted,delegate(bool v){try{audio.Muted=v;}catch(Exception ex){Log.Write(ex.Message);}});mute.FontSize=11;mute.Margin=new Thickness(0,9,0,0);sound.Children.Add(mute);root.Children.Add(new GlassPiece(sound,18,new Thickness(14)){Margin=new Thickness(0,0,0,12)});
            Loaded+=async delegate
            {
                var brightness=await System.Threading.Tasks.Task.Run(delegate{return Brightness.Get();});if(closed||!brightness.HasValue)return;
                Height=Math.Min(Height+77,SystemParameters.WorkArea.Height-70);var card=new StackPanel();card.Children.Add(Design.Text("Brillo",13,true));var level=new Slider {Minimum=0,Maximum=100,Value=brightness.Value,Margin=new Thickness(0,8,0,0)};
                Action update=async delegate {int value=(int)level.Value;try{await System.Threading.Tasks.Task.Run(delegate {Brightness.Set(value);});}catch(Exception ex){Log.Write(ex.Message);}};level.PreviewMouseLeftButtonUp+=delegate {update();};level.PreviewKeyUp+=delegate {update();};card.Children.Add(level);root.Children.Insert(3,new GlassPiece(card,18,new Thickness(14)){Margin=new Thickness(0,0,0,12)});
            };
            var bottom=new DockPanel();Native.POWER power;Native.GetSystemPowerStatus(out power);var battery=Design.Text(power.BatteryFlag==128||power.BatteryLifePercent==255?"Alimentación por corriente":"Batería · "+power.BatteryLifePercent+"%",10,false);battery.SetResourceReference(TextBlock.ForegroundProperty,"GlassMuted");
            var theme=Design.Circle(Design.Dark?"sun":"moon",delegate {desktop.Preferences.Theme=Design.Dark?"Light":"Dark";Design.Dismiss(this,delegate {desktop.ApplyPreferences(desktop.Preferences,null);desktop.OpenControl();});},"Cambiar apariencia");DockPanel.SetDock(theme,System.Windows.Controls.Dock.Right);bottom.Children.Add(theme);bottom.Children.Add(battery);root.Children.Add(bottom);
            Content=Design.Glass(new ScrollViewer { Content=root,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled },26,false);SourceInitialized+=delegate {Native.Glass(this,Design.Dark);};Closed+=delegate {closed=true;media.Dispose();audio.Dispose();};KeyDown+=delegate(object sender,KeyEventArgs e){if(e.Key==Key.Escape)Design.Dismiss(this);};
        }
        private static Button Tile(string icon,string label,string detail,bool blue,string uri,Action action=null,bool compact=false)
        {
            var b=Design.Button("",delegate {try{if(action!=null)action();else Process.Start(new ProcessStartInfo(uri){UseShellExecute=true});}catch(Exception ex){MessageBox.Show(ex.Message,"MacFlow");}},false);b.Margin=new Thickness(4);b.Padding=new Thickness(14,11,14,11);b.Height=compact?70:86;b.Background=Brushes.Transparent;
            var stack=new StackPanel {HorizontalAlignment=HorizontalAlignment.Left};var symbol=new Border {Width=29,Height=29,CornerRadius=new CornerRadius(15),Background=blue?Design.Brush("#327BEE"):Design.Brush(Design.Dark?"#30FFFFFF":"#30A5B4CB"),Child=Design.Symbol(icon,17,blue?Brushes.White:Design.Ink),HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,0,0,5)};
            stack.Children.Add(Design.Text(label,12,true));var sub=Design.Text(detail,10,false);sub.SetResourceReference(TextBlock.ForegroundProperty,"GlassMuted");sub.Margin=new Thickness(0,3,0,0);stack.Children.Add(sub);UIElement content;
            if(compact){symbol.Margin=new Thickness(0,0,8,0);symbol.VerticalAlignment=VerticalAlignment.Center;var row=new DockPanel();DockPanel.SetDock(symbol,System.Windows.Controls.Dock.Left);row.Children.Add(symbol);stack.VerticalAlignment=VerticalAlignment.Center;row.Children.Add(stack);content=row;}
            else{stack.Children.Insert(0,symbol);content=stack;}
            b.Padding=new Thickness(0);b.Background=Brushes.Transparent;b.BorderThickness=new Thickness(0);b.HorizontalContentAlignment=HorizontalAlignment.Stretch;b.VerticalContentAlignment=VerticalAlignment.Stretch;b.Content=new GlassPiece(content,18,new Thickness(12,10,12,10));return b;
        }
    }
}

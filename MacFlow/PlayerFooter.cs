using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
namespace MacFlow
{
    internal sealed class PlayerFooter : DockPanel,IDisposable
    {
        private readonly IControlBackend backend;private readonly Slider volume;private readonly TextBlock status;
        private readonly DispatcherTimer write=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(70)};
        private bool updating,disposed;private double? pending;
        internal PlayerFooter(bool testing)
        {
            backend=testing?(IControlBackend)new ControlFixture():new ControlBackend();Margin=new Thickness(0,9,0,0);
            var output=Design.Button("",delegate{try{backend.Settings("ms-settings:sound");}catch{status.Text="No se pudo abrir la salida de sonido.";}},false);output.Width=32;output.Height=30;output.MinHeight=0;output.Padding=new Thickness(4);output.Background=Brushes.Transparent;output.Content=ActivityIsland.Transport("output",23);output.ToolTip="Elegir salida de audio en Windows";System.Windows.Automation.AutomationProperties.SetName(output,"Elegir salida de audio");DockPanel.SetDock(output,Dock.Right);Children.Add(output);
            var icon=new Border{Width=25,Child=Design.Symbol("volume",14,Design.Brush("#C9D3D8")),Margin=new Thickness(0,0,5,0)};DockPanel.SetDock(icon,Dock.Left);Children.Add(icon);
            volume=new Slider{Minimum=0,Maximum=100,Height=24,VerticalAlignment=VerticalAlignment.Center,SmallChange=1,LargeChange=5};ActivityIsland.StyleTimeline(volume);System.Windows.Automation.AutomationProperties.SetName(volume,"Volumen del sistema");Children.Add(volume);status=Design.Text("",9,false);ToolTip=status;
            volume.ValueChanged+=delegate{if(updating||disposed)return;pending=volume.Value;if(!write.IsEnabled)write.Start();};write.Tick+=delegate{Flush();};Loaded+=delegate{Refresh();};Unloaded+=delegate{write.Stop();pending=null;};
        }
        internal void Refresh(){if(disposed||!IsVisible||pending.HasValue||volume.IsMouseCaptureWithin||volume.IsKeyboardFocusWithin)return;try{var audio=backend.Sound();updating=true;volume.IsEnabled=audio.Available;if(audio.Available)volume.Value=audio.Volume;}catch{volume.IsEnabled=false;status.Text="Salida de audio no disponible.";}finally{updating=false;}}
        private void Flush(){write.Stop();if(disposed||!pending.HasValue)return;double value=pending.Value;pending=null;try{backend.Volume(value);}catch{status.Text="No se pudo cambiar el volumen.";}}
        public void Dispose(){if(disposed)return;write.Stop();pending=null;disposed=true;backend.Dispose();}
    }
}

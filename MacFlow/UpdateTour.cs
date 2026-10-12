using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
namespace MacFlow
{
    // Demonstrations drive the real controls. The illustrated pointer is our
    // own click-through window; it never moves or captures the user's cursor.
    internal sealed class UpdateTour : Window
    {
        private readonly Desktop desktop;private readonly TextBlock title,description,step;
        private readonly DispatcherTimer next=new DispatcherTimer{Interval=TimeSpan.FromSeconds(7)};
        private Window example,cursor;private int index;private bool rendering,closed;private readonly Stopwatch elapsed=new Stopwatch();
        internal int Step{get{return index;}}
        internal bool FrameRunning{get{return rendering;}}
        internal bool Completed{get;private set;}
        internal UpdateTour(Desktop desktop)
        {
            this.desktop=desktop;Title="MacFlow · Descubrí las novedades";Width=420;Height=250;Left=(SystemParameters.PrimaryScreenWidth-Width)/2;Top=SystemParameters.WorkArea.Top+90;
            WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;ShowInTaskbar=false;Topmost=true;Background=Brushes.Transparent;
            var root=new StackPanel();step=Design.Text("",11,true);root.Children.Add(step);title=Design.Text("",24,true);title.Margin=new Thickness(0,10,0,10);root.Children.Add(title);description=Design.Text("",13,false);description.TextWrapping=TextWrapping.Wrap;root.Children.Add(description);
            var buttons=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(0,16,0,0)};buttons.Children.Add(ModuleUI.Button("Omitir",Close,false));buttons.Children.Add(Design.Button("Siguiente",Advance,true));root.Children.Add(buttons);Content=Design.Glass(root,24,false);
            SourceInitialized+=delegate{Native.Glass(this,Design.Dark);};Loaded+=delegate{ShowStep();};next.Tick+=delegate{Advance();};Closed+=delegate{closed=true;next.Stop();ClearExample();desktop.TourDock(false);};KeyDown+=delegate(object s,KeyEventArgs e){if(e.Key==Key.Escape)Close();};
            if(desktop.IsTesting){IsHitTestVisible=false;ShowActivated=false;SourceInitialized+=delegate{Native.NoActivate(this);};}
        }
        internal void Advance(){if(closed)return;next.Stop();ClearExample();if(++index>=4){Completed=true;Close();return;}ShowStep();}
        private void ShowStep()
        {
            step.Text="MACFLOW "+Product.VersionText+"  ·  "+(index+1)+" / 4";desktop.TourDock(true);
            switch(index)
            {
                case 0:
                    title.Text="Todo empieza en el Dock.";description.Text="Acercá el cursor: los iconos crecen suavemente dentro del cristal. Tocá una aplicación para abrirla o recuperar su ventana.";
                    cursor=new Window{Width=28,Height=36,AllowsTransparency=true,Background=Brushes.Transparent,WindowStyle=WindowStyle.None,ResizeMode=ResizeMode.NoResize,Topmost=true,ShowInTaskbar=false,ShowActivated=false,IsHitTestVisible=false};
                    cursor.Content=new Path{Data=Geometry.Parse("M 3,2 L 3,26 L 9,20 L 14,31 L 19,29 L 14,18 L 23,18 Z"),Fill=Brushes.White,Stroke=Design.Brush("#233142"),StrokeThickness=1.4};cursor.SourceInitialized+=delegate{var h=new WindowInteropHelper(cursor).Handle;Native.SetWindowLong(h,-20,Native.GetWindowLong(h,-20)|0x20|0x80|0x08000000);};cursor.Show();
                    if(Motion.Enabled){elapsed.Restart();rendering=true;CompositionTarget.Rendering+=Frame;}else MovePointer(.5);break;
                case 1:
                    title.Text="Un escritorio que te acompaña.";description.Text="Calendario, notas, tareas, clima y más. Agregalos desde Widgets y arrastrá sus títulos para acomodarlos. Esta muestra no cambia tu configuración.";
                    example=new DesktopWidget(desktop.Widgets,new WidgetSpec{Kind="Calendar",Size="L"});example.Left=28;example.Top=SystemParameters.WorkArea.Top+60;example.Width=340;example.Height=340;example.IsHitTestVisible=false;example.ShowActivated=false;example.Show();Design.Reveal(example);break;
                case 2:
                    title.Text="Tus controles, juntos.";description.Text="Wi-Fi, Bluetooth, música, sonido y brillo. Abrí el Centro de control desde la barra superior. Los controles reflejan el estado real del equipo.";
                    example=new ControlCenter(desktop);example.Left=SystemParameters.PrimaryScreenWidth-example.Width-18;example.Top=SystemParameters.WorkArea.Top+48;example.ShowActivated=false;example.IsHitTestVisible=false;example.Show();Design.Reveal(example);break;
                case 3:
                    title.Text="Cada proyecto, en su lugar.";description.Text="Stage Manager muestra tus ventanas. Guardá grupos de trabajo y recuperá su distribución desde Tu escritorio → Stage Manager.";
                    var available=desktop.Windows.Where(w=>w.ProcessId!=(uint)Process.GetCurrentProcess().Id&&Native.Matches(w)).Take(4).ToList();var sidebar=new StageSidebar(desktop.Stage);example=sidebar;sidebar.Position(Math.Max(1,available.Count));sidebar.Build(available);sidebar.IsHitTestVisible=false;sidebar.ShowActivated=false;sidebar.Show();Design.Reveal(sidebar);break;
            }
            Design.Reveal(this);next.Start();
        }
        private void Frame(object sender,EventArgs e)
        {
            if(closed||cursor==null){StopFrames();return;}double progress=Math.Min(1,elapsed.Elapsed.TotalSeconds/4.8);double fraction=.12+.76*(.5-.5*Math.Cos(Math.PI*progress));MovePointer(fraction);desktop.ProbeDockMotion(fraction);if(progress>=1){StopFrames();desktop.StopDockProbe();if(cursor!=null)cursor.Close();cursor=null;}
        }
        private void MovePointer(double fraction)
        {if(cursor==null||desktop.DockItems==null||!desktop.Dock.IsVisible)return;var p=desktop.DockItems.PointToScreen(new Point(desktop.DockItems.ActualWidth*fraction,desktop.DockItems.ActualHeight*.55));var source=PresentationSource.FromVisual(cursor);if(source!=null)p=source.CompositionTarget.TransformFromDevice.Transform(p);cursor.Left=p.X;cursor.Top=p.Y;}
        private void StopFrames(){if(rendering){CompositionTarget.Rendering-=Frame;rendering=false;}elapsed.Stop();}
        private void ClearExample(){StopFrames();desktop.StopDockProbe();if(cursor!=null){cursor.Close();cursor=null;}if(example!=null){example.Close();example=null;}}
    }
}

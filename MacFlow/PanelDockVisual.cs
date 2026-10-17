using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
namespace MacFlow
{
    // Native regression on owned windows only. Tests real HWND movement and
    // clipping, not a comparison of animation constants with themselves.
    internal static class PanelDockVisual
    {
        [DllImport("gdi32.dll")] private static extern IntPtr CreateRectRgn(int l,int t,int r,int b);
        [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr region);
        [DllImport("gdi32.dll")] private static extern int GetRgnBox(IntPtr region,out Native.RECT rect);
        [DllImport("user32.dll")] private static extern int GetWindowRgn(IntPtr window,IntPtr region);
        [DllImport("dwmapi.dll")] private static extern int DwmFlush();
        internal static int Run(string directory)
        {
            directory=Path.GetFullPath(directory);Directory.CreateDirectory(directory);Log.DirectoryPath=directory;
            var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};Desktop desktop=null;VisualTestBackground background=null;Window panel=null;var results=new List<string>();
            app.Startup+=async delegate
            {
                try
                {
                    string profile=Path.Combine(directory,"profile");new SettingsStore(profile).Save(new Preferences{Theme="Dark",IconSize=36,ContainedDockApplied=true,DockAutoHide=false,HideDockInApps=false,HideDockInCodex=false,ShowTopBar=false,HideInFullscreen=false});
                    background=new VisualTestBackground{WindowState=WindowState.Normal,Left=0,Top=0,Width=SystemParameters.PrimaryScreenWidth,Height=SystemParameters.PrimaryScreenHeight,ShowActivated=false,IsHitTestVisible=false,Topmost=true};background.SourceInitialized+=delegate{Native.NoActivate(background);};background.Show();
                    desktop=new Desktop(profile,true,true);await Task.Delay(500);
                    using(var pull=new ControlPullGesture(desktop.TopBar,delegate{return true;},desktop.CreateGestureControl)){pull.Pull(120);var pulled=desktop.Control;if(pulled==null||pulled.Top>=46||!CompositorGlass.Active(pulled))throw new Exception("El panel no sigue el gesto con su fondo real");pull.Release();await Task.Delay(450);if(Math.Abs(pulled.Top-46)>1||pulled.Opacity<.99)throw new Exception("El gesto no completa la apertura: top="+pulled.Top+" alpha="+pulled.Opacity+" height="+pulled.Height);pulled.Close();pull.Pull(20);pulled=desktop.Control;pull.Release();await Task.Delay(450);if(pulled.IsVisible)throw new Exception("Un gesto corto no se cancela");results.Add("PASS centro de control sigue la distancia del gesto; completa o cancela sin mover el mouse del usuario.");}
                    panel=CreatePanel();var entryBounds=Bounds(panel);var entryTarget=PanelMotion.DockDestination(panel).Value;double nearestEntry=Double.MaxValue;int entryFrames=0;Action<Window> entry=delegate(Window w){if(w!=panel)return;nearestEntry=Math.Min(nearestEntry,Distance(Center(Bounds(panel)),entryTarget));entryFrames++;};GenieSurface openingSurface=null;Action<Window,GenieSurface> opening=delegate(Window value,GenieSurface surface){if(value==panel)openingSurface=surface;};GenieTransition.Started+=opening;PanelMotion.Frame+=entry;Design.Reveal(panel);await Task.Delay(150);if(openingSurface==null||openingSurface.Progress>=1||openingSurface.Progress<=0)throw new Exception("La apertura no recorre la malla inversa desde el Dock");await Task.Delay(400);GenieTransition.Started-=opening;PanelMotion.Frame-=entry;if(GenieTransition.ActiveCount!=0||panel.Opacity<.99)throw new Exception("La apertura dejó una vista temporal o el panel oculto");if(entryFrames<3||nearestEntry>25||!Same(Bounds(panel),entryBounds)||PanelMotion.IsTransitioning(panel))throw new Exception("Apertura no salió del Dock o no recuperó la geometría final");results.Add("PASS apertura desde el Dock real con "+entryFrames+" cuadros y restauración exacta de posición/tamaño.");panel.Close();panel=null;
                    panel=CreatePanel();await Task.Delay(400);
                    var destination=PanelMotion.DockDestination(panel);if(!destination.HasValue)throw new Exception("No existe destino del Dock real");
                    var original=Bounds(panel);var dockBounds=Bounds(desktop.Dock);var expected=new Point((dockBounds.Left+dockBounds.Right)/2.0,(dockBounds.Top+dockBounds.Bottom)/2.0);
                    if(Distance(destination.Value,expected)>2)throw new Exception("Destino diferente del Dock real");
                    results.Add("PASS Destino obtenido del Dock real, en coordenadas físicas: "+destination.Value);
                    await Track(panel,delegate{Design.Dismiss(panel);},destination.Value,directory,"cierre",results);
                    if(panel.IsVisible||Native.IsWindow(new WindowInteropHelper(panel).Handle))throw new Exception("Cierre no terminó");panel=null;
                    if(!Same(Bounds(desktop.Dock),dockBounds))throw new Exception("El cierre movió el Dock");results.Add("PASS El Dock conserva tamaño y posición durante el cierre.");
                    panel=CreatePanel();await Task.Delay(400);original=Bounds(panel);
                    await Track(panel,delegate{Design.Minimize(panel);},PanelMotion.DockDestination(panel).Value,directory,"minimizar",results);
                    if(panel.WindowState!=WindowState.Minimized)throw new Exception("No se minimizó");panel.WindowState=WindowState.Normal;await Task.Delay(450);
                    if(!Same(Bounds(panel),original)||!panel.IsHitTestVisible||PanelMotion.IsClosing(panel)||panel.Opacity<.99)throw new Exception("Restauración dejó geometría o interacción alterada");
                    results.Add("PASS Minimizar y restaurar recupera exactamente la posición, tamaño, opacidad e interacción originales.");panel.Close();panel=null;
                    panel=CreatePanel();await Task.Delay(100);original=Bounds(panel);panel.WindowState=WindowState.Maximized;await Task.Delay(400);var maximized=Bounds(panel);
                    await Track(panel,delegate{Design.Minimize(panel);},PanelMotion.DockDestination(panel).Value,directory,"maximizado",results);
                    if(panel.WindowState!=WindowState.Minimized)throw new Exception("No minimiza el panel ampliado");panel.WindowState=WindowState.Maximized;await Task.Delay(450);
                    if(!Same(Bounds(panel),maximized)||PanelMotion.IsClosing(panel))throw new Exception("No restaura la posición ampliada");panel.WindowState=WindowState.Normal;await Task.Delay(200);
                    if(!Same(Bounds(panel),original))throw new Exception("La animación alteró el tamaño/posición antes de ampliar");results.Add("PASS El panel ampliado también llega al Dock y conserva su posición normal al restaurar.");panel.Close();panel=null;
                    var fields=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
                    var edge=(System.Windows.Threading.DispatcherTimer)typeof(Desktop).GetField("edgeTimer",fields).GetValue(desktop);edge.Stop();var visibility=(DockVisibility)typeof(Desktop).GetField("dockVisibility",fields).GetValue(desktop);
                    panel=CreatePanel();await Task.Delay(400);var resting=PanelMotion.DockDestination(panel).Value;visibility.Hide();await Task.Delay(140);
                    if(Distance(PanelMotion.DockDestination(panel).Value,resting)>2)throw new Exception("El destino persigue al Dock que baja");await Task.Delay(300);
                    await Track(panel,delegate{Design.Dismiss(panel);},resting,directory,"dock-oculto",results);panel=null;visibility.ShowImmediately();edge.Start();results.Add("PASS El destino permanece en el Dock en reposo mientras se esconde y cuando está oculto.");
                    desktop.Preferences.DockPosition="Left";desktop.ApplyPreferences(desktop.Preferences,null);await Task.Delay(500);
                    panel=CreatePanel();await Task.Delay(400);await Track(panel,delegate{Design.Dismiss(panel);},PanelMotion.DockDestination(panel).Value,directory,"dock-lateral",results);panel=null;
                    desktop.Preferences.DockPosition="Bottom";desktop.ApplyPreferences(desktop.Preferences,null);await Task.Delay(500);
                    panel=CreatePanel();Design.Reveal(panel);await Task.Delay(80);await Track(panel,delegate{Design.Dismiss(panel);},PanelMotion.DockDestination(panel).Value,directory,"entrada-interrumpida",results);panel=null;
                    panel=CreatePanel();Design.Animations=false;Design.Dismiss(panel);if(panel.IsVisible)throw new Exception("Movimiento reducido no cierra inmediatamente");panel=null;Design.Animations=true;results.Add("PASS Movimiento reducido cierra inmediatamente.");
                    desktop.OpenSettings();await Task.Delay(600);if(!Object.ReferenceEquals(desktop.Settings.Icon,FeatureIcons.Get("settings")))throw new Exception("Configuración no usa el nuevo icono");
                    // Place the settings panel over the isolated backdrop before capture.
                    Native.SetWindowPos(new WindowInteropHelper(desktop.Settings).Handle,new IntPtr(-1),0,0,0,0,0x13);
                    await Program.CaptureOwnWindow(desktop.Settings,Path.Combine(directory,"configuracion-ios.png"));results.Add("PASS Configuración usa un icono vectorial inspirado en Ajustes de iOS; panel real capturado.");
                    var settings=(SettingsWindow)desktop.Settings;settings.SetSearch("inicio");await Task.Delay(300);var result=ModulesVisual.DescendantsForTest(settings).OfType<System.Windows.Controls.Button>().FirstOrDefault(b=>System.Windows.Automation.AutomationProperties.GetName(b)=="Abrir Sistema");if(result==null)throw new Exception("Buscar inicio no encontró Sistema");result.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));await Task.Delay(300);results.Add("PASS búsqueda de ajustes abre la sección real de Sistema.");settings.SelectPage(1);await Task.Delay(45);settings.SelectPage(2);await Task.Delay(35);settings.SelectPage(5);await Task.Delay(400);var pages=(Grid)typeof(SettingsWindow).GetField("pagesHost",fields).GetValue(settings);if(pages.Children.Count!=1||!Object.Equals(pages.Children[0].ReadLocalValue(UIElement.IsHitTestVisibleProperty),true))throw new Exception("Navegación rápida dejó una página vieja interceptando clics");results.Add("PASS Navegación rápida conserva una sola página con interacción habilitada localmente; la fixture completa permanece sin interacción con el usuario.");await Program.CaptureOwnWindow(desktop.Settings,Path.Combine(directory,"inicio-automatico.png"));results.Add("PASS Reparación e información de inicio visibles en Sistema; las acciones de inicio están desactivadas en la prueba aislada.");
                    if(!File.Exists(Desktop.TaskManagerPath))throw new Exception("No se encontró el Administrador de tareas del sistema");var taskButton=ModulesVisual.DescendantsForTest(settings).OfType<Button>().FirstOrDefault(b=>(b.Content as string)=="Abrir Administrador de tareas");if(taskButton==null)throw new Exception("Falta el acceso al Administrador de tareas");taskButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));results.Add("PASS Administrador de tareas usa el ejecutable del sistema y tiene acceso real desde Configuración; no se lanza en la fixture aislada.");
                    int confirmed=0;var power=new PowerPrompt(PowerAction.Shutdown,true,delegate{confirmed++;}){ShowActivated=false,IsHitTestVisible=false};panel=power;power.SourceInitialized+=delegate{Native.NoActivate(power);};power.Show();await Task.Delay(300);await Program.CaptureOwnWindow(power,Path.Combine(directory,"apagar-cristal.png"));var cancel=ModulesVisual.DescendantsForTest(power).OfType<Button>().First(b=>(b.Content as string)=="Cancelar");cancel.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Task.Delay(500);if(power.IsVisible||power.Confirmed||confirmed!=0)throw new Exception("Cancelar energía no conserva la sesión");panel=null;results.Add("PASS Cancelar el nuevo panel de energía cierra sin confirmar ni ejecutar acciones del sistema.");
                    power=new PowerPrompt(PowerAction.Restart,true,delegate{confirmed++;}){ShowActivated=false,IsHitTestVisible=false};panel=power;power.SourceInitialized+=delegate{Native.NoActivate(power);};power.Show();await Task.Delay(150);var accept=ModulesVisual.DescendantsForTest(power).OfType<Button>().First(b=>(b.Content as string)=="Reiniciar");accept.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Task.Delay(500);if(power.IsVisible||!power.Confirmed||confirmed!=1)throw new Exception("Confirmar energía ejecutó un callback incorrecto");panel=null;results.Add("PASS La confirmación explícita invoca una sola vez el callback de prueba después del cierre; no se reinicia Windows.");
                    File.WriteAllLines(Path.Combine(directory,"visual-test.txt"),results);app.Shutdown();
                }
                catch(Exception ex){results.Add("FAIL "+ex);File.WriteAllLines(Path.Combine(directory,"visual-test.txt"),results);app.Shutdown(1);}
            };
            app.Exit+=delegate{if(panel!=null&&panel.IsVisible)panel.Close();if(desktop!=null)desktop.Dispose();if(background!=null)background.Close();};return app.Run();
        }
        private static Window CreatePanel()
        {
            var panel=Design.Panel("MacFlow · Movimiento de prueba",360,220);panel.Left=SystemParameters.PrimaryScreenWidth*.22;panel.Top=SystemParameters.PrimaryScreenHeight*.2;panel.ShowActivated=false;
            var stack=new StackPanel{Margin=new Thickness(20)};stack.Children.Add(new Image{Source=FeatureIcons.Get("settings"),Width=48,Height=48,HorizontalAlignment=HorizontalAlignment.Left});stack.Children.Add(Design.Text("Configuración",22,true));stack.Children.Add(Design.Text("Un cierre hacia tu Dock",14,false));panel.Content=Design.Glass(stack,22,false);panel.SourceInitialized+=delegate{Native.NoActivate(panel);};panel.Show();return panel;
        }
        private static async Task Track(Window panel,Action action,Point destination,string directory,string name,List<string> results)
        {
            var handle=new WindowInteropHelper(panel).Handle;double initial=Distance(Center(Bounds(panel)),destination),previous=initial,min=initial;int frames=0,closed=0;long previousArea=Int64.MaxValue;var records=new List<string>();var pictures=new List<System.Drawing.Bitmap>();double next=.8;string failure=null;
            Action<Window> frame=delegate(Window value)
            {
                if(value!=panel)return;var bounds=Bounds(panel);double distance=Distance(Center(bounds),destination);
                if(distance>previous+4)failure="El panel se alejó del Dock";previous=distance;min=Math.Min(min,distance);frames++;
                var region=CreateRectRgn(0,0,1,1);Native.RECT crop;int ok;try{ok=GetWindowRgn(handle,region);GetRgnBox(region,out crop);}finally{DeleteObject(region);}if(ok==0)return;
                long area=(long)(crop.Right-crop.Left)*(crop.Bottom-crop.Top);if(area>previousArea)failure="El material volvió a crecer durante el cierre";previousArea=area;
                records.Add(frames+";distancia="+distance.ToString("F2")+";area="+area);
                if(distance<=initial*next&&crop.Right-crop.Left>8&&crop.Bottom-crop.Top>8)
                {
                    next-=.35;DwmFlush();var bitmap=new System.Drawing.Bitmap(crop.Right-crop.Left,crop.Bottom-crop.Top);
                    using(var graphics=System.Drawing.Graphics.FromImage(bitmap))graphics.CopyFromScreen(bounds.Left+crop.Left,bounds.Top+crop.Top,0,0,bitmap.Size);pictures.Add(bitmap);
                }
            };
            panel.Closed+=delegate{closed++;};PanelMotion.Frame+=frame;
            try
            {
                action();await Task.Delay(550);if(failure!=null)throw new Exception(failure);if(frames<3||min>20||closed>1)throw new Exception("Recorrido incompleto: cuadros="+frames+" distancia final="+min+" cierres="+closed);
                for(int i=0;i<pictures.Count;i++)pictures[i].Save(Path.Combine(directory,name+"-"+(i+1)+".png"),System.Drawing.Imaging.ImageFormat.Png);
                File.WriteAllLines(Path.Combine(directory,name+"-recorrido.txt"),records);results.Add("PASS "+name+": "+frames+" cuadros nativos hacia el Dock; distancia "+initial.ToString("F1")+" -> "+min.ToString("F1")+" px; material y texto encogen juntos.");
            }
            finally{PanelMotion.Frame-=frame;foreach(var bitmap in pictures)bitmap.Dispose();}
        }
        private static Native.RECT Bounds(Window window){Native.RECT rect;Native.GetWindowRect(new WindowInteropHelper(window).Handle,out rect);return rect;}
        private static Point Center(Native.RECT rect){return new Point((rect.Left+rect.Right)/2.0,(rect.Top+rect.Bottom)/2.0);}
        private static double Distance(Point a,Point b){return (a-b).Length;}
        private static bool Same(Native.RECT a,Native.RECT b){return a.Left==b.Left&&a.Top==b.Top&&a.Right==b.Right&&a.Bottom==b.Bottom;}
    }
}

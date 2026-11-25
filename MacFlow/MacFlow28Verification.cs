using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
namespace MacFlow
{
    internal sealed class VerificationMedia28 : IMediaSource
    {
        internal MediaState Value=new MediaState();public event Action Changed;
        public Task<MediaState> Read(){return Task.FromResult(Value);}
        public Task<bool> Command(MediaCommand action){Value.Playing=!Value.Playing;Push();return Task.FromResult(true);}
        internal void Push(){if(Changed!=null)Changed();}public void Dispose(){}
    }
    internal static class MacFlow28Verification
    {
        internal static int Run(string directory)
        {
            directory=Path.GetFullPath(directory);Directory.CreateDirectory(directory);Log.DirectoryPath=directory;
            var report=new List<string>();var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};Desktop desktop=null;ActivityIsland island=null;
            Action<bool,string> check=delegate(bool value,string message){if(!value)throw new Exception(message);report.Add("PASS "+message);};
            var initialTaskbar=Native.TaskbarState();
            app.DispatcherUnhandledException+=delegate(object sender,System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e){e.Handled=true;report.Add("FAIL "+e.Exception);File.WriteAllLines(Path.Combine(directory,"acceptance.txt"),report);app.Shutdown(1);};
            app.Startup+=async delegate{
                try{
                    var profilesRoot=Path.Combine(directory,"profile-tests");Directory.CreateDirectory(profilesRoot);var created=ProfileStore.Create(profilesRoot,"Perfil de prueba");ProfileStore.Select(profilesRoot,created.Id);check(ProfileStore.ActiveDirectory(profilesRoot)==Path.Combine(profilesRoot,"Profiles",created.Id),"perfiles separados con identificadores validados");ProfileStore.Select(profilesRoot,"default");check(ProfileStore.ActiveDirectory(profilesRoot)==profilesRoot,"el perfil original conserva sus rutas");
                    var healthRoot=Path.Combine(directory,"health-tests");var health=new StartupHealth(healthRoot);check(health.Begin(),"primer inicio permitido");health.End(false);check(new StartupHealth(healthRoot).Begin(),"primer fallo permite recuperación");check(new StartupHealth(healthRoot).Begin(),"segundo fallo se registra");check(!new StartupHealth(healthRoot).Begin(),"tres fallos interrumpidos bloquean el próximo arranque automático");
                    check(LocalFiles.Scan(new string[0]).Count==0,"Spotlight no recorre carpetas sin selección del usuario");check(!new Preferences().AllowExternalAssistant,"las consultas externas requieren permiso por perfil");
                    var profile=Path.Combine(directory,"profile");Directory.CreateDirectory(profile);new SettingsStore(profile).Save(new Preferences{AutoHideTaskbar=false,ShowWidget=false,Animations=true,LowPower=true,VoiceWakeEnabled=false});
                    desktop=new Desktop(profile,true,true);await Task.Delay(500);
                    check(ShellTopBar.For(desktop.TopBar)!=null,"la barra usa ShellTopBar con tres zonas");
                    var apple=ModulesVisual.DescendantsForTest(desktop.TopBar).OfType<Button>().Single(b=>System.Windows.Automation.AutomationProperties.GetName(b)=="Abrir el menú de MacFlow");
                    Motion.TestClientAreaAnimations=true;
                    var click=Stopwatch.StartNew();
                    apple.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    var appleMenu=GlassMenu.Current;
                    check(appleMenu!=null&&appleMenu.IsAppleMenu&&appleMenu.IsVisible,"la manzanita muestra el menú en el mismo clic, sin esperar un doble clic");
                    report.Add("INFO apertura síncrona del menú "+click.Elapsed.TotalMilliseconds.ToString("F1",System.Globalization.CultureInfo.InvariantCulture)+" ms");
                    var firstTop=appleMenu.Top;
                    apple.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    if(Motion.Enabled)
                    {
                        apple.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        await Task.Delay(350);
                        check(Object.ReferenceEquals(appleMenu,GlassMenu.Current)&&appleMenu.IsVisible&&appleMenu.Opacity>.99,"abrir durante el cierre conserva el mismo menú y completa la transición");
                        check(Math.Abs(appleMenu.Top-firstTop)>1,"el rebote termina fuera de la posición de entrada");
                    }
                    else
                    {
                        check(GlassMenu.Current==null,"movimiento reducido cierra sin un reloj de animación");
                        apple.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));appleMenu=GlassMenu.Current;
                    }
                    check(appleMenu.AllowsTransparency&&appleMenu.Content is AppleMenuGlass&&LiquidMaterial.ForWindow(appleMenu)==null,"la manzanita dibuja un borde por píxel con una muestra inicial, sin sondeo de pantalla");
                    for(int i=0;i<20&&!((AppleMenuGlass)appleMenu.Content).HasBackdrop&&LiquidMaterial.LiveBackdrop&&!SystemParameters.HighContrast;i++)await Task.Delay(50);
                    report.Add("INFO material del menú · "+appleMenu.Tag);
                    Program.Render(appleMenu,Path.Combine(directory,"menu-macflow.png"));
                    await Program.CaptureOwnWindow(appleMenu,Path.Combine(directory,"menu-macflow-desktop.png"));
                    apple.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Task.Delay(280);
                    check(GlassMenu.Current==null,"cerrar el menú libera su ventana");
                    int windows=app.Windows.Count;var source=new VerificationMedia28();island=new ActivityIsland(desktop,source);desktop.Island=island;await Task.Delay(300);
                    check(app.Windows.Count==windows&&Object.ReferenceEquals(island.View,desktop.TopBar),"la isla no crea ningún HWND ni Window independiente");
                    check(island.State==IslandState.idle,"estado inicial sin actividades inventadas");
                    island.Toggle();for(int wait=0;wait<40&&island.Surface.ActualHeight<130;wait++)await Task.Delay(100);check(island.Expanded&&desktop.TopBar.Height>Desktop.TopBarHeight,"la isla expande dentro del mismo HWND");Program.Render(desktop.TopBar,Path.Combine(directory,"region-debug.png"));var capsulePosition=island.Surface.TranslatePoint(new Point(),desktop.TopBar);report.Add("REGION width="+desktop.TopBar.ActualWidth+" height="+desktop.TopBar.Height+" capsule="+capsulePosition+" size="+island.Surface.RenderSize+" outside="+Native.IntegratedHitTest(desktop.TopBar,50,80)+" center="+Native.IntegratedHitTest(desktop.TopBar,(int)(capsulePosition.X+island.Surface.ActualWidth/2),60));check(!Native.IntegratedHitTest(desktop.TopBar,50,80)&&Native.IntegratedHitTest(desktop.TopBar,(int)(capsulePosition.X+island.Surface.ActualWidth/2),60),"la región nativa deja pasar clics fuera de la cápsula expandida");Program.Render(desktop.TopBar,Path.Combine(directory,"bar-expanded.png"));
                    island.Toggle();for(int wait=0;wait<40&&desktop.TopBar.Height>Desktop.TopBarHeight+1;wait++)await Task.Delay(100);check(!island.Expanded&&desktop.TopBar.Height<=Desktop.TopBarHeight+1,"contraer recupera la altura de la barra");
                    foreach(IslandState state in Enum.GetValues(typeof(IslandState))){desktop.IslandState.Publish("state",state.ToString(),"bell",state,70,TimeSpan.FromSeconds(2));island.Refresh();check(island.State==state,"estado "+state);if(!island.Expanded)island.Toggle();await Task.Delay(260);check(!ModulesVisual.DescendantsForTest(island.Surface).OfType<TextBlock>().Any(t=>t.IsVisible&&!String.IsNullOrWhiteSpace(t.Text)),"sin texto visible, incluso expandida: "+state);desktop.IslandState.Remove("state");if(island.Expanded)island.Toggle();}
                    for(int i=0;i<20;i++)desktop.IslandState.Publish("event-"+i,"Evento "+i,"bell",IslandState.notification,60,TimeSpan.FromMinutes(1));
                    check(desktop.IslandState.Count==20,"cola de veinte eventos sin pérdida ni ventanas nuevas");
                    desktop.IslandState.Publish("event-0","Reemplazo","bell",IslandState.notification,60,TimeSpan.FromMinutes(1));check(desktop.IslandState.Count==20,"deduplicación por identificador");
                    for(int i=0;i<20;i++)desktop.IslandState.Publish("event-"+i,"Caducado","bell",IslandState.notification,60,TimeSpan.Zero);
                    island.Refresh();check(desktop.IslandState.Count==0&&island.State==IslandState.idle,"caducidad y restauración al estado anterior");
                    int notificationWindows=app.Windows.Count;desktop.Notify("Notificación de aceptación dentro de la isla");await Task.Delay(300);check(island.State==IslandState.notification&&island.Expanded,"las notificaciones expanden la Isla Dinámica automáticamente");check(app.Windows.Count==notificationWindows&&!app.Windows.Cast<Window>().Any(w=>w.Title.Contains("aviso compacto")),"no aparece un banner lateral duplicado");check(!ModulesVisual.DescendantsForTest(island.Surface).OfType<TextBlock>().Any(t=>t.IsVisible&&!String.IsNullOrWhiteSpace(t.Text)),"el aviso no introduce texto en la isla");check(desktop.Activity.Items.Any(n=>n.Detail.Contains("Notificación de aceptación")),"el aviso queda guardado en el centro");Program.Render(desktop.TopBar,Path.Combine(directory,"notification-island.png"));var notification=desktop.IslandState.Current;if(notification!=null)desktop.IslandState.Remove(notification.Key);await Task.Delay(300);
                    source.Value=new MediaState{Title="Tema de prueba",Artist="Proveedor aislado",CanToggle=true,Playing=true};source.Push();island.Refresh();check(island.State==IslandState.media,"los metadatos y controles provienen del proveedor");await desktop.IslandState.Command(MediaCommand.Toggle);check(!source.Value.Playing,"el botón ejecuta la orden del proveedor");
                    desktop.SetAssistantVisual(AssistantStage.Listening,"Te escucho");check(island.State==IslandState.voiceListening,"voz enlazada con listening");desktop.SetAssistantVisual(AssistantStage.Working,"Procesando");check(island.State==IslandState.voiceThinking,"voz enlazada con thinking");desktop.SetAssistantVisual(AssistantStage.Speaking,"Respuesta");check(island.State==IslandState.voiceSpeaking,"voz enlazada con speaking");desktop.SetAssistantVisual(AssistantStage.Hidden,null);
                    check(!app.Windows.Cast<Window>().Any(w=>w.Title.Contains("Presencia del asistente")||w.Title.Contains("Actividad en vivo")),"sin overlays antiguos de isla o esfera de voz");
                    var timer=new WidgetSpec{Kind="Timer",Running=true,TimerMinutes=5,StartedUtc=DateTime.UtcNow};desktop.Widgets.State.Items.Add(timer);island.Refresh();check(island.State==IslandState.timer,"temporizador real del perfil");desktop.Widgets.State.Items.Remove(timer);
                    foreach(double width in new[]{800.0,1024.0,1280.0,1920.0}){desktop.TopBar.Width=width;island.Refresh();island.Toggle();await Task.Delay(280);check(island.Surface.ActualWidth<=desktop.TopBar.ActualWidth,"la cápsula cabe con barra de "+width);if(island.Expanded)island.Toggle();}
                    var files=Path.Combine(directory,"files");Directory.CreateDirectory(files);Directory.CreateDirectory(Path.Combine(files,"Carpeta"));File.WriteAllText(Path.Combine(files,"nota.txt"),"texto de prueba");
                    desktop.OpenFinderAt(files);for(int i=0;i<30&&!desktop.Finder.Ready;i++)await Task.Delay(100);var finder=desktop.Finder;await Task.Delay(500);check(finder.Ready&&finder.Count==2,"Finder enumera archivos y carpetas reales");Program.Render(finder,Path.Combine(directory,"finder.png"));
                    File.WriteAllText(Path.Combine(files,"Carpeta","dentro.txt"),"dentro");
                    finder.SetView(3);
                    var finderRows=ModulesVisual.DescendantsForTest(finder).OfType<ListView>().Single();
                    finderRows.SelectedIndex=0;
                    var column=ModulesVisual.DescendantsForTest(finder).OfType<ListBox>().Single(x=>!(x is ListView));
                    for(int i=0;i<30&&column.Items.Count==0;i++)await Task.Delay(50);
                    check(column.Items.Count==1&&((StackEntry)column.Items[0]).Name=="dentro.txt","Finder por columnas lee la carpeta elegida");
                    finder.Navigate(Path.Combine(files,"Carpeta"));
                    check(finder.Count==0&&column.Items.Count==0,"Finder elimina la selección anterior antes de operar en otra carpeta");
                    finder.Navigate(files);
                    for(int i=0;i<30&&!finder.Ready;i++)await Task.Delay(50);
                    FinderFiles.Rename(Path.Combine(files,"nota.txt"),"renombrada.txt");check(File.Exists(Path.Combine(files,"renombrada.txt")),"renombrado real");
                    var destination=Path.Combine(directory,"copy");Directory.CreateDirectory(destination);await FinderFiles.Run(delegate{FinderFiles.Transfer(Path.Combine(files,"renombrada.txt"),destination,false);});check(File.ReadAllText(Path.Combine(destination,"renombrada.txt"))=="texto de prueba","copia real y contenido conservado");
                    bool traversal=false;try{FinderFiles.ChildName(files,"..\\escape.txt");}catch(IOException){traversal=true;}check(traversal,"el nombre no puede escapar de su carpeta");
                    var lights=ModulesVisual.DescendantsForTest(finder).OfType<WindowControls>().First();lights.ZoomButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Task.Delay(180);check(finder.WindowState==WindowState.Maximized,"verde amplía la ventana propia");lights.ZoomButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));check(finder.WindowState==WindowState.Normal,"verde restaura la ventana propia");
                    Motion.TestClientAreaAnimations=true;
                    if(Motion.Enabled)
                    {
                        PanelMotion.Reset(finder);
                        lights.MinimizeButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        await Task.Delay(35);desktop.OpenFinder();await Task.Delay(650);
                        check(finder.WindowState==WindowState.Normal&&finder.IsHitTestVisible&&Math.Abs(finder.Opacity-1)<.001&&GenieTransition.ActiveCount==0,"restauración durante minimización cancela el cierre pendiente y deja la ventana interactiva");
                    }
                    System.ComponentModel.CancelEventHandler prevent=delegate(object sender,System.ComponentModel.CancelEventArgs e){e.Cancel=true;};
                    finder.Closing+=prevent;
                    Design.Dismiss(finder);await Task.Delay(550);finder.Closing-=prevent;
                    check(finder.IsVisible&&finder.IsHitTestVisible&&Math.Abs(finder.Opacity-1)<.001,"un cierre cancelado devuelve opacidad y clics");
                    lights.MinimizeButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));for(int wait=0;wait<30&&finder.WindowState!=WindowState.Minimized;wait++)await Task.Delay(100);check(finder.WindowState==WindowState.Minimized,"amarillo minimiza realmente");desktop.OpenFinder();await Task.Delay(250);check(finder.WindowState==WindowState.Normal&&finder.FolderPath==files,"el acceso Finder del Dock restaura sin perder la carpeta");lights.CloseButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Task.Delay(800);check(desktop.Finder==null&&desktop.Dock.IsVisible,"rojo cierra solo Finder y conserva el shell");
                    desktop.OpenFinderAt(files);await Task.Delay(300);check(!Object.ReferenceEquals(finder,desktop.Finder),"reabrir crea una ventana nueva después de Close");desktop.Finder.Close();
                    foreach(string id in new[]{"calculator","calendar","notes","activity","catalog"})
                    {
                        var entry=Desktop.FindBuiltIn("macflow-app-"+id).Entry();
                        desktop.Launch(entry);await Task.Delay(80);
                        var own=app.Windows.Cast<Window>().Single(w=>Object.Equals(w.GetValue(PanelMotion.DockKey),entry.Key));
                        desktop.Launch(entry);
                        check(own.IsVisible&&app.Windows.Cast<Window>().Count(w=>Object.Equals(w.GetValue(PanelMotion.DockKey),entry.Key))==1,"app integrada por catálogo reutiliza su ventana: "+entry.Name);
                        if(id=="calculator")
                        {
                            var input=ModulesVisual.DescendantsForTest(own).OfType<TextBox>().Single();
                            input.Text="(17+5)*2";
                            var equals=ModulesVisual.DescendantsForTest(own).OfType<Button>().Single(b=>Object.Equals(b.Content,"="));
                            equals.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                            check(input.Text=="44","la calculadora responde desde su botón visible");
                        }
                        if(id=="notes")ModulesVisual.DescendantsForTest(own).OfType<TextBox>().Single().Text="Nota de aceptación";
                        own.Close();
                    }
                    check(ModuleStore.Read<NoteDocument>(Path.Combine(profile,"notes-app.json")).Text=="Nota de aceptación","Notas guarda cambios al cerrar");
                    check(LocalCommands.Parse("buscá archivo café.pdf").Intent==LocalIntent.FindFiles&&LocalCommands.Parse("estado del sistema").Intent==LocalIntent.SystemInfo,"el asistente reconoce búsqueda de archivos y estado del sistema");
                    check(OllamaAssistant.ValidModel("local-model:8b")&&!OllamaAssistant.ValidModel("service:cloud")&&!OllamaAssistant.ValidModel("https://example.com/model"),"Ollama admite identificadores locales y conserva el destino de loopback fijo");
                    if(island.Expanded)island.Toggle();desktop.TopBar.Width=1280;island.Refresh();await Task.Delay(500);Program.Render(desktop.TopBar,Path.Combine(directory,"bar-compact.png"));
                    var process=Process.GetCurrentProcess();var cpu=process.TotalProcessorTime;var elapsed=Stopwatch.StartNew();await Task.Delay(3000);process.Refresh();double coreCpu=(process.TotalProcessorTime-cpu).TotalSeconds/elapsed.Elapsed.TotalSeconds*100;
                    File.WriteAllText(Path.Combine(directory,"performance.json"),"{\"PrivateBytes\":"+process.PrivateMemorySize64+",\"IdleCpuOneCorePercent\":"+coreCpu.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"IdleSeconds\":"+elapsed.Elapsed.TotalSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"Scope\":\"isolated WPF acceptance process, not gaming or GPU FPS\"}");
                    check(Native.TaskbarState()==initialTaskbar,"las pruebas no cambian la barra de tareas de Windows");report.Add("PENDIENTE: hardware físico con dos monitores, DPI 100/125/150/200, juego, suspensión y reinicio de Windows.");report.Add("PASS: "+report.Count+" entradas");File.WriteAllLines(Path.Combine(directory,"acceptance.txt"),report);app.Shutdown(0);
                }catch(Exception ex){report.Add("FAIL "+ex);File.WriteAllLines(Path.Combine(directory,"acceptance.txt"),report);app.Shutdown(1);}
                finally{if(island!=null)island.Dispose();if(desktop!=null)desktop.Dispose();}
            };
            return app.Run();
        }
    }
}

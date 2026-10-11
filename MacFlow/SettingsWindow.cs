using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Path = System.IO.Path;

namespace MacFlow
{
    public sealed class SettingsWindow : Window
    {
        private readonly Desktop desktop;
        private readonly Preferences draft;
        private readonly StackPanel page;
        private readonly TextBlock title, subtitle, status;
        private readonly List<Button> navigation = new List<Button>();
        private bool startupValue, startupAvailable;
        private int selected;
        private string wallpaperSelection;
        private Image wallpaperPreview;
        private TextBlock wallpaperName;
        private Button wallpaperApply;
        private int wallpaperRequest;
        private int pageGeneration;
        private bool windowClosed;
        public SettingsWindow(Desktop controller, bool testing)
        {
            desktop=controller; var p=desktop.Preferences;
            draft=new Preferences { Theme=p.Theme, Accent=p.Accent, IconSize=p.IconSize, ContainedDockApplied=p.ContainedDockApplied, Animations=p.Animations, AutoHideTaskbar=p.AutoHideTaskbar, ShowTopBar=p.ShowTopBar, HideInFullscreen=p.HideInFullscreen, GlassOpacity=p.GlassOpacity, LiveGlass=p.LiveGlass, DockAutoHide=p.DockAutoHide, HideDockInApps=p.HideDockInApps, DockPosition=p.DockPosition, ShowNetwork=p.ShowNetwork, ShowBattery=p.ShowBattery, ShowVolume=p.ShowVolume, LowPower=p.LowPower,WindowsMenu=p.WindowsMenu,ProtectDockEdge=p.ProtectDockEdge,HideDockInCodex=p.HideDockInCodex,AppleWindowControls=p.AppleWindowControls, Hotkey=p.Hotkey, ShowWidget=p.ShowWidget, UpdateRepository=p.UpdateRepository };
            startupAvailable=!testing && Startup.TryRead(out startupValue);
            draft.LocalUpdateFolder=p.LocalUpdateFolder;draft.UpdateFeedUrl=p.UpdateFeedUrl;draft.LidAnimation=p.LidAnimation;draft.WelcomeAfterUpdate=p.WelcomeAfterUpdate;draft.LastWelcomedVersion=p.LastWelcomedVersion;draft.CheckUpdatesAutomatically=p.CheckUpdatesAutomatically;draft.WallpaperPath=p.WallpaperPath;wallpaperSelection=p.WallpaperPath;
            Title="MacFlow · Configuración";Width=800;Height=610;MinWidth=750;MinHeight=520;WindowStyle=WindowStyle.None;AllowsTransparency=false;ResizeMode=ResizeMode.CanResizeWithGrip;ShowInTaskbar=true;Background=Brushes.Transparent;FontFamily=new FontFamily("Segoe UI Variable Text, Segoe UI");
            var layout=new Grid();layout.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(184)});layout.ColumnDefinitions.Add(new ColumnDefinition());
            var side=new DockPanel{Margin=new Thickness(12,12,12,15)};
            var top=new StackPanel();
            var lights=new WindowControls(this,delegate{Design.Dismiss(this);}){Margin=new Thickness(0,0,0,22)};top.Children.Add(lights);
            var brand=Design.Text("MacFlow",18,true);brand.Margin=new Thickness(7,0,0,3);top.Children.Add(brand);
            var caption=Design.Text("Configuración",11,false);caption.SetResourceReference(TextBlock.ForegroundProperty,"GlassMuted");caption.Margin=new Thickness(7,3,0,24);top.Children.Add(caption);
            string[] names={"Apariencia","Fondos","Dock","Barra superior","Rendimiento","Sistema"}; string[] symbols={"sun","display","grid","control","display","settings"};
            for(int i=0;i<names.Length;i++)
            {
                int index=i;var b=Design.Button("",delegate{ShowPage(index);},false);b.HorizontalContentAlignment=HorizontalAlignment.Left;b.Padding=new Thickness(7,6,7,6);b.Margin=new Thickness(0,2,0,2);b.BorderThickness=new Thickness(0);
                System.Windows.Automation.AutomationProperties.SetName(b,names[i]);
                string[] colors={"#9277D7","#29A0D7","#007AFF","#6373CF","#35B58A","#85858E"};
                var row=new StackPanel{Orientation=Orientation.Horizontal};row.Children.Add(new Border{Child=Design.Symbol(symbols[i],15,Brushes.White),Width=23,Height=23,CornerRadius=new CornerRadius(6),Background=Design.Brush(colors[i]),Margin=new Thickness(0,0,8,0)});row.Children.Add(Design.Text(names[i],12,false));b.Content=row;navigation.Add(b);top.Children.Add(b);
            }
            DockPanel.SetDock(top,System.Windows.Controls.Dock.Top); side.Children.Add(top);
            var version=Design.Text("MacFlow "+Product.VersionText,10,false);version.SetResourceReference(TextBlock.ForegroundProperty,"GlassMuted");version.VerticalAlignment=VerticalAlignment.Bottom;version.Margin=new Thickness(7,0,0,2);side.Children.Add(version);
            layout.Children.Add(new Border{Background=Design.SidebarSurface,Child=side});
            var main=new DockPanel{Margin=new Thickness(25,22,25,18)};var contentSurface=new Border{Background=Design.ContentSurface,Child=main};Grid.SetColumn(contentSurface,1);layout.Children.Add(contentSurface);
            main.Resources["GlassCardSurface"]=Design.CardSurface;
            var header=new StackPanel{Margin=new Thickness(0,0,0,20)};title=Design.Text("",21,true);subtitle=Design.Text("",12,false);subtitle.SetResourceReference(TextBlock.ForegroundProperty,"GlassMuted");subtitle.Margin=new Thickness(0,6,0,0);header.Children.Add(title);header.Children.Add(subtitle);DockPanel.SetDock(header,System.Windows.Controls.Dock.Top);main.Children.Add(header);
            var foot=new DockPanel{Margin=new Thickness(0,12,0,0)};status=Design.Text("",10,false);status.SetResourceReference(TextBlock.ForegroundProperty,"GlassMuted");status.TextWrapping=TextWrapping.Wrap;var apply=Design.Button("Aplicar",Apply,true);System.Windows.Automation.AutomationProperties.SetName(apply,"Aplicar cambios");apply.MinWidth=85;apply.Margin=new Thickness(8,0,0,0);DockPanel.SetDock(apply,System.Windows.Controls.Dock.Right);foot.Children.Add(apply);foot.Children.Add(status);DockPanel.SetDock(foot,System.Windows.Controls.Dock.Bottom);main.Children.Add(foot);
            page=new StackPanel(); main.Children.Add(new ScrollViewer { Content=page,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled });
            var shell=Design.Glass(layout,16,false);shell.Padding=new Thickness(0);Content=shell;SourceInitialized+=delegate{Native.Glass(this,Design.Dark);};header.MouseLeftButtonDown+=delegate{DragMove();};top.MouseLeftButtonDown+=delegate(object sender,MouseButtonEventArgs e){if(e.OriginalSource is TextBlock&&!e.Handled){var node=(DependencyObject)e.OriginalSource;while(node!=null){if(node is System.Windows.Controls.Primitives.ButtonBase)return;node=VisualTreeHelper.GetParent(node);}DragMove();}};
            KeyDown+=delegate(object sender,KeyEventArgs e) { if(e.Key==Key.Escape) {e.Handled=true;Design.Dismiss(this);} };Closed+=delegate {windowClosed=true;wallpaperRequest++;};ShowPage(0);
        }
        private static Button Light(string color,Action action,string tooltip)
        {
            var b=Design.Button("",action,false); b.Width=13;b.Height=13;b.Padding=new Thickness(0);b.Margin=new Thickness(0,0,8,0);b.Background=Design.Brush(color);b.BorderThickness=new Thickness(0);b.ToolTip=tooltip;return b;
        }
        private void ShowPage(int index)
        {
            selected=index;page.Children.Clear();for(int i=0;i<navigation.Count;i++){navigation[i].Background=i==index?Design.Brush("#007AFF"):Brushes.Transparent;navigation[i].Resources["GlassInk"]=i==index?Brushes.White:Design.Ink;}
            wallpaperRequest++;pageGeneration++;
            if(index==0) Appearance();else if(index==1) WallpaperPage();else if(index==2) Dock();else if(index==3) Bar();else if(index==4) Performance();else SystemPage();
            if(Design.Animations) page.BeginAnimation(UIElement.OpacityProperty,new System.Windows.Media.Animation.DoubleAnimation(0,1,TimeSpan.FromMilliseconds(170)));
        }
        private void Heading(string text,string description) { title.Text=text;subtitle.Text=description; }
        private StackPanel Section(string label)
        {
            var labelText=Design.Text(label,12,true);labelText.Margin=new Thickness(2,0,0,9);page.Children.Add(labelText);var contents=new StackPanel();page.Children.Add(Design.Card(contents));return contents;
        }
        private static Grid Row(string name,string detail,UIElement control)
        {
            var row=new Grid { Margin=new Thickness(0,5,0,5) };row.ColumnDefinitions.Add(new ColumnDefinition());row.ColumnDefinitions.Add(new ColumnDefinition {Width=GridLength.Auto});var text=new StackPanel {Margin=new Thickness(0,0,16,0),VerticalAlignment=VerticalAlignment.Center};text.Children.Add(Design.Text(name,13,true));if(!String.IsNullOrEmpty(detail)){var hint=Design.Text(detail,11,false);hint.SetResourceReference(TextBlock.ForegroundProperty,"GlassMuted");hint.TextWrapping=TextWrapping.Wrap;hint.Margin=new Thickness(0,4,0,0);text.Children.Add(hint);}row.Children.Add(text);Grid.SetColumn(control,1);row.Children.Add(control);return row;
        }
        private void Toggle(StackPanel section,string name,string detail,bool value,Action<bool> change)
        {
            var check=Design.Check("",value,change);System.Windows.Automation.AutomationProperties.SetName(check,name);check.Width=45;check.Margin=new Thickness(0);section.Children.Add(Row(name,detail,check));
        }
        private void Appearance()
        {
            Heading("Apariencia","Elegí cómo se ve tu escritorio.");
            var themes=new StackPanel {Orientation=Orientation.Horizontal,Margin=new Thickness(0,0,0,18)};
            themes.Children.Add(Design.Button("Claro",delegate {draft.Theme="Light";ShowPage(0);},draft.Theme=="Light"));
            themes.Children.Add(Design.Button("Oscuro",delegate {draft.Theme="Dark";ShowPage(0);},draft.Theme=="Dark"));page.Children.Add(themes);
            var material=Section("Cristal");material.Children.Add(Row("Intensidad del cristal","Transparencia del Dock y los menús.",Design.Symbol("sun",20,Design.Muted)));
            var transparency=new Slider {Minimum=.06,Maximum=.65,Value=draft.GlassOpacity,Margin=new Thickness(0,12,0,3)};transparency.ValueChanged+=delegate {draft.GlassOpacity=transparency.Value;};material.Children.Add(transparency);material.Children.Add(Row("Más transparente","",Design.Text("Más contraste",10,false)));
            var colors=Section("Color del acento");var swatches=new StackPanel {Orientation=Orientation.Horizontal};string[] palette={"#8DAEFF","#B8A1FF","#7CDEC1","#FF9BC2","#F5CC85"};string[] labels={"Azul","Lavanda","Menta","Rosa","Ámbar"};
            for(int i=0;i<palette.Length;i++){string color=palette[i];var b=Design.Button("",delegate{draft.Accent=color;ShowPage(0);},false);var circle=new Grid{Width=26,Height=26};circle.Children.Add(new System.Windows.Shapes.Ellipse{Fill=Design.Brush(color),Stroke=Design.Brush("#20000000"),StrokeThickness=.5});if(draft.Accent==color){var mark=Design.Text("✓",14,true);mark.Foreground=Brushes.White;mark.HorizontalAlignment=HorizontalAlignment.Center;circle.Children.Add(mark);}b.Content=circle;b.Width=33;b.Height=33;b.Padding=new Thickness(0);b.Background=Brushes.Transparent;b.BorderThickness=new Thickness(0);b.ToolTip=labels[i];System.Windows.Automation.AutomationProperties.SetName(b,labels[i]);b.Margin=new Thickness(0,0,9,0);swatches.Children.Add(b);}colors.Children.Add(swatches);
            var extras=Section("Escritorio");Toggle(extras,"Widget de fecha y hora","Una pequeña tarjeta en tu escritorio.",draft.ShowWidget,delegate(bool v){draft.ShowWidget=v;});
            extras.Children.Add(Design.Button("Elegir fondo de pantalla…",delegate {ShowPage(1);},false));
        }
        private async void WallpaperPage()
        {
            Heading("Fondos","Elegí un fondo que haga brillar el cristal.");
            var preview=Section("Vista previa");wallpaperPreview=new Image {Height=145,Width=258,Stretch=Stretch.Uniform};
            var previewRow=new DockPanel();var actions=new StackPanel {Margin=new Thickness(16,0,0,0),VerticalAlignment=VerticalAlignment.Center};DockPanel.SetDock(actions,System.Windows.Controls.Dock.Right);previewRow.Children.Add(actions);
            actions.Children.Add(Design.Button("Elegir imagen…",BrowseWallpaper,false));
            wallpaperApply=Design.Button("Usar este fondo",delegate {try{if(desktop.SetWallpaper(wallpaperSelection))status.Text="Fondo aplicado. Podés restaurar el anterior cuando quieras.";else status.Text="Vista previa comprobada; este modo no cambia Windows.";}catch(Exception ex){status.Text="No se pudo aplicar el fondo: "+ex.Message;}},true);wallpaperApply.IsEnabled=false;actions.Children.Add(wallpaperApply);
            actions.Children.Add(Design.Button("Restaurar anterior",delegate {try{desktop.RestoreWallpaper();wallpaperSelection="";status.Text="Fondo anterior restaurado.";}catch(Exception ex){status.Text="No se pudo restaurar: "+ex.Message;}},false));
            var frame=new Border {CornerRadius=new CornerRadius(14),Child=wallpaperPreview,HorizontalAlignment=HorizontalAlignment.Left,Clip=new RectangleGeometry(new Rect(0,0,258,145),14,14)};previewRow.Children.Add(frame);preview.Children.Add(previewRow);
            wallpaperName=Design.Text("Elegí una imagen de la galería o de tu computadora.",11,false);wallpaperName.Margin=new Thickness(0,10,0,0);preview.Children.Add(wallpaperName);
            var choices=Section("Fondos MacFlow");var tiles=new System.Windows.Controls.Primitives.UniformGrid {Columns=4};choices.Children.Add(tiles);
            var note=Design.Text("Elegir una imagen aplica el fondo y lo recuerda. Al pausar o salir vuelve el anterior; al iniciar MacFlow recupera tu selección.",10,false);note.TextWrapping=TextWrapping.Wrap;page.Children.Add(note);
            int request=pageGeneration,selectionRequest=wallpaperRequest;
            try
            {
                var backgrounds=await System.Threading.Tasks.Task.Run(delegate {return WallpaperLibrary.Load(desktop.Store.DirectoryPath);});
                if(windowClosed||selected!=1||request!=pageGeneration)return;
                foreach(var background in backgrounds)
                {
                    var choice=background;var content=new StackPanel();content.Children.Add(new Image {Source=choice.Thumbnail,Height=58,Stretch=Stretch.UniformToFill});var label=Design.Text(choice.Name,11,true);label.Margin=new Thickness(0,7,0,0);label.TextAlignment=TextAlignment.Center;content.Children.Add(label);
                    var button=Design.Button("",delegate {SelectWallpaper(choice.File,true);},false);button.Content=content;button.Padding=new Thickness(6);button.Margin=new Thickness(0,0,6,0);System.Windows.Automation.AutomationProperties.SetName(button,choice.Name);tiles.Children.Add(button);
                }
                if(selectionRequest==wallpaperRequest)SelectWallpaper(String.IsNullOrEmpty(wallpaperSelection)?backgrounds[0].File:wallpaperSelection);
            }
            catch(Exception ex){if(!windowClosed&&selected==1)wallpaperName.Text="No se pudo cargar la galería: "+ex.Message;}
        }
        private void BrowseWallpaper()
        {
            var dialog=new Microsoft.Win32.OpenFileDialog {Title="Elegir fondo de pantalla",Filter="Imágenes|*.jpg;*.jpeg;*.png;*.bmp",CheckFileExists=true};if(dialog.ShowDialog(this)==true)SelectWallpaper(dialog.FileName,true);
        }
        private async void SelectWallpaper(string file,bool apply=false)
        {
            int request=++wallpaperRequest;wallpaperApply.IsEnabled=false;
            try
            {
                var image=await System.Threading.Tasks.Task.Run(delegate {return WallpaperLibrary.Decode(file,1000);});
                if(windowClosed||selected!=1||request!=wallpaperRequest)return;
                wallpaperSelection=file;wallpaperPreview.Source=image;wallpaperName.Text=Path.GetFileName(file);wallpaperApply.IsEnabled=true;
                if(apply){status.Text=desktop.SetWallpaper(file)?"Fondo aplicado y guardado.":"Vista previa · este modo de prueba no cambia Windows.";}
            }
            catch(Exception ex){if(!windowClosed&&selected==1&&request==wallpaperRequest)wallpaperName.Text="No se pudo abrir esa imagen: "+ex.Message;}
        }
        public bool WallpaperPreviewReady {get{return selected==1&&wallpaperPreview!=null&&wallpaperPreview.Source!=null;}}
        private void Dock()
        {
            Heading("Dock","Tus aplicaciones favoritas, siempre cerca.");var settings=Section("Tamaño y comportamiento");var sizeLabel=Design.Text(draft.IconSize+" px",12,false);settings.Children.Add(Row("Tamaño de los iconos","",sizeLabel));var size=new Slider {Minimum=32,Maximum=72,Value=draft.IconSize,TickFrequency=4,IsSnapToTickEnabled=true,Margin=new Thickness(0,8,0,12)};size.ValueChanged+=delegate {draft.IconSize=(int)size.Value;sizeLabel.Text=draft.IconSize+" px";};settings.Children.Add(size);
            Toggle(settings,"Ocultar automáticamente","Aparece al acercar el cursor al borde.",draft.DockAutoHide,delegate(bool v){draft.DockAutoHide=v;});Toggle(settings,"Animación y ampliación","Movimiento suave al pasar el cursor.",draft.Animations,delegate(bool v){draft.Animations=v;});
            var pos=new ComboBox {ItemsSource=new[]{"Abajo","Izquierda","Derecha"},SelectedIndex=draft.DockPosition=="Left" ? 1 : draft.DockPosition=="Right" ? 2 : 0,Width=150};pos.SelectionChanged+=delegate {draft.DockPosition=pos.SelectedIndex==1 ? "Left" : pos.SelectedIndex==2 ? "Right" : "Bottom";};settings.Children.Add(Row("Posición","Pantalla principal",pos));
            var contextual=Section("Mientras trabajás");Toggle(contextual,"Apartar en todas las aplicaciones","Visible en el escritorio; se recupera en el borde de la pantalla.",draft.HideDockInApps,delegate(bool v){draft.HideDockInApps=v;});Toggle(contextual,"Apartar el Dock en Codex","Se retira suavemente y vuelve al acercar el mouse al borde.",draft.HideDockInCodex,delegate(bool v){draft.HideDockInCodex=v;});
            var pinned=Section("Aplicaciones fijadas");var list=new ListBox {ItemsSource=desktop.Preferences.Pins.Select(p=>p.Name).ToList(),Height=140};pinned.Children.Add(list);var buttons=new StackPanel {Orientation=Orientation.Horizontal,Margin=new Thickness(0,8,0,0)};
            Action<int> move=delegate(int direction){int i=list.SelectedIndex,target=i+direction;if(i<0||target<0||target>=desktop.Preferences.Pins.Count)return;var item=desktop.Preferences.Pins[i];desktop.Preferences.Pins.RemoveAt(i);desktop.Preferences.Pins.Insert(target,item);desktop.Save();desktop.DrawDock();list.ItemsSource=desktop.Preferences.Pins.Select(p=>p.Name).ToList();list.SelectedIndex=target;};buttons.Children.Add(Design.Button("←",delegate{move(-1);},false));buttons.Children.Add(Design.Button("→",delegate{move(1);},false));buttons.Children.Add(Design.Button("Quitar",delegate{if(list.SelectedIndex>=0){desktop.Pin(desktop.Preferences.Pins[list.SelectedIndex]);list.ItemsSource=desktop.Preferences.Pins.Select(p=>p.Name).ToList();}},false));
            buttons.Children.Add(Design.Button("Agregar…",delegate{var dialog=new Microsoft.Win32.OpenFileDialog {Filter="Programas y accesos directos|*.exe;*.lnk"};if(dialog.ShowDialog(this)==true){desktop.Pin(new AppEntry{Name=Path.GetFileNameWithoutExtension(dialog.FileName),Target=dialog.FileName,Executable=dialog.FileName.EndsWith(".exe",StringComparison.OrdinalIgnoreCase)?dialog.FileName:null});list.ItemsSource=desktop.Preferences.Pins.Select(p=>p.Name).ToList();}},false));pinned.Children.Add(buttons);
        }
        private void Bar()
        {
            Heading("Barra superior","Una barra de cristal con reloj y controles del sistema.");var section=Section("Elementos visibles");Toggle(section,"Barra superior","Sin contorno exterior, esquinas suaves y altura de 40 puntos.",draft.ShowTopBar,delegate(bool v){draft.ShowTopBar=v;});Toggle(section,"Wi-Fi y red","Estado de tu conexión",draft.ShowNetwork,delegate(bool v){draft.ShowNetwork=v;});Toggle(section,"Batería","Porcentaje o alimentación por corriente",draft.ShowBattery,delegate(bool v){draft.ShowBattery=v;});Toggle(section,"Volumen","Nivel del dispositivo de salida",draft.ShowVolume,delegate(bool v){draft.ShowVolume=v;});
            var integration=Section("Integración con Windows");Toggle(integration,"Ocultar la barra de tareas original","Se oculta mientras MacFlow está activo; vuelve al salir.",draft.AutoHideTaskbar,delegate(bool v){draft.AutoHideTaskbar=v;});Toggle(integration,"Borde del Dock protegido","Acercá el cursor al borde para recuperar el Dock.",draft.ProtectDockEdge,delegate(bool v){draft.ProtectDockEdge=v;});var note=Design.Text("La barra superior incluye controles rojo, amarillo y verde para la ventana activa. Los botones originales de cada programa se conservan.",12,false);note.SetResourceReference(TextBlock.ForegroundProperty,"GlassMuted");note.TextWrapping=TextWrapping.Wrap;integration.Children.Add(note);
        }
        private void Performance()
        {
            Heading("Rendimiento","Fluido cuando lo usás. Tranquilo cuando no.");var glass=Section("Cristal");Toggle(glass,"Cristal con desenfoque","Fondo real y reflejos suaves; aparece en capturas del escritorio.",draft.LiveGlass,delegate(bool v){draft.LiveGlass=v;});var section=Section("Movimiento y recursos");Toggle(section,"Animaciones suaves","Transiciones y ampliación del Dock",draft.Animations,delegate(bool v){draft.Animations=v;});Toggle(section,"Ahorro de recursos","Actualiza ventanas cada 2 segundos.",draft.LowPower,delegate(bool v){draft.LowPower=v;});Toggle(section,"Ocultar en pantalla completa","Para juegos, películas y presentaciones.",draft.HideInFullscreen,delegate(bool v){draft.HideInFullscreen=v;});var process=Process.GetCurrentProcess();var resources=Section("Esta sesión");resources.Children.Add(Row("Memoria privada", "Incluye WPF y la integración de Windows.",Design.Text((process.PrivateMemorySize64/1048576)+" MB",15,true)));resources.Children.Add(Row("Aplicaciones detectadas","Sin conexión a un servicio externo.",Design.Text(desktop.AppCount.ToString(),15,true)));
        }
        private void SystemPage()
        {
            Heading("Sistema","MacFlow se adapta a tu Windows.");var section=Section("Inicio y accesos");var start=Design.Check("",startupValue,delegate(bool v){startupValue=v;});start.Width=45;start.IsEnabled=startupAvailable;section.Children.Add(Row("Iniciar con Windows",startupAvailable?"Solo para tu usuario":"No disponible en este modo o entorno",start));
            Toggle(section,"Saludo después de actualizar","Hola aparece una vez por versión nueva.",draft.WelcomeAfterUpdate,delegate(bool v){draft.WelcomeAfterUpdate=v;});section.Children.Add(Design.Button("Ver bienvenida · Hola",desktop.OpenWelcome,false));
            Toggle(section,"Menú con la tecla Windows","Una pulsación abre MacFlow. Win+E y otros atajos siguen en Windows.",draft.WindowsMenu,delegate(bool v){draft.WindowsMenu=v;});section.Children.Add(Design.Button("Tus ventanas · minimizar, ampliar y cerrar",desktop.OpenOverview,false));section.Children.Add(Design.Button("Abrir menú de energía",desktop.OpenPowerMenu,false));
            var lid=Section("Tapa de la notebook");Toggle(lid,"Transición al abrir y cerrar la tapa","Pliegue con desenfoque y movimiento suave.",draft.LidAnimation,delegate(bool v){draft.LidAnimation=v;});lid.Children.Add(Design.Button("Probar pliegue del escritorio",desktop.PreviewLidAnimation,false));var lidNote=Design.Text(desktop.LidStatus+"\nSin sensor, el efecto responde al aviso de apertura/cierre. La suspensión puede interrumpirlo.",11,false);lidNote.TextWrapping=TextWrapping.Wrap;lidNote.Margin=new Thickness(0,8,0,0);lid.Children.Add(lidNote);
            var hotkey=new ComboBox {ItemsSource=new[]{"Alt + Espacio","Ctrl + Alt + Espacio","Ctrl + Espacio"},SelectedIndex=draft.Hotkey=="CtrlAltSpace"?1:draft.Hotkey=="CtrlSpace"?2:0,Width=185};hotkey.SelectionChanged+=delegate {draft.Hotkey=hotkey.SelectedIndex==1?"CtrlAltSpace":hotkey.SelectedIndex==2?"CtrlSpace":"AltSpace";};section.Children.Add(Row("Atajo del buscador","Si está ocupado, podés usar el Dock.",hotkey));
            var update=Section("Actualizaciones");Toggle(update,"Buscar automáticamente","Consulta el canal al iniciar y cada 12 horas, sin instalar por tu cuenta.",draft.CheckUpdatesAutomatically,delegate(bool v){draft.CheckUpdatesAutomatically=v;});update.Children.Add(Design.Button("Abrir actualizaciones",delegate{desktop.OpenUpdates(draft.UpdateRepository,draft.UpdateFeedUrl);},false));var repo=new TextBox{Text=draft.UpdateRepository??"",Padding=new Thickness(10),Background=Design.Brush("#12FFFFFF"),BorderThickness=new Thickness(0),ToolTip="Canal opcional de GitHub: propietario/repositorio"};repo.SetResourceReference(Control.ForegroundProperty,"GlassInk");repo.SetResourceReference(TextBox.CaretBrushProperty,"GlassInk");repo.TextChanged+=delegate{draft.UpdateRepository=repo.Text.Trim();};update.Children.Add(Design.Text("Canal de GitHub (opcional)",11,false));repo.Margin=new Thickness(0,8,0,8);update.Children.Add(repo);
            if(!String.IsNullOrWhiteSpace(draft.LocalUpdateFolder))update.Children.Add(Design.Text("Canal de esta computadora conectado: recibirás un aviso cuando haya otra versión preparada.",11,false));
            var feed=new TextBox {Text=draft.UpdateFeedUrl??"",Padding=new Thickness(10),Background=Design.Brush("#12FFFFFF"),BorderThickness=new Thickness(0),ToolTip="Enlace HTTPS al manifiesto publicado de MacFlow"};feed.SetResourceReference(Control.ForegroundProperty,"GlassInk");feed.SetResourceReference(TextBox.CaretBrushProperty,"GlassInk");feed.TextChanged+=delegate{draft.UpdateFeedUrl=feed.Text.Trim();};update.Children.Add(Design.Text("Enlace de actualizaciones (HTTPS)",11,false));update.Children.Add(feed);
            var recovery=Section("Restauración");recovery.Children.Add(Design.Button("Restaurar Windows y cerrar MacFlow",delegate {Application.Current.Shutdown();},false));recovery.Children.Add(Design.Button("Desinstalar MacFlow…",delegate {var exe=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Uninstall.exe");if(File.Exists(exe))Process.Start(new ProcessStartInfo(exe){UseShellExecute=true});else MessageBox.Show("Esta es la versión portable. Cerrá MacFlow y eliminá su carpeta para quitarla.","MacFlow");},false));
        }
        private void Apply()
        {
            try {if(!String.IsNullOrWhiteSpace(draft.UpdateFeedUrl)&&!UpdateService.ValidFeed(draft.UpdateFeedUrl))throw new IOException("El enlace de actualizaciones debe usar HTTPS.");draft.Pins=desktop.Preferences.Pins;draft.LastWelcomedVersion=desktop.Preferences.LastWelcomedVersion;draft.WallpaperPath=desktop.Preferences.WallpaperPath;desktop.ApplyPreferences(draft,startupAvailable?(bool?)startupValue:null);Close();desktop.OpenSettings();}catch(Exception ex){status.Text=ex.Message;}
        }
        public void SelectPage(int pageIndex) {ShowPage(pageIndex);}
    }
}

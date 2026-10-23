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
        private Preferences draft;
        private StackPanel page;private readonly Grid pagesHost;private readonly ScrollViewer pageScroll;
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
        private readonly TextBox settingsQuery;
        private bool changingQuery;
        private static readonly string[] PageNames={"Apariencia","Fondos","Dock","Barra superior","Rendimiento","Inicio y recuperación","Isla dinámica","Centro de control","Widgets","Notificaciones","Accesibilidad","Sonido","Privacidad","Actualizaciones","Atajos y asistente","Liquid Glass","Preferencias"};
        private static readonly string[] PageKeywords={"tema claro oscuro automatico color iconos","imagen wallpaper galeria fotografia escritorio","iconos favoritos tamaño ampliacion ocultar","barra wifi bateria volumen reloj windows","ahorro memoria recursos rendimiento","inicio arranque restaurar diagnostico","notch musica timer temporizador shelf archivos isla","centro control brillo sonido wifi bluetooth","widgets clima calendario reloj notas red","avisos notificaciones historial","movimiento reducido transparencia contraste","sonido multimedia volumen sonidos","pantalla bloqueo reloj privacidad","update version actualizacion canal","teclado atajos asistente siri voz microfono","vidrio blur reflejos liquid glass","importar exportar restaurar preferencias copia"};
        public SettingsWindow(Desktop controller, bool testing)
        {
            desktop=controller; var p=desktop.Preferences;
            draft=PreferencesIO.Clone(p);startupAvailable=!testing&&Startup.TryRead(out startupValue);wallpaperSelection=p.WallpaperPath;
            Title="MacOS · Configuración";Width=1016;Height=734;MinWidth=760;MinHeight=540;WindowStyle=WindowStyle.None;AllowsTransparency=false;ResizeMode=ResizeMode.CanResizeWithGrip;ShowInTaskbar=true;Background=Brushes.Transparent;FontFamily=new FontFamily("Segoe UI Variable Text, Segoe UI");
            var layout=new Grid();layout.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(238)});layout.ColumnDefinitions.Add(new ColumnDefinition());
            var side=new DockPanel{Margin=new Thickness(14,16,14,18)};
            var top=new StackPanel();
            var lights=new WindowControls(this,delegate{Design.Dismiss(this);}){Margin=new Thickness(0,0,0,18)};top.Children.Add(lights);
            Icon=FeatureIcons.Get("settings");
            var identity=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(7,0,0,22)};
            identity.Children.Add(new Image{Source=Icon,Width=36,Height=36,Margin=new Thickness(0,0,9,0),VerticalAlignment=VerticalAlignment.Center});
            var labels=new StackPanel{VerticalAlignment=VerticalAlignment.Center};var brand=Design.Text("MacOS",18,true);labels.Children.Add(brand);
            var caption=Design.Text("Configuración · "+Product.DisplayVersion,11,false);caption.SetResourceReference(TextBlock.ForegroundProperty,"GlassMuted");caption.Margin=new Thickness(0,3,0,0);labels.Children.Add(caption);identity.Children.Add(labels);top.Children.Add(identity);
            var searchLabel=Design.Text("Buscar ajustes",11,false);searchLabel.Margin=new Thickness(7,0,0,7);searchLabel.SetResourceReference(TextBlock.ForegroundProperty,"GlassMuted");top.Children.Add(searchLabel);
            settingsQuery=new TextBox{Padding=new Thickness(9,4,9,4),FontSize=13,BorderThickness=new Thickness(0),Background=Brushes.Transparent,Margin=new Thickness(0)};settingsQuery.SetResourceReference(Control.ForegroundProperty,"GlassInk");settingsQuery.SetResourceReference(TextBox.CaretBrushProperty,"GlassInk");System.Windows.Automation.AutomationProperties.SetName(settingsQuery,"Buscar ajustes");top.Children.Add(new Border{CornerRadius=new CornerRadius(10),Background=Design.Brush(Design.Dark?"#30000000":"#12000000"),BorderBrush=Design.Brush(Design.Dark?"#12FFFFFF":"#0C000000"),BorderThickness=new Thickness(.5),Margin=new Thickness(4,0,4,14),Child=settingsQuery});
            string[] names=PageNames; string[] symbols={"appearance","wallpaper","dock","topbar","performance","settings","music","control","calendar","bell","eye","volume","lock","download","keyboard","sun","folder"};
            for(int i=0;i<names.Length;i++)
            {
                int index=i;var b=Design.Button("",delegate{ShowPage(index);},false);b.HorizontalContentAlignment=HorizontalAlignment.Left;b.Padding=new Thickness(10,4,10,4);b.Margin=new Thickness(0,1,0,1);b.BorderThickness=new Thickness(0);
                System.Windows.Automation.AutomationProperties.SetName(b,names[i]);
                string[] iconColors={"#9E64D4","#38A1D8","#647288","#687FD1","#51A982","#85868D"};var row=new StackPanel{Orientation=Orientation.Horizontal};row.Children.Add(new Border{Child=Design.Symbol(symbols[i],16,Brushes.White),Width=24,Height=24,CornerRadius=new CornerRadius(7),Background=Design.Brush(iconColors[i%iconColors.Length]),Margin=new Thickness(0,0,9,0)});row.Children.Add(Design.Text(names[i],12,false));b.Content=row;b.ToolTip=names[i];navigation.Add(b);top.Children.Add(b);
            }
            DockPanel.SetDock(top,System.Windows.Controls.Dock.Top); 
            var version=Design.Text("MacOS "+Product.DisplayVersion,10,false);version.SetResourceReference(TextBlock.ForegroundProperty,"GlassMuted");version.VerticalAlignment=VerticalAlignment.Bottom;version.Margin=new Thickness(7,9,0,2);DockPanel.SetDock(version,System.Windows.Controls.Dock.Bottom);side.Children.Add(version);side.Children.Add(new ScrollViewer{Content=top,ClipToBounds=true,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled});
            layout.Children.Add(new Border{Background=Design.SidebarSurface,Child=side});
            var main=new DockPanel{Margin=new Thickness(30,26,30,22)};var contentSurface=new Border{Background=Design.ContentSurface,Child=main};Grid.SetColumn(contentSurface,1);layout.Children.Add(contentSurface);
            main.Resources["GlassCardSurface"]=Design.CardSurface;
            var header=new StackPanel{Margin=new Thickness(0,0,0,22)};title=Design.Text("",25,true);subtitle=Design.Text("",13,false);subtitle.SetResourceReference(TextBlock.ForegroundProperty,"GlassMuted");subtitle.TextWrapping=TextWrapping.Wrap;subtitle.Margin=new Thickness(0,7,0,0);header.Children.Add(title);header.Children.Add(subtitle);DockPanel.SetDock(header,System.Windows.Controls.Dock.Top);main.Children.Add(header);
            var foot=new DockPanel{Margin=new Thickness(0,12,0,0)};status=Design.Text("",10,false);status.SetResourceReference(TextBlock.ForegroundProperty,"GlassMuted");status.TextWrapping=TextWrapping.Wrap;var apply=Design.Button("Aplicar",Apply,true);System.Windows.Automation.AutomationProperties.SetName(apply,"Aplicar cambios");apply.MinWidth=85;apply.Margin=new Thickness(8,0,0,0);DockPanel.SetDock(apply,System.Windows.Controls.Dock.Right);foot.Children.Add(apply);foot.Children.Add(status);DockPanel.SetDock(foot,System.Windows.Controls.Dock.Bottom);main.Children.Add(foot);
            page=new StackPanel();pagesHost=new Grid();pagesHost.Children.Add(page);pageScroll=new ScrollViewer {Content=pagesHost,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};main.Children.Add(pageScroll);
            var shell=Design.Glass(layout,20,false);shell.Padding=new Thickness(0);Content=shell;SourceInitialized+=delegate{Native.Glass(this,Design.Dark);};header.MouseLeftButtonDown+=delegate{DragMove();};top.MouseLeftButtonDown+=delegate(object sender,MouseButtonEventArgs e){if(e.OriginalSource is TextBlock&&!e.Handled){var node=(DependencyObject)e.OriginalSource;while(node!=null){if(node is System.Windows.Controls.Primitives.ButtonBase)return;node=VisualTreeHelper.GetParent(node);}DragMove();}};
            settingsQuery.TextChanged+=delegate{if(!changingQuery)SearchSettings();};
            KeyDown+=delegate(object sender,KeyEventArgs e) { if(e.Key==Key.Escape) {e.Handled=true;if(settingsQuery.Text.Length>0)settingsQuery.Clear();else Design.Dismiss(this);} };Closed+=delegate {windowClosed=true;wallpaperRequest++;};ShowPage(0);
        }
        private static Button Light(string color,Action action,string tooltip)
        {
            var b=Design.Button("",action,false); b.Width=13;b.Height=13;b.Padding=new Thickness(0);b.Margin=new Thickness(0,0,8,0);b.Background=Design.Brush(color);b.BorderThickness=new Thickness(0);b.ToolTip=tooltip;return b;
        }
        private void ShowPage(int index)
        {
            if(settingsQuery.Text.Length>0){changingQuery=true;settingsQuery.Clear();changingQuery=false;}
            int direction=index.CompareTo(selected);selected=index;var previous=page;page=new StackPanel();for(int i=0;i<navigation.Count;i++){navigation[i].Background=i==index?Design.Brush("#007AFF"):Brushes.Transparent;navigation[i].Resources["GlassInk"]=i==index?Brushes.White:Design.Ink;}
            wallpaperRequest++;pageGeneration++;
            if(index==0)Appearance();else if(index==1)WallpaperPage();else if(index==2)Dock();else if(index==3)Bar();else if(index==4)Performance();else if(index==5)SystemPage();else if(index==6)NotchPage();else if(index==7)ControlPage();else if(index==8)WidgetsPage();else if(index==9)NotificationPage();else if(index==10)AccessPage();else if(index==11)SoundPage();else if(index==12)PrivacyPage();else if(index==13){SystemPage();KeepSections("Actualizaciones");Heading("Actualizaciones","Descargas verificadas y canales configurables.");}else if(index==14){SystemPage();KeepSections("Inicio y accesos");Heading("Atajos y asistente","Voz local y Gemini en Chrome para investigar.");}else if(index==15){Appearance();KeepSections("Cristal");Heading("Liquid Glass","Materiales reales, lectura clara y reflejos contenidos.");}else TransferPage();
            pageScroll.ScrollToVerticalOffset(0);ContentTransition.Show(pagesHost,previous.Children.Count==0?null:previous,page,direction);
        }
        internal void SetSearch(string query){settingsQuery.Text=query;}
        internal FrameworkElement TourPreview()
        {
            // Reuse the actual search controls at readable size, rather than
            // shrinking the entire settings window into the presentation.
            SetSearch("inicio");var root=new StackPanel();var header=title.Parent as Panel;if(header!=null){header.Children.Remove(title);header.Children.Remove(subtitle);}root.Children.Add(title);subtitle.Margin=new Thickness(0,6,0,12);root.Children.Add(subtitle);
            var holder=settingsQuery.Parent as Border;if(holder!=null)holder.Child=null;settingsQuery.Margin=new Thickness(0,0,0,13);settingsQuery.Background=Design.Brush("#16FFFFFF");root.Children.Add(settingsQuery);pageScroll.Content=null;root.Children.Add(pagesHost);var glass=Design.Glass(root,22,false);glass.Padding=new Thickness(22);return glass;
        }
        private void SearchSettings()
        {
            string query=settingsQuery.Text.Trim();if(query.Length==0){ShowPage(selected);return;}
            wallpaperRequest++;pageGeneration++;var previous=page;page=new StackPanel();Heading("Buscar ajustes","Encontrá una opción y abrí su sección.");int count=0;
            for(int i=0;i<PageNames.Length;i++)
            {
                if(!SearchMatches(PageNames[i]+" "+PageKeywords[i],query))continue;int target=i;var content=new DockPanel();var arrow=Design.Symbol("chevron",16,Design.Muted);DockPanel.SetDock(arrow,System.Windows.Controls.Dock.Right);content.Children.Add(arrow);var label=Design.Text(PageNames[i],14,true);content.Children.Add(label);var button=Design.Button("",delegate{ShowPage(target);},false);button.Content=content;button.Padding=new Thickness(15);button.HorizontalContentAlignment=HorizontalAlignment.Stretch;button.Margin=new Thickness(0,0,0,8);System.Windows.Automation.AutomationProperties.SetName(button,"Abrir "+PageNames[i]);page.Children.Add(button);count++;
            }
            if(count==0){var message=Design.Text("No encontramos ese ajuste",17,true);message.Margin=new Thickness(0,20,0,8);page.Children.Add(message);var hint=Design.Text("Probá con fondo, iconos, inicio o animaciones.",13,false);hint.TextWrapping=TextWrapping.Wrap;page.Children.Add(hint);}pageScroll.ScrollToVerticalOffset(0);ContentTransition.Show(pagesHost,previous,page,0);
        }
        internal static bool SearchMatches(string text,string query)
        {return query.Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries).All(word=>System.Globalization.CultureInfo.CurrentCulture.CompareInfo.IndexOf(text,word,System.Globalization.CompareOptions.IgnoreCase|System.Globalization.CompareOptions.IgnoreNonSpace)>=0);}
        private void Heading(string text,string description) { title.Text=text;subtitle.Text=description; }
        private StackPanel Section(string label)
        {
            var labelText=Design.Text(label,13,true);labelText.Margin=new Thickness(2,0,0,10);page.Children.Add(labelText);var contents=new StackPanel();var card=Design.Card(contents);card.Padding=new Thickness(18,11,18,11);card.CornerRadius=new CornerRadius(13);page.Children.Add(card);return contents;
        }
        private static Grid Row(string name,string detail,UIElement control)
        {
            var row=new Grid { Margin=new Thickness(0,8,0,8) };row.ColumnDefinitions.Add(new ColumnDefinition());row.ColumnDefinitions.Add(new ColumnDefinition {Width=GridLength.Auto});var text=new StackPanel {Margin=new Thickness(0,0,16,0),VerticalAlignment=VerticalAlignment.Center};text.Children.Add(Design.Text(name,13,true));if(!String.IsNullOrEmpty(detail)){var hint=Design.Text(detail,12,false);hint.SetResourceReference(TextBlock.ForegroundProperty,"GlassMuted");hint.TextWrapping=TextWrapping.Wrap;hint.Margin=new Thickness(0,4,0,0);text.Children.Add(hint);}row.Children.Add(text);Grid.SetColumn(control,1);row.Children.Add(control);return row;
        }
        private void Toggle(StackPanel section,string name,string detail,bool value,Action<bool> change)
        {
            var check=Design.Check("",value,change);System.Windows.Automation.AutomationProperties.SetName(check,name);check.Width=45;check.Margin=new Thickness(0);section.Children.Add(Row(name,detail,check));
        }
        private void Appearance()
        {
            Heading("Apariencia","Elegí cómo se ve tu escritorio.");
            var themes=new StackPanel {Orientation=Orientation.Horizontal,Margin=new Thickness(0,0,0,18)};
            themes.Children.Add(ThemeChoice("Light","Claro"));themes.Children.Add(ThemeChoice("Dark","Oscuro"));themes.Children.Add(ThemeChoice("Auto","Automático"));page.Children.Add(themes);
            var material=Section("Cristal");material.Children.Add(Row("Intensidad del cristal","Transparencia del Dock y los menús.",Design.Symbol("sun",20,Design.Muted)));
            var transparency=new Slider {Minimum=.06,Maximum=.65,Value=draft.GlassOpacity,Margin=new Thickness(0,12,0,3)};transparency.ValueChanged+=delegate {draft.GlassOpacity=transparency.Value;};material.Children.Add(transparency);material.Children.Add(Row("Más transparente","",Design.Text("Más contraste",10,false)));
            var colors=Section("Color del acento");var swatches=new StackPanel {Orientation=Orientation.Horizontal};string[] palette={"#8DAEFF","#B8A1FF","#7CDEC1","#FF9BC2","#F5CC85","#FF9F0A"};string[] labels={"Azul","Lavanda","Menta","Rosa","Ámbar","Mandarina"};
            for(int i=0;i<palette.Length;i++){string color=palette[i];var b=Design.Button("",delegate{draft.Accent=color;ShowPage(0);},false);var circle=new Grid{Width=26,Height=26};circle.Children.Add(new System.Windows.Shapes.Ellipse{Fill=Design.Brush(color),Stroke=Design.Brush("#20000000"),StrokeThickness=.5});if(draft.Accent==color){var mark=Design.Text("✓",14,true);mark.Foreground=Brushes.White;mark.HorizontalAlignment=HorizontalAlignment.Center;circle.Children.Add(mark);}b.Content=circle;b.Width=33;b.Height=33;b.Padding=new Thickness(0);b.Background=Brushes.Transparent;b.BorderThickness=new Thickness(0);b.ToolTip=labels[i];System.Windows.Automation.AutomationProperties.SetName(b,labels[i]);b.Margin=new Thickness(0,0,9,0);swatches.Children.Add(b);}colors.Children.Add(swatches);
            var extras=Section("Escritorio");Toggle(extras,"Widgets de escritorio","Relojes, calendario, clima, notas y más.",draft.ShowWidget,delegate(bool v){draft.ShowWidget=v;});extras.Children.Add(Design.Button("Explorar y configurar widgets",delegate{desktop.OpenWidgets(null);},false));extras.Children.Add(Design.Button("Stage Manager y grupos de trabajo",desktop.OpenStageManager,false));
            extras.Children.Add(Design.Button("Elegir fondo de pantalla…",delegate {ShowPage(1);},false));extras.Children.Add(Design.Button("Personalizar pantalla de MacFlow…",delegate{PrivacyAppearance.Open(desktop,draft);},false));
        }
        private Button ThemeChoice(string theme,string label)
        {
            var card=new StackPanel{Width=132};var preview=new Grid{Height=76,ClipToBounds=true,Background=new LinearGradientBrush((Color)ColorConverter.ConvertFromString(theme=="Light"?"#B9DAF2":"#343449"),(Color)ColorConverter.ConvertFromString(theme=="Light"?"#E8D9CE":"#624960"),45)};
            var miniature=new Grid{Margin=new Thickness(12,12,10,12),Background=Design.Brush(theme=="Light"?"#DDF5F5F7":"#D925252B")};miniature.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(28)});miniature.ColumnDefinitions.Add(new ColumnDefinition());var sidebar=new StackPanel{Background=Design.Brush(theme=="Light"?"#18202A40":"#15FFFFFF"),Margin=new Thickness(0,0,6,0)};for(int i=0;i<4;i++)sidebar.Children.Add(new Border{Height=3,Margin=new Thickness(5,4,5,0),CornerRadius=new CornerRadius(1.5),Background=Design.Brush(i==1?"#8DAEFF":"#50949AA5")});miniature.Children.Add(sidebar);var lines=new StackPanel{Margin=new Thickness(3,9,8,0)};for(int i=0;i<3;i++)lines.Children.Add(new Border{Height=i==0?5:3,Margin=new Thickness(0,0,i*6,7),CornerRadius=new CornerRadius(2),Background=Design.Brush(theme=="Light"?"#505C6572":"#70D9D9E3")});Grid.SetColumn(lines,1);miniature.Children.Add(lines);preview.Children.Add(miniature);card.Children.Add(new Border{CornerRadius=new CornerRadius(12),Clip=new RectangleGeometry(new Rect(0,0,132,76),12,12),Child=preview});var name=Design.Text(label,12,draft.Theme==theme);name.HorizontalAlignment=HorizontalAlignment.Center;name.Margin=new Thickness(0,10,0,0);card.Children.Add(name);var button=Design.Button("",delegate{draft.Theme=theme;ShowPage(0);},false);button.Content=card;button.Padding=new Thickness(7);button.Margin=new Thickness(0,0,10,0);button.Background=Design.Brush(draft.Theme==theme?"#228DAEFF":"#08FFFFFF");System.Windows.Automation.AutomationProperties.SetName(button,label);return button;
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
            var choices=Section("Fondos MacOS");choices.Children.Add(Design.Text("Tierra gira según la hora local. Se actualiza cada minuto mientras el escritorio está visible; elegir otro fondo detiene el giro.",11,false));var tiles=new System.Windows.Controls.Primitives.UniformGrid {Columns=4};choices.Children.Add(tiles);
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
        internal void SetDockAutoHide(bool value){draft.SetDockAutoHide(value);if(!value)draft.HideInFullscreen=false;desktop.ApplyDockBehavior(draft);}
        private void Dock()
        {
            Heading("Dock","Tus aplicaciones favoritas, siempre cerca.");var settings=Section("Tamaño y comportamiento");var sizeLabel=Design.Text(draft.IconSize+" px",12,false);settings.Children.Add(Row("Tamaño de los iconos","",sizeLabel));var size=new Slider {Minimum=32,Maximum=72,Value=draft.IconSize,TickFrequency=4,IsSnapToTickEnabled=true,Margin=new Thickness(0,8,0,12)};size.ValueChanged+=delegate {draft.IconSize=(int)size.Value;sizeLabel.Text=draft.IconSize+" px";};settings.Children.Add(size);
            Toggle(settings,"Ocultar automáticamente","Se guarda al instante. Desactivar conserva el Dock visible incluso en pantalla completa.",draft.AnyDockAutoHide,delegate(bool v){SetDockAutoHide(v);ShowPage(2);});Toggle(settings,"Animación y ampliación","Movimiento suave al pasar el cursor.",draft.Animations,delegate(bool v){draft.Animations=v;});
            settings.Children.Add(Row("Animaciones de otras aplicaciones","Windows conserva sus transiciones para evitar capturas y animaciones superpuestas.",Design.Symbol("windows",18,Design.Muted)));
            var pos=new ComboBox {ItemsSource=new[]{"Abajo","Izquierda","Derecha"},SelectedIndex=draft.DockPosition=="Left" ? 1 : draft.DockPosition=="Right" ? 2 : 0,Width=150};pos.SelectionChanged+=delegate {draft.DockPosition=pos.SelectedIndex==1 ? "Left" : pos.SelectedIndex==2 ? "Right" : "Bottom";};settings.Children.Add(Row("Posición","Pantalla principal",pos));
            var iconMode=new ComboBox{ItemsSource=new[]{"Automáticos","Claros","Oscuros","Originales"},SelectedIndex=draft.IconAppearance=="Light"?1:draft.IconAppearance=="Dark"?2:draft.IconAppearance=="Original"?3:0,Width=160};iconMode.SelectionChanged+=delegate{draft.IconAppearance=iconMode.SelectedIndex==1?"Light":iconMode.SelectedIndex==2?"Dark":iconMode.SelectedIndex==3?"Original":"Auto";};settings.Children.Add(Row("Apariencia de iconos","Automáticos sigue la apariencia de MacFlow. Aplicá para guardar.",iconMode));
            var contextual=Section("Mientras trabajás");Toggle(contextual,"Apartar en todas las aplicaciones","Visible en el escritorio; se recupera en el borde de la pantalla.",draft.HideDockInApps,delegate(bool v){draft.HideDockInApps=v;desktop.ApplyDockBehavior(draft);});Toggle(contextual,"Apartar el Dock en Codex","Se retira suavemente y vuelve al acercar el mouse al borde.",draft.HideDockInCodex,delegate(bool v){draft.HideDockInCodex=v;desktop.ApplyDockBehavior(draft);});
            var pinned=Section("Aplicaciones fijadas");var list=new ListBox {ItemsSource=desktop.Preferences.Pins.Select(p=>p.Name).ToList(),Height=140};pinned.Children.Add(list);var buttons=new StackPanel {Orientation=Orientation.Horizontal,Margin=new Thickness(0,8,0,0)};
            Action<int> move=delegate(int direction){int i=list.SelectedIndex,target=i+direction;if(i<0||target<0||target>=desktop.Preferences.Pins.Count)return;var item=desktop.Preferences.Pins[i];desktop.Preferences.Pins.RemoveAt(i);desktop.Preferences.Pins.Insert(target,item);desktop.Save();desktop.DrawDock();list.ItemsSource=desktop.Preferences.Pins.Select(p=>p.Name).ToList();list.SelectedIndex=target;};buttons.Children.Add(Design.Button("←",delegate{move(-1);},false));buttons.Children.Add(Design.Button("→",delegate{move(1);},false));buttons.Children.Add(Design.Button("Quitar",delegate{if(list.SelectedIndex>=0){desktop.Pin(desktop.Preferences.Pins[list.SelectedIndex]);list.ItemsSource=desktop.Preferences.Pins.Select(p=>p.Name).ToList();}},false));
            buttons.Children.Add(Design.Button("Agregar…",delegate{var dialog=new Microsoft.Win32.OpenFileDialog {Filter="Programas y accesos directos|*.exe;*.lnk"};if(dialog.ShowDialog(this)==true){desktop.Pin(new AppEntry{Name=Path.GetFileNameWithoutExtension(dialog.FileName),Target=dialog.FileName,Executable=dialog.FileName.EndsWith(".exe",StringComparison.OrdinalIgnoreCase)?dialog.FileName:null});list.ItemsSource=desktop.Preferences.Pins.Select(p=>p.Name).ToList();}},false));pinned.Children.Add(buttons);
        }
        private void Bar()
        {
            Heading("Barra superior","Una barra de cristal con reloj y controles del sistema.");var section=Section("Elementos visibles");Toggle(section,"Barra superior","Sin contorno exterior, esquinas suaves y altura de 40 puntos.",draft.ShowTopBar,delegate(bool v){draft.ShowTopBar=v;});Toggle(section,"Wi-Fi y red","Estado de tu conexión",draft.ShowNetwork,delegate(bool v){draft.ShowNetwork=v;});Toggle(section,"Batería","Porcentaje o alimentación por corriente",draft.ShowBattery,delegate(bool v){draft.ShowBattery=v;});Toggle(section,"Volumen","Nivel del dispositivo de salida",draft.ShowVolume,delegate(bool v){draft.ShowVolume=v;});
            Toggle(section,"Isla dinámica","Integrado al borde superior para música, actividades y Shelf.",draft.ShowActivityIsland!=false,delegate(bool v){draft.ShowActivityIsland=v;});var controlLayout=Section("Centro de control");var layoutChoice=new ComboBox{ItemsSource=new[]{"Mac"},SelectedIndex=0,Width=150};layoutChoice.SelectionChanged+=delegate{draft.ControlCenterLayout="Mac";};controlLayout.Children.Add(Row("Distribución","Automático sigue el modo del escritorio.",layoutChoice));var integration=Section("Integración con Windows");Toggle(integration,"Ocultar la barra de tareas original","Se oculta mientras MacFlow está activo; vuelve al salir.",draft.AutoHideTaskbar,delegate(bool v){draft.AutoHideTaskbar=v;});Toggle(integration,"Borde del Dock protegido","Acercá el cursor al borde para recuperar el Dock.",draft.ProtectDockEdge,delegate(bool v){draft.ProtectDockEdge=v;});var note=Design.Text("La barra superior incluye controles rojo, amarillo y verde para la ventana activa. Los botones originales de cada programa se conservan.",12,false);note.SetResourceReference(TextBlock.ForegroundProperty,"GlassMuted");note.TextWrapping=TextWrapping.Wrap;integration.Children.Add(note);
        }
        private void Performance()
        {
            Heading("Rendimiento","Fluido cuando lo usás. Tranquilo cuando no.");var glass=Section("Cristal");Toggle(glass,"Cristal con desenfoque","Fondo real y reflejos suaves; aparece en capturas del escritorio.",draft.LiveGlass,delegate(bool v){draft.LiveGlass=v;});var section=Section("Movimiento y recursos");Toggle(section,"Animaciones suaves","Transiciones y ampliación del Dock",draft.Animations,delegate(bool v){draft.Animations=v;});Toggle(section,"Ahorro de recursos","Actualiza ventanas cada 2 segundos.",draft.LowPower,delegate(bool v){draft.LowPower=v;});Toggle(section,"Ocultar en pantalla completa","Para juegos, películas y presentaciones.",draft.HideInFullscreen,delegate(bool v){draft.HideInFullscreen=v;});var process=Process.GetCurrentProcess();var resources=Section("Esta sesión");resources.Children.Add(Row("Memoria privada", "Incluye WPF y la integración de Windows.",Design.Text((process.PrivateMemorySize64/1048576)+" MB",15,true)));resources.Children.Add(Row("Aplicaciones detectadas","Sin conexión a un servicio externo.",Design.Text(desktop.AppCount.ToString(),15,true)));
        }
        private void SystemPage()
        {
            Heading("Sistema","El escritorio se adapta a tu Windows.");var section=Section("Inicio y accesos");var start=Design.Check("",startupValue,delegate(bool v){startupValue=v;});start.Width=45;start.IsEnabled=startupAvailable;var startupInfo=desktop.IsTesting?null:Startup.Inspect();section.Children.Add(Row("Iniciar con Windows",startupInfo==null?"No disponible en este modo de prueba":startupInfo.Description,start));
            var repair=Design.Button("Reparar inicio automático",delegate{try{Startup.Enabled=true;startupValue=true;ShowPage(5);status.Text="Acceso de inicio reparado. Windows puede deshabilitarlo desde Aplicaciones de inicio.";}catch(Exception ex){status.Text=ex.Message;}},false);repair.IsEnabled=startupAvailable;section.Children.Add(repair);
            section.Children.Add(Design.Button("Revisar aplicaciones de inicio en Windows",delegate{Process.Start(new ProcessStartInfo("ms-settings:startupapps"){UseShellExecute=true});},false));
            section.Children.Add(Design.Button("Abrir Administrador de tareas",desktop.OpenTaskManager,false));
            section.Children.Add(Design.Button("Abrir asistente local",desktop.OpenAssistant,false));Toggle(section,"Sonidos de MacOS","Avisos breves originales. No se reproducen grabaciones de Apple.",draft.Sounds!=false,delegate(bool v){draft.Sounds=v;});
            var launch=desktop.IsTesting?null:LaunchDiagnostics.Read(desktop.Store.DirectoryPath);var launchText=Design.Text(launch==null?"La próxima apertura registrará el tiempo hasta mostrar el Dock.":"Último inicio: "+launch.Mode+" · "+launch.Phase+(launch.FirstFrameMs>0?" · Dock en "+(launch.FirstFrameMs/1000).ToString("F2")+" s":""),12,false);launchText.TextWrapping=TextWrapping.Wrap;section.Children.Add(launchText);
            draft.ShowSessionGreeting=false;
            Toggle(section,"Saludo después de actualizar","Hola aparece una vez por versión nueva.",draft.WelcomeAfterUpdate,delegate(bool v){draft.WelcomeAfterUpdate=v;});section.Children.Add(Design.Button("Repetir Hola y demostración",desktop.ReplayWelcome,false));
            var winAction=new ComboBox{ItemsSource=new[]{"Búsqueda escrita","Sin acción","Menú de MacOS","Menú de Windows"},SelectedIndex=draft.WindowsKeyAction=="MacFlow"?2:draft.WindowsKeyAction=="Windows"?3:draft.WindowsKeyAction=="Disabled"?1:0,Width=165};winAction.SelectionChanged+=delegate{draft.WindowsKeyAction=winAction.SelectedIndex==2?"MacFlow":winAction.SelectedIndex==3?"Windows":winAction.SelectedIndex==1?"Disabled":"Search";draft.WindowsMenu=winAction.SelectedIndex!=3;};section.Children.Add(Row("Tecla Windows sola","Abre el buscador sin activar dictado. Win+E y las demás combinaciones se conservan.",winAction));
            Toggle(section,"Hey Siri","Escucha local de la frase de activación. Micrófono indicado en la barra; se pausa al bloquear Windows. Incluye reconocimiento local en español; no requiere instalar un motor de Windows.",draft.VoiceWakeEnabled!=false,delegate(bool v){draft.VoiceWakeEnabled=v;});var voiceStatus=Design.Text(desktop.WakeStatus??"Se inicia después de mostrar el escritorio de MacFlow.",11,false);voiceStatus.TextWrapping=TextWrapping.Wrap;section.Children.Add(voiceStatus);section.Children.Add(Design.Button("Permisos del micrófono",delegate{Process.Start("ms-settings:privacy-microphone");},false));section.Children.Add(Design.Button("Probar búsqueda escrita",desktop.OpenIntelligentSearch,false));
            Toggle(section,"Deslizar el centro de control","Arrastrá hacia abajo con el botón izquierdo o derecho desde los indicadores de la barra superior.",draft.ControlCenterGesture!=false,delegate(bool v){draft.ControlCenterGesture=v;});section.Children.Add(Design.Button("Tus ventanas · minimizar, ampliar y cerrar",desktop.OpenOverview,false));section.Children.Add(Design.Button("Abrir menú de energía",desktop.OpenPowerMenu,false));
            var lid=Section("Tapa de la notebook");Toggle(lid,"Transición al abrir y cerrar la tapa","Pliegue con desenfoque y movimiento suave.",draft.LidAnimation,delegate(bool v){draft.LidAnimation=v;});lid.Children.Add(Design.Button("Probar pliegue del escritorio",desktop.PreviewLidAnimation,false));var lidNote=Design.Text(desktop.LidStatus+"\nSin sensor, el efecto responde al aviso de apertura/cierre. La suspensión puede interrumpirlo.",11,false);lidNote.TextWrapping=TextWrapping.Wrap;lidNote.Margin=new Thickness(0,8,0,0);lid.Children.Add(lidNote);
            var hotkey=new ComboBox {ItemsSource=new[]{"Alt + Espacio","Ctrl + Alt + Espacio","Ctrl + Espacio"},SelectedIndex=draft.Hotkey=="CtrlAltSpace"?1:draft.Hotkey=="CtrlSpace"?2:0,Width=185};hotkey.SelectionChanged+=delegate {draft.Hotkey=hotkey.SelectedIndex==1?"CtrlAltSpace":hotkey.SelectedIndex==2?"CtrlSpace":"AltSpace";};section.Children.Add(Row("Atajo del buscador","Si está ocupado, podés usar el Dock.",hotkey));
            var update=Section("Actualizaciones");Toggle(update,"Buscar automáticamente","Consulta el canal al iniciar y cada 12 horas, sin instalar por tu cuenta.",draft.CheckUpdatesAutomatically,delegate(bool v){draft.CheckUpdatesAutomatically=v;});update.Children.Add(Design.Button("Abrir actualizaciones",delegate{desktop.OpenUpdates(draft.UpdateRepository,draft.UpdateFeedUrl);},false));var repo=new TextBox{Text=draft.UpdateRepository??"",Padding=new Thickness(10),Background=Design.Brush("#12FFFFFF"),BorderThickness=new Thickness(0),ToolTip="Canal opcional de GitHub: propietario/repositorio"};repo.SetResourceReference(Control.ForegroundProperty,"GlassInk");repo.SetResourceReference(TextBox.CaretBrushProperty,"GlassInk");repo.TextChanged+=delegate{draft.UpdateRepository=repo.Text.Trim();};update.Children.Add(Design.Text("Canal de GitHub (opcional)",11,false));repo.Margin=new Thickness(0,8,0,8);update.Children.Add(repo);
            if(!String.IsNullOrWhiteSpace(draft.LocalUpdateFolder))update.Children.Add(Design.Text("Canal de esta computadora conectado: recibirás un aviso cuando haya otra versión preparada.",11,false));
            var feed=new TextBox {Text=draft.UpdateFeedUrl??"",Padding=new Thickness(10),Background=Design.Brush("#12FFFFFF"),BorderThickness=new Thickness(0),ToolTip="Enlace HTTPS al manifiesto publicado de MacOS"};feed.SetResourceReference(Control.ForegroundProperty,"GlassInk");feed.SetResourceReference(TextBox.CaretBrushProperty,"GlassInk");feed.TextChanged+=delegate{draft.UpdateFeedUrl=feed.Text.Trim();};update.Children.Add(Design.Text("Enlace de actualizaciones (HTTPS)",11,false));update.Children.Add(feed);
            var recovery=Section("Restauración");recovery.Children.Add(Design.Button("Restaurar Windows y cerrar MacOS",delegate {Application.Current.Shutdown();},false));recovery.Children.Add(Design.Button("Desinstalar MacOS…",delegate {var exe=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Uninstall.exe");if(File.Exists(exe))Process.Start(new ProcessStartInfo(exe){UseShellExecute=true});else MessageBox.Show("Esta es la versión portable. Cerrá MacFlow y eliminá su carpeta para quitarla.","MacOS");},false));
        }

        private void KeepSections(params string[] labels)
        {
            bool keep=false;foreach(UIElement child in page.Children.Cast<UIElement>().ToArray()){var text=child as TextBlock;if(text!=null)keep=labels.Contains(text.Text);if(!keep)page.Children.Remove(child);}
        }
        private void NotchPage()
        {
            Heading("Isla dinámica","Una cápsula flotante que reúne tu actividad y se transforma con ella.");var section=Section("Aspecto y comportamiento");
            Toggle(section,"Mostrar isla dinámica","En cada pantalla con barra superior.",draft.ShowActivityIsland!=false,v=>draft.ShowActivityIsland=v);
            var size=new Slider{Minimum=.85,Maximum=1.2,Value=draft.NotchScale,Width=150};size.ValueChanged+=delegate{draft.NotchScale=size.Value;};section.Children.Add(Row("Tamaño","De compacto a amplio, sin mover la barra.",size));
            Toggle(section,"Expandir al acercar el cursor","También podés abrirlo con un clic.",draft.NotchHover,v=>draft.NotchHover=v);
            var contents=Section("Contenido");Toggle(contents,"Música","Carátula, progreso y controles del reproductor real.",draft.NotchMedia,v=>draft.NotchMedia=v);Toggle(contents,"Temporizadores","Prioridad sobre música mientras están activos.",draft.NotchTimers,v=>draft.NotchTimers=v);Toggle(contents,"Avisos","Acciones y avisos generados por el escritorio.",draft.NotchNotifications,v=>draft.NotchNotifications=v);Toggle(contents,"Shelf","Copias locales, límites de tamaño y arrastrar para recuperar.",draft.NotchShelf,v=>draft.NotchShelf=v);
        }
        private void ControlPage()
        {
            Heading("Centro de control","Red, sonido, pantalla y multimedia en un panel compacto.");var section=Section("Interacción");Toggle(section,"Deslizar desde la barra","Mantener y deslizar desde batería, red o controles.",draft.ControlCenterGesture!=false,v=>draft.ControlCenterGesture=v);section.Children.Add(Design.Button("Abrir Centro de control",desktop.OpenControl,false));section.Children.Add(Design.Button("Redes Wi-Fi",desktop.OpenWifi,false));section.Children.Add(Design.Button("Dispositivos Bluetooth",desktop.OpenBluetooth,false));
        }
        private void WidgetsPage()
        {
            Heading("Widgets","Datos reales y piezas que podés acomodar a tu gusto.");var section=Section("Escritorio");Toggle(section,"Mostrar widgets","Calendario, clima, música, batería, notas, red y rendimiento.",draft.ShowWidget,v=>draft.ShowWidget=v);section.Children.Add(Design.Button("Abrir galería y configurar",delegate{desktop.OpenWidgets(null);},true));section.Children.Add(Design.Button("Stage Manager",desktop.OpenStageManager,false));
        }
        private void NotificationPage()
        {
            Heading("Notificaciones","Avisos locales y conexión autorizada con Windows.");var section=Section("Avisos");Toggle(section,"Paneles de notificación","Presentación de avisos de MacOS.",draft.ShowNotifications,v=>draft.ShowNotifications=v);Toggle(section,"Mostrar en la isla dinámica","Avisos breves que no reemplazan temporizadores.",draft.NotchNotifications,v=>draft.NotchNotifications=v);
            var check=Design.Check("",draft.ShowSystemNotifications,v=>draft.ShowSystemNotifications=v);check.IsEnabled=WindowsNotifications.HasIdentity&&!desktop.IsTesting;section.Children.Add(Row("Avisos de otras aplicaciones",desktop.WindowsNotificationStatus,check));var authorize=Design.Button("Autorizar acceso en Windows",async delegate{status.Text=await desktop.AuthorizeWindowsNotifications();},false);authorize.IsEnabled=WindowsNotifications.HasIdentity&&!desktop.IsTesting;section.Children.Add(authorize);Toggle(section,"Previsualizaciones en pantalla de MacOS","Muestra el texto de otras apps; desactivado deja sólo la aplicación.",draft.LockNotificationPreviews,v=>draft.LockNotificationPreviews=v);section.Children.Add(Design.Button("Abrir historial",desktop.OpenActivity,false));
        }
        private void AccessPage(){Heading("Accesibilidad","Adaptá el movimiento y la lectura.");var section=Section("Movimiento y contraste");Toggle(section,"Reducir movimiento","Desactiva animaciones del escritorio.",!draft.Animations,v=>draft.Animations=!v);Toggle(section,"Transparencia con desenfoque","Desactivala para usar materiales de contraste estable.",draft.LiveGlass,v=>draft.LiveGlass=v);Toggle(section,"Bajo consumo","Menos sondeos y reflejos costosos.",draft.LowPower,v=>draft.LowPower=v);}
        private void SoundPage(){Heading("Sonido","Multimedia y voz con los proveedores de Windows.");var section=Section("Reproducción");Toggle(section,"Sonidos de interfaz","Señales originales, breves y opcionales.",draft.Sounds!=false,v=>draft.Sounds=v);section.Children.Add(Design.Button("Controles multimedia",desktop.OpenControl,false));section.Children.Add(Design.Button("Probar asistente",desktop.OpenAssistant,false));}
        private void PrivacyPage(){Heading("Privacidad","Pantalla propia, sin sustituir el bloqueo seguro de Windows.");var section=Section("Personalización");Toggle(section,"Pantalla al volver de suspensión","Se muestra tras el desbloqueo de Windows; no reemplaza su seguridad.",draft.ShowPrivacyAfterResume,v=>draft.ShowPrivacyAfterResume=v);Toggle(section,"Tierra en el bloqueo","Planeta 3D y luz por hora local; no cambia el escritorio.",draft.PrivacyAstronomy,v=>draft.PrivacyAstronomy=v);section.Children.Add(Design.Button("Fondo, reloj y color…",delegate{PrivacyAppearance.Open(desktop,draft);},true));Toggle(section,"Activación por voz","Usa el micrófono sólo si lo elegís; indicador visible.",draft.VoiceWakeEnabled==true,v=>draft.VoiceWakeEnabled=v);}
        private void TransferPage()
        {
            Heading("Preferencias","Copias portables de tus ajustes. El inicio automático no se importa.");var section=Section("Copias y restauración");
            section.Children.Add(Design.Button("Exportar ajustes…",delegate{try{var file=new Microsoft.Win32.SaveFileDialog{Filter="Preferencias JSON|*.json",FileName="MacOS-preferencias.json"};if(file.ShowDialog()==true){PreferencesIO.Write(file.FileName,draft);status.Text="Preferencias exportadas.";}}catch(Exception ex){status.Text=ex.Message;}},false));
            section.Children.Add(Design.Button("Importar ajustes…",delegate{try{var file=new Microsoft.Win32.OpenFileDialog{Filter="Preferencias JSON|*.json"};if(file.ShowDialog()==true){draft=PreferencesIO.Read(file.FileName);status.Text="Preferencias cargadas. Revisalas y pulsá Aplicar.";}}catch(Exception ex){status.Text=ex.Message;}},false));
            section.Children.Add(Design.Button("Restaurar ajustes predeterminados",delegate{draft=new Preferences();draft.AutoHideTaskbar=false;draft.CheckUpdatesAutomatically=false;status.Text="Predeterminados cargados. Aplicar no cambia tu opción de inicio automático.";},false));
        }
        private void Apply()
        {
            try {if(!String.IsNullOrWhiteSpace(draft.UpdateFeedUrl)&&!UpdateService.ValidFeed(draft.UpdateFeedUrl))throw new IOException("El enlace de actualizaciones debe usar HTTPS.");draft.Pins=desktop.Preferences.Pins;draft.LastWelcomedVersion=desktop.Preferences.LastWelcomedVersion;draft.WallpaperPath=desktop.Preferences.WallpaperPath;desktop.ApplyPreferences(draft,startupAvailable?(bool?)startupValue:null);Close();desktop.OpenSettings();}catch(Exception ex){status.Text=ex.Message;}
        }
        public void SelectPage(int pageIndex) {ShowPage(pageIndex);}
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace MacFlow
{
    public sealed class Spotlight : Window
    {
        private readonly Desktop desktop;
        private readonly TextBox query;
        private readonly ListBox results;
        private readonly TextBlock status;
        private readonly DispatcherTimer debounce;
        private bool files;
        internal bool Intelligent{get;private set;}private readonly WindowEntry commandTarget;
        public Spotlight(Desktop controller):this(controller,false,null){}
        internal Spotlight(Desktop controller,bool intelligent,WindowEntry target)
        {
            desktop=controller;Intelligent=intelligent;commandTarget=target;Title=intelligent?"MacFlow · Búsqueda inteligente":"MacFlow · Spotlight";Width=660;Height=454;WindowStyle=WindowStyle.None;AllowsTransparency=false;ResizeMode=ResizeMode.NoResize;ShowInTaskbar=false;Topmost=true;Background=Brushes.Transparent;
            var root=new DockPanel();var header=new DockPanel {Margin=new Thickness(0,0,0,14)};header.Children.Add(new Border {Child=Design.Symbol("search",26,Design.Muted),Margin=new Thickness(0,0,14,0),VerticalAlignment=VerticalAlignment.Center});query=new TextBox {FontSize=25,Padding=new Thickness(0,8,0,8),Foreground=Design.Ink,Background=Brushes.Transparent,BorderThickness=new Thickness(0),CaretBrush=Design.Ink};query.SetResourceReference(Control.ForegroundProperty,"GlassInk");query.SetResourceReference(TextBox.CaretBrushProperty,"GlassInk");System.Windows.Automation.AutomationProperties.SetName(query,intelligent?"Búsqueda inteligente sin voz":"Buscar en MacFlow");var queryLayer=new Grid();queryLayer.Children.Add(query);var placeholder=Design.Text(intelligent?"Buscá o escribí una orden…":"Buscar en MacFlow",22,false);placeholder.SetResourceReference(TextBlock.ForegroundProperty,"GlassMuted");placeholder.IsHitTestVisible=false;placeholder.VerticalAlignment=VerticalAlignment.Center;queryLayer.Children.Add(placeholder);query.TextChanged+=delegate{placeholder.Visibility=query.Text.Length==0?Visibility.Visible:Visibility.Collapsed;};header.Children.Add(queryLayer);DockPanel.SetDock(header,System.Windows.Controls.Dock.Top);root.Children.Add(header);
            var modes=new StackPanel {Orientation=Orientation.Horizontal,Margin=new Thickness(0,0,0,10)};modes.Children.Add(Design.Button("Aplicaciones",delegate{files=false;Refresh();},false));modes.Children.Add(Design.Button("Archivos",delegate{files=true;Refresh();},false));var key=Design.Text("  > acciones   ·   2 + 2 para calcular",11,false);key.SetResourceReference(TextBlock.ForegroundProperty,"GlassMuted");modes.Children.Add(key);DockPanel.SetDock(modes,System.Windows.Controls.Dock.Top);root.Children.Add(modes);
            var foot=new DockPanel {Margin=new Thickness(0,12,0,0)};status=Design.Text("",10,false);status.SetResourceReference(TextBlock.ForegroundProperty,"GlassMuted");var hint=Design.Text("↵ abrir   Ctrl+P vista rápida   esc cerrar",10,false);hint.SetResourceReference(TextBlock.ForegroundProperty,"GlassMuted");DockPanel.SetDock(hint,System.Windows.Controls.Dock.Right);foot.Children.Add(hint);foot.Children.Add(status);DockPanel.SetDock(foot,System.Windows.Controls.Dock.Bottom);root.Children.Add(foot);
            results=new ListBox {Background=Brushes.Transparent,BorderThickness=new Thickness(0),Padding=new Thickness(0),HorizontalContentAlignment=HorizontalAlignment.Stretch};root.Children.Add(results);Content=Design.Glass(root,26,false);
            debounce=new DispatcherTimer {Interval=TimeSpan.FromMilliseconds(120)};debounce.Tick+=delegate{debounce.Stop();Refresh();};query.TextChanged+=delegate{debounce.Stop();debounce.Start();};
            PreviewKeyDown+=delegate(object sender,KeyEventArgs e)
            {
                var selected=results.SelectedItem as ListBoxItem;var action=selected==null?null:selected.Tag as SearchAction;
                if(e.Key==Key.Escape){Design.Dismiss(this);e.Handled=true;}
                else if(e.Key==Key.Down&&results.Items.Count>0){results.SelectedIndex=Math.Min(results.Items.Count-1,results.SelectedIndex+1);results.ScrollIntoView(results.SelectedItem);e.Handled=true;}
                else if(e.Key==Key.Up&&results.Items.Count>0){results.SelectedIndex=Math.Max(0,results.SelectedIndex-1);results.ScrollIntoView(results.SelectedItem);e.Handled=true;}
                else if(e.Key==Key.Enter&&action!=null){Design.Dismiss(this,action.Execute);e.Handled=true;}
                else if(e.Key==Key.P&&(Keyboard.Modifiers&ModifierKeys.Control)!=0&&action!=null&&!String.IsNullOrEmpty(action.FilePath)){desktop.OpenPreview(action.FilePath);e.Handled=true;}
                else if(e.Key==Key.D&&(Keyboard.Modifiers&ModifierKeys.Control)!=0&&action!=null&&action.App!=null){desktop.Pin(action.App);Refresh();e.Handled=true;}
            };
            results.MouseDoubleClick+=delegate{var item=results.SelectedItem as ListBoxItem;if(item!=null){var action=(SearchAction)item.Tag;Design.Dismiss(this,action.Execute);}};
            SourceInitialized+=delegate{Native.Glass(this,Design.Dark);};Loaded+=delegate{if(!desktop.IsTesting)query.Focus();};Closed+=delegate{debounce.Stop();};Refresh();
        }
        public void SetQuery(string text,bool fileMode=false){files=fileMode;query.Text=text;Refresh();}
        public int ResultCount {get{return results.Items.Count;}}
        public void Refresh()
        {
            results.Items.Clear();string text=query.Text.Trim(),answer;var actions=new List<SearchAction>();
            if(Intelligent&&!files&&LocalCommands.Parse(text).Intent!=LocalIntent.Unknown){string command=text;actions.Add(new SearchAction{Name=text,Detail="Orden local · Enter para ejecutar",Execute=delegate{desktop.RunAssistantCommand(command,commandTarget);}});}
            if(Calculator.TryEvaluate(text,out answer)) {var result=answer;actions.Add(new SearchAction {Name=answer,Detail="Resultado · Enter para copiar",Execute=delegate{Clipboard.SetText(result);}});}
            if(text.StartsWith(">"))
            {
                var commands=new[]{new SearchAction{Name="Aplicaciones",Detail="Mostrar todas las aplicaciones",Execute=desktop.OpenLaunchpad},new SearchAction{Name="Centro de control",Detail="Sonido, redes y pantalla",Execute=desktop.OpenControl},new SearchAction{Name="Configuración",Detail="Personalizar MacFlow",Execute=desktop.OpenSettings},new SearchAction{Name="Modo iPad",Detail="Inicio, ventanas flotantes y multitarea",Execute=desktop.ToggleTabletMode},new SearchAction{Name="Concentración",Detail="Temporizador con pausa y sesión persistente",Execute=desktop.OpenFocus},new SearchAction{Name="Pilas de carpetas",Detail="Archivos y vista rápida",Execute=delegate{desktop.OpenStack(FolderStack.Downloads());}},new SearchAction{Name="Tu escritorio",Detail="Menú de cristal · Ctrl+Alt+D",Execute=desktop.OpenDesktopMenu},new SearchAction{Name="Ventanas",Detail="Mostrar ventanas abiertas",Execute=desktop.OpenOverview},new SearchAction{Name="Explorador",Detail="Abrir archivos",Execute=delegate{desktop.Launch(new AppEntry{Name="Explorador",Target="explorer.exe"});}},new SearchAction{Name="Salir",Detail="Restaurar Windows y cerrar MacFlow",Execute=delegate{Application.Current.Shutdown();}}};
                actions.AddRange(commands.Where(a=>CultureInfo.CurrentCulture.CompareInfo.IndexOf(a.Name,text.Substring(1).Trim(),CompareOptions.IgnoreCase|CompareOptions.IgnoreNonSpace)>=0));
            }
            else if(files)
            {
                if(text.Length>=2)foreach(var path in desktop.Files.Where(p=>CultureInfo.CurrentCulture.CompareInfo.IndexOf(Path.GetFileName(p),text,CompareOptions.IgnoreCase|CompareOptions.IgnoreNonSpace)>=0).Take(35)){string file=path;actions.Add(new SearchAction{Name=Path.GetFileName(file),Detail=Path.GetDirectoryName(file),FilePath=file,Execute=delegate{try{Process.Start(new ProcessStartInfo(file){UseShellExecute=true});}catch(Exception ex){MessageBox.Show(ex.Message,"MacFlow");}}});}
            }
            else foreach(var app in desktop.Apps.Where(a=>CultureInfo.CurrentCulture.CompareInfo.IndexOf(a.Name,text,CompareOptions.IgnoreCase|CompareOptions.IgnoreNonSpace)>=0).OrderBy(a=>String.Equals(a.Name,text,StringComparison.CurrentCultureIgnoreCase)?0:a.Name.StartsWith(text,StringComparison.CurrentCultureIgnoreCase)?1:2).Take(40)){var captured=app;actions.Add(new SearchAction{Name=app.Name,Detail=desktop.Preferences.Pins.Any(p=>p.Key==app.Key)?"Aplicación · Fijada en el Dock":"Aplicación",App=app,Execute=delegate{desktop.Launch(captured);}});}
            if(Intelligent&&!files&&!String.IsNullOrWhiteSpace(text)&&LocalCommands.Parse(text).Intent==LocalIntent.Unknown&&!text.StartsWith(">")){string term=text;actions.Add(new SearchAction{Name="Buscar «"+text+"»",Detail="Abrir búsqueda en Chrome",Execute=delegate{desktop.RunAssistantCommand("Buscá "+term,commandTarget);}});}
            foreach(var action in actions)
            {
                var row=new Grid {Margin=new Thickness(4),Height=40,ClipToBounds=true};row.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(43)});row.ColumnDefinitions.Add(new ColumnDefinition());row.Children.Add(new Border {Width=30,Height=30,HorizontalAlignment=HorizontalAlignment.Left,ClipToBounds=true,Child=action.App==null?Design.Symbol(files?"folder":"search",26,Design.Muted):Design.AppIcon(action.App,30)});var label=new StackPanel{VerticalAlignment=VerticalAlignment.Center};label.Children.Add(Design.Text(action.Name,13,true));var sub=Design.Text(action.Detail,10,false);sub.SetResourceReference(TextBlock.ForegroundProperty,"GlassMuted");sub.Margin=new Thickness(0,4,0,0);label.Children.Add(sub);Grid.SetColumn(label,1);row.Children.Add(label);results.Items.Add(new ListBoxItem {Content=row,Tag=action,MaxHeight=60});
            }
            if(results.Items.Count>0)results.SelectedIndex=0;status.Text=files?(text.Length<2?"Escribí al menos 2 caracteres":actions.Count+" resultados · "+desktop.Files.Count+" archivos indexados"):actions.Count+" resultados · "+desktop.AppCount+" aplicaciones";
        }
    }
}

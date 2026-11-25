using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
namespace MacFlow
{
    public sealed class FinderWindow : FeaturePanel
    {
        private readonly DockPanel column=new DockPanel{Width=225,Visibility=Visibility.Collapsed,Margin=new Thickness(8,0,0,0)};
        private readonly ListBox children=new ListBox{DisplayMemberPath="Name",Background=Brushes.Transparent,BorderThickness=new Thickness(0)};
        private readonly TextBlock columnTitle=ModuleUI.Caption("");
        private CancellationTokenSource columnLoad;
        private string columnPath;
        private readonly Desktop desktop;private readonly FinderMetadata metadata;private readonly string metadataFile;private readonly ContentControl gallery=new ContentControl{Height=190,Visibility=Visibility.Collapsed,Margin=new Thickness(0,6,0,6)};private int previewGeneration;private readonly TextBox address,filter;private readonly ListView list=new ListView();private readonly TextBlock status;private readonly StackPanel breadcrumbs;private readonly ComboBox view;
        private List<StackEntry> entries=new List<StackEntry>();private CancellationTokenSource scan;private bool busy,closed;private int generation;private readonly Stack<string> back=new Stack<string>();
        public string FolderPath{get;private set;}internal int Count{get{return list.Items.Count;}}internal bool Ready{get;private set;}
        public FinderWindow(Desktop owner,string path):base("Finder",860,590)
        {
            desktop=owner;metadataFile=Path.Combine(desktop.Store.DirectoryPath,"finder.json");metadata=FinderMetadata.Load(metadataFile);MinWidth=560;MinHeight=380;
            var navigation=new DockPanel();var backButton=Design.Button("‹",delegate{if(back.Count>0)Navigate(back.Pop(),false);},false);backButton.ToolTip="Volver";DockPanel.SetDock(backButton,Dock.Left);navigation.Children.Add(backButton);
            var up=Design.Button("↑",delegate{if(FolderPath==null)return;var parent=Directory.GetParent(FolderPath);if(parent!=null)Navigate(parent.FullName);},false);up.ToolTip="Carpeta superior";DockPanel.SetDock(up,Dock.Left);navigation.Children.Add(up);
            address=ModuleUI.Input(path);address.PreviewKeyDown+=delegate(object s,KeyEventArgs e){if(e.Key==Key.Enter){Navigate(address.Text);e.Handled=true;}};navigation.Children.Add(address);DockPanel.SetDock(navigation,Dock.Top);Body.Children.Add(navigation);
            breadcrumbs=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(0,5,0,5)};DockPanel.SetDock(breadcrumbs,Dock.Top);Body.Children.Add(breadcrumbs);
            var tools=new WrapPanel();foreach(string command in new[]{"Nueva carpeta","Copiar","Mover","Renombrar","Papelera","Vista previa","Restaurar","Favorito","Etiqueta","Ocultos"}){string action=command;tools.Children.Add(Design.Button(command,delegate{Action(action);},false));}
            view=new ComboBox{ItemsSource=new[]{"Lista","Iconos","Galería","Columnas"},SelectedIndex=0,Margin=new Thickness(6),Width=100};view.SelectionChanged+=delegate{Render();};tools.Children.Add(view);DockPanel.SetDock(tools,Dock.Top);Body.Children.Add(tools);
            status=Caption("");DockPanel.SetDock(status,Dock.Bottom);Body.Children.Add(status);
            var sidebar=new StackPanel{Width=135,Margin=new Thickness(0,10,12,0)};
            Place(sidebar,"Escritorio",Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));Place(sidebar,"Documentos",Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));Place(sidebar,"Descargas",FolderStack.Downloads());
            sidebar.Children.Add(Design.Button("Favoritos",delegate{var choices=new List<GlassCommand>();foreach(string saved in metadata.Favorites.Where(Directory.Exists)){string place=saved;choices.Add(new GlassCommand(place,delegate{Navigate(place);}));}if(choices.Count==0){status.Text="Usá Favorito para guardar la carpeta actual.";return;}GlassMenu.Open(this,"Favoritos",choices,desktop.IsTesting);},false));
            foreach(var drive in DriveInfo.GetDrives().Where(d=>d.IsReady))Place(sidebar,drive.Name,drive.RootDirectory.FullName);
            sidebar.Children.Add(Design.Button("Elegir…",Choose,false));DockPanel.SetDock(sidebar,Dock.Left);Body.Children.Add(sidebar);
            var main=new DockPanel();filter=ModuleUI.Input("");filter.ToolTip="Filtrar esta carpeta";System.Windows.Automation.AutomationProperties.SetName(filter,"Filtrar archivos de esta carpeta");filter.TextChanged+=delegate{Render();};DockPanel.SetDock(filter,Dock.Top);main.Children.Add(filter);DockPanel.SetDock(gallery,Dock.Bottom);main.Children.Add(gallery);
            DockPanel.SetDock(columnTitle,Dock.Top);column.Children.Add(columnTitle);column.Children.Add(children);
            children.SetResourceReference(Control.ForegroundProperty,"GlassInk");
            System.Windows.Automation.AutomationProperties.SetName(children,"Contenido de la carpeta seleccionada");
            children.MouseDoubleClick+=delegate{OpenEntry(children.SelectedItem as StackEntry);};
            children.PreviewKeyDown+=delegate(object s,KeyEventArgs e){if(e.Key==Key.Enter){OpenEntry(children.SelectedItem as StackEntry);e.Handled=true;}else if(e.Key==Key.Left){list.Focus();e.Handled=true;}};
            DockPanel.SetDock(column,Dock.Right);main.Children.Add(column);
            list.SelectionMode=SelectionMode.Single;list.Background=Brushes.Transparent;list.SetResourceReference(Control.ForegroundProperty,"GlassInk");list.BorderThickness=new Thickness(0);VirtualizingStackPanel.SetIsVirtualizing(list,true);ScrollViewer.SetCanContentScroll(list,true);
            list.SelectionChanged+=delegate{UpdateGallery();UpdateColumn();};list.MouseDoubleClick+=delegate{OpenSelection();};list.PreviewKeyDown+=delegate(object s,KeyEventArgs e){if(e.Key==Key.Enter){OpenSelection();e.Handled=true;}else if(e.Key==Key.F5&&FolderPath!=null){Navigate(FolderPath,false);e.Handled=true;}else if(e.Key==Key.Right&&columnPath!=null&&children.Items.Count>0){children.SelectedIndex=0;children.Focus();e.Handled=true;}};main.Children.Add(list);Body.Children.Add(main);
            PreviewKeyDown+=delegate(object sender,KeyEventArgs e){if((Keyboard.Modifiers&ModifierKeys.Control)!=0&&e.Key==Key.L){address.Focus();address.SelectAll();e.Handled=true;}else if((Keyboard.Modifiers&ModifierKeys.Control)!=0&&e.Key==Key.F){filter.Focus();e.Handled=true;}else if(!(e.OriginalSource is TextBox)&&e.Key==Key.F2){Action("Renombrar");e.Handled=true;}else if(!(e.OriginalSource is TextBox)&&e.Key==Key.Delete){Action("Papelera");e.Handled=true;}};
            Closing+=delegate(object s,System.ComponentModel.CancelEventArgs e){if(busy){e.Cancel=true;status.Text="Esperá a que termine o cancelá el diálogo de Windows antes de cerrar.";}};
            Closed+=delegate{closed=true;ClearColumn();if(scan!=null){scan.Cancel();scan.Dispose();}};Navigate(path);
        }
        private void Place(StackPanel sidebar,string name,string path){sidebar.Children.Add(Design.Button(name,delegate{Navigate(path);},false));}
        private void Choose(){using(var dialog=new System.Windows.Forms.FolderBrowserDialog()){dialog.Description="Elegí una carpeta para Finder";if(dialog.ShowDialog()==System.Windows.Forms.DialogResult.OK)Navigate(dialog.SelectedPath);}}
        internal async void Navigate(string path,bool remember=true)
        {
            if(closed||busy)return;int token=++generation;Ready=false;if(scan!=null){scan.Cancel();scan.Dispose();}scan=new CancellationTokenSource();var cancel=scan.Token;
            entries.Clear();list.Items.Clear();++previewGeneration;gallery.Content=null;ClearColumn();breadcrumbs.Children.Clear();status.Text="Leyendo carpeta…";
            try{path=Path.GetFullPath(path);
                var values=await Task.Run(delegate{cancel.ThrowIfCancellationRequested();return metadata.Scan(path,cancel);},cancel);
                if(closed||token!=generation)return;
                if(remember&&FolderPath!=null&&!String.Equals(FolderPath,path,StringComparison.OrdinalIgnoreCase))back.Push(FolderPath);
                FolderPath=path;address.Text=path;entries=values;Ready=true;Crumbs();Render();
            }catch(OperationCanceledException){}catch(Exception ex){if(!closed&&token==generation){status.Text=ex.Message;Ready=true;}}
        }
        private void Crumbs(){breadcrumbs.Children.Clear();string full=FolderPath;var parts=new List<string>();while(!String.IsNullOrEmpty(full)){parts.Add(full);var parent=Directory.GetParent(full);full=parent==null?null:parent.FullName;}parts.Reverse();foreach(string part in parts.TakeLastCompat(5)){string target=part;var button=Design.Button(Path.GetFileName(part.TrimEnd('\\')),delegate{Navigate(target);},false);if(String.IsNullOrEmpty((string)button.Content))button.Content=part;button.FontSize=10;button.Padding=new Thickness(4,2,4,2);breadcrumbs.Children.Add(button);}}
        private void Render()
        {
            if(filter==null||view==null)return;
            column.Visibility=view.SelectedIndex==3?Visibility.Visible:Visibility.Collapsed;
            list.Items.Clear();ClearColumn();if(column.Visibility==Visibility.Visible)columnTitle.Text="Seleccioná una carpeta";
            if(!Ready)return;
            string query=filter.Text??"";var filtered=entries.Where(e=>e.Name.IndexOf(query,StringComparison.CurrentCultureIgnoreCase)>=0||metadata.Tag(e.Path).IndexOf(query,StringComparison.CurrentCultureIgnoreCase)>=0).ToArray();
            bool icons=view.SelectedIndex==1;gallery.Visibility=view.SelectedIndex==2?Visibility.Visible:Visibility.Collapsed;list.View=null;
            if(icons){var panel=new FrameworkElementFactory(typeof(WrapPanel));panel.SetValue(WrapPanel.OrientationProperty,Orientation.Horizontal);list.ItemsPanel=new ItemsPanelTemplate(panel);}
            else list.ItemsPanel=new ItemsPanelTemplate(new FrameworkElementFactory(typeof(VirtualizingStackPanel)));
            foreach(var item in filtered.Take(5000)){var row=new ListViewItem{Tag=item,Padding=new Thickness(6),HorizontalContentAlignment=HorizontalAlignment.Stretch};var content=new StackPanel{Orientation=icons?Orientation.Vertical:Orientation.Horizontal};content.Children.Add(Design.Text(item.Folder?"▣":"▤",icons?30:18,true));var label=Design.Text(item.Name,12,false);label.TextTrimming=TextTrimming.CharacterEllipsis;label.Margin=new Thickness(8,0,8,0);if(icons){label.Width=100;label.TextWrapping=TextWrapping.Wrap;row.Width=130;row.Height=100;}else label.Width=310;content.Children.Add(label);if(metadata.Tag(item.Path).Length>0)content.Children.Add(Caption("● "+metadata.Tag(item.Path)));if(!icons)content.Children.Add(Caption(item.Folder?"Carpeta":(item.Size/1024.0).ToString("N1")+" KB · "+item.Modified.ToLocalTime().ToString("g")));row.Content=content;System.Windows.Automation.AutomationProperties.SetName(row,item.Name+(item.Folder?" · carpeta":" · archivo")+" "+metadata.Tag(item.Path));list.Items.Add(row);}
            if(!busy)status.Text=filtered.Length+" elementos"+(filtered.Length>5000?" · se muestran los primeros 5000":"")+(view.SelectedIndex==2?" · vista previa integrada; Abrir usa la aplicación de Windows":"");
        }
        private async void UpdateGallery()
        {
            if(view.SelectedIndex!=2)return;int token=++previewGeneration;var item=Selected;gallery.Content=Caption(item==null?"Seleccioná un archivo para ver su contenido.":"Cargando vista previa…");if(item==null)return;if(item.Folder){gallery.Content=Caption("Carpeta · "+item.Name);return;}
            try{
                string extension=Path.GetExtension(item.Path).ToLowerInvariant();object preview=null;
                if(new[]{".png",".jpg",".jpeg",".bmp",".gif"}.Contains(extension)&&item.Size<25000000){preview=await Task.Run(delegate{var image=new System.Windows.Media.Imaging.BitmapImage();image.BeginInit();image.CacheOption=System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;image.DecodePixelWidth=700;image.UriSource=new Uri(item.Path);image.EndInit();image.Freeze();return image;});}
                else if(new[]{".txt",".md",".json",".csv",".log",".cs"}.Contains(extension)){preview=await Task.Run(delegate{using(var reader=new StreamReader(item.Path)){var text=new char[6000];int read=reader.ReadBlock(text,0,text.Length);return new string(text,0,read);}});}
                if(closed||token!=previewGeneration)return;var bitmap=preview as System.Windows.Media.Imaging.BitmapSource;
                if(bitmap!=null)gallery.Content=new Image{Source=bitmap,Stretch=Stretch.Uniform};else if(preview is string)gallery.Content=new ScrollViewer{Content=Caption((string)preview)};else gallery.Content=Caption("Sin vista previa integrada para este formato. Usá Abrir o Vista previa para consultar las opciones de Windows.");
            }catch(Exception ex){if(!closed&&token==previewGeneration)gallery.Content=Caption(ex.Message);}
        }
        private StackEntry Selected{get{var row=list.SelectedItem as ListViewItem;return row==null?null:row.Tag as StackEntry;}}
        private void ClearColumn()
        {
            if(columnLoad!=null){columnLoad.Cancel();columnLoad.Dispose();columnLoad=null;}
            columnPath=null;children.ItemsSource=null;columnTitle.Text="";
        }
        private async void UpdateColumn()
        {
            ClearColumn();
            if(view==null||view.SelectedIndex!=3)return;
            var entry=Selected;
            if(entry==null||!entry.Folder){columnTitle.Text=entry==null?"Seleccioná una carpeta":entry.Name+" · "+FilePreview.FormatSize(entry.Size);return;}
            columnPath=entry.Path;columnTitle.Text=entry.Name+" · Cargando…";
            columnLoad=new CancellationTokenSource();
            var token=columnLoad.Token;
            try
            {
                var items=await Task.Run(delegate{return metadata.Scan(entry.Path,token);},token);
                if(closed||token.IsCancellationRequested)return;
                children.ItemsSource=items.Take(5000).ToArray();
                columnTitle.Text=entry.Name+" · "+items.Count+" elementos"+(items.Count>5000?" (primeros 5000)":"");
            }
            catch(OperationCanceledException){}
            catch(Exception ex){if(!closed&&!token.IsCancellationRequested)columnTitle.Text=ex.Message;}
        }
        private void OpenSelection(){OpenEntry(Selected);}
        private void OpenEntry(StackEntry entry){if(entry==null)return;if(entry.Folder)Navigate(entry.Path);else try{System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(entry.Path){UseShellExecute=true});}catch(Exception ex){status.Text=ex.Message;}}
        internal void ExecuteCommand(string action){Action(action);}
        internal void SetView(int index){view.SelectedIndex=index;}
        private async void Action(string action)
        {
            if(busy||!Ready||FolderPath==null)return;var selected=Selected;
            if(action=="Ocultos"){metadata.ShowHidden=!metadata.ShowHidden;ModuleStore.Write(metadataFile,metadata);Navigate(FolderPath,false);return;}
            if(action=="Favorito"){if(metadata.Favorites.Contains(FolderPath))metadata.Favorites.Remove(FolderPath);else metadata.Favorites.Add(FolderPath);ModuleStore.Write(metadataFile,metadata);status.Text="Favoritos guardados para este perfil.";return;}
            if(action=="Etiqueta"){if(selected==null)return;string tag=NamePrompt.Ask(this,"Etiqueta de MacFlow",metadata.Tag(selected.Path));if(tag==null)return;tag=tag.Trim();if(tag.Length>48||tag.Any(Char.IsControl)){status.Text="La etiqueta admite hasta 48 caracteres.";return;}if(tag.Length==0)metadata.Tags.Remove(selected.Path);else metadata.Tags[selected.Path]=tag;ModuleStore.Write(metadataFile,metadata);Render();return;}
            if(action=="Vista previa"){if(selected!=null&&!selected.Folder)desktop.OpenPreview(selected.Path);return;}
            if(action=="Restaurar"){try{System.Diagnostics.Process.Start("explorer.exe","shell:RecycleBinFolder");status.Text="En la Papelera de Windows elegí el archivo y Restaurar.";}catch(Exception ex){status.Text=ex.Message;}return;}
            if(action=="Nueva carpeta"){string name=NamePrompt.Ask(this,"Nueva carpeta","");if(name!=null)await Operate(delegate{string destination=FinderFiles.ChildName(FolderPath,name);if(Directory.Exists(destination)||File.Exists(destination))throw new IOException("Ya existe un elemento con ese nombre.");Directory.CreateDirectory(destination);},"Creando carpeta");return;}
            if(selected==null){status.Text="Elegí un archivo o carpeta.";return;}
            if(action=="Renombrar"){string name=NamePrompt.Ask(this,"Renombrar",selected.Name);if(name!=null)await Operate(delegate{FinderFiles.Rename(selected.Path,name);},"Renombrando");return;}
            if(action=="Papelera"){if(MessageBox.Show(this,"¿Enviar «"+selected.Name+"» a la Papelera de Windows?","Finder",MessageBoxButton.YesNo,MessageBoxImage.Question)==MessageBoxResult.Yes)await Operate(delegate{FinderFiles.Trash(selected.Path);},"Enviando a la Papelera");return;}
            using(var dialog=new System.Windows.Forms.FolderBrowserDialog()){dialog.Description=action+" «"+selected.Name+"» a…";if(dialog.ShowDialog()!=System.Windows.Forms.DialogResult.OK)return;string destination=dialog.SelectedPath;await Operate(delegate{FinderFiles.Transfer(selected.Path,destination,action=="Mover");},action+" · "+selected.Name);}
        }
        private async Task Operate(System.Action operation,string description)
        {
            busy=true;status.Text=description;string id="finder-"+Guid.NewGuid().ToString("N");desktop.IslandState.Publish(id,description,"folder",IslandState.transfer,75,TimeSpan.FromHours(12));
            try{await FinderFiles.Run(operation);if(!closed)status.Text="Operación completada.";}catch(OperationCanceledException){if(!closed)status.Text="Operación cancelada.";}catch(Exception ex){if(!closed)status.Text=ex.Message;desktop.IslandState.Publish("finder-error",ex.Message,"folder",IslandState.error,100,TimeSpan.FromSeconds(5));}finally{desktop.IslandState.Remove(id);busy=false;if(!closed)Navigate(FolderPath,false);}
        }
        protected override bool CanCloseWindow(){return !busy;}
    }
    internal static class FinderEnumerable{internal static System.Collections.Generic.IEnumerable<T> TakeLastCompat<T>(this System.Collections.Generic.IEnumerable<T> values,int count){return values.Reverse().Take(count).Reverse();}}
    internal sealed class NamePrompt : FeaturePanel
    {
        private readonly TextBox value;private string answer;
        private NamePrompt(string heading,string initial):base(heading,380,220){MinHeight=180;ResizeMode=ResizeMode.NoResize;value=ModuleUI.Input(initial,240);Body.Children.Add(value);var ok=Design.Button("Aceptar",delegate{answer=value.Text;Close();},true);ok.IsDefault=true;DockPanel.SetDock(ok,Dock.Bottom);Body.Children.Add(ok);Body.Children.Remove(value);Body.Children.Add(value);Loaded+=delegate{value.Focus();value.SelectAll();};}
        internal static string Ask(Window owner,string title,string initial){var prompt=new NamePrompt(title,initial){Owner=owner,WindowStartupLocation=WindowStartupLocation.CenterOwner};prompt.ShowDialog();return prompt.answer;}
    }
}

using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
namespace MacFlow
{
    // Persisted Notch* preference names remain compatible with 27.2.1.
    // The actual visible surface is a floating Dynamic Island.
    internal static class NotchSurface
    {
        internal static Geometry Outline(double width,double height)
        {
            double w=Math.Max(60,width),h=Math.Max(24,height);
            double expansion=Math.Max(0,Math.Min(1,(h-34)/224));
            double radius=17+15*Motion.Smooth(expansion);
            var geometry=new RectangleGeometry(new Rect(0,0,w,h),radius,radius);geometry.Freeze();return geometry;
        }
        internal static double Width(Preferences p,double available,bool expanded,bool active)
        {return Math.Max(60,Math.Min(available-16,(expanded?416:active?226:152)*p.NotchScale));}
    }
    internal static class PreferencesIO
    {
        internal static Preferences Clone(Preferences value)
        {using(var stream=new MemoryStream()){new DataContractJsonSerializer(typeof(Preferences)).WriteObject(stream,value);stream.Position=0;return (Preferences)new DataContractJsonSerializer(typeof(Preferences)).ReadObject(stream);}}
        internal static Preferences Read(string path)
        {
            if(new FileInfo(path).Length>262144)throw new IOException("El archivo de ajustes supera 256 KB.");
            using(var stream=File.OpenRead(path)){var p=(Preferences)new DataContractJsonSerializer(typeof(Preferences)).ReadObject(stream);if(p==null)throw new IOException("No contiene preferencias válidas.");p.Validate();if(!String.IsNullOrWhiteSpace(p.UpdateFeedUrl)&&!UpdateService.ValidFeed(p.UpdateFeedUrl))throw new IOException("El canal de actualizaciones debe usar HTTPS.");return p;}
        }
        internal static void Write(string path,Preferences value)
        {using(var stream=File.Create(path))new DataContractJsonSerializer(typeof(Preferences)).WriteObject(stream,value);}
    }
    internal sealed class NotchShelfView : DockPanel
    {
        private readonly ShelfStore store;private readonly StackPanel rows=new StackPanel();private readonly TextBlock status;
        private bool busy,closed;
        internal NotchShelfView(Desktop desktop)
        {
            store=new ShelfStore(desktop.Store.DirectoryPath);Margin=new Thickness(32,22,32,32);
            var heading=new DockPanel();var add=Design.Button("Añadir…",Browse,false);add.Background=Design.Brush("#22FFFFFF");add.Foreground=Brushes.White;DockPanel.SetDock(add,Dock.Right);heading.Children.Add(add);heading.Children.Add(Design.Text("Shelf",16,true));DockPanel.SetDock(heading,Dock.Top);Children.Add(heading);
            status=Design.Text("Soltá archivos aquí · copias locales · hasta 128 MB",11,false);status.Foreground=Design.Brush("#A8ABB3");status.Margin=new Thickness(0,6,0,10);DockPanel.SetDock(status,Dock.Top);Children.Add(status);
            Children.Add(new ScrollViewer{Content=rows,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});Unloaded+=delegate{closed=true;};Loaded+=delegate{closed=false;Refresh();};
        }
        private void Browse(){var d=new Microsoft.Win32.OpenFileDialog{Multiselect=true,Title="Añadir al Shelf"};if(d.ShowDialog()==true)Add(d.FileNames);}
        internal async void Add(string[] paths)
        {if(busy||closed)return;busy=true;try{foreach(string path in paths.Take(32)){await store.Add(path);if(closed)return;}Refresh();status.Text="Copias listas · arrastrá para recuperar";}catch(Exception ex){if(!closed)status.Text=ex.Message;}finally{busy=false;}}
        private void Refresh()
        {
            rows.Children.Clear();foreach(string file in store.Files){string path=file;var line=new DockPanel();var remove=Design.Button("×",delegate{try{store.Remove(path);Refresh();}catch(Exception ex){status.Text=ex.Message;}},false);remove.Foreground=Brushes.White;remove.Background=Brushes.Transparent;remove.ToolTip="Retirar copia";DockPanel.SetDock(remove,Dock.Right);line.Children.Add(remove);var b=Design.Button(ShelfStore.Name(path),delegate{},false);b.Foreground=Brushes.White;b.Background=Design.Brush("#12FFFFFF");b.HorizontalContentAlignment=HorizontalAlignment.Left;Point start=new Point();b.PreviewMouseLeftButtonDown+=delegate(object s,MouseButtonEventArgs e){start=e.GetPosition(b);};b.MouseMove+=delegate(object s,MouseEventArgs e){if(e.LeftButton==MouseButtonState.Pressed&&(e.GetPosition(b)-start).Length>8)DragDrop.DoDragDrop(b,new DataObject(DataFormats.FileDrop,new[]{path}),DragDropEffects.Copy);};line.Children.Add(b);rows.Children.Add(line);}
            if(rows.Children.Count==0){var empty=Design.Text("Tu espacio temporal para archivos",14,false);empty.Foreground=Design.Brush("#90939B");empty.Margin=new Thickness(0,25,0,0);rows.Children.Add(empty);}
        }
    }
}

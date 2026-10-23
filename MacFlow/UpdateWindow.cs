using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
namespace MacFlow
{
    internal sealed class UpdateWindow:Window
    {
        private readonly string repository,localFolder,feed;
        private readonly CancellationTokenSource lifetime=new CancellationTokenSource();
        private readonly TextBlock status,notes;
        private readonly ProgressBar progress;
        private readonly Button check,local,download,install;
        private UpdatePackage package;
        private string prepared;
        private bool busy,closed;
        internal UpdateWindow(string channel,string folder="",string feedUrl="")
        {
            repository=channel;localFolder=folder;feed=feedUrl;Title="MacOS · Actualizaciones";Width=560;Height=470;WindowStyle=WindowStyle.None;AllowsTransparency=false;ResizeMode=ResizeMode.NoResize;Background=Brushes.Transparent;ShowInTaskbar=false;Topmost=true;
            var layout=new DockPanel();var header=new StackPanel();header.Children.Add(new WindowControls(this,delegate{Design.Dismiss(this);}){Margin=new Thickness(0,0,0,13)});header.Children.Add(Design.Text("Actualizaciones",25,true));header.Children.Add(Design.Text("MacOS "+Product.VersionText,12,false));DockPanel.SetDock(header,Dock.Top);layout.Children.Add(header);
            var actions=new StackPanel{Margin=new Thickness(0,15,0,0)};var row=new StackPanel{Orientation=Orientation.Horizontal};check=Design.Button("Buscar",Check,false);local=Design.Button("Elegir paquete…",PickLocal,false);row.Children.Add(check);row.Children.Add(local);actions.Children.Add(row);
            var next=new StackPanel{Orientation=Orientation.Horizontal};download=Design.Button("Descargar",Download,true);install=Design.Button("Actualizar ahora",Install,true);next.Children.Add(download);next.Children.Add(install);actions.Children.Add(next);DockPanel.SetDock(actions,Dock.Bottom);layout.Children.Add(actions);
            var body=new StackPanel{Margin=new Thickness(0,20,0,0)};status=Design.Text((UpdateService.ValidRepository(repository)||UpdateService.ValidFeed(feed))?"Buscá si hay una versión nueva disponible.":"El canal en línea todavía no está publicado. Podés actualizar con un paquete local.",14,true);status.TextWrapping=TextWrapping.Wrap;body.Children.Add(status);progress=new ProgressBar{Minimum=0,Maximum=100,Height=4,Margin=new Thickness(0,17,0,15),Visibility=Visibility.Collapsed};body.Children.Add(progress);
            notes=Design.Text("Tus preferencias se conservan. La bienvenida aparece una vez al cambiar de versión.",12,false);notes.TextWrapping=TextWrapping.Wrap;body.Children.Add(new ScrollViewer{Content=notes,MaxHeight=120,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});layout.Children.Add(body);
            if(UpdateService.HasChannel(repository,localFolder,feed)){status.Text="Buscando versiones nuevas en el canal conectado…";Loaded+=delegate{Check();};}
            Content=Design.Glass(layout,24,false);SourceInitialized+=delegate{Native.Glass(this,Design.Dark);};Closed+=delegate{closed=true;lifetime.Cancel();if(!busy)lifetime.Dispose();};KeyDown+=delegate(object s,KeyEventArgs e){if(e.Key==Key.Escape){e.Handled=true;Design.Dismiss(this);}};Controls();
        }
        private void Controls(){if(closed)return;check.IsEnabled=!busy;local.IsEnabled=!busy;download.IsEnabled=!busy&&package!=null&&prepared==null;install.IsEnabled=!busy&&prepared!=null;}
        private void SetBusy(bool value){busy=value;Controls();}
        private void Report(Exception ex){if(closed)return;status.Text=ex is OperationCanceledException?"Operación cancelada.":ex.Message;}
        private void Selected(UpdatePackage value)
        {package=value;prepared=null;progress.Value=0;progress.Visibility=Visibility.Collapsed;status.Text="Nueva versión "+value.Version+" disponible.";notes.Text=String.IsNullOrWhiteSpace(value.Notes)?"Se conservarán tus preferencias. Después de actualizar verás Hola una sola vez.":value.Notes.Substring(0,Math.Min(4000,value.Notes.Length));download.Content=value.LocalFile==null?"Descargar":"Preparar actualización";Controls();}
        private async void Check()
        {
            if(busy||closed)return;SetBusy(true);status.Text="Buscando actualizaciones…";
            try{if(!UpdateService.HasChannel(repository,localFolder,feed))throw new IOException("Todavía no hay un canal conectado. Podés elegir el paquete local.");var found=await UpdateService.Check(repository,localFolder,feed,lifetime.Token);if(closed)return;if(found==null){package=null;prepared=null;status.Text="No hay una versión más nueva en los canales conectados.";}else Selected(found);}
            catch(Exception ex){Report(ex);}finally{SetBusy(false);if(closed)lifetime.Dispose();}
        }
        private async void PickLocal()
        {
            if(busy||closed)return;var dialog=new Microsoft.Win32.OpenFileDialog{Title="Elegir paquete de actualización de MacOS",Filter="Instalador o paquete de MacFlow|*.exe;MacFlow-update.json|Instalador de MacFlow|*.exe|Manifiesto de MacFlow|MacFlow-update.json"};if(dialog.ShowDialog(this)!=true)return;SetBusy(true);
            try{var selected=await Task.Run(()=>String.Equals(Path.GetExtension(dialog.FileName),".exe",StringComparison.OrdinalIgnoreCase)?UpdateService.ReadInstaller(dialog.FileName,null,true,lifetime.Token):UpdateService.ReadLocal(dialog.FileName),lifetime.Token);if(!closed)Selected(selected);}
            catch(Exception ex){Report(ex);}finally{SetBusy(false);if(closed)lifetime.Dispose();}
        }
        private async void Download()
        {
            if(busy||closed||package==null)return;SetBusy(true);status.Text=package.LocalFile==null?"Descargando la nueva versión…":"Preparando el paquete…";progress.Visibility=Visibility.Visible;progress.Value=0;
            try
            {
                var cache=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MacFlow","Updates");var reporter=new Progress<int>(value=>{if(!closed)progress.Value=value;});var file=await UpdateService.Prepare(package,cache,reporter,lifetime.Token);if(closed)return;prepared=file;status.Text="Actualización lista. Integridad y versión comprobadas.";notes.Text="Actualizar ahora abre el asistente. MacOS se cerrará cuando confirmes la instalación y conservará tus ajustes.";
            }
            catch(Exception ex){prepared=null;Report(ex);}finally{SetBusy(false);if(closed)lifetime.Dispose();}
        }
        private async void Install()
        {
            if(busy||closed||prepared==null)return;SetBusy(true);status.Text="Comprobando el instalador…";
            try{await Task.Run(delegate{UpdateService.Verify(prepared,package,lifetime.Token);UpdateService.VerifyInstallerIdentity(prepared,package);},lifetime.Token);if(closed)return;Process.Start(new ProcessStartInfo(prepared,"--update"){UseShellExecute=true});status.Text="Asistente abierto. Confirmá la actualización allí.";}
            catch(Exception ex){Report(ex);}finally{SetBusy(false);if(closed)lifetime.Dispose();}
        }
        internal void SetPackageForTest(UpdatePackage value){Selected(value);}
    }
}

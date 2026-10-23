using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace MacFlow
{
    internal static class IconsVisual
    {
        internal static int Run(string directory)
        {
            directory=Path.GetFullPath(directory);Directory.CreateDirectory(directory);Log.DirectoryPath=directory;var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};Window panel=null;
            app.Startup+=async delegate
            {
                try
                {
                    Design.InstallTheme();foreach(bool dark in new[]{false,true})
                    {
                        Design.Dark=dark;Design.InstallTheme();var root=new StackPanel{Margin=new Thickness(24)};root.Children.Add(Design.Text("MacOS · Iconos",22,true));
                        var artwork=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(0,16,0,20)};root.Children.Add(artwork);
                        foreach(var key in new[]{"folder","grid","windows","settings","focus","preview","macflow"})
                        {var item=new StackPanel{Width=86};item.Children.Add(new Image{Source=FeatureIcons.Get(key),Width=48,Height=48});var label=Design.Text(key,10,false);label.TextAlignment=TextAlignment.Center;label.Margin=new Thickness(0,8,0,0);item.Children.Add(label);artwork.Children.Add(item);}
                        var symbols=new WrapPanel{Width=630};root.Children.Add(symbols);string[] names={"wifi","wifi-low","wifi-off","bluetooth","volume","volume-low","volume-off","control","search","grid","display","sun","moon","settings","windows","power","play","folder","check","close","appearance","wallpaper","dock","topbar","performance","network"};
                        foreach(var key in names)
                        {var item=new StackPanel{Width=70,Height=64};var symbol=(FrameworkElement)Design.Symbol(key,22,Design.Ink);symbol.HorizontalAlignment=HorizontalAlignment.Center;item.Children.Add(symbol);var label=Design.Text(key,9,false);label.TextAlignment=TextAlignment.Center;label.Margin=new Thickness(0,8,0,0);item.Children.Add(label);symbols.Children.Add(item);}
                        panel=new Window{Title="MacOS · Prueba de iconos",Width=680,Height=420,Left=70,Top=70,WindowStyle=WindowStyle.None,ResizeMode=ResizeMode.NoResize,ShowInTaskbar=false,ShowActivated=false,Topmost=true,Background=dark?Design.Brush("#17202B"):Design.Brush("#F1F4F8"),Content=root};panel.SourceInitialized+=delegate{Native.NoActivate(panel);};panel.Show();await Task.Delay(220);await Program.CaptureOwnWindow(panel,Path.Combine(directory,dark?"iconos-oscuro.png":"iconos-claro.png"));panel.Close();panel=null;
                    }
                    File.WriteAllText(Path.Combine(directory,"visual-test.txt"),"PASS 26 símbolos originales y 7 accesos vectoriales renderizados en claro y oscuro, incluido Configuración. Fixture de controles reales sin acciones del sistema.");app.Shutdown();
                }
                catch(Exception ex){File.WriteAllText(Path.Combine(directory,"visual-test.txt"),"FAIL "+ex);if(panel!=null)panel.Close();app.Shutdown(1);}
            };return app.Run();
        }
    }
}

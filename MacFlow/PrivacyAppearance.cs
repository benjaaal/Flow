using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace MacFlow
{
    // Preferences affect only MacFlow's recoverable visual curtain.
    internal sealed class PrivacyAppearance : Window
    {
        private static PrivacyAppearance current;
        private readonly Desktop desktop;private readonly Preferences draft;
        private readonly Image picture=new Image{Stretch=Stretch.UniformToFill};
        private readonly PrivacyClock clock=new PrivacyClock{Width=300,Height=95};
        private readonly TextBlock status;private string path,weight,color;private double scale;private bool closed;private int request;
        internal static PrivacyAppearance Open(Desktop desktop,Preferences draft)
        {if(WindowLife.Alive(current)){current.Activate();return current;}var view=new PrivacyAppearance(desktop,draft);current=view;view.Closed+=delegate{if(current==view)current=null;};DisplaySpace.Center(view,.15);view.Show();Design.Reveal(view);return view;}
        internal PrivacyAppearance(Desktop controller,Preferences preferences)
        {
            desktop=controller;draft=preferences;draft.Validate();path=draft.PrivacyWallpaperPath;weight=draft.PrivacyClockWeight;color=draft.PrivacyClockColor;scale=draft.PrivacyClockScale;
            Title="MacFlow · Personalizar pantalla";Width=650;Height=650;WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;ShowInTaskbar=false;Topmost=true;Background=Brushes.Transparent;WindowLife.Track(this);
            var content=new StackPanel();var header=new DockPanel();var close=Design.Button("Cerrar",delegate{Design.Dismiss(this);},false);DockPanel.SetDock(close,Dock.Right);header.Children.Add(close);header.Children.Add(Design.Text("Tu pantalla",24,true));content.Children.Add(header);
            var scene=new Grid{Height=230,Margin=new Thickness(0,16,0,18),ClipToBounds=true};scene.Children.Add(picture);scene.Children.Add(new Border{Background=Design.Brush("#28000000")});var heading=new StackPanel{HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Top,Margin=new Thickness(0,24,0,0)};var date=Design.Text(DateTime.Now.ToString("dddd d 'de' MMMM"),13,true);date.Foreground=Brushes.White;date.HorizontalAlignment=HorizontalAlignment.Center;heading.Children.Add(date);heading.Children.Add(clock);scene.Children.Add(heading);scene.SizeChanged+=delegate{scene.Clip=new RectangleGeometry(new Rect(0,0,scene.ActualWidth,scene.ActualHeight),22,22);};content.Children.Add(scene);
            var images=new WrapPanel();images.Children.Add(Design.Button("Elegir imagen…",Pick,false));images.Children.Add(Design.Button("Usar fondo del escritorio",delegate{path="";LoadPicture();},false));content.Children.Add(images);
            var row=new DockPanel{Margin=new Thickness(0,16,0,10)};var styles=new ComboBox{Width=150,ItemsSource=new[]{"Fino","Medio","Seminegrita","Negrita"},SelectedIndex=weight=="Regular"?0:weight=="Medium"?1:weight=="Bold"?3:2};styles.SelectionChanged+=delegate{weight=styles.SelectedIndex==0?"Regular":styles.SelectedIndex==1?"Medium":styles.SelectedIndex==3?"Bold":"Semibold";UpdateClock();};DockPanel.SetDock(styles,Dock.Right);row.Children.Add(styles);row.Children.Add(Design.Text("Grosor del reloj",13,true));content.Children.Add(row);
            var sizes=new Slider{Minimum=.75,Maximum=1.25,Value=scale,Margin=new Thickness(0,4,0,10)};System.Windows.Automation.AutomationProperties.SetName(sizes,"Tamaño del reloj");sizes.ValueChanged+=delegate{scale=sizes.Value;UpdateClock();};content.Children.Add(Design.Text("Tamaño del reloj",12,false));content.Children.Add(sizes);
            var swatches=new WrapPanel();foreach(var choice in new[]{"#FFFFFF","#FFE6C7","#E0E8FF","#E0FFF2"}){string ink=choice;var button=Design.Button("",delegate{color=ink;UpdateClock();},false);button.Width=34;button.Height=34;button.Margin=new Thickness(0,0,10,0);button.Padding=new Thickness(6);button.Content=new System.Windows.Shapes.Ellipse{Width=20,Height=20,Fill=Design.Brush(ink)};System.Windows.Automation.AutomationProperties.SetName(button,"Color de reloj "+ink);swatches.Children.Add(button);}content.Children.Add(swatches);
            status=Design.Text("Estos ajustes cambian únicamente la pantalla visual de MacFlow.",11,false);status.TextWrapping=TextWrapping.Wrap;status.Margin=new Thickness(0,14,0,12);content.Children.Add(status);
            var actions=new WrapPanel{HorizontalAlignment=HorizontalAlignment.Right};actions.Children.Add(Design.Button("Vista previa",delegate{var preview=new PrivacyScreen(desktop,true,null,Snapshot()){Width=760,Height=650};DisplaySpace.Center(preview,.10);preview.Show();},false));actions.Children.Add(Design.Button("Guardar",Save,true));content.Children.Add(actions);
            Content=Design.Glass(new ScrollViewer{Content=content,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled},26,false);SourceInitialized+=delegate{Native.Glass(this,Design.Dark);};Closed+=delegate{closed=true;request++;};PreviewKeyDown+=delegate(object sender,System.Windows.Input.KeyEventArgs e){if(e.Key==System.Windows.Input.Key.Escape){e.Handled=true;Design.Dismiss(this);}};clock.Set(DateTime.Now.ToString("HH:mm"));UpdateClock();LoadPicture();
        }
        private Preferences Snapshot(){return new Preferences{PrivacyWallpaperPath=path,PrivacyClockColor=color,PrivacyClockWeight=weight,PrivacyClockScale=scale,WallpaperPath=desktop.Preferences.WallpaperPath};}
        private void UpdateClock(){clock.Appearance(weight,color);clock.Width=300*scale;clock.Height=95*scale;}
        private async void LoadPicture(){int ticket=++request;var source=await WelcomeWallpaper.Load(String.IsNullOrWhiteSpace(path)?desktop.Preferences.WallpaperPath:path);if(!closed&&ticket==request)picture.Source=source;}
        private void Pick(){var dialog=new Microsoft.Win32.OpenFileDialog{Filter="Imágenes|*.jpg;*.jpeg;*.png;*.bmp",CheckFileExists=true};if(dialog.ShowDialog(this)==true){path=dialog.FileName;LoadPicture();}}
        internal void Save(){try{Copy(Snapshot(),draft);Copy(draft,desktop.Preferences);desktop.Save();status.Text="Guardado. Se usará al abrir la pantalla de MacFlow.";}catch(Exception ex){status.Text="No se pudo guardar: "+ex.Message;}}
        internal static void Copy(Preferences source,Preferences target){target.PrivacyWallpaperPath=source.PrivacyWallpaperPath;target.PrivacyClockWeight=source.PrivacyClockWeight;target.PrivacyClockColor=source.PrivacyClockColor;target.PrivacyClockScale=source.PrivacyClockScale;}
    }
}

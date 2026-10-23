using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace MacFlow
{
    // Compact desktop notification; foreground never passes through the blur.
    internal sealed class LockNotice : Border
    {
        internal readonly TranslateTransform Shift=new TranslateTransform();
        private readonly WallpaperGlass material;private readonly TextBlock title,detail,age;private readonly Border icon;
        internal LockNotice(FrameworkElement wallpaper,Action dismiss)
        {
            Margin=new Thickness(0,0,0,9);RenderTransform=Shift;
            var row=new Grid();row.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(42)});row.ColumnDefinitions.Add(new ColumnDefinition());
            icon=new Border{Width=30,Height=30,CornerRadius=new CornerRadius(8),VerticalAlignment=VerticalAlignment.Top,Margin=new Thickness(0,2,12,0),Child=new Image{Source=FeatureIcons.Get("macflow"),Stretch=Stretch.Uniform}};row.Children.Add(icon);
            var body=new StackPanel();Grid.SetColumn(body,1);row.Children.Add(body);
            var header=new DockPanel();age=Text("",10,false);age.Opacity=.62;age.Margin=new Thickness(8,1,0,0);DockPanel.SetDock(age,Dock.Right);header.Children.Add(age);title=Text("",12,true);title.TextTrimming=TextTrimming.CharacterEllipsis;header.Children.Add(title);body.Children.Add(header);
            detail=Text("",12,false);detail.TextWrapping=TextWrapping.Wrap;detail.TextTrimming=TextTrimming.CharacterEllipsis;detail.MaxHeight=36;detail.Opacity=.88;detail.Margin=new Thickness(0,4,0,0);body.Children.Add(detail);
            var root=new Grid();material=new WallpaperGlass(wallpaper,row,20,new Thickness(14,12,16,13),true);root.Children.Add(material);
            var close=Design.Button("×",delegate{dismiss();},false);close.Width=close.Height=22;close.MinHeight=0;close.Padding=new Thickness(0);close.Foreground=Brushes.White;close.Background=Design.Brush("#700C1018");close.HorizontalAlignment=HorizontalAlignment.Right;close.VerticalAlignment=VerticalAlignment.Top;close.Margin=new Thickness(0,-4,-4,0);close.Opacity=0;close.Focusable=true;close.ToolTip="Descartar aviso";System.Windows.Automation.AutomationProperties.SetName(close,"Descartar aviso");root.Children.Add(close);
            MouseEnter+=delegate{Motion.Fade(close,close.Opacity,1,120);};MouseLeave+=delegate{if(!close.IsKeyboardFocused)Motion.Fade(close,close.Opacity,0,120);};close.GotKeyboardFocus+=delegate{close.Opacity=1;};close.LostKeyboardFocus+=delegate{if(!IsMouseOver)close.Opacity=0;};Child=root;
        }
        private static TextBlock Text(string value,double size,bool bold){var text=Design.Text(value,size,bold);text.Foreground=Brushes.White;return text;}
        internal void RefreshBackdrop(){material.Update();}
        internal void Update(ActivityItem item,string message)
        {title.Text=item.Title;detail.Text=message;double minutes=Math.Max(0,(DateTime.UtcNow-item.CreatedUtc).TotalMinutes);age.Text=minutes<1?"Ahora":minutes<60?((int)minutes)+" min":((int)(minutes/60))+" h";if(item.External)icon.Child=Design.Symbol("bell",20,Brushes.White);}
    }
}

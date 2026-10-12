using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Forms=System.Windows.Forms;
namespace MacFlow
{
    public sealed class GlassCommand
    {
        public string Label;public Action Action;public string Icon;
        public GlassCommand(string label,Action action,string icon=null){Label=label;Action=action;Icon=icon;}
    }
    public sealed class GlassMenu : Window
    {
        public static GlassMenu Current;
        internal Window Anchor;
        private bool closing;
        public GlassMenu(string title,IList<GlassCommand> commands,bool dismissOnDeactivate=true)
        {
            Title="MacFlow · Menú";Width=286;Height=Math.Min(430,commands.Count*34+68);WindowStyle=WindowStyle.None;AllowsTransparency=false;ResizeMode=ResizeMode.NoResize;Topmost=true;ShowInTaskbar=false;Background=Brushes.Transparent;
            var root=new DockPanel();var heading=Design.Text(title,11,true);heading.Margin=new Thickness(10,0,0,10);DockPanel.SetDock(heading,System.Windows.Controls.Dock.Top);root.Children.Add(heading);var rows=new StackPanel();
            foreach(var command in commands){var captured=command;var b=Design.Button("",delegate {Design.Dismiss(this,captured.Action);},false);var text=Design.Text(command.Label,12,false);text.HorizontalAlignment=HorizontalAlignment.Left;if(String.IsNullOrEmpty(command.Icon))b.Content=text;else{var row=new DockPanel();row.Children.Add(new Border{Child=Design.Symbol(command.Icon,17,Design.Ink),Margin=new Thickness(0,0,12,0),VerticalAlignment=VerticalAlignment.Center});text.VerticalAlignment=VerticalAlignment.Center;row.Children.Add(text);b.Content=row;}b.Background=Brushes.Transparent;b.BorderThickness=new Thickness(0);b.Margin=new Thickness(0,1,0,1);b.Padding=new Thickness(9,6,9,6);b.HorizontalContentAlignment=HorizontalAlignment.Left;rows.Children.Add(b);}
            root.Children.Add(new ScrollViewer {Content=rows,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled});var glass=Design.Glass(root,22,false);glass.Padding=new Thickness(10);Content=glass;
            SourceInitialized+=delegate {Native.Glass(this,Design.Dark);};KeyDown+=delegate(object sender,KeyEventArgs e){if(e.Key==Key.Escape&&!closing){e.Handled=true;closing=true;Design.Dismiss(this);}};Closing+=delegate {closing=true;};Deactivated+=delegate {if(dismissOnDeactivate&&!closing){closing=true;Design.Dismiss(this);}};Closed+=delegate {if(Current==this)Current=null;};
        }
        public static void Open(Window anchor,string title,IList<GlassCommand> commands,bool nonInteractive=false)
        {
            if(Current!=null)Design.Dismiss(Current);var menu=new GlassMenu(title,commands,!nonInteractive);menu.Anchor=anchor;Current=menu;
            if(anchor is Launchpad)menu.Owner=anchor;
            if(nonInteractive){menu.ShowActivated=false;menu.IsHitTestVisible=false;menu.SourceInitialized+=delegate{Native.NoActivate(menu);};}
            var source=PresentationSource.FromVisual(anchor);var matrix=source==null?Matrix.Identity:source.CompositionTarget.TransformFromDevice;var cursor=Forms.Cursor.Position;var pos=matrix.Transform(new Point(cursor.X,cursor.Y));var screen=Forms.Screen.FromPoint(cursor).WorkingArea;var top=matrix.Transform(new Point(screen.Left,screen.Top));var bottom=matrix.Transform(new Point(screen.Right,screen.Bottom));
            menu.Left=Math.Max(top.X+8,Math.Min(pos.X-12,bottom.X-menu.Width-8));menu.Top=Math.Max(top.Y+8,Math.Min(pos.Y+9,bottom.Y-menu.Height-8));menu.Show();Design.Reveal(menu);
        }
    }
}

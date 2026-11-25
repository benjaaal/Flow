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
        internal bool IsAppleMenu{get;private set;}
        private readonly AppleMenuGlass appleGlass;
        private bool closing;
        private double restingTop;
        public GlassMenu(string title,IList<GlassCommand> commands,bool dismissOnDeactivate=true,bool apple=false)
        {
            IsAppleMenu=apple;
            Title=apple?"MacFlow · Menú principal":"MacOS · Menú";Width=apple?304:270;Height=apple?Math.Min(515,commands.Count*39+75):Math.Min(430,commands.Count*32+60);WindowStyle=WindowStyle.None;AllowsTransparency=apple;ResizeMode=ResizeMode.NoResize;Topmost=true;ShowInTaskbar=false;Background=Brushes.Transparent;
            var root=new DockPanel();var heading=Design.Text(title,11,true);heading.Margin=new Thickness(10,0,0,10);DockPanel.SetDock(heading,System.Windows.Controls.Dock.Top);root.Children.Add(heading);var rows=new StackPanel();
            foreach(var command in commands){var captured=command;var b=Design.Button("",delegate {HideMenu(captured.Action);},false);var text=Design.Text(command.Label,apple?13:12,false);text.HorizontalAlignment=HorizontalAlignment.Left;if(String.IsNullOrEmpty(command.Icon))b.Content=text;else{var row=new DockPanel();row.Children.Add(new Border{Child=Design.Symbol(command.Icon,17,Design.Ink),Margin=new Thickness(0,0,12,0),VerticalAlignment=VerticalAlignment.Center});text.VerticalAlignment=VerticalAlignment.Center;row.Children.Add(text);b.Content=row;}b.Background=Brushes.Transparent;b.BorderThickness=new Thickness(0);b.Margin=new Thickness(0,1,0,1);b.Padding=new Thickness(10,6,10,6);b.HorizontalContentAlignment=HorizontalAlignment.Left;System.Windows.Automation.AutomationProperties.SetName(b,command.Label);rows.Children.Add(b);}
            root.Children.Add(new ScrollViewer {Content=rows,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled});
            if(apple){heading.Margin=new Thickness(11,4,0,8);appleGlass=new AppleMenuGlass(root);Content=appleGlass;}
            else{var glass=Design.Glass(root,13,false);glass.Padding=new Thickness(8);Content=glass;SourceInitialized+=delegate {Native.Glass(this,Design.Dark);};}
            KeyDown+=delegate(object sender,KeyEventArgs e){if(e.Key==Key.Escape&&!closing){e.Handled=true;HideMenu();}};
            Closing+=delegate {closing=true;};
            Deactivated+=delegate {if(dismissOnDeactivate&&!closing)HideMenu();};
            Closed+=delegate {if(Current==this)Current=null;};
        }
        private void HideMenu(Action after=null)
        {
            if(closing)return;
            closing=true;
            if(!IsAppleMenu){Design.Dismiss(this,after);return;}
            Motion.To(this,OpacityProperty,0,135,true,delegate{if(closing&&WindowLife.Alive(this)){Close();if(after!=null)after();}},null,true);
        }
        private void ShowApple()
        {
            closing=false;
            if(ShowActivated&&!IsActive)Activate();
            if(Opacity<.999)Motion.To(this,OpacityProperty,1,150,false,null,null,true);
            else{Motion.Cancel(this,OpacityProperty);Opacity=1;}
            Motion.MenuBounce(this,restingTop);
        }
        internal static void ToggleApple(Window anchor,FrameworkElement trigger,IList<GlassCommand> commands,bool nonInteractive)
        {
            if(Current!=null&&Current.IsAppleMenu&&Current.Anchor==anchor&&Current.IsVisible)
            {
                if(Current.closing)Current.ShowApple();else Current.HideMenu();
                return;
            }
            if(Current!=null)Current.HideMenu();
            var menu=new GlassMenu("MacFlow",commands,!nonInteractive,true){Anchor=anchor,ShowActivated=!nonInteractive,WindowStartupLocation=WindowStartupLocation.Manual};
            Current=menu;
            if(nonInteractive){menu.IsHitTestVisible=false;menu.SourceInitialized+=delegate{Native.NoActivate(menu);};}
            var display=DisplaySpace.For(anchor);
            var work=display.WorkingPixels;
            var point=trigger!=null&&trigger.IsLoaded?trigger.PointToScreen(new Point(0,trigger.ActualHeight)):anchor.PointToScreen(new Point(10,Desktop.TopBarHeight));
            menu.Width=Math.Min(menu.Width,Math.Max(1,display.WorkingDips.Width-16));menu.Height=Math.Min(menu.Height,Math.Max(1,display.WorkingDips.Height-24));
            double left=Math.Max(work.Left+8*display.Scale,Math.Min(point.X,work.Right-(menu.Width+8)*display.Scale));
            double top=Math.Max(work.Top+10*display.Scale,Math.Min(point.Y+6*display.Scale,work.Bottom-(menu.Height+10)*display.Scale));
            double offset=Motion.Enabled?7:0;
            int sampleLeft=(int)Math.Round(left+5*display.Scale),sampleTop=(int)Math.Round(top+3*display.Scale);
            menu.appleGlass.Sample(sampleLeft,sampleTop,(int)Math.Round((menu.Width-10)*display.Scale),(int)Math.Round((menu.Height-10)*display.Scale));
            menu.Tag="Material sólido";
            DisplaySpace.Place(menu,display,(left-display.Pixels.Left)/display.Scale,(top-display.Pixels.Top)/display.Scale-offset);
            menu.Show();
            menu.restingTop=top/display.Scale;
            menu.Top=menu.restingTop-offset;
            menu.ShowApple();
        }
        public static void Open(Window anchor,string title,IList<GlassCommand> commands,bool nonInteractive=false)
        {
            if(Current!=null)Current.HideMenu();var menu=new GlassMenu(title,commands,!nonInteractive);menu.Anchor=anchor;Current=menu;
            if(anchor is Launchpad)menu.Owner=anchor;
            if(nonInteractive){menu.ShowActivated=false;menu.IsHitTestVisible=false;menu.SourceInitialized+=delegate{Native.NoActivate(menu);};}
            var source=PresentationSource.FromVisual(anchor);var matrix=source==null?Matrix.Identity:source.CompositionTarget.TransformFromDevice;var cursor=Forms.Cursor.Position;var pos=matrix.Transform(new Point(cursor.X,cursor.Y));var screen=Forms.Screen.FromPoint(cursor).WorkingArea;var top=matrix.Transform(new Point(screen.Left,screen.Top));var bottom=matrix.Transform(new Point(screen.Right,screen.Bottom));
            menu.Left=Math.Max(top.X+8,Math.Min(pos.X-12,bottom.X-menu.Width-8));menu.Top=Math.Max(top.Y+8,Math.Min(pos.Y+9,bottom.Y-menu.Height-8));menu.Show();Design.Reveal(menu);
        }
    }
}

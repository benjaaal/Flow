using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
namespace MacFlow
{
    // A separate, interruptible sphere-to-band surface. Never occupies the notch.
    internal sealed class AssistantPresence : IDisposable
    {
        private readonly Desktop desktop;private readonly Window window;private readonly Border shell;private readonly AssistantVisual lens;private readonly TextBlock text;private readonly StackPanel actions;private readonly IslandMotion motion;private readonly DispatcherTimer exit;
        private Window anchor;private bool disposed;
        internal Window View{get{return window;}}internal FrameworkElement Surface{get{return shell;}}
        internal bool ActionsAvailable{get{return actions.IsVisible&&actions.IsHitTestVisible;}}
        internal bool Rendering{get{return motion.Running||lens.Rendering;}}
        internal AssistantPresence(Desktop desktop)
        {
            this.desktop=desktop;anchor=desktop.ActivityBar;
            window=new Window{Title="MacOS · Presencia del asistente",WindowStyle=WindowStyle.None,ResizeMode=ResizeMode.NoResize,AllowsTransparency=true,Background=Brushes.Transparent,ShowInTaskbar=false,ShowActivated=false,Topmost=true,Width=464,Height=112};
            var layers=new Grid();lens=new AssistantVisual{Ribbon=true};layers.Children.Add(lens);
            text=Design.Text("",12,false);text.Foreground=Brushes.White;text.TextTrimming=TextTrimming.CharacterEllipsis;text.VerticalAlignment=VerticalAlignment.Bottom;text.Margin=new Thickness(24,0,90,18);layers.Children.Add(text);
            actions=new StackPanel{Orientation=Orientation.Horizontal,VerticalAlignment=VerticalAlignment.Bottom,HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,0,16,12)};
            foreach(string symbol in new[]{"keyboard","close"}){string key=symbol;var button=Design.Button("",delegate{if(key=="keyboard")desktop.ShowAssistantInput();else desktop.StopAssistantVoice();},false);button.Content=Design.Symbol(symbol,16,Brushes.White);button.Width=button.Height=28;button.MinHeight=0;button.Padding=new Thickness(5);button.Margin=new Thickness(2);button.Background=Brushes.Transparent;button.ToolTip=symbol=="keyboard"?"Escribir una orden":"Detener asistente";System.Windows.Automation.AutomationProperties.SetName(button,(string)button.ToolTip);actions.Children.Add(button);}layers.Children.Add(actions);
            shell=new Border{Background=Brushes.Transparent,Child=layers,Width=92,Height=92,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Top};window.Content=shell;WindowLife.Track(window);motion=new IslandMotion(window,shell,Center,false,true);
            shell.SizeChanged+=delegate{UpdateContent();};
            window.SourceInitialized+=delegate{Native.NoActivate(window);Native.RemoveGlassFrame(window);};window.KeyDown+=delegate(object sender,System.Windows.Input.KeyEventArgs e){if(e.Key==System.Windows.Input.Key.Escape)desktop.StopAssistantVoice();};
            exit=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(280)};exit.Tick+=delegate{exit.Stop();window.Hide();lens.Set(AssistantStage.Hidden);motion.Stop();};
        }
        private void UpdateContent(){double opacity=Math.Max(0,Math.Min(1,(shell.Width/shell.Height-1.7)/2));text.Opacity=actions.Opacity=opacity;actions.IsHitTestVisible=opacity>.85;}
        private void Center(){var area=DisplaySpace.For(anchor);double frame=Math.Min(464,area.Width-16);if(window.Width!=frame)window.Width=frame;window.Left=area.Dips.Left+(area.Width-window.Width)/2;}
        internal void Update(AssistantStage stage,string message)
        {
            if(disposed)return;exit.Stop();
            if(stage==AssistantStage.Hidden){if(!window.IsVisible)return;lens.Set(AssistantStage.Ready);text.Visibility=actions.Visibility=Visibility.Collapsed;motion.To(42,42,window.Top+10);Motion.Fade(window,window.Opacity,0,240);if(Motion.Enabled)exit.Start();else{window.Hide();lens.Set(AssistantStage.Hidden);}return;}
            anchor=desktop.ActivityBar;bool orb=stage==AssistantStage.Appearance||stage==AssistantStage.Listening;var area=DisplaySpace.For(anchor);double top=area.Dips.Top+Desktop.TopBarReserve+16;
            window.BeginAnimation(UIElement.OpacityProperty,null);window.Opacity=1;text.Text=message??"";text.Visibility=actions.Visibility=orb?Visibility.Collapsed:Visibility.Visible;
            if(!window.IsVisible){window.Top=top;Center();window.Show();Motion.Fade(window,0,1,200);}lens.Set(stage);UpdateContent();motion.To(orb?92:Math.Min(440,area.Width-32),92,top);
            System.Windows.Automation.AutomationProperties.SetName(window,"Asistente · "+(message??stage.ToString()));
        }
        public void Dispose(){if(disposed)return;disposed=true;exit.Stop();motion.Dispose();lens.Dispose();WindowLife.Close(window);}
    }
}

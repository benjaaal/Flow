using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
namespace MacFlow
{
    internal sealed class GlassNotifications : IDisposable
    {
        private readonly List<Window> windows=new List<Window>();private readonly Action open;private bool disposed;
        private readonly Dictionary<Window,DisplayArea> areas=new Dictionary<Window,DisplayArea>();
        internal int Count{get{return windows.Count;}}
        internal GlassNotifications(Action open){this.open=open;}
        internal Window Show(string title,string message,bool testing=false)
        {
            if(disposed)return null;if(windows.Count>=3)windows[0].Close();
            if(!testing)SoundCue.Play("notice");
            var window=Design.Panel("MacFlow · Notificación",350,100);window.ShowActivated=false;var root=new DockPanel();var close=Design.Button("×",null,false);close.Width=22;close.Height=22;close.VerticalAlignment=VerticalAlignment.Top;close.Padding=new Thickness(0);close.Margin=new Thickness(0);close.Background=Brushes.Transparent;close.ToolTip="Descartar notificación";System.Windows.Automation.AutomationProperties.SetName(close,"Descartar notificación");DockPanel.SetDock(close,Dock.Right);root.Children.Add(close);
            var text=new StackPanel();var heading=Design.Text(title,13,true);text.Children.Add(heading);var body=Design.Text(message,12,false);body.TextWrapping=TextWrapping.Wrap;body.TextTrimming=TextTrimming.CharacterEllipsis;body.MaxHeight=51;body.Margin=new Thickness(0,6,0,0);body.SetResourceReference(TextBlock.ForegroundProperty,"GlassMuted");text.Children.Add(body);root.Children.Add(text);var plate=Design.Glass(root,22,false);plate.Padding=new Thickness(17);window.Content=plate;
            var timeout=new DispatcherTimer(DispatcherPriority.Background){Interval=TimeSpan.FromSeconds(5)};bool closing=false;
            Action dismiss=delegate{if(closing||!window.IsVisible)return;closing=true;timeout.Stop();double top=window.Top;if(!Motion.Enabled){window.Close();return;}var slide=new DoubleAnimation(top,top-32,TimeSpan.FromMilliseconds(230)){EasingFunction=Motion.Ease(true)};slide.Completed+=delegate{window.Close();};Timeline.SetDesiredFrameRate(slide,60);window.BeginAnimation(Window.TopProperty,slide);Motion.Fade(window,1,0,230);};
            close.Click+=delegate{dismiss();};timeout.Tick+=delegate{dismiss();};window.MouseEnter+=delegate{timeout.Stop();};window.MouseLeave+=delegate{if(!closing)timeout.Start();};window.MouseLeftButtonUp+=delegate(object s,MouseButtonEventArgs e){if(e.OriginalSource==close)return;var item=e.OriginalSource as DependencyObject;while(item!=null){if(item==close)return;item=item is Visual?VisualTreeHelper.GetParent(item):LogicalTreeHelper.GetParent(item);}dismiss();if(open!=null)open();};
            window.Closed+=delegate{timeout.Stop();windows.Remove(window);areas.Remove(window);Reflow();};window.SourceInitialized+=delegate{Native.NoActivate(window);};windows.Add(window);var area=DisplaySpace.Current;areas[window]=area;DisplaySpace.Place(window,area,14,52);Reflow();if(testing)window.IsHitTestVisible=false;window.Show();
            double target=window.Top;if(Motion.Enabled){var slide=new DoubleAnimation(area.Dips.Top-window.Height,target,TimeSpan.FromMilliseconds(440)){EasingFunction=new SpringEase{EasingMode=EasingMode.EaseIn},FillBehavior=FillBehavior.Stop};Timeline.SetDesiredFrameRate(slide,60);window.BeginAnimation(Window.TopProperty,slide);Motion.Fade(window,0,1,260);}timeout.Start();return window;
        }
        private void Reflow(){var counts=new Dictionary<string,int>();foreach(var window in windows){var area=areas[window];int count;counts.TryGetValue(area.Device,out count);window.Left=area.Dips.Left+14;window.Top=area.Dips.Top+52+count*110;counts[area.Device]=count+1;}}
        internal void Clear(){foreach(var w in windows.ToArray())w.Close();}
        public void Dispose(){disposed=true;Clear();}
    }
}

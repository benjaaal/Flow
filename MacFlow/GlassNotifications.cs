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
        internal int Count{get{return windows.Count;}}
        internal GlassNotifications(Action open){this.open=open;}
        internal Window Show(string title,string message,bool testing=false)
        {
            if(disposed)return null;if(windows.Count>=3)windows[0].Close();
            var window=Design.Panel("MacFlow · Notificación",350,100);window.ShowActivated=false;var root=new DockPanel();var close=Design.Button("×",null,false);close.Width=22;close.Height=22;close.VerticalAlignment=VerticalAlignment.Top;close.Padding=new Thickness(0);close.Margin=new Thickness(0);close.Background=Brushes.Transparent;close.ToolTip="Descartar notificación";System.Windows.Automation.AutomationProperties.SetName(close,"Descartar notificación");DockPanel.SetDock(close,Dock.Right);root.Children.Add(close);
            var text=new StackPanel();var heading=Design.Text(title,13,true);text.Children.Add(heading);var body=Design.Text(message,12,false);body.TextWrapping=TextWrapping.Wrap;body.TextTrimming=TextTrimming.CharacterEllipsis;body.MaxHeight=51;body.Margin=new Thickness(0,6,0,0);body.SetResourceReference(TextBlock.ForegroundProperty,"GlassMuted");text.Children.Add(body);root.Children.Add(text);var plate=Design.Glass(root,22,false);plate.Padding=new Thickness(17);window.Content=plate;
            var timeout=new DispatcherTimer(DispatcherPriority.Background){Interval=TimeSpan.FromSeconds(5)};bool closing=false;
            Action dismiss=delegate{if(closing||!window.IsVisible)return;closing=true;timeout.Stop();double left=window.Left;if(!Motion.Enabled){window.Close();return;}var slide=new DoubleAnimation(left,left+65,TimeSpan.FromMilliseconds(230)){EasingFunction=Motion.Ease(true)};slide.Completed+=delegate{window.Close();};Timeline.SetDesiredFrameRate(slide,60);window.BeginAnimation(Window.LeftProperty,slide);Motion.Fade(window,1,0,230);};
            close.Click+=delegate{dismiss();};timeout.Tick+=delegate{dismiss();};window.MouseEnter+=delegate{timeout.Stop();};window.MouseLeave+=delegate{if(!closing)timeout.Start();};window.MouseLeftButtonUp+=delegate(object s,MouseButtonEventArgs e){if(e.OriginalSource==close)return;var item=e.OriginalSource as DependencyObject;while(item!=null){if(item==close)return;item=item is Visual?VisualTreeHelper.GetParent(item):LogicalTreeHelper.GetParent(item);}dismiss();if(open!=null)open();};
            window.Closed+=delegate{timeout.Stop();windows.Remove(window);Reflow();};window.SourceInitialized+=delegate{Native.NoActivate(window);};windows.Add(window);Reflow();if(testing)window.IsHitTestVisible=false;window.Show();
            double target=window.Left;if(Motion.Enabled){var slide=new DoubleAnimation(target+80,target,TimeSpan.FromMilliseconds(420)){EasingFunction=new SpringEase{EasingMode=EasingMode.EaseIn},FillBehavior=FillBehavior.Stop};Timeline.SetDesiredFrameRate(slide,60);window.BeginAnimation(Window.LeftProperty,slide);Motion.Fade(window,0,1,260);}timeout.Start();return window;
        }
        private void Reflow(){var area=SystemParameters.WorkArea;for(int i=0;i<windows.Count;i++){windows[i].Left=area.Right-windows[i].Width-16;windows[i].Top=area.Top+16+i*110;}}
        internal void Clear(){foreach(var w in windows.ToArray())w.Close();}
        public void Dispose(){disposed=true;Clear();}
    }
}

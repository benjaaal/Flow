using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace MacFlow
{
    public sealed class ClockWidget : Window
    {
        private readonly DispatcherTimer timer;
        public ClockWidget()
        {
            Title="MacFlow · Fecha y hora";Width=222;Height=171;WindowStyle=WindowStyle.None;AllowsTransparency=false;ResizeMode=ResizeMode.NoResize;ShowInTaskbar=false;ShowActivated=false;
            Background=Brushes.Transparent;var stack=new StackPanel();var day=Design.Text("",12,true);day.Foreground=Design.Brush(Design.Accent);var date=Design.Text("",50,false);var month=Design.Text("",12,true);var time=Design.Text("",13,false);time.SetResourceReference(TextBlock.ForegroundProperty,"GlassMuted");time.Margin=new Thickness(0,8,0,0);
            stack.Children.Add(day);stack.Children.Add(date);stack.Children.Add(month);stack.Children.Add(time);Content=Design.Glass(stack,22,false);
            Action update=delegate {var now=DateTime.Now;day.Text=now.ToString("dddd",CultureInfo.CurrentCulture).ToUpperInvariant();date.Text=now.Day.ToString();month.Text=now.ToString("MMMM yyyy",CultureInfo.CurrentCulture);time.Text=now.ToString("HH:mm");};
            timer=new DispatcherTimer {Interval=TimeSpan.FromSeconds(15)};timer.Tick+=delegate {update();};timer.Start();update();
            SourceInitialized+=delegate {Native.Glass(this,Design.Dark);Native.NoActivate(this);};Closed+=delegate {timer.Stop();};
        }
    }
}

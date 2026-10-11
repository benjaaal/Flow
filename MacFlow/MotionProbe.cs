using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
namespace MacFlow
{
    // Integration instrumentation; never used during normal application sessions.
    public sealed class MotionProbe : IDisposable
    {
        private readonly Stopwatch watch=Stopwatch.StartNew();
        private readonly List<double> intervals=new List<double>();
        private readonly List<double> dispatchDelays=new List<double>();
        private readonly List<FrameworkElement> icons=new List<FrameworkElement>();
        private readonly DispatcherTimer pulse;
        private readonly DispatcherTimer heartbeat;
        private double previousFrame=-1,previousTick;
        private bool grow;
        private readonly Desktop desktop;
        public MotionProbe(Desktop desktop)
        {
            this.desktop=desktop;Collect(desktop.Dock);CompositionTarget.Rendering+=Rendered;
            // Keep motion continuous: a longer interval would include deliberately
            // idle gaps in the frame statistics and mislabel them as missed frames.
            pulse=new DispatcherTimer(DispatcherPriority.Input){Interval=TimeSpan.FromMilliseconds(100)};
            pulse.Tick+=delegate {grow=!grow;desktop.ProbeDockMotion(grow?.32:.68);};pulse.Start();
            previousTick=watch.Elapsed.TotalMilliseconds;
            heartbeat=new DispatcherTimer(DispatcherPriority.Input){Interval=TimeSpan.FromMilliseconds(16)};
            heartbeat.Tick+=delegate {double now=watch.Elapsed.TotalMilliseconds;dispatchDelays.Add(Math.Max(0,now-previousTick-16));previousTick=now;};heartbeat.Start();
        }
        private void Collect(DependencyObject element)
        {
            var border=element as Border;if(border!=null&&(border.RenderTransform is ScaleTransform||border.RenderTransform is TransformGroup)&&border.Width>=32&&border.Width<=72)icons.Add(border);
            for(int i=0;i<VisualTreeHelper.GetChildrenCount(element);i++)Collect(VisualTreeHelper.GetChild(element,i));
        }
        private void Rendered(object sender,EventArgs e)
        {
            double now=watch.Elapsed.TotalMilliseconds;
            if(previousFrame>=0&&now-previousFrame>.2)intervals.Add(now-previousFrame);previousFrame=now;
        }
        private static double Percentile(List<double> samples,double p){if(samples.Count==0)return 0;var sorted=samples.OrderBy(v=>v).ToArray();return sorted[Math.Min(sorted.Length-1,(int)Math.Floor((sorted.Length-1)*p))];}
        public string Report()
        {
            var ci=CultureInfo.InvariantCulture;
            return "\nDock animation seconds: "+watch.Elapsed.TotalSeconds.ToString("F2",ci)+"\nAnimated icons: "+icons.Count+"\nWPF frame intervals sampled: "+intervals.Count+"\nFrame interval median ms: "+Percentile(intervals,.5).ToString("F2",ci)+"\nFrame interval p95 ms: "+Percentile(intervals,.95).ToString("F2",ci)+"\nDispatcher delay p95 ms: "+Percentile(dispatchDelays,.95).ToString("F2",ci)+"\nFrame samples measure WPF delivery on this machine; not display FPS across all hardware.";
        }
        public void Dispose(){pulse.Stop();heartbeat.Stop();CompositionTarget.Rendering-=Rendered;desktop.StopDockProbe();}
    }
}

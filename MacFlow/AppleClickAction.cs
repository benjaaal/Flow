using System;
using System.Windows.Threading;
namespace MacFlow
{
    // Delay a single action so a double-click cannot also open the menu.
    internal sealed class AppleClickAction : IDisposable
    {
        private readonly DispatcherTimer timer;private readonly Action single,doubled;private bool pending;
        internal AppleClickAction(Action single,Action doubled,int interval)
        {this.single=single;this.doubled=doubled;timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(interval)};timer.Tick+=delegate{Flush();};}
        internal void Click(){if(pending){pending=false;timer.Stop();doubled();}else{pending=true;timer.Start();}}
        internal void Flush(){timer.Stop();if(!pending)return;pending=false;single();}
        public void Dispose(){pending=false;timer.Stop();}
    }
}

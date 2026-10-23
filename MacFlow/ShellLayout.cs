using System;
namespace MacFlow
{
    // A position notification is not a loss of the taskbar lease. Coalesce
    // shell callbacks and never restore Explorer just to negotiate layout.
    internal sealed class ShellLayout : IDisposable
    {
        private readonly Action<Action> post;private readonly Action monitors,bars;
        private bool pending,topology,disposed;
        internal ShellLayout(Action<Action> schedule,Action monitorLayout,Action barLayout){post=schedule;monitors=monitorLayout;bars=barLayout;}
        internal void Request(bool monitorChange){if(disposed)return;topology|=monitorChange;if(pending)return;pending=true;post(Drain);}
        private void Drain(){pending=false;if(disposed)return;bool change=topology;topology=false;if(change)monitors();else bars();}
        public void Dispose(){disposed=true;pending=topology=false;}
    }
    internal sealed class AppbarReservation
    {
        private Native.RECT previous;private bool known;
        internal bool Update(Native.RECT rect,Action commit)
        {
            if(known&&previous.Left==rect.Left&&previous.Top==rect.Top&&previous.Right==rect.Right&&previous.Bottom==rect.Bottom)return false;
            commit();previous=rect;known=true;return true;
        }
        internal void Invalidate(){known=false;}
    }
}

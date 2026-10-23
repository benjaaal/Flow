using System;
using System.Runtime.InteropServices;
using System.Windows.Threading;
namespace MacFlow
{
    // Out-of-context foreground notifications; no Explorer injection or input.
    internal sealed class ForegroundEvents:IDisposable
    {
        private delegate void Callback(IntPtr hook,uint kind,IntPtr hwnd,int objectId,int child,uint thread,uint time);
        [DllImport("user32.dll")]private static extern IntPtr SetWinEventHook(uint first,uint last,IntPtr module,Callback callback,uint process,uint thread,uint flags);
        [DllImport("user32.dll")]private static extern bool UnhookWinEvent(IntPtr hook);
        private readonly Callback callback;private IntPtr hook;private bool pending,disposed;
        internal ForegroundEvents(Dispatcher dispatcher,Action changed)
        {
            callback=delegate{if(disposed||pending||dispatcher.HasShutdownStarted)return;pending=true;dispatcher.BeginInvoke(DispatcherPriority.Input,new Action(delegate{pending=false;if(!disposed)changed();}));};
            hook=SetWinEventHook(3,3,IntPtr.Zero,callback,0,0,0);if(hook==IntPtr.Zero)Log.Write("Avisos de foco no disponibles; se conserva la comprobación de respaldo.");
        }
        public void Dispose(){disposed=true;if(hook!=IntPtr.Zero){UnhookWinEvent(hook);hook=IntPtr.Zero;}}
    }
}

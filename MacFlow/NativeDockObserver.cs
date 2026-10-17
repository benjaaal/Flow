using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Media.Imaging;
namespace MacFlow
{
    // Out-of-context lifecycle notifications. No interception, subclassing,
    // injection, suppression of save dialogs or changes to foreign window styles.
    internal sealed class NativeDockObserver : IDisposable
    {
        private delegate void WinEvent(IntPtr hook,uint kind,IntPtr window,int objectId,int child,uint thread,uint time);
        [DllImport("user32.dll")]private static extern IntPtr SetWinEventHook(uint first,uint last,IntPtr module,WinEvent callback,uint process,uint thread,uint flags);
        [DllImport("user32.dll")]private static extern bool UnhookWinEvent(IntPtr hook);
        private sealed class Tracked{internal WindowEntry Entry;internal Native.RECT Bounds;internal DateTime Seen;}
        private readonly Dictionary<IntPtr,Tracked> known=new Dictionary<IntPtr,Tracked>();private readonly List<IntPtr> hooks=new List<IntPtr>();private readonly WinEvent callback;
        private readonly Dispatcher dispatcher;private readonly DispatcherTimer timer;private readonly Func<bool> enabled;private readonly Func<List<WindowEntry>> enumerate;
        private readonly Func<IntPtr> foreground;
        internal readonly List<string> Trace=new List<string>();private readonly bool fixture;
        private BitmapSource image;private IntPtr captured;private DateTime capturedAt;private bool busy,disposed,initialized;
        internal int CachedImages{get{return image==null?0:1;}}
        internal NativeDockObserver(Dispatcher dispatcher,Func<bool> enabled,Func<List<WindowEntry>> enumerate=null,bool ownFixture=false,Func<IntPtr> fixtureForeground=null)
        {
            this.dispatcher=dispatcher;this.enabled=enabled;this.enumerate=enumerate??Native.Windows;
            fixture=ownFixture;
            foreground=ownFixture&&fixtureForeground!=null?fixtureForeground:Native.GetForegroundWindow;
            callback=delegate(IntPtr hook,uint kind,IntPtr window,int objectId,int child,uint thread,uint time){if(disposed||window==IntPtr.Zero||objectId!=0||child!=0||!enabled())return;dispatcher.BeginInvoke(new Action(delegate{Observed(kind,window);}));};
            foreach(uint kind in new uint[]{0x3,0x8001,0x8002,0x8003,0x16,0x17}){var hook=SetWinEventHook(kind,kind,IntPtr.Zero,callback,0,0,ownFixture?0u:2u);if(hook!=IntPtr.Zero)hooks.Add(hook);}
            timer=new DispatcherTimer(DispatcherPriority.Background){Interval=TimeSpan.FromMilliseconds(550)};timer.Tick+=async delegate{await Sample();};timer.Start();
        }
        internal async Task Sample()
        {
            if(disposed)return;if(!enabled()){known.Clear();image=null;captured=IntPtr.Zero;initialized=false;return;}
            foreach(var item in known.Values.ToArray())if(!Native.Matches(item.Entry)||Native.IsIconic(item.Entry.Handle)||!Native.IsWindowVisible(item.Entry.Handle))Observed(0,item.Entry.Handle);
            var entries=enumerate();var now=DateTime.UtcNow;
            foreach(var entry in entries.Take(100)){Native.RECT bounds;if(Native.IsIconic(entry.Handle)||!Native.GetWindowRect(entry.Handle,out bounds)||bounds.Right-bounds.Left<80||bounds.Bottom-bounds.Top<50)continue;bool fresh=!known.ContainsKey(entry.Handle);known[entry.Handle]=new Tracked{Entry=entry,Bounds=bounds,Seen=now};if(fresh&&initialized)Opening(entry,bounds);}
            initialized=true;
            foreach(var key in known.Where(p=>now-p.Value.Seen>TimeSpan.FromMinutes(2)).Select(p=>p.Key).ToArray())known.Remove(key);
            Tracked current;if(!known.TryGetValue(foreground(),out current)){if(now-capturedAt>TimeSpan.FromSeconds(5)){image=null;captured=IntPtr.Zero;}return;}
            if(busy||captured==current.Entry.Handle&&now-capturedAt<TimeSpan.FromSeconds(2.1))return;busy=true;
            try{var bitmap=await Task.Run(()=>WindowCloseTransition.Capture(current.Entry,current.Bounds));if(!disposed&&enabled()&&Native.Matches(current.Entry)){image=bitmap;captured=current.Entry.Handle;capturedAt=now;}}
            finally{busy=false;}
        }
        private async void Observed(uint kind,IntPtr handle)
        {
            if(fixture)Trace.Add(kind.ToString("X")+" "+handle+" known="+known.ContainsKey(handle));
            if(kind==3||kind==0x8002){await Sample();return;}
            if(disposed||!enabled()||WindowCloseTransition.IsPending(handle))return;Tracked item;if(!known.TryGetValue(handle,out item))return;
            // Destruction and hide often arrive together. Consume first, then
            // wait for the owner's actual final state, including save prompts.
            known.Remove(handle);var snapshot=captured==handle&&DateTime.UtcNow-capturedAt<TimeSpan.FromSeconds(4)?image:null;
            await Task.Delay(60);if(disposed||!enabled()||WindowCloseTransition.IsPending(handle))return;
            bool gone=!Native.Matches(item.Entry),minimized=!gone&&Native.IsIconic(handle),hidden=!gone&&!Native.IsWindowVisible(handle);
            if(!gone&&!minimized&&!hidden)return;
            if(snapshot==null)snapshot=WindowCloseTransition.FallbackCard(item.Entry);
            WindowCloseTransition.AnimateObserved(item.Entry,item.Bounds,snapshot,minimized);
        }
        public void Dispose(){if(disposed)return;disposed=true;timer.Stop();foreach(var hook in hooks)UnhookWinEvent(hook);hooks.Clear();known.Clear();image=null;captured=IntPtr.Zero;}
        private async void Opening(WindowEntry entry,Native.RECT bounds)
        {
            try{var capture=Task.Run(()=>WindowCloseTransition.Capture(entry,bounds));var result=await Task.WhenAny(capture,Task.Delay(180));var snapshot=result==capture?await capture:null;if(disposed||!enabled()||!Native.Matches(entry)||Native.IsIconic(entry.Handle)||!Native.IsWindowVisible(entry.Handle))return;WindowCloseTransition.AnimateObserved(entry,bounds,snapshot??WindowCloseTransition.FallbackCard(entry),false,true);}catch(Exception ex){Log.Write("Apertura hacia el Dock: "+ex.Message);}
        }
    }
}

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Forms=System.Windows.Forms;

namespace MacFlow
{
    // A separate nonactivating surface; never changes a foreign window's frame.
    public sealed class CaptionButtons : Window,IDisposable
    {
        private delegate void EventCallback(IntPtr hook,uint kind,IntPtr hwnd,int objectId,int child,uint thread,uint time);
        [DllImport("user32.dll")]private static extern IntPtr SetWinEventHook(uint start,uint end,IntPtr module,EventCallback callback,uint process,uint thread,uint flags);
        [DllImport("user32.dll")]private static extern bool UnhookWinEvent(IntPtr hook);
        [DllImport("user32.dll")]private static extern IntPtr GetDC(IntPtr hwnd);
        [DllImport("user32.dll")]private static extern int ReleaseDC(IntPtr hwnd,IntPtr dc);
        [DllImport("gdi32.dll")]private static extern uint GetPixel(IntPtr dc,int x,int y);
        private readonly EventCallback eventCallback;
        private readonly Action<string> report;
        private readonly Border plate;
        internal readonly WindowControls Lights;
        private IntPtr foregroundHook,locationHook,ownHandle;
        private WindowEntry target;
        private IntPtr pressedHandle;
        private uint pressedProcess;
        private Native.RECT previous;
        private bool enabled,queued,disposed,testing;
        private DateTime sampled=DateTime.MinValue;
        public CaptionButtons(Action<string> failure,bool watchEvents=true)
        {
            report=failure;Title="MacFlow · Controles de ventana";Width=138;Height=32;WindowStyle=WindowStyle.None;AllowsTransparency=true;ResizeMode=ResizeMode.NoResize;ShowInTaskbar=false;ShowActivated=false;Topmost=true;Focusable=false;Background=Brushes.Transparent;
            Lights=new WindowControls(delegate{Act(0);},delegate{Act(1);},delegate{Act(2);}){HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center};
            Lights.CloseButton.ToolTip="Cerrar ventana activa";System.Windows.Automation.AutomationProperties.SetName(Lights.CloseButton,"Cerrar ventana activa");
            plate=new Border{Child=Lights,CornerRadius=new CornerRadius(5),Background=Design.Brush(Design.Dark?"#20252B":"#F5F5F5"),BorderBrush=Design.Brush("#12FFFFFF"),BorderThickness=new Thickness(.5)};Content=plate;
            PreviewMouseLeftButtonDown+=delegate{pressedHandle=target==null?IntPtr.Zero:target.Handle;pressedProcess=target==null?0:target.ProcessId;};
            SourceInitialized+=delegate{Native.NoActivate(this);ownHandle=new WindowInteropHelper(this).Handle;};
            eventCallback=delegate(IntPtr hook,uint kind,IntPtr hwnd,int objectId,int child,uint thread,uint time){if(disposed||!enabled)return;if(kind==3||(target!=null&&hwnd==target.Handle&&objectId==0&&child==0))QueueUpdate();};
            if(watchEvents){foregroundHook=SetWinEventHook(3,3,IntPtr.Zero,eventCallback,0,0,2);locationHook=SetWinEventHook(0x800B,0x800B,IntPtr.Zero,eventCallback,0,0,2);}
        }
        private void QueueUpdate()
        {
            if(queued||disposed)return;queued=true;Dispatcher.BeginInvoke(DispatcherPriority.Render,new Action(delegate{queued=false;Update(enabled);}));
        }
        public void Update(bool show)
        {
            if(disposed)return;enabled=show;if(!show){target=null;if(IsVisible)Hide();return;}
            var hwnd=Native.GetForegroundWindow();uint pid;Native.GetWindowThreadProcessId(hwnd,out pid);
            if(pid==0||pid==(uint)Process.GetCurrentProcess().Id||Native.GetWindowTextLength(hwnd)==0||hwnd==Native.FindWindow("Progman",null)||hwnd==Native.FindWindow("Shell_TrayWnd",null)){target=null;if(IsVisible)Hide();return;}
            Present(new WindowEntry{Handle=hwnd,ProcessId=pid});
        }
        internal void PresentTest(WindowEntry entry){testing=true;enabled=true;Present(entry);}
        private void Present(WindowEntry entry)
        {
            Native.RECT area;
            if(!TryBounds(entry,out area)){target=null;if(IsVisible)Hide();return;}
            bool changed=target==null||target.Handle!=entry.Handle||target.ProcessId!=entry.ProcessId;
            target=entry;int style=Native.GetWindowLong(entry.Handle,-16);Lights.MinimizeButton.IsEnabled=(style&0x20000)!=0;Lights.ZoomButton.IsEnabled=(style&0x10000)!=0;
            bool moved=area.Left!=previous.Left||area.Top!=previous.Top||area.Right!=previous.Right||area.Bottom!=previous.Bottom;
            if(changed||moved||DateTime.UtcNow-sampled>TimeSpan.FromSeconds(1)){Sample(area);sampled=DateTime.UtcNow;}
            if(ownHandle==IntPtr.Zero)new WindowInteropHelper(this).EnsureHandle();
            if(!IsVisible){Show();changed=true;}
            if(changed||moved)Native.SetWindowPos(ownHandle,new IntPtr(-1),area.Left,area.Top,area.Right-area.Left,area.Bottom-area.Top,0x10);
            previous=area;
        }
        internal static bool TryBounds(WindowEntry entry,out Native.RECT area)
        {
            area=new Native.RECT();if(entry==null||entry.Handle==IntPtr.Zero||entry.ProcessId==0||!Native.IsWindow(entry.Handle)||!Native.IsWindowVisible(entry.Handle)||Native.IsIconic(entry.Handle))return false;
            uint pid;Native.GetWindowThreadProcessId(entry.Handle,out pid);if(pid!=entry.ProcessId)return false;
            Native.RECT frame,caption;if(!Native.GetWindowRect(entry.Handle,out frame)||Native.DwmGetWindowRectAttribute(entry.Handle,5,out caption,16)!=0)return false;
            int width=caption.Right-caption.Left,height=caption.Bottom-caption.Top;
            // Only use documented, plausible caption bounds. No guessed positions
            // over custom toolbars, dialogs, full-screen content or other monitors.
            if(width<72||width>420||height<18||height>100||caption.Left<0||caption.Top<0||caption.Right>frame.Right-frame.Left+2||caption.Bottom>frame.Bottom-frame.Top)return false;
            area=new Native.RECT{Left=frame.Left+caption.Left,Top=frame.Top+caption.Top,Right=frame.Left+caption.Right,Bottom=frame.Top+caption.Bottom};var screen=Forms.Screen.PrimaryScreen.Bounds;
            // Maximized frames include the invisible resize margin outside the
            // monitor. Clip only that documented margin, not arbitrary content.
            if(area.Left<screen.Left-16||area.Right>screen.Right+16||area.Top<screen.Top-16||area.Bottom>screen.Bottom)return false;
            area.Left=Math.Max(area.Left,screen.Left);area.Right=Math.Min(area.Right,screen.Right);area.Top=Math.Max(area.Top,screen.Top);
            return area.Right-area.Left>=72&&area.Bottom-area.Top>=18;
        }
        private void Sample(Native.RECT area)
        {
            var screen=Forms.Screen.PrimaryScreen.Bounds;int x=Math.Max(screen.Left,area.Left-8),y=(area.Top+area.Bottom)/2;var dc=GetDC(IntPtr.Zero);if(dc==IntPtr.Zero)return;
            try{uint rgb=GetPixel(dc,x,y);if(rgb==UInt32.MaxValue)return;var color=Color.FromRgb((byte)(rgb&255),(byte)((rgb>>8)&255),(byte)((rgb>>16)&255));var brush=new SolidColorBrush(color);brush.Freeze();plate.Background=brush;}
            finally{ReleaseDC(IntPtr.Zero,dc);}
        }
        private void Act(int action)
        {
            var entry=target;bool changed=pressedHandle!=IntPtr.Zero&&(entry==null||entry.Handle!=pressedHandle||entry.ProcessId!=pressedProcess);pressedHandle=IntPtr.Zero;pressedProcess=0;
            if(changed||entry==null||(!testing&&Native.GetForegroundWindow()!=entry.Handle))return;
            bool result=action==0?Native.RequestClose(entry):action==1?Native.RequestMinimize(entry):Native.RequestZoom(entry);
            if(!result&&report!=null)report("Windows no permitió esta acción. Usá los controles propios de la aplicación.");
            if(action<2){target=null;if(IsVisible)Hide();}else QueueUpdate();
        }
        public void Dispose(){if(disposed)return;disposed=true;enabled=false;if(foregroundHook!=IntPtr.Zero)UnhookWinEvent(foregroundHook);if(locationHook!=IntPtr.Zero)UnhookWinEvent(locationHook);foregroundHook=locationHook=IntPtr.Zero;target=null;Close();}
    }
}

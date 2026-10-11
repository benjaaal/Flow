using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Forms=System.Windows.Forms;

namespace MacFlow
{
    // Own, nonactivating 4-physical-pixel reveal surface. No Explorer styles,
    // taskbar handles or mouse hooks are changed to protect the Dock edge.
    public sealed class DockEdge : Window
    {
        [StructLayout(LayoutKind.Sequential)]private struct PixelPoint{public int X,Y;}
        [DllImport("user32.dll")]private static extern IntPtr WindowFromPoint(PixelPoint point);
        [DllImport("user32.dll")]private static extern IntPtr GetAncestor(IntPtr h,uint flags);
        private IntPtr handle,lastForeground;
        private DateTime recovered=DateTime.MinValue;
        public DockEdge(Action reveal)
        {
            Title="MacFlow · Borde del Dock";WindowStyle=WindowStyle.None;AllowsTransparency=true;ResizeMode=ResizeMode.NoResize;ShowActivated=false;ShowInTaskbar=false;Topmost=true;Focusable=false;
            var surface=new SolidColorBrush(Color.FromArgb(1,0,0,0));surface.Freeze();Background=surface;
            SourceInitialized+=delegate{Native.NoActivate(this);handle=new WindowInteropHelper(this).Handle;};MouseEnter+=delegate{reveal();};MouseLeftButtonUp+=delegate{reveal();};
        }
        public void KeepEdgeAvailable()
        {
            if(!IsVisible||handle==IntPtr.Zero)return;var foreground=Native.GetForegroundWindow();bool changed=foreground!=lastForeground;lastForeground=foreground;
            var cursor=Forms.Cursor.Position;var screen=Forms.Screen.PrimaryScreen.Bounds;bool near=cursor.X>=screen.Left&&cursor.X<screen.Right&&cursor.Y>=screen.Bottom-64&&cursor.Y<screen.Bottom;
            if(!changed&&!near)return;
            if(near&&GetAncestor(WindowFromPoint(new PixelPoint{X=cursor.X,Y=screen.Bottom-1}),2)==handle)return;
            if(DateTime.UtcNow-recovered<TimeSpan.FromMilliseconds(250))return;recovered=DateTime.UtcNow;
            // Change only our reveal surface's order, without activating it.
            Native.SetWindowPos(handle,new IntPtr(-1),0,0,0,0,0x13);
        }
        public void Position(Window dock)
        {
            var source=PresentationSource.FromVisual(dock);if(source==null)return;var matrix=source.CompositionTarget.TransformFromDevice;var screen=Forms.Screen.PrimaryScreen.Bounds;
            var top=matrix.Transform(new Point(screen.Left,screen.Bottom-4));var bottom=matrix.Transform(new Point(screen.Right,screen.Bottom));
            Left=top.X;Top=top.Y;Width=bottom.X-top.X;Height=bottom.Y-top.Y;
        }
    }
}

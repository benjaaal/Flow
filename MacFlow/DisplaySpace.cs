using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Forms=System.Windows.Forms;
namespace MacFlow
{
    // Screen rectangles are physical pixels; UI dimensions remain DIPs.
    // Never apply the primary window's scale to another display's origin.
    internal sealed class DisplayArea
    {
        internal string Device;internal Rect Pixels;internal double Scale;
        internal DisplayArea(string device,Rect pixels,double scale){Device=device;Pixels=pixels;Scale=scale>0?scale:1;}
        internal double Width{get{return Pixels.Width/Scale;}}internal double Height{get{return Pixels.Height/Scale;}}
        internal Rect Dips{get{return new Rect(Pixels.Left/Scale,Pixels.Top/Scale,Width,Height);}}
    }
    internal static class DisplaySpace
    {
        [StructLayout(LayoutKind.Sequential)]private struct XY{internal int X,Y;}
        [DllImport("user32.dll")]private static extern IntPtr MonitorFromPoint(XY point,uint flags);
        [DllImport("shcore.dll")]private static extern int GetDpiForMonitor(IntPtr monitor,int kind,out uint x,out uint y);
        internal static DisplayArea Of(Forms.Screen screen)
        {
            double scale=1;try{uint x,y;var b=screen.Bounds;if(GetDpiForMonitor(MonitorFromPoint(new XY{X=b.Left+b.Width/2,Y=b.Top+b.Height/2},2),0,out x,out y)==0)scale=x/96.0;}catch{}
            return new DisplayArea(screen.DeviceName,new Rect(screen.Bounds.Left,screen.Bounds.Top,screen.Bounds.Width,screen.Bounds.Height),scale);
        }
        internal static DisplayArea Current{get{return Of(Forms.Screen.FromPoint(Forms.Cursor.Position));}}
        internal static DisplayArea For(Window window){var h=new WindowInteropHelper(window).Handle;return Of(h==IntPtr.Zero?Forms.Screen.PrimaryScreen:Forms.Screen.FromHandle(h));}
        internal static DisplayArea ForHandle(IntPtr handle){return Of(Forms.Screen.FromHandle(handle));}
        internal static Rect Center(DisplayArea area,Size size,double fraction)
        {var b=area.Dips;double w=Math.Min(size.Width,Math.Max(200,b.Width-32)),h=Math.Min(size.Height,Math.Max(100,b.Height-68));return new Rect(b.Left+(b.Width-w)/2,b.Top+Math.Max(48,Math.Min(b.Height-h-12,b.Height*fraction)),w,h);}
        internal static void Place(Window window,DisplayArea area,double x,double y)
        {
            var handle=new WindowInteropHelper(window).EnsureHandle();
            Native.SetWindowPos(handle,IntPtr.Zero,(int)Math.Round(area.Pixels.Left+x*area.Scale),(int)Math.Round(area.Pixels.Top+y*area.Scale),(int)Math.Round(window.Width*area.Scale),(int)Math.Round(window.Height*area.Scale),0x14);
        }
        internal static void Center(Window window,double fraction)
        {var area=Current;var box=Center(area,new Size(window.Width,window.Height),fraction);window.Width=box.Width;window.Height=box.Height;Place(window,area,(box.Left-area.Dips.Left),(box.Top-area.Dips.Top));}
        internal static void Right(Window window,double top=50)
        {var area=Current;window.Width=Math.Min(window.Width,area.Width-24);window.Height=Math.Min(window.Height,area.Height-top-12);Place(window,area,area.Width-window.Width-12,top);}
        internal static double UpperEdge(Window window){return For(window).Dips.Top;}
    }
}

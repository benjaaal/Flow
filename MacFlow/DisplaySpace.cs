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
        internal string Device,StableId;internal Rect Pixels,WorkingPixels;internal double Scale;
        internal DisplayArea(string device,Rect pixels,double scale){Device=StableId=device;Pixels=WorkingPixels=pixels;Scale=scale>0?scale:1;}
        internal double Width{get{return Pixels.Width/Scale;}}internal double Height{get{return Pixels.Height/Scale;}}
        internal Rect Dips{get{return new Rect(Pixels.Left/Scale,Pixels.Top/Scale,Width,Height);}}
        internal Rect WorkingDips{get{return new Rect(WorkingPixels.Left/Scale,WorkingPixels.Top/Scale,WorkingPixels.Width/Scale,WorkingPixels.Height/Scale);}}
        internal bool Matches(string value){return value==Device||value==StableId;}
    }
    internal static class DisplaySpace
    {
        [StructLayout(LayoutKind.Sequential)]private struct XY{internal int X,Y;}
        [DllImport("user32.dll")]private static extern IntPtr MonitorFromPoint(XY point,uint flags);
        [DllImport("shcore.dll")]private static extern int GetDpiForMonitor(IntPtr monitor,int kind,out uint x,out uint y);
        [DllImport("shcore.dll")]private static extern int GetScaleFactorForMonitor(IntPtr monitor,out int scale);
        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]private struct DeviceInfo
        {internal int Size;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=32)]internal string Name;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=128)]internal string Description;internal uint Flags;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=128)]internal string Id;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=128)]internal string Key;}
        [DllImport("user32.dll",CharSet=CharSet.Unicode)]private static extern bool EnumDisplayDevices(string name,uint index,ref DeviceInfo info,uint flags);
        private static readonly System.Collections.Generic.Dictionary<string,DisplayArea> cache=new System.Collections.Generic.Dictionary<string,DisplayArea>();
        private static DateTime refreshed;
        internal const double ReservedTop=36;
        internal static void Invalidate(){cache.Clear();refreshed=DateTime.UtcNow;}
        internal static DisplayArea Of(Forms.Screen screen)
        {
            if(DateTime.UtcNow-refreshed>TimeSpan.FromSeconds(2))Invalidate();DisplayArea cached;if(cache.TryGetValue(screen.DeviceName,out cached))return cached;
            double scale=1;var b=screen.Bounds;try{int factor;if(GetScaleFactorForMonitor(MonitorFromPoint(new XY{X=b.Left+b.Width/2,Y=b.Top+b.Height/2},2),out factor)==0&&factor>=100&&factor<=500)scale=factor/100.0;}catch(Exception ex){Log.Write("Escala de pantalla no disponible: "+ex.GetType().Name);}
            var area=new DisplayArea(screen.DeviceName,new Rect(b.Left,b.Top,b.Width,b.Height),scale);var work=screen.WorkingArea;area.WorkingPixels=new Rect(work.Left,work.Top,work.Width,work.Height);
            var device=new DeviceInfo{Size=Marshal.SizeOf(typeof(DeviceInfo))};if(EnumDisplayDevices(screen.DeviceName,0,ref device,1)&&!String.IsNullOrEmpty(device.Id))area.StableId=device.Id;
            cache[screen.DeviceName]=area;return area;
        }
        internal static DisplayArea Current{get{return Of(Forms.Screen.FromPoint(Forms.Cursor.Position));}}
        internal static DisplayArea For(Window window){var h=new WindowInteropHelper(window).Handle;return Of(h==IntPtr.Zero?Forms.Screen.PrimaryScreen:Forms.Screen.FromHandle(h));}
        internal static DisplayArea ForHandle(IntPtr handle){return Of(Forms.Screen.FromHandle(handle));}
        internal static Rect Center(DisplayArea area,Size size,double fraction)
        {var b=area.WorkingDips;double minimumTop=Math.Max(b.Top,area.Dips.Top+ReservedTop+8);double w=Math.Min(size.Width,Math.Max(1,b.Width-32)),h=Math.Min(size.Height,Math.Max(1,b.Bottom-minimumTop-12));return new Rect(b.Left+(b.Width-w)/2,Math.Max(minimumTop,Math.Min(b.Bottom-h-12,b.Top+b.Height*fraction)),w,h);}
        internal static void Place(Window window,DisplayArea area,double x,double y)
        {
            var handle=new WindowInteropHelper(window).EnsureHandle();
            Native.SetWindowPos(handle,IntPtr.Zero,(int)Math.Round(area.Pixels.Left+x*area.Scale),(int)Math.Round(area.Pixels.Top+y*area.Scale),(int)Math.Round(window.Width*area.Scale),(int)Math.Round(window.Height*area.Scale),0x14);
        }
        internal static void Center(Window window,double fraction)
        {var area=Current;var box=Center(area,new Size(window.Width,window.Height),fraction);window.Width=box.Width;window.Height=box.Height;Place(window,area,(box.Left-area.Dips.Left),(box.Top-area.Dips.Top));}
        internal static void Right(Window window,double top=50)
        {var area=Current;var work=area.WorkingDips;top=Math.Max(top,work.Top-area.Dips.Top+8);window.Width=Math.Min(window.Width,Math.Max(1,work.Width-24));window.Height=Math.Min(window.Height,Math.Max(1,work.Bottom-area.Dips.Top-top-12));Place(window,area,work.Right-area.Dips.Left-window.Width-12,top);}
        internal static double UpperEdge(Window window){return For(window).Dips.Top;}
    }
}

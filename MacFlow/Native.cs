using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace MacFlow
{
    public static class Native
    {
        [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
        [StructLayout(LayoutKind.Sequential)] public struct APPBARDATA { public int cbSize; public IntPtr hWnd; public uint uCallbackMessage, uEdge; public RECT rc; public IntPtr lParam; }
        [StructLayout(LayoutKind.Sequential)] public struct MARGINS { public int Left, Right, Top, Bottom; }
        [StructLayout(LayoutKind.Sequential)] public struct POWER { public byte ACLineStatus, BatteryFlag, BatteryLifePercent, SystemStatusFlag; public int BatteryLifeTime, BatteryFullLifeTime; }
        [StructLayout(LayoutKind.Sequential)] public struct THUMBNAIL {public uint Flags;public RECT Destination,Source;public byte Opacity;[MarshalAs(UnmanagedType.Bool)]public bool Visible;[MarshalAs(UnmanagedType.Bool)]public bool ClientOnly;}
        [DllImport("dwmapi.dll")] public static extern int DwmRegisterThumbnail(IntPtr dest,IntPtr source,out IntPtr thumbnail);
        [DllImport("dwmapi.dll")] public static extern int DwmUpdateThumbnailProperties(IntPtr thumbnail,ref THUMBNAIL props);
        [DllImport("dwmapi.dll")] public static extern int DwmUnregisterThumbnail(IntPtr thumbnail);
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)] public struct FILEINFO { public IntPtr hIcon; public int iIcon; public uint attributes; [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string display; [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)] public string type; }
        public delegate bool EnumProc(IntPtr hwnd, IntPtr arg);
        [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc callback, IntPtr arg);
        [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
        [DllImport("user32.dll")] public static extern bool IsWindow(IntPtr h);
        [DllImport("user32.dll",SetLastError=true)] private static extern bool PostMessage(IntPtr h,uint message,IntPtr wParam,IntPtr lParam);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetWindowText(IntPtr h, StringBuilder value, int max);
        [DllImport("user32.dll")] public static extern int GetWindowTextLength(IntPtr h);
        [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
        [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] public static extern IntPtr GetShellWindow();
        [DllImport("user32.dll",CharSet=CharSet.Unicode)] private static extern int GetClassName(IntPtr h,StringBuilder name,int length);
        [DllImport("user32.dll")] public static extern IntPtr GetWindow(IntPtr h, uint command);
        [DllImport("user32.dll", EntryPoint = "GetWindowLongW")] public static extern int GetWindowLong(IntPtr h, int index);
        [DllImport("user32.dll", EntryPoint = "SetWindowLongW")] public static extern int SetWindowLong(IntPtr h, int index, int value);
        [DllImport("user32.dll")] public static extern bool ShowWindowAsync(IntPtr h, int command);
        [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr h);
        [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
        [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT rect);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr FindWindow(string cls, string name);
        [DllImport("user32.dll")] public static extern bool RegisterHotKey(IntPtr h, int id, uint modifiers, uint key);
        [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr h, int id);
        [DllImport("user32.dll",CharSet=CharSet.Unicode)] public static extern int RegisterWindowMessage(string message);
        [DllImport("user32.dll")] public static extern bool DestroyIcon(IntPtr h);
        [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int w, int height, uint flags);
        [DllImport("user32.dll")] public static extern void keybd_event(byte key,byte scan,uint flags,UIntPtr extra);
        [DllImport("kernel32.dll")] public static extern bool GetSystemPowerStatus(out POWER power);
        [DllImport("kernel32.dll")] public static extern IntPtr OpenProcess(uint access, bool inherit, uint pid);
        [DllImport("kernel32.dll")] public static extern bool CloseHandle(IntPtr h);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] public static extern bool QueryFullProcessImageName(IntPtr h, uint flags, StringBuilder buffer, ref int size);
        [DllImport("shell32.dll")] public static extern UIntPtr SHAppBarMessage(uint message, ref APPBARDATA data);
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr SHGetFileInfo(string path, uint attributes, ref FILEINFO info, uint size, uint flags);
        [DllImport("dwmapi.dll")] public static extern int DwmSetWindowAttribute(IntPtr h, int attr, ref int value, int size);
        [DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(IntPtr h, int attr, out int value, int size);
        [DllImport("dwmapi.dll",EntryPoint="DwmGetWindowAttribute")] public static extern int DwmGetWindowRectAttribute(IntPtr h,int attr,out RECT value,int size);
        [DllImport("dwmapi.dll")] public static extern int DwmExtendFrameIntoClientArea(IntPtr h, ref MARGINS margins);
        [DllImport("dwmapi.dll")] public static extern int DwmIsCompositionEnabled([MarshalAs(UnmanagedType.Bool)]out bool enabled);
        [DllImport("gdi32.dll")] private static extern IntPtr CreateRoundRectRgn(int left,int top,int right,int bottom,int width,int height);
        [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr value);
        [DllImport("user32.dll")] private static extern int SetWindowRgn(IntPtr window,IntPtr region,bool redraw);
        [StructLayout(LayoutKind.Sequential)]private struct ACCENT {public int State,Flags;public uint Color;public int Animation;}
        [StructLayout(LayoutKind.Sequential)]private struct COMPOSITION {public int Attribute;public IntPtr Data;public int Size;}
        [DllImport("user32.dll")]private static extern bool SetWindowCompositionAttribute(IntPtr h,ref COMPOSITION value);
        private static bool ClearCompositorGlass(IntPtr h,bool dark)
        {
            // Optional compositor accent API, applied only to our HWNDs.
            // If unavailable, retain the documented Windows 11 backdrop.
            int systemNone=1;IntPtr memory=Marshal.AllocHGlobal(Marshal.SizeOf(typeof(ACCENT)));
            try
            {
                // Clear blur avoids acrylic's grain and opaque tint beneath the
                // foreground glass. Tint and edge light belong to Design.Glass.
                var accent=new ACCENT{State=LiquidMaterial.LiveBackdrop?3:0,Flags=0,Color=0};
                Marshal.StructureToPtr(accent,memory,false);var data=new COMPOSITION{Attribute=19,Data=memory,Size=Marshal.SizeOf(typeof(ACCENT))};
                DwmSetWindowAttribute(h,38,ref systemNone,4);
                if(SetWindowCompositionAttribute(h,ref data))return true;
            }
            catch(EntryPointNotFoundException){}finally{Marshal.FreeHGlobal(memory);}
            int fallback=LiquidMaterial.LiveBackdrop?3:1;DwmSetWindowAttribute(h,38,ref fallback,4);return false;
        }
        private static readonly DependencyProperty GlassRegionHook=DependencyProperty.RegisterAttached("GlassRegionHook",typeof(bool),typeof(Native),new PropertyMetadata(false));
        internal static double GlassRadius(Window window)
        {
            var border=window.Content as System.Windows.Controls.Border;
            var grid=window.Content as System.Windows.Controls.Grid;
            if(border==null&&grid!=null)for(int i=grid.Children.Count-1;i>=0;i--){var wrapper=grid.Children[i] as System.Windows.Controls.Border;var candidate=wrapper==null?null:wrapper.Child as System.Windows.Controls.Border;if(candidate!=null&&candidate.CornerRadius.TopLeft>0){border=candidate;break;}}
            return border!=null&&border.CornerRadius.TopLeft>0?border.CornerRadius.TopLeft:window.Title.Contains("barra superior")?12:window.Title.Contains("Dock")?18:22;
        }
        private static void RoundGlass(Window window)
        {
            if(PanelMotion.IsClosing(window))return;
            SetGlassClip(window,1);
        }
        internal static void SetGlassClip(Window window,double fraction)
        {
            var source=PresentationSource.FromVisual(window);if(source==null||window.ActualWidth<=0)return;
            double scale=source.CompositionTarget.TransformToDevice.M11,remaining=Math.Max(0,Math.Min(1,fraction));
            double width=window.ActualWidth*scale,height=window.ActualHeight*scale,left=width*(1-remaining)/2,top=height*(1-remaining)/2;
            double radius=Math.Min(GlassRadius(window)*scale,Math.Min(width,height)*remaining/2);
            var region=CreateRoundRectRgn((int)left,(int)top,(int)Math.Ceiling(width-left),(int)Math.Ceiling(height-top),Math.Max(1,(int)(radius*2)),Math.Max(1,(int)(radius*2)));
            if(region!=IntPtr.Zero&&SetWindowRgn(new WindowInteropHelper(window).Handle,region,true)==0)DeleteObject(region);
        }

        public static uint TaskbarState()
        {
            var d = new APPBARDATA { cbSize = Marshal.SizeOf(typeof(APPBARDATA)), hWnd = FindWindow("Shell_TrayWnd", null) };
            return (uint)SHAppBarMessage(4, ref d).ToUInt64();
        }
        public static bool IsDesktopWindow(IntPtr window)
        {
            if(window==IntPtr.Zero||!IsWindow(window))return false;
            if(window==GetShellWindow())return true;
            var name=new StringBuilder(100);GetClassName(window,name,name.Capacity);
            if(name.ToString()!="WorkerW"&&name.ToString()!="Progman")return false;
            uint pid;GetWindowThreadProcessId(window,out pid);
            uint shellPid;GetWindowThreadProcessId(GetShellWindow(),out shellPid);if(pid!=0&&pid==shellPid)return true;
            return String.Equals(ProcessPath(pid),System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),"explorer.exe"),StringComparison.OrdinalIgnoreCase);
        }
        public static bool IsDesktopContext(IntPtr window)
        {
            // During Show Desktop/last-window minimize, Windows can briefly
            // retain the old foreground handle or report no foreground window.
            if(window==IntPtr.Zero||!IsWindow(window)||IsIconic(window)||!IsWindowVisible(window)||IsDesktopWindow(window))return true;
            RECT bounds;if(!GetWindowRect(window,out bounds)||bounds.Right>bounds.Left&&bounds.Bottom>bounds.Top)return false;
            uint pid;GetWindowThreadProcessId(window,out pid);return IsDesktopProxy(bounds,ProcessPath(pid));
        }
        internal static bool IsDesktopProxy(RECT bounds,string executable)
        {return (bounds.Right<=bounds.Left||bounds.Bottom<=bounds.Top)&&String.Equals(executable,System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),"explorer.exe"),StringComparison.OrdinalIgnoreCase);}
        public static bool SetTaskbarState(uint value)
        {
            var d = new APPBARDATA { cbSize = Marshal.SizeOf(typeof(APPBARDATA)), hWnd = FindWindow("Shell_TrayWnd", null), lParam = new IntPtr(value) };
            if (d.hWnd == IntPtr.Zero) return false;
            SHAppBarMessage(10, ref d); return TaskbarState() == value;
        }
        public static string ProcessPath(uint pid)
        {
            var h = OpenProcess(0x1000, false, pid); if (h == IntPtr.Zero) return "";
            try { int size = 2048; var buffer = new StringBuilder(size); return QueryFullProcessImageName(h, 0, buffer, ref size) ? buffer.ToString() : ""; }
            finally { CloseHandle(h); }
        }
        public static string Title(IntPtr h) { var b = new StringBuilder(1024); GetWindowText(h, b, b.Capacity); return b.ToString(); }
        public static List<WindowEntry> Windows()
        {
            var result = new List<WindowEntry>(); uint self = (uint)Process.GetCurrentProcess().Id;
            EnumWindows(delegate(IntPtr h, IntPtr arg)
            {
                if (!IsWindowVisible(h) || GetWindowTextLength(h) == 0) return true;
                uint pid; GetWindowThreadProcessId(h, out pid); if (pid == self) return true;
                int style = GetWindowLong(h, -20), cloaked;
                if ((style & 0x80) != 0 || (GetWindow(h, 4) != IntPtr.Zero && (style & 0x40000) == 0)) return true;
                if (DwmGetWindowAttribute(h, 14, out cloaked, 4) == 0 && cloaked != 0) return true;
                string title = Title(h); if (title == "Program Manager") return true;
                result.Add(new WindowEntry { Handle = h,ProcessId=pid,Title = title, Executable = ProcessPath(pid) }); return true;
            }, IntPtr.Zero);
            return result;
        }
        public static bool Activate(WindowEntry w)
        {
            if (!Matches(w)) return false;
            if (IsIconic(w.Handle)) ShowWindowAsync(w.Handle, 9);
            return SetForegroundWindow(w.Handle);
        }
        public static bool Matches(WindowEntry window)
        {
            if(window==null||window.Handle==IntPtr.Zero||window.ProcessId==0||!IsWindow(window.Handle))return false;uint pid;GetWindowThreadProcessId(window.Handle,out pid);return pid==window.ProcessId;
        }
        public static bool RequestClose(WindowEntry window)
        {
            if(window==null||window.Handle==IntPtr.Zero||window.ProcessId==0||!IsWindow(window.Handle))return false;
            uint current;GetWindowThreadProcessId(window.Handle,out current);if(current!=window.ProcessId)return false;
            // Asynchronous normal close; the application can cancel or ask to save.
            // No process termination or foreign-window style changes.
            return PostMessage(window.Handle,0x0010,IntPtr.Zero,IntPtr.Zero);
        }
        public static bool RequestMinimize(WindowEntry window)
        {
            if(window==null||window.Handle==IntPtr.Zero||window.ProcessId==0||!IsWindow(window.Handle))return false;
            uint current;GetWindowThreadProcessId(window.Handle,out current);if(current!=window.ProcessId)return false;
            // Public asynchronous minimize request. Do not rewrite foreign chrome
            // or animate another application's window on our UI thread.
            return IsIconic(window.Handle)||ShowWindowAsync(window.Handle,6);
        }
        public static bool RequestZoom(WindowEntry window)
        {
            if(window==null||window.Handle==IntPtr.Zero||window.ProcessId==0||!IsWindow(window.Handle))return false;
            uint current;GetWindowThreadProcessId(window.Handle,out current);if(current!=window.ProcessId)return false;
            int style=GetWindowLong(window.Handle,-16);if((style&0x10000)==0)return false;
            return ShowWindowAsync(window.Handle,(style&0x1000000)!=0?9:3);
        }
        public static void Glass(Window window, bool dark)
        {
            if(new WindowInteropHelper(window).Handle==IntPtr.Zero)return;
            // Background handling adapted from WPF UI WindowBackdrop.cs (MIT).
            // Copyright (C) Leszek Pomianowski and WPF UI Contributors.
            // Full license and source link are in THIRD-PARTY-NOTICES.md.
            if(window.AllowsTransparency){LiquidMaterial.Apply(window,dark);return;}
            // Remove the nonclient frame of our own borderless windows, so the
            // compositor does not leave a solid Windows strip under the glass.
            if(System.Windows.Shell.WindowChrome.GetWindowChrome(window)==null)
                System.Windows.Shell.WindowChrome.SetWindowChrome(window,new System.Windows.Shell.WindowChrome{CaptionHeight=0,ResizeBorderThickness=new Thickness(0),GlassFrameThickness=new Thickness(0),CornerRadius=new CornerRadius(0),UseAeroCaptionButtons=false});
            if(!(bool)window.GetValue(GlassRegionHook)){window.SetValue(GlassRegionHook,true);window.SizeChanged+=delegate{RoundGlass(window);};window.Loaded+=delegate{RoundGlass(window);};}
            var h = new WindowInteropHelper(window).Handle;
            int noFrame=1;DwmSetWindowAttribute(h,2,ref noFrame,4);
            int value = dark ? 1 : 0; DwmSetWindowAttribute(h, 20, ref value, 4);
            value = 2; DwmSetWindowAttribute(h, 33, ref value, 4);
            // DWMWA_COLOR_NONE removes the solid Windows outline on our own windows.
            value = unchecked((int)0xFFFFFFFE); DwmSetWindowAttribute(h,34,ref value,4);
            bool composition;DwmIsCompositionEnabled(out composition);
            if(!composition||SystemParameters.HighContrast){value=1;DwmSetWindowAttribute(h,38,ref value,4);window.Background=SystemParameters.HighContrast?SystemColors.WindowBrush:Design.Brush(dark?"#252C39":"#F0F2F6");window.Tag="Solid fallback";return;}
            // Public Windows 11 22H2 backdrop API; solid WPF background is the fallback.
            value = LiquidMaterial.LiveBackdrop?3:1;
            var source=HwndSource.FromHwnd(h);
            var margin=new MARGINS {Left=-1,Right=-1,Top=-1,Bottom=-1};
            int frame=DwmExtendFrameIntoClientArea(h,ref margin);int status=DwmSetWindowAttribute(h,38,ref value,4);
            if (status == 0 && frame == 0)
            {
                window.SetCurrentValue(System.Windows.Controls.Control.BackgroundProperty,Brushes.Transparent);
                if(source!=null&&source.CompositionTarget!=null)source.CompositionTarget.BackgroundColor=Colors.Transparent;
                window.Tag=LiquidMaterial.LiveBackdrop?"Native Desktop Acrylic":"Clear glass fallback";
                if(ClearCompositorGlass(h,dark))window.Tag=LiquidMaterial.LiveBackdrop?"Compositor glass · recordable":"Clear glass fallback";
            }
            else {value=1;DwmSetWindowAttribute(h,38,ref value,4);var empty=new MARGINS();DwmExtendFrameIntoClientArea(h,ref empty);window.Background=Design.Brush(dark?"#28282A":"#F5F5F7");window.Tag="Solid fallback";}
            LiquidMaterial.Apply(window,dark);RoundGlass(window);
        }
        public static void NoActivate(Window window)
        {
            var h = new WindowInteropHelper(window).Handle; SetWindowLong(h, -20, GetWindowLong(h, -20) | 0x08000000 | 0x80);
        }
    }

    [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")] internal class MMDeviceEnumerator { }
    [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] internal interface IMMDeviceEnumerator
    {
        [PreserveSig] int EnumAudioEndpoints(int flow, uint state, out IntPtr devices);
        [PreserveSig] int GetDefaultAudioEndpoint(int flow, int role, out IMMDevice device);
    }
    [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] internal interface IMMDevice
    {
        [PreserveSig] int Activate(ref Guid id, uint context, IntPtr activation, [MarshalAs(UnmanagedType.IUnknown)] out object result);
    }
    [ComImport, Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] internal interface IAudioEndpointVolume
    {
        [PreserveSig] int RegisterControlChangeNotify(IntPtr callback); [PreserveSig] int UnregisterControlChangeNotify(IntPtr callback); [PreserveSig] int GetChannelCount(out uint count);
        [PreserveSig] int SetMasterVolumeLevel(float level, ref Guid context); [PreserveSig] int SetMasterVolumeLevelScalar(float level, ref Guid context);
        [PreserveSig] int GetMasterVolumeLevel(out float level); [PreserveSig] int GetMasterVolumeLevelScalar(out float level);
        [PreserveSig] int SetChannelVolumeLevel(uint ch, float level, ref Guid context); [PreserveSig] int SetChannelVolumeLevelScalar(uint ch, float level, ref Guid context);
        [PreserveSig] int GetChannelVolumeLevel(uint ch, out float level); [PreserveSig] int GetChannelVolumeLevelScalar(uint ch, out float level);
        [PreserveSig] int SetMute([MarshalAs(UnmanagedType.Bool)] bool mute, ref Guid context); [PreserveSig] int GetMute([MarshalAs(UnmanagedType.Bool)] out bool mute);
    }
    public sealed class Audio : IDisposable
    {
        private object enumerator; private IMMDevice device; private IAudioEndpointVolume endpoint;
        public Audio()
        {
            try
            {
                enumerator = new MMDeviceEnumerator(); int hr = ((IMMDeviceEnumerator)enumerator).GetDefaultAudioEndpoint(0, 0, out device); Marshal.ThrowExceptionForHR(hr);
                object ep; Guid id = typeof(IAudioEndpointVolume).GUID; Marshal.ThrowExceptionForHR(device.Activate(ref id, 23, IntPtr.Zero, out ep)); endpoint = (IAudioEndpointVolume)ep;
            }
            catch { Dispose(); }
        }
        public bool Available { get { return endpoint != null; } }
        public float Volume { get { if (endpoint == null) return 0; float v; Marshal.ThrowExceptionForHR(endpoint.GetMasterVolumeLevelScalar(out v)); return v; } set { if (endpoint != null) { var id = Guid.Empty; Marshal.ThrowExceptionForHR(endpoint.SetMasterVolumeLevelScalar(Math.Max(0, Math.Min(1, value)), ref id)); } } }
        public bool Muted { get { if (endpoint == null) return false; bool v; Marshal.ThrowExceptionForHR(endpoint.GetMute(out v)); return v; } set { if (endpoint != null) { var id = Guid.Empty; Marshal.ThrowExceptionForHR(endpoint.SetMute(value, ref id)); } } }
        public void Dispose() { if (endpoint != null) Marshal.ReleaseComObject(endpoint); if (device != null) Marshal.ReleaseComObject(device); if (enumerator != null) Marshal.ReleaseComObject(enumerator); endpoint = null; device = null; enumerator = null; }
    }
}

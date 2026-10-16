using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Windows.UI.Composition;
using Windows.UI.Composition.Desktop;
namespace MacFlow
{
    // A clipped compositor visual samples behind our HWND. It never captures
    // the desktop, excludes the app from recordings or paints a bitmap UI.
    internal sealed class CompositorGlass : IDisposable
    {
        [ComImport,Guid("29E691FA-4567-4DCA-B319-D0F207EB6807"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IDesktopInterop
        {
            void CreateDesktopWindowTarget(IntPtr hwnd,[MarshalAs(UnmanagedType.Bool)]bool topmost,out IntPtr target);
            void EnsureOnThread(uint thread);
        }
        [StructLayout(LayoutKind.Sequential)]private struct QueueOptions{public int Size,Thread,Apartment;}
        [DllImport("CoreMessaging.dll")]private static extern int CreateDispatcherQueueController(QueueOptions options,out IntPtr controller);
        private delegate IntPtr WindowProcedure(IntPtr window,uint message,IntPtr w,IntPtr l);
        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]private struct WindowClass{public uint Size,Style;public WindowProcedure Procedure;public int ClassExtra,WindowExtra;public IntPtr Instance,Icon,Cursor,Background;public string Menu,Name;public IntPtr SmallIcon;}
        [DllImport("user32.dll",CharSet=CharSet.Unicode)]private static extern ushort RegisterClassEx(ref WindowClass value);
        [DllImport("user32.dll",CharSet=CharSet.Unicode)]private static extern IntPtr CreateWindowEx(uint extended,string type,string name,uint style,int x,int y,int width,int height,IntPtr parent,IntPtr menu,IntPtr instance,IntPtr data);
        [DllImport("user32.dll")]private static extern bool DestroyWindow(IntPtr window);
        [DllImport("user32.dll",CharSet=CharSet.Unicode)]private static extern IntPtr DefWindowProc(IntPtr window,uint message,IntPtr w,IntPtr l);
        [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]private static extern IntPtr GetModuleHandle(string module);
        private static readonly WindowProcedure procedure=delegate(IntPtr h,uint m,IntPtr w,IntPtr l){return m==0x84?new IntPtr(-1):DefWindowProc(h,m,w,l);};
        [StructLayout(LayoutKind.Sequential)]private struct NativeMessage{public IntPtr Window;public uint Message;public UIntPtr W;public IntPtr L;public uint Time;public int X,Y;public uint Private;}
        [DllImport("user32.dll")]private static extern bool PeekMessage(out NativeMessage message,IntPtr window,uint first,uint last,uint remove);
        [DllImport("user32.dll")]private static extern bool TranslateMessage(ref NativeMessage message);
        [DllImport("user32.dll")]private static extern IntPtr DispatchMessage(ref NativeMessage message);
        [DllImport("user32.dll")]private static extern uint MsgWaitForMultipleObjects(uint count,IntPtr handles,bool all,uint milliseconds,uint wakeMask);
        private static void Shutdown()
        {
            Log.Write("Compositor: cierre iniciado");foreach(var value in new List<CompositorGlass>(windows.Values))value.Dispose();windows.Clear();
            Log.Write("Compositor: superficies liberadas");
            if(queue==null)return;var controller=queue;queue=null;
            try
            {
                // Keep the STA message loop alive while Windows drains its composition queue.
                var action=controller.ShutdownQueueAsync();
                while(action.Status==Windows.Foundation.AsyncStatus.Started)
                {
                    NativeMessage message;while(PeekMessage(out message,IntPtr.Zero,0,0,1)){if(message.Message==0x12)continue;TranslateMessage(ref message);DispatchMessage(ref message);}
                    if(action.Status==Windows.Foundation.AsyncStatus.Started)MsgWaitForMultipleObjects(0,IntPtr.Zero,false,10,0x4FF);
                }
                action.GetResults();action.Close();Log.Write("Compositor: cola cerrada");
            }
            catch(Exception ex){Log.Write("Cierre del compositor: "+ex.Message);}
            if(compositor!=null){compositor.Dispose();compositor=null;}
        }
        private static bool registered;
        private static Windows.System.DispatcherQueueController queue;
        private static Compositor compositor;
        private static readonly Dictionary<Window,CompositorGlass> windows=new Dictionary<Window,CompositorGlass>();
        private readonly Window window;
        private DesktopWindowTarget target;
        private SpriteVisual visual;
        private CompositionRoundedRectangleGeometry geometry;
        private CompositionGeometricClip clip;
        private CompositionBrush brush;
        private IntPtr backgroundWindow;
        private HwndSource source;
        private bool disposed;
        private float cachedWidth=-1,cachedHeight=-1,cachedRemaining=-1,cachedRadius=-1,cachedOpacity=-1;
        private bool cachedVisible,cachedBackdrop,positionKnown;
        private Native.RECT cachedBounds;
        private CompositorGlass(Window window)
        {
            this.window=window;
            try{Initialize();}catch{Dispose();throw;}
        }
        private void Initialize()
        {
            if(compositor==null)
            {
                IntPtr pointer;Marshal.ThrowExceptionForHR(CreateDispatcherQueueController(new QueueOptions{Size=12,Thread=2,Apartment=2},out pointer));
                try{queue=(Windows.System.DispatcherQueueController)Marshal.GetObjectForIUnknown(pointer);}finally{Marshal.Release(pointer);}
                compositor=new Compositor();if(Application.Current!=null)Application.Current.Exit+=delegate{Shutdown();};
            }
            if(!registered){var wc=new WindowClass{Size=(uint)Marshal.SizeOf(typeof(WindowClass)),Procedure=procedure,Instance=GetModuleHandle(null),Name="MacFlow.ClippedBackdrop"};if(RegisterClassEx(ref wc)==0)throw new InvalidOperationException("No se pudo crear la clase de cristal.");registered=true;}
            backgroundWindow=CreateWindowEx(0x00200000|0x08000000|0x80|0x20,"MacFlow.ClippedBackdrop","",0x80000000,0,0,1,1,IntPtr.Zero,IntPtr.Zero,GetModuleHandle(null),IntPtr.Zero);
            if(backgroundWindow==IntPtr.Zero)throw new InvalidOperationException("No se pudo crear el fondo de cristal.");
            int enabled=1;Native.DwmSetWindowAttribute(backgroundWindow,17,ref enabled,4);Native.DwmSetWindowAttribute(backgroundWindow,33,ref enabled,4);
            IntPtr raw;((IDesktopInterop)(object)compositor).CreateDesktopWindowTarget(backgroundWindow,true,out raw);
            try{target=(DesktopWindowTarget)Marshal.GetObjectForIUnknown(raw);}finally{Marshal.Release(raw);}
            brush=compositor.CreateHostBackdropBrush();visual=compositor.CreateSpriteVisual();visual.Brush=brush;
            geometry=compositor.CreateRoundedRectangleGeometry();clip=compositor.CreateGeometricClip(geometry);visual.Clip=clip;target.Root=visual;
            source=HwndSource.FromHwnd(new WindowInteropHelper(window).Handle);if(source!=null)source.AddHook(Hook);
            window.Closed+=Closed;window.IsVisibleChanged+=Visibility;Update(1);
        }
        internal static bool Apply(Window window)
        {
            if(window.AllowsTransparency||!LiquidMaterial.LiveBackdrop||SystemParameters.HighContrast)return false;
            CompositorGlass value;
            if(windows.TryGetValue(window,out value)){value.Update(1);return true;}
            Native.RECT bounds;if(!Native.GetWindowRect(new WindowInteropHelper(window).Handle,out bounds))return false;bool onScreen=false;
            foreach(var screen in System.Windows.Forms.Screen.AllScreens)if(screen.Bounds.IntersectsWith(System.Drawing.Rectangle.FromLTRB(bounds.Left,bounds.Top,bounds.Right,bounds.Bottom))){onScreen=true;break;}
            if(!onScreen)return false;
            try{value=new CompositorGlass(window);windows.Add(window,value);return true;}
            catch(Exception ex){Log.Write("Composición recortada: "+ex.Message);return false;}
        }
        internal static void Resize(Window window,double fraction)
        {CompositorGlass value;if(windows.TryGetValue(window,out value))value.Update(fraction);}
        internal static bool Active(Window window){return windows.ContainsKey(window);}
        internal static void Reorder()
        {foreach(var value in windows.Values)if(!value.disposed&&value.window.IsVisible){var h=new WindowInteropHelper(value.window).Handle;if(Native.GetWindow(value.backgroundWindow,3)!=h)Native.SetWindowPos(value.backgroundWindow,h,0,0,0,0,0x13);}}
        private static bool reorderPending;
        internal static void QueueReorder()
        {var app=Application.Current;if(reorderPending||app==null||app.Dispatcher.HasShutdownStarted)return;reorderPending=true;app.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Render,new Action(delegate{reorderPending=false;Reorder();}));}
        private void Update(double fraction)
        {
            if(disposed)return;lastFraction=fraction;var source=PresentationSource.FromVisual(window);if(source==null)return;
            double scale=source.CompositionTarget.TransformToDevice.M11;float width=(float)Math.Max(1,window.ActualWidth*scale),height=(float)Math.Max(1,window.ActualHeight*scale),remaining=(float)Math.Max(.001,Math.Min(1,fraction));
            float radius=(float)Math.Min(Native.GlassRadius(window)*scale,Math.Min(width,height)*remaining/2);
            if(width!=cachedWidth||height!=cachedHeight||remaining!=cachedRemaining)
            {visual.Size=new Vector2(width,height);geometry.Offset=new Vector2(width*(1-remaining)/2,height*(1-remaining)/2);geometry.Size=new Vector2(width*remaining,height*remaining);cachedWidth=width;cachedHeight=height;cachedRemaining=remaining;}
            if(radius!=cachedRadius){geometry.CornerRadius=new Vector2(radius,radius);cachedRadius=radius;}
            bool clear=window.Title.Contains("Dock")||window.Title.Contains("barra superior")||window.Title.Contains("Centro de control")||window.Title.Contains("Conexiones")||window.Title.Contains("Widget ·");float opacity=(float)(window.Opacity*BackdropCoverage(Design.GlassOpacity,clear));
            if(cachedOpacity!=opacity){cachedOpacity=opacity;visual.Opacity=cachedOpacity;}
            if(cachedBackdrop!=LiquidMaterial.LiveBackdrop||!positionKnown){cachedBackdrop=LiquidMaterial.LiveBackdrop;visual.IsVisible=cachedBackdrop;}
            Native.RECT bounds;var h=new WindowInteropHelper(window).Handle;bool visible=window.IsVisible&&window.WindowState!=WindowState.Minimized;
            if(Native.GetWindowRect(h,out bounds)&&(!positionKnown||cachedVisible!=visible||bounds.Left!=cachedBounds.Left||bounds.Top!=cachedBounds.Top||bounds.Right!=cachedBounds.Right||bounds.Bottom!=cachedBounds.Bottom))
            {Native.SetWindowPos(backgroundWindow,h,bounds.Left,bounds.Top,bounds.Right-bounds.Left,bounds.Bottom-bounds.Top,visible?0x50u:0x90u);cachedBounds=bounds;cachedVisible=visible;positionKnown=true;}
        }
        private IntPtr Hook(IntPtr h,int message,IntPtr w,IntPtr l,ref bool handled){if(message==0x47&&visual!=null&&!disposed)try{Update(PanelMotion.IsTransitioning(window)?lastFraction:1);QueueReorder();}catch(Exception ex){Log.Write("Cristal suspendido: "+ex.Message);}return IntPtr.Zero;}
        private double lastFraction=1;
        internal static double BackdropCoverage(double intensity,bool clear=false)
        {return (clear?.80:.88)+(clear?.14:.10)*Math.Max(0,Math.Min(1,(intensity-.06)/.59));}
        private void Visibility(object sender,DependencyPropertyChangedEventArgs e){Update(lastFraction);}
        private void Closed(object sender,EventArgs e){Dispose();windows.Remove(window);}
        public void Dispose()
        {if(disposed)return;disposed=true;window.Closed-=Closed;window.IsVisibleChanged-=Visibility;if(source!=null)source.RemoveHook(Hook);if(target!=null)target.Root=null;if(visual!=null)visual.Dispose();if(clip!=null)clip.Dispose();if(geometry!=null)geometry.Dispose();if(brush!=null)brush.Dispose();if(target!=null)target.Dispose();if(backgroundWindow!=IntPtr.Zero){DestroyWindow(backgroundWindow);backgroundWindow=IntPtr.Zero;}}
    }
}

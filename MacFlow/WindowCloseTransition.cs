using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace MacFlow
{
    // Animate our short-lived representation after a normal close/minimize succeeds.
    // Never hide, resize, cloak, restyle or inject into the source application.
    internal static class WindowCloseTransition
    {
        [DllImport("user32.dll")]private static extern bool PrintWindow(IntPtr window,IntPtr dc,uint flags);
        [DllImport("gdi32.dll")]private static extern bool DeleteObject(IntPtr value);
        private static readonly HashSet<IntPtr> pending=new HashSet<IntPtr>();
        private static readonly object captureGate=new object();
        private static bool capturing;
        internal static event Action<Window> PreviewStarted;
        internal static bool Request(WindowEntry window,Action failure,bool minimize=false)
        {
            if(!Native.Matches(window))return false;
            if(!Motion.Enabled||Native.IsIconic(window.Handle))return Send(window,minimize);
            if(!pending.Add(window.Handle))return true;
            Run(window,failure,minimize);return true;
        }
        private static bool Send(WindowEntry entry,bool minimize){return minimize?Native.RequestMinimize(entry):Native.RequestClose(entry);}
        private static async void Run(WindowEntry entry,Action failure,bool minimize)
        {
            Window preview=null;
            try
            {
                Native.RECT bounds;if(!Native.GetWindowRect(entry.Handle,out bounds)){if(!Send(entry,minimize)&&failure!=null)failure();return;}
                var capture=Task.Run(()=>Capture(entry,bounds));var finished=await Task.WhenAny(capture,Task.Delay(240));
                BitmapSource image=finished==capture?await capture:null;
                if(!Native.Matches(entry))return;
                if(image!=null)
                {
                    double scale=DisplaySpace.ForHandle(entry.Handle).Scale;
                    preview=new Window{Title=minimize?"MacFlow · Minimizar ventana":"MacFlow · Cerrar ventana",WindowStyle=WindowStyle.None,ResizeMode=ResizeMode.NoResize,AllowsTransparency=true,Background=Brushes.Transparent,ShowInTaskbar=false,ShowActivated=false,Topmost=true,IsHitTestVisible=false,Width=(bounds.Right-bounds.Left)/scale,Height=(bounds.Bottom-bounds.Top)/scale,Left=bounds.Left/scale,Top=bounds.Top/scale,Content=new Image{Source=image,Stretch=Stretch.Fill}};
                    preview.SourceInitialized+=delegate{Native.NoActivate(preview);var h=new WindowInteropHelper(preview).Handle;Native.SetWindowLong(h,-20,Native.GetWindowLong(h,-20)|0x20);};preview.Show();
                    Native.SetWindowPos(new WindowInteropHelper(preview).Handle,new IntPtr(-1),bounds.Left,bounds.Top,bounds.Right-bounds.Left,bounds.Bottom-bounds.Top,0x50);
                }
                if(!Send(entry,minimize)){if(failure!=null)failure();return;}
                for(int i=0;i<12&&Native.Matches(entry)&&(!minimize||!Native.IsIconic(entry.Handle));i++)
                {
                    await Task.Delay(25);var foreground=Native.GetForegroundWindow();uint pid;Native.GetWindowThreadProcessId(foreground,out pid);
                    // A save/confirmation dialog must remain fully visible.
                    if(!minimize&&foreground!=entry.Handle&&pid==entry.ProcessId&&Native.GetWindow(foreground,4)!=IntPtr.Zero)break;
                }
                if(preview!=null&&(minimize?Native.Matches(entry)&&Native.IsIconic(entry.Handle):!Native.Matches(entry)))
                {
                    var started=PreviewStarted;if(started!=null)started(preview);
                    Design.Dismiss(preview);await Task.Delay(450);
                }
            }
            catch(Exception ex){Log.Write("Transición hacia el Dock: "+ex.Message);if(Native.Matches(entry)&&!Send(entry,minimize)&&failure!=null)failure();}
            finally{if(preview!=null&&preview.IsVisible)preview.Close();pending.Remove(entry.Handle);}
        }
        private static BitmapSource Capture(WindowEntry entry,Native.RECT bounds)
        {
            lock(captureGate){if(capturing)return null;capturing=true;}
            try
            {
                int width=bounds.Right-bounds.Left,height=bounds.Bottom-bounds.Top;if(width<=0||height<=0||(long)width*height>8000000||!Native.Matches(entry))return null;
                using(var bitmap=new System.Drawing.Bitmap(width,height,System.Drawing.Imaging.PixelFormat.Format24bppRgb))using(var graphics=System.Drawing.Graphics.FromImage(bitmap))
                {
                    IntPtr dc=graphics.GetHdc();bool rendered;try{rendered=PrintWindow(entry.Handle,dc,2);}finally{graphics.ReleaseHdc(dc);}if(!rendered)return null;
                    int min=255,max=0;for(int y=height/8;y<height;y+=Math.Max(1,height/8))for(int x=width/8;x<width;x+=Math.Max(1,width/8)){var pixel=bitmap.GetPixel(x,y);int light=(pixel.R+pixel.G+pixel.B)/3;min=Math.Min(min,light);max=Math.Max(max,light);}if(max-min<8)return null;
                    IntPtr handle=bitmap.GetHbitmap();try{var result=Imaging.CreateBitmapSourceFromHBitmap(handle,IntPtr.Zero,Int32Rect.Empty,BitmapSizeOptions.FromEmptyOptions());result.Freeze();return result;}finally{DeleteObject(handle);}
                }
            }
            catch{return null;}finally{lock(captureGate)capturing=false;}
        }
    }
}

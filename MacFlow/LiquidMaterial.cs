using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
namespace MacFlow
{
    public sealed class LiquidLens : ShaderEffect
    {
        private static readonly PixelShader shader=Load();
        private static PixelShader Load(){var value=new PixelShader();using(var stream=typeof(LiquidLens).Assembly.GetManifestResourceStream("MacFlow.LiquidGlass.ps"))value.SetStreamSource(stream);value.Freeze();return value;}
        public static readonly DependencyProperty InputProperty=RegisterPixelShaderSamplerProperty("Input",typeof(LiquidLens),0);
        public static readonly DependencyProperty SizeProperty=DependencyProperty.Register("Size",typeof(Point),typeof(LiquidLens),new UIPropertyMetadata(new Point(400,200),PixelShaderConstantCallback(0)));
        public static readonly DependencyProperty RadiusProperty=DependencyProperty.Register("Radius",typeof(double),typeof(LiquidLens),new UIPropertyMetadata(26.0,PixelShaderConstantCallback(1)));
        public static readonly DependencyProperty TintProperty=DependencyProperty.Register("Tint",typeof(Color),typeof(LiquidLens),new UIPropertyMetadata(Color.FromArgb(24,255,255,255),PixelShaderConstantCallback(2)));
        public static readonly DependencyProperty StrengthProperty=DependencyProperty.Register("Strength",typeof(double),typeof(LiquidLens),new UIPropertyMetadata(3.0,PixelShaderConstantCallback(3)));
        public static readonly DependencyProperty ReadabilityProperty=DependencyProperty.Register("Readability",typeof(double),typeof(LiquidLens),new UIPropertyMetadata(0.0,PixelShaderConstantCallback(4)));
        public static readonly DependencyProperty PointerProperty=DependencyProperty.Register("Pointer",typeof(Point),typeof(LiquidLens),new UIPropertyMetadata(new Point(.3,.2),PixelShaderConstantCallback(5)));
        public Brush Input{get{return (Brush)GetValue(InputProperty);}set{SetValue(InputProperty,value);}}
        public Point Size{get{return (Point)GetValue(SizeProperty);}set{SetValue(SizeProperty,value);}}
        public double Radius{get{return (double)GetValue(RadiusProperty);}set{SetValue(RadiusProperty,value);}}
        public Color Tint{get{return (Color)GetValue(TintProperty);}set{SetValue(TintProperty,value);}}
        public double Strength{get{return (double)GetValue(StrengthProperty);}set{SetValue(StrengthProperty,value);}}
        public double Readability{get{return (double)GetValue(ReadabilityProperty);}set{SetValue(ReadabilityProperty,value);}}
        public Point Pointer{get{return (Point)GetValue(PointerProperty);}set{SetValue(PointerProperty,value);}}
        public LiquidLens(){PixelShader=shader;UpdateShaderValue(InputProperty);UpdateShaderValue(SizeProperty);UpdateShaderValue(RadiusProperty);UpdateShaderValue(TintProperty);UpdateShaderValue(StrengthProperty);UpdateShaderValue(ReadabilityProperty);UpdateShaderValue(PointerProperty);}
    }
    public sealed class LiquidMaterial : IDisposable
    {
        private static readonly Dictionary<Window,LiquidMaterial> instances=new Dictionary<Window,LiquidMaterial>();
        private static readonly object CaptureGate=new object();
        private static volatile bool captureSuspended;
        [DllImport("user32.dll",SetLastError=true)] private static extern bool SetWindowDisplayAffinity(IntPtr window,uint affinity);
        [DllImport("dwmapi.dll")] private static extern int DwmFlush();
        [DllImport("user32.dll")]private static extern IntPtr GetDC(IntPtr window);
        [DllImport("user32.dll")]private static extern int ReleaseDC(IntPtr window,IntPtr dc);
        [DllImport("gdi32.dll",SetLastError=true)]private static extern bool StretchBlt(IntPtr target,int x,int y,int width,int height,IntPtr source,int sourceX,int sourceY,int sourceWidth,int sourceHeight,uint operation);
        [DllImport("gdi32.dll",SetLastError=true)]private static extern int SetStretchBltMode(IntPtr dc,int mode);
        [DllImport("gdi32.dll",SetLastError=true)]private static extern bool SetBrushOrgEx(IntPtr dc,int x,int y,IntPtr previous);
        [StructLayout(LayoutKind.Sequential)]private struct SamplePoint{public int X,Y;}
        [DllImport("user32.dll")]private static extern IntPtr WindowFromPoint(SamplePoint point);
        [DllImport("user32.dll")]private static extern IntPtr GetAncestor(IntPtr handle,uint flags);
        [DllImport("gdi32.dll")]private static extern uint GetPixel(IntPtr dc,int x,int y);
        private readonly Window window;private readonly Image image;private readonly Border lensLayer;private readonly LiquidLens lens;private readonly DispatcherTimer timer;
        private Grid surface;private Border readabilityLayer;private CrystalLight light;private System.Drawing.Bitmap capture;private System.Drawing.Graphics graphics;private WriteableBitmap texture;private byte[] pixels;
        private bool wrapping,enabled,captureBusy,lightText,toneKnown;private volatile bool disposed;private int quietFrames;private long lastHash;
        private readonly DispatcherTimer toneTimer;private bool requestedDark,toneBusy;private Border materialFace;
        public static bool LowPower,LiveBackdrop=true;
        internal event Action BackdropChanged;
        internal ImageSource BackdropTexture {get{return enabled?texture:null;}}
        internal double LeftInset {get{return Inset;}}
        internal double UpperInset {get{return TopInset;}}
        internal double TextureScale {get{return .5;}}
        private double nativeReadability;
        private int stableTone;
        internal double Readability {get{return lens==null?nativeReadability:lens.Readability;}}
        internal static LiquidMaterial ForWindow(Window target){LiquidMaterial result;return target!=null&&instances.TryGetValue(target,out result)?result:null;}
        internal static void DesktopChanged(){foreach(var material in instances.Values){if(material.disposed||!material.window.IsVisible)continue;if(!material.window.AllowsTransparency){material.RefreshContrast();continue;}if(!material.enabled)continue;material.quietFrames=0;material.timer.Interval=TimeSpan.FromMilliseconds(166);material.Refresh();}}
        public static void Apply(Window window,bool dark)
        {
            LiquidMaterial material;if(!instances.TryGetValue(window,out material)){material=new LiquidMaterial(window);instances.Add(window,material);}material.Update(dark);
        }
        public static void SetLensForTest(Window window,double strength)
        {LiquidMaterial material;if(instances.TryGetValue(window,out material)&&material.lens!=null)material.lens.Strength=strength;}
        private LiquidMaterial(Window target)
        {
            window=target;image=new Image {Stretch=Stretch.Fill,IsHitTestVisible=false};
            lensLayer=new Border {Child=image,IsHitTestVisible=false};
            if(window.AllowsTransparency)
            {image.Effect=new BlurEffect{Radius=18,RenderingBias=RenderingBias.Performance};lens=new LiquidLens();lensLayer.CacheMode=new BitmapCache(.75);if(RenderCapability.IsPixelShaderVersionSupported(3,0))lensLayer.Effect=lens;}
            timer=new DispatcherTimer(DispatcherPriority.Background){Interval=TimeSpan.FromMilliseconds(166)};timer.Tick+=delegate{Refresh();};
            toneTimer=new DispatcherTimer(DispatcherPriority.Background){Interval=TimeSpan.FromMilliseconds(400)};toneTimer.Tick+=delegate{toneTimer.Interval=TimeSpan.FromMilliseconds(window.IsMouseOver?350:stableTone>=3?LowPower?2500:1400:600);RefreshContrast();};window.MouseEnter+=delegate{stableTone=0;RefreshContrast();};
            var descriptor=System.ComponentModel.DependencyPropertyDescriptor.FromProperty(ContentControl.ContentProperty,typeof(Window));descriptor.AddValueChanged(window,ContentChanged);
            window.SizeChanged+=delegate{Resize();};window.LocationChanged+=delegate{quietFrames=0;timer.Interval=TimeSpan.FromMilliseconds(LowPower?333:166);};window.IsVisibleChanged+=delegate{if(window.IsVisible&&enabled){Refresh();timer.Start();}else timer.Stop();if(window.IsVisible&&!SystemParameters.HighContrast)toneTimer.Start();else toneTimer.Stop();};
            window.Closed+=delegate{descriptor.RemoveValueChanged(window,ContentChanged);Dispose();instances.Remove(window);};Wrap();
        }
        private void ContentChanged(object sender,EventArgs args){if(!wrapping)Wrap();}
        private void Wrap()
        {
            if(surface!=null&&window.Content==surface)return;var content=window.Content as UIElement;wrapping=true;
            try
            {
                window.Content=null;if(surface!=null)surface.Children.Clear();if(lensLayer.Parent is Border)((Border)lensLayer.Parent).Child=null;
                // Move the former outer margin into content padding. Keep every
                // control at its original position, with one full-sized material.
                var glass=content as Border;materialFace=glass;
                if(glass!=null&&!(bool)glass.GetValue(PaddingMerged))
                {double gap=window.Title.Contains("barra superior")||window.Title.Contains("Dock")?0:2;var p=glass.Padding;glass.Padding=new Thickness(p.Left+gap,p.Top+gap,p.Right+gap,p.Bottom+gap);glass.SetValue(PaddingMerged,true);}
                surface=new Grid {Background=Brushes.Transparent};var shadow=new Border {Margin=new Thickness(Inset,TopInset,Inset,Inset),Child=lensLayer};
                if(Inset>0)shadow.Effect=new DropShadowEffect {BlurRadius=10,ShadowDepth=2,Opacity=.10,Color=Color.FromRgb(19,35,56),RenderingBias=RenderingBias.Performance};
                surface.Children.Add(shadow);readabilityLayer=new Border{Margin=new Thickness(Inset,TopInset,Inset,Inset),CornerRadius=new CornerRadius(Radius),IsHitTestVisible=false};surface.Children.Add(readabilityLayer);if(content!=null)surface.Children.Add(new Border {Margin=new Thickness(Inset,TopInset,Inset,Inset),Child=content});
                light=new CrystalLight(Radius,window.Title.Contains("barra superior")?.4:window.Title.Contains("Dock")?.95:1){Margin=new Thickness(Inset,TopInset,Inset,Inset)};surface.Children.Add(light);window.Content=surface;Resize();
            }
            finally{wrapping=false;}
        }
        private static readonly DependencyProperty PaddingMerged=DependencyProperty.RegisterAttached("PaddingMerged",typeof(bool),typeof(LiquidMaterial),new PropertyMetadata(false));
        private double Inset{get{return 0;}}
        private double TopInset{get{return Inset;}}
        private double Radius{get{return Native.GlassRadius(window);}}
        private void Resize()
        {
            if(surface==null)return;double width=Math.Max(1,window.ActualWidth-Inset*2),height=Math.Max(1,window.ActualHeight-TopInset-Inset);lensLayer.Clip=new RectangleGeometry(new Rect(0,0,width,height),Radius,Radius);if(lens!=null){lens.Size=new Point(width,height);lens.Radius=Radius;}
            if(light!=null)light.SetCorner(Radius);
        }
        private void Update(bool dark)
        {
            // The Windows compositor samples behind the HWND directly. No screen
            // copies or capture exclusion are needed in the recordable backend.
            if(!window.AllowsTransparency)
            {
                enabled=false;timer.Stop();SetWindowDisplayAffinity(new WindowInteropHelper(window).Handle,0);lensLayer.Visibility=Visibility.Collapsed;surface.Background=Brushes.Transparent;
                if(!toneKnown||requestedDark!=dark){requestedDark=dark;toneKnown=false;SetReadability(dark);}if(window.IsVisible&&!SystemParameters.HighContrast)toneTimer.Start();Resize();return;
            }
            window.Background=Brushes.Transparent;var source=PresentationSource.FromVisual(window) as HwndSource;if(source!=null)source.CompositionTarget.BackgroundColor=Colors.Transparent;
            lens.Tint=dark?Color.FromArgb((byte)Math.Round(255*(.04+Design.GlassOpacity*.16)),22,22,24):Color.FromArgb((byte)Math.Round(255*(.025+Design.GlassOpacity*.15)),255,255,255);
            toneKnown=false;SetReadability(dark);lens.Strength=6.0;
            enabled=false;var h=new WindowInteropHelper(window).Handle;bool affinity=SetWindowDisplayAffinity(h,0);
            if(enabled&&!affinity){enabled=false;Log.Write("Fondo vivo no disponible: "+Marshal.GetLastWin32Error());}
            lensLayer.Visibility=enabled?Visibility.Visible:Visibility.Collapsed;surface.Background=SystemParameters.HighContrast?SystemColors.WindowBrush:Brushes.Transparent;
            UpdateReadabilityLayer();
            quietFrames=0;timer.Interval=TimeSpan.FromMilliseconds(LowPower?333:166);
            window.Tag=enabled?(lensLayer.Effect!=null?"Live backdrop + GPU lens displacement":"Live backdrop + blur; lens unavailable"):"Clear glass fallback";if(window.IsVisible&&enabled){Refresh();timer.Start();}else timer.Stop();Resize();
        }
        private async void Refresh()
        {
            if(disposed||captureSuspended||!enabled||captureBusy||!window.IsVisible||window.WindowState==WindowState.Minimized)return;
            Native.RECT rect;var h=new WindowInteropHelper(window).Handle;if(!Native.GetWindowRect(h,out rect))return;
            var source=PresentationSource.FromVisual(window);double scale=source==null?1:source.CompositionTarget.TransformToDevice.M11;int inset=(int)Math.Round(Inset*scale),top=(int)Math.Round(TopInset*scale);rect.Left+=inset;rect.Top+=top;rect.Right-=inset;rect.Bottom-=inset;
            int sourceWidth=rect.Right-rect.Left,sourceHeight=rect.Bottom-rect.Top;if(sourceWidth<=0||sourceHeight<=0||sourceWidth>6000||sourceHeight>4000)return;
            // Downsample only the blurred backdrop, never foreground text/icons.
            int width=Math.Max(1,(int)Math.Ceiling(sourceWidth*TextureScale)),height=Math.Max(1,(int)Math.Ceiling(sourceHeight*TextureScale));captureBusy=true;
            try
            {
                var result=await System.Threading.Tasks.Task.Run(delegate
                {
                    lock(CaptureGate)
                    {
                        if(disposed)return new double[]{0,0};
                        if(capture==null||capture.Width!=width||capture.Height!=height){if(graphics!=null)graphics.Dispose();if(capture!=null)capture.Dispose();capture=new System.Drawing.Bitmap(width,height,System.Drawing.Imaging.PixelFormat.Format32bppArgb);graphics=System.Drawing.Graphics.FromImage(capture);pixels=new byte[width*height*4];}
                        IntPtr screen=GetDC(IntPtr.Zero),destination=IntPtr.Zero;
                        try
                        {
                            destination=graphics.GetHdc();
                            // Average color samples instead of GDI's monochrome AND scan mode.
                            // This preserves smooth colored reflections when shrinking the backdrop.
                            if(SetStretchBltMode(destination,4)==0||!SetBrushOrgEx(destination,0,0,IntPtr.Zero))throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                            if(screen==IntPtr.Zero||!StretchBlt(destination,0,0,width,height,screen,rect.Left,rect.Top,sourceWidth,sourceHeight,0x00CC0020))throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                        }
                        finally{if(destination!=IntPtr.Zero)graphics.ReleaseHdc(destination);if(screen!=IntPtr.Zero)ReleaseDC(IntPtr.Zero,screen);}
                        var data=capture.LockBits(new System.Drawing.Rectangle(0,0,width,height),System.Drawing.Imaging.ImageLockMode.ReadOnly,System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                        try{Marshal.Copy(data.Scan0,pixels,0,pixels.Length);}finally{capture.UnlockBits(data);}
                        long hash=17;double luminance=0;int samples=0;
                        for(int i=0;i<pixels.Length-4;i+=1004){hash=unchecked(hash*31+pixels[i]+pixels[i+1]*3+pixels[i+2]*7);luminance+=(pixels[i]*.0722+pixels[i+1]*.7152+pixels[i+2]*.2126)/255;samples++;}
                        for(int i=3;i<pixels.Length;i+=4)pixels[i]=255;
                        // Keep hash integral despite the compact result payload.
                        return new double[]{luminance/Math.Max(1,samples),hash%1000000007};
                    }
                });
                if(disposed||captureSuspended)return;double average=result[0];bool next=window.Title.Contains("Configuración")||window.Title.Contains("Aplicaciones")||window.Title.Contains("Conexiones")?Design.Dark:!toneKnown?average<.50:lightText?average<.62:average<.38;
                if(!toneKnown||next!=lightText){toneKnown=true;SetReadability(next);}
                long hashValue=(long)result[1];if(texture==null||texture.PixelWidth!=width||texture.PixelHeight!=height){texture=new WriteableBitmap(width,height,96,96,PixelFormats.Bgra32,null);image.Source=texture;lastHash=0;}
                if(hashValue!=lastHash){quietFrames=0;lastHash=hashValue;texture.WritePixels(new Int32Rect(0,0,width,height),pixels,width*4,0);if(BackdropChanged!=null)BackdropChanged();}else quietFrames++;
                timer.Interval=TimeSpan.FromMilliseconds(window.IsMouseOver?166:quietFrames>=4?(LowPower?500:333):(LowPower?250:166));
            }
            catch(Exception ex){if(!disposed){Log.Write("Cristal: "+ex.Message);enabled=false;timer.Stop();lensLayer.Visibility=Visibility.Collapsed;SetReadability(Design.Dark);SetWindowDisplayAffinity(h,0);window.Tag="Clear glass fallback";}}
            finally{captureBusy=false;if(disposed)ReleaseBuffers();}
        }
        private void SetReadability(bool whiteText)
        {
            string widgetMode=window.TryFindResource("GlassWidgetSurface") as string;if(widgetMode=="Dark")whiteText=true;else if(widgetMode=="Light")whiteText=false;
            lightText=whiteText;nativeReadability=window.Title.Contains("Conexiones")?(whiteText?-.24:.56):(whiteText?-.32:.66);if(lens!=null)lens.Readability=nativeReadability;
            if(materialFace!=null&&!SystemParameters.HighContrast)materialFace.Background=widgetMode=="Dark"?(Brush)Design.Brush("#CE15161A"):widgetMode=="Light"?(Brush)Design.Brush("#CFFFFFFF"):Design.MaterialTint(widgetMode=="Clear"||window.Title.Contains("Dock")||window.Title.Contains("barra superior"),whiteText);
            window.Resources["GlassInk"]=SystemParameters.HighContrast?SystemColors.WindowTextBrush:Design.Brush(whiteText?"#F8FAFF":"#19212D");
            window.Resources["GlassMuted"]=SystemParameters.HighContrast?SystemColors.WindowTextBrush:Design.Brush(whiteText?"#DCE4EF":"#303B4B");
            window.Resources["GlassCardSurface"]=Design.Brush(whiteText?"#161C222B":"#22FFFFFF");window.Resources["GlassTileSurface"]=Design.Brush(whiteText?"#16141A23":"#22FFFFFF");
            UpdateReadabilityLayer();if(BackdropChanged!=null)BackdropChanged();
        }
        internal static bool LightInkForLuminance(double average,bool known,bool wasLight)
        {return !known?average<.52:wasLight?average<.62:average<.42;}
        internal static async System.Threading.Tasks.Task<bool?> WelcomeTone(Window window)
        {
            var handle=new WindowInteropHelper(window).Handle;Native.RECT rect;if(!Native.GetWindowRect(handle,out rect))return null;
            // One read of five unused pixels in our own welcome window. No image
            // is stored; the readback runs off the UI thread and never polls.
            return await System.Threading.Tasks.Task.Run<bool?>(delegate
            {
                double total=0;int count=0;var dc=GetDC(IntPtr.Zero);if(dc==IntPtr.Zero)return null;
                try{for(int i=0;i<5;i++){int x=rect.Left+(int)((rect.Right-rect.Left)*(.18+i*.16)),y=rect.Top+(int)((rect.Bottom-rect.Top)*(i%2==0?.12:.88));var under=WindowFromPoint(new SamplePoint{X=x,Y=y});if(under!=handle&&GetAncestor(under,2)!=handle)continue;uint color=GetPixel(dc,x,y);if(color==0xFFFFFFFF)continue;total+=((color&255)*.2126+((color>>8)&255)*.7152+((color>>16)&255)*.0722)/255;count++;}}finally{ReleaseDC(IntPtr.Zero,dc);}
                WelcomeToneDiagnostic="muestras="+count+"; luminancia="+(count==0?0:total/count).ToString("F3");return count<3?(bool?)null:total/count<.48;
            });
        }
        internal static string WelcomeToneDiagnostic;
        private async void RefreshContrast()
        {
            if(disposed||toneBusy||!window.IsVisible||window.WindowState==WindowState.Minimized||SystemParameters.HighContrast)return;
            string fixedMode=window.TryFindResource("GlassWidgetSurface") as string;if(fixedMode=="Dark"||fixedMode=="Light"){if(!toneKnown||lightText!=(fixedMode=="Dark")){toneKnown=true;SetReadability(fixedMode=="Dark");}stableTone=4;return;}
            var handle=new WindowInteropHelper(window).Handle;Native.RECT rect;if(!Native.GetWindowRect(handle,out rect)||rect.Right-rect.Left<40||rect.Bottom-rect.Top<25)return;
            var source=PresentationSource.FromVisual(window);double dpi=source==null?1:source.CompositionTarget.TransformToDevice.M11;int padding=(int)Math.Round((Inset+10)*dpi);
            // Read five pixels in our own unused glass margin, without saving a
            // screen image or feeding a screenshot back into the material.
            // Skip an occluded pixel so another window cannot determine our ink.
            // GDI readback may wait for the GPU. Never perform it on the UI
            // dispatcher, and allow only one pending contrast read per window.
            toneBusy=true;
            try
            {
                var sample=await System.Threading.Tasks.Task.Run(delegate
                {
                    double total=0;int count=0;IntPtr dc=GetDC(IntPtr.Zero);if(dc==IntPtr.Zero)return new double[]{0,0};
                    try{for(int i=0;i<5;i++){
                        int x=rect.Left+(int)((rect.Right-rect.Left)*(.18+i*.16)),y=i%2==0?rect.Top+padding:rect.Bottom-padding;
                        var under=WindowFromPoint(new SamplePoint{X=x,Y=y});if(under!=handle&&GetAncestor(under,2)!=handle)continue;
                        uint color=GetPixel(dc,x,y);if(color==0xFFFFFFFF)continue;total+=((color&255)*.2126+((color>>8)&255)*.7152+((color>>16)&255)*.0722)/255;count++;
                    }}finally{ReleaseDC(IntPtr.Zero,dc);}return new double[]{total,count};
                });
                Native.RECT current;if(disposed||sample[1]<3||!window.IsVisible||window.WindowState==WindowState.Minimized||!Native.GetWindowRect(handle,out current)||current.Left!=rect.Left||current.Top!=rect.Top||current.Right!=rect.Right||current.Bottom!=rect.Bottom)return;
                bool next=LightInkForLuminance(sample[0]/sample[1],toneKnown,lightText);toneKnown=true;if(next!=lightText){stableTone=0;SetReadability(next);}else stableTone=Math.Min(4,stableTone+1);
            }
            catch(Exception ex){if(!disposed)Log.Write("Contraste del cristal: "+ex.Message);}
            finally{toneBusy=false;}
        }
        private void UpdateReadabilityLayer()
        {
            if(readabilityLayer==null)return;
            // Shader-free/disabled backdrop uses a tint behind content; never
            // opacity, blur or shadow on the text itself.
            readabilityLayer.Background=!window.AllowsTransparency?Brushes.Transparent:SystemParameters.HighContrast||enabled&&lensLayer.Effect!=null?Brushes.Transparent:Design.Brush(lightText?"#BD111820":"#CEFFFFFF");
        }
        public static async System.Threading.Tasks.Task CaptureOwnWindow(Window window,string path)
        {
            var h=new WindowInteropHelper(window).Handle;LiquidMaterial material;bool restore=instances.TryGetValue(window,out material)&&material.enabled;
            // Test capture only. Raise our fixture/window without taking keyboard
            // focus, then wait for the compositor instead of capturing an occluder.
            Window fixture=null;foreach(Window candidate in Application.Current.Windows)if(candidate.Title=="MacFlow · Fondo de prueba"){fixture=candidate;break;}
            try
            {
                if(fixture!=null){Native.SetWindowPos(h,new IntPtr(-1),0,0,0,0,0x13);Native.SetWindowPos(new WindowInteropHelper(fixture).Handle,h,0,0,0,0,0x13);if(material!=null)material.Refresh();await System.Threading.Tasks.Task.Delay(600);}
                captureSuspended=true;
                lock(CaptureGate){SetWindowDisplayAffinity(h,0);Native.SetWindowPos(h,new IntPtr(-1),0,0,0,0,0x13);}
                CompositorGlass.Resize(window,1);CompositorGlass.Reorder();
                await System.Threading.Tasks.Task.Delay(100);
                lock(CaptureGate){DwmFlush();Native.RECT r;if(!Native.GetWindowRect(h,out r))throw new System.ComponentModel.Win32Exception();using(var bitmap=new System.Drawing.Bitmap(r.Right-r.Left,r.Bottom-r.Top))using(var g=System.Drawing.Graphics.FromImage(bitmap)){g.CopyFromScreen(r.Left,r.Top,0,0,bitmap.Size);bitmap.Save(path,System.Drawing.Imaging.ImageFormat.Png);}}
            }
            finally
            {
                lock(CaptureGate){SetWindowDisplayAffinity(h,0);captureSuspended=false;}
                // Keep the isolated backdrop immediately behind its test window.
                // Sending it below unrelated apps raced the asynchronous contrast
                // probe and changed the tested background after the screenshot.
                if(fixture!=null&&window.IsVisible)Native.SetWindowPos(new WindowInteropHelper(fixture).Handle,h,0,0,0,0,0x13);
            }
        }
        private void ReleaseBuffers(){if(graphics!=null){graphics.Dispose();graphics=null;}if(capture!=null){capture.Dispose();capture=null;}texture=null;pixels=null;lastHash=0;}
        public void Dispose(){if(disposed)return;disposed=true;timer.Stop();toneTimer.Stop();BackdropChanged=null;SetWindowDisplayAffinity(new WindowInteropHelper(window).Handle,0);if(!captureBusy)ReleaseBuffers();image.Source=null;}
    }
}

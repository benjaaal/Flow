using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Forms=System.Windows.Forms;
namespace MacFlow
{
    internal sealed class LidFold : ShaderEffect
    {
        private static readonly PixelShader shader=Load();
        private static PixelShader Load(){var value=new PixelShader();using(var stream=typeof(LidFold).Assembly.GetManifestResourceStream("MacFlow.LidFold.ps"))value.SetStreamSource(stream);value.Freeze();return value;}
        public static readonly DependencyProperty InputProperty=RegisterPixelShaderSamplerProperty("Input",typeof(LidFold),0);
        public static readonly DependencyProperty ProgressProperty=DependencyProperty.Register("Progress",typeof(double),typeof(LidFold),new UIPropertyMetadata(0.0,PixelShaderConstantCallback(0)));
        public static readonly DependencyProperty PixelProperty=DependencyProperty.Register("Pixel",typeof(Point),typeof(LidFold),new UIPropertyMetadata(new Point(1.0/1280,1.0/720),PixelShaderConstantCallback(1)));
        public double Progress{get{return (double)GetValue(ProgressProperty);}set{SetValue(ProgressProperty,value);}}
        internal LidFold(){PixelShader=shader;UpdateShaderValue(InputProperty);UpdateShaderValue(ProgressProperty);UpdateShaderValue(PixelProperty);}
    }
    internal sealed class LidTransition : IDisposable
    {
        private static readonly Guid LidGuid=new Guid("BA3E0F4D-B817-4094-A2D1-D56379E6A0F3");
        [DllImport("user32.dll")]private static extern IntPtr RegisterPowerSettingNotification(IntPtr recipient,ref Guid guid,uint flags);
        [DllImport("user32.dll")]private static extern bool UnregisterPowerSettingNotification(IntPtr handle);
        [DllImport("user32.dll")]private static extern IntPtr OpenInputDesktop(uint flags,bool inherit,uint access);
        [DllImport("user32.dll",CharSet=CharSet.Unicode)]private static extern bool GetUserObjectInformation(IntPtr handle,int index,System.Text.StringBuilder name,int count,out int needed);
        [DllImport("user32.dll")]private static extern bool CloseDesktop(IntPtr desktop);
        private readonly Window host;private readonly Func<bool> enabled;private readonly HwndSource source;private readonly HwndSourceHook hook;
        private IntPtr registration;private bool? lidOpen;private bool disposed,pendingOpen;private string failure;
        private Window overlay;internal Window View{get{return overlay;}}private Image image;private LidFold effect;private LidPlane plane;private double Progress{get{return plane!=null?plane.Progress:effect==null?0:effect.Progress;}set{if(plane!=null)plane.Progress=value;else if(effect!=null)effect.Progress=value;}}private bool following,rendering;private double wanted;private TimeSpan? previousFrame;private readonly System.Windows.Threading.DispatcherTimer timeout;
        private readonly HingeTracker hinge;
        internal string Status{get{return (failure==null?"":failure+" · ")+hinge.Status;}}
        internal LidTransition(Window owner,Func<bool> isEnabled)
        {
            host=owner;enabled=isEnabled;source=HwndSource.FromHwnd(new WindowInteropHelper(owner).Handle);hook=Handle;source.AddHook(hook);
            Guid id=LidGuid;registration=RegisterPowerSettingNotification(source.Handle,ref id,0);
            if(registration==IntPtr.Zero)Log.Write("Tapa: el dispositivo no registró avisos; la prueba manual sigue disponible.");
            timeout=new System.Windows.Threading.DispatcherTimer{Interval=TimeSpan.FromMilliseconds(1100)};timeout.Tick+=delegate{timeout.Stop();if(following){wanted=0;Listen();}else Cancel();};SystemEvents.SessionSwitch+=Session;
            hinge=new HingeTracker(host.Dispatcher,ObserveAngle);
        }
        private void Session(object sender,SessionSwitchEventArgs args){if(disposed||host.Dispatcher.HasShutdownStarted)return;host.Dispatcher.BeginInvoke(new Action(async delegate{if(disposed)return;Cancel();if(args.Reason==SessionSwitchReason.SessionUnlock&&enabled()){await System.Threading.Tasks.Task.Delay(350);if(!disposed&&enabled()){pendingOpen=false;Play(true,false);}}}));}
        internal static bool? ParseSetting(IntPtr payload)
        {
            if(payload==IntPtr.Zero)return null;Guid id=(Guid)Marshal.PtrToStructure(payload,typeof(Guid));if(id!=LidGuid||Marshal.ReadInt32(payload,16)!=4)return null;int state=Marshal.ReadInt32(payload,20);return state==0?(bool?)false:state==1?(bool?)true:null;
        }
        private IntPtr Handle(IntPtr hwnd,int message,IntPtr w,IntPtr l,ref bool handled)
        {
            if(disposed||message!=0x218)return IntPtr.Zero;
            if(w.ToInt32()==4){pendingOpen=enabled();Cancel();return IntPtr.Zero;}
            if(w.ToInt32()==18){pendingOpen=enabled();return IntPtr.Zero;}
            if(w.ToInt32()!=0x8013)return IntPtr.Zero;var state=ParseSetting(l);if(!state.HasValue)return IntPtr.Zero;
            // The first notification describes the current state, not a movement.
            bool changed=lidOpen.HasValue&&lidOpen.Value!=state.Value;lidOpen=state;
            if(changed&&enabled()&&!hinge.Available){pendingOpen=!state.Value||!Unlocked();if(!state.Value||Unlocked())Play(state.Value,false);}return IntPtr.Zero;
        }
        private static bool Unlocked()
        {
            var desktop=OpenInputDesktop(0,false,1);if(desktop==IntPtr.Zero)return false;try{var name=new System.Text.StringBuilder(80);int needed;return GetUserObjectInformation(desktop,2,name,160,out needed)&&name.ToString()=="Default";}finally{CloseDesktop(desktop);}
        }
        internal bool Preview(){if(disposed)return false;Play(false,true);return overlay!=null;}
        internal bool PreviewFixture(BitmapSource texture){if(disposed)return false;Play(false,true,false,texture);return overlay!=null;}
        internal static double AngleProgress(double angle){return Double.IsNaN(angle)||Double.IsInfinity(angle)?0:Math.Max(0,Math.Min(.95,(80-angle)/80*.95));}
        private void ObserveAngle(double angle)
        {
            if(disposed||!enabled()){Cancel();return;}double target=AngleProgress(angle);
            if(overlay==null){if(target<.002)return;Play(false,false,true);}
            if(!following||effect==null&&plane==null)return;wanted=target;Listen();timeout.Stop();timeout.Start();
        }
        private void Listen(){if(rendering)return;rendering=true;previousFrame=null;CompositionTarget.Rendering+=Frame;}
        private void Frame(object sender,EventArgs args)
        {
            if(effect==null&&plane==null){Cancel();return;}var stamp=((RenderingEventArgs)args).RenderingTime;double dt=previousFrame.HasValue?Math.Min(.05,Math.Max(0,(stamp-previousFrame.Value).TotalSeconds)):1.0/60;previousFrame=stamp;
            Progress+=(wanted-Progress)*(1-Math.Exp(-dt/.085));
            if(Math.Abs(Progress-wanted)<.001){Progress=wanted;CompositionTarget.Rendering-=Frame;rendering=false;previousFrame=null;if(wanted==0)Cancel();}
        }
        private void Play(bool opening,bool preview,bool follow=false,BitmapSource fixture=null)
        {
            Cancel();failure=null;if(disposed)return;if(fixture==null&&!Unlocked()){failure="Windows está bloqueado; el pliegue se mostrará al volver al escritorio";return;}
            try
            {
                var bounds=Forms.Screen.PrimaryScreen.Bounds;
                BitmapSource picture=fixture;
                // One capture before creating the overlay. Never persisted or
                // sent anywhere; no affinity exclusion or repeated screenshots.
                if(picture==null)using(var original=new System.Drawing.Bitmap(bounds.Width,bounds.Height))
                {using(var graphics=System.Drawing.Graphics.FromImage(original))graphics.CopyFromScreen(bounds.Left,bounds.Top,0,0,bounds.Size);using(var reduced=new System.Drawing.Bitmap(original,Math.Min(1280,bounds.Width),Math.Max(1,(int)(bounds.Height*Math.Min(1,1280.0/bounds.Width)))))using(var stream=new System.IO.MemoryStream()){reduced.Save(stream,System.Drawing.Imaging.ImageFormat.Png);stream.Position=0;var bitmap=new BitmapImage();bitmap.BeginInit();bitmap.CacheOption=BitmapCacheOption.OnLoad;bitmap.StreamSource=stream;bitmap.EndInit();bitmap.Freeze();picture=bitmap;}}
                var fromDevice=PresentationSource.FromVisual(host).CompositionTarget.TransformFromDevice;var origin=fromDevice.Transform(new Point(bounds.Left,bounds.Top));var extent=fromDevice.Transform(new Point(bounds.Width,bounds.Height));
                overlay=new Window{Title="MacFlow · Transición de tapa",Left=origin.X,Top=origin.Y,Width=extent.X,Height=extent.Y,WindowStyle=WindowStyle.None,AllowsTransparency=true,Background=Brushes.Transparent,ResizeMode=ResizeMode.NoResize,ShowActivated=false,ShowInTaskbar=false,Topmost=true,Focusable=false,IsHitTestVisible=false};
                var own=overlay;image=new Image{Source=picture,Stretch=Stretch.Fill};var fold=new LidFold();effect=fold;fold.SetValue(LidFold.PixelProperty,new Point(1.0/picture.PixelWidth,1.0/picture.PixelHeight));if(RenderCapability.IsPixelShaderVersionSupported(3,0)){image.Effect=fold;own.Content=image;}else{effect=null;plane=new LidPlane(picture);own.Content=plane;}
                own.SourceInitialized+=delegate{Native.NoActivate(own);var h=new WindowInteropHelper(own).Handle;Native.SetWindowLong(h,-20,Native.GetWindowLong(h,-20)|0x20);Native.SetWindowPos(h,new IntPtr(-1),bounds.Left,bounds.Top,bounds.Width,bounds.Height,0x10);};own.Show();
                if(follow){following=true;timeout.Start();return;}
                DoubleAnimationBase animation;
                if(preview){var keys=new DoubleAnimationUsingKeyFrames();keys.KeyFrames.Add(new EasingDoubleKeyFrame(0,KeyTime.FromTimeSpan(TimeSpan.Zero)));keys.KeyFrames.Add(new EasingDoubleKeyFrame(.86,KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(420)),new CubicEase{EasingMode=EasingMode.EaseInOut}));keys.KeyFrames.Add(new EasingDoubleKeyFrame(0,KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(920)),new CubicEase{EasingMode=EasingMode.EaseInOut}));animation=keys;}
                else animation=new DoubleAnimation(opening?.92:0,opening?0:.92,TimeSpan.FromMilliseconds(opening?520:380)){EasingFunction=new CubicEase{EasingMode=EasingMode.EaseInOut}};
                Timeline.SetDesiredFrameRate(animation,60);animation.Completed+=delegate{if(overlay==own)Cancel();};if(plane!=null)plane.BeginAnimation(LidPlane.ProgressProperty,animation);else fold.BeginAnimation(LidFold.ProgressProperty,animation);timeout.Start();
            }
            catch(Exception ex){Cancel();failure="No se pudo dibujar el pliegue: "+ex.Message;Log.Write("Transición de tapa: "+ex);}
        }
        internal void Cancel(){if(effect!=null)effect.BeginAnimation(LidFold.ProgressProperty,null);if(plane!=null)plane.BeginAnimation(LidPlane.ProgressProperty,null);if(timeout!=null)timeout.Stop();if(rendering)CompositionTarget.Rendering-=Frame;rendering=following=false;previousFrame=null;effect=null;plane=null;var current=overlay;overlay=null;if(image!=null){image.Source=null;image.Effect=null;image=null;}if(current!=null)current.Close();}
        public void Dispose(){if(disposed)return;disposed=true;Cancel();hinge.Dispose();SystemEvents.SessionSwitch-=Session;if(source!=null)source.RemoveHook(hook);if(registration!=IntPtr.Zero)UnregisterPowerSettingNotification(registration);registration=IntPtr.Zero;}
    }
}

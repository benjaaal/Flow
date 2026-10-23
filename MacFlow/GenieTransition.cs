using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
namespace MacFlow
{
    // A short-lived, proportional texture of an OWN window for minimize/restore.
    // The application UI remains real controls and live compositor glass.
    // No frame-by-frame desktop capture and no changes to foreign HWNDs.
    internal static class GenieTransition
    {
        [DllImport("gdi32.dll")]private static extern bool DeleteObject(IntPtr value);
        internal static int ActiveCount;
        internal static event Action<Window,GenieSurface> Started;
        private sealed class Transition{internal Action<bool,int> Retarget;}
        private static readonly System.Collections.Generic.Dictionary<Window,Transition> active=new System.Collections.Generic.Dictionary<Window,Transition>();
        internal static bool DockUsesVertical(Point target,Rect monitor){return Math.Min(Math.Abs(target.Y-monitor.Top),Math.Abs(monitor.Bottom-target.Y))<=Math.Min(Math.Abs(target.X-monitor.Left),Math.Abs(monitor.Right-target.X));}
        internal static bool TryStart(Window source,int duration,bool opening=false)
        {
            Transition existing;if(active.TryGetValue(source,out existing)){existing.Retarget(opening,duration);return true;}
            if(!Motion.Enabled||RenderCapability.Tier==0||!source.IsVisible||PanelMotion.IsTransitioning(source)||PanelMotion.DockDestination==null||ActiveCount>=2)return false;
            Window overlay=null;Action abort=null;
            try
            {
                var destination=PanelMotion.DockDestination(source);if(!destination.HasValue)return false;
                Native.RECT rect;if(!Native.GetWindowRect(new WindowInteropHelper(source).Handle,out rect))return false;
                int width=rect.Right-rect.Left,height=rect.Bottom-rect.Top;if(width<2||height<2||width>4096||height>4096||(long)width*height>4000000)return false;
                double overlayWidth=Math.Max(rect.Right,destination.Value.X)-Math.Min(rect.Left,destination.Value.X)+6,overlayHeight=Math.Max(rect.Bottom,destination.Value.Y)-Math.Min(rect.Top,destination.Value.Y)+6;
                if(overlayWidth*overlayHeight>8000000)return false;
                BitmapSource texture;var image=source.Content as Image;
                if(image!=null&&image.Source is BitmapSource)texture=(BitmapSource)image.Source;
                else
                {
                    // CopyFromScreen also copied overlapping Docks and windows
                    // into the funnel, producing a visible second Dock. Render
                    // only our own client visual; native app previews already
                    // supply their own PrintWindow texture in the Image branch.
                    var content=source.Content as Visual;if(content==null)return false;
                    var viewSource=PresentationSource.FromVisual(source);double dpi=viewSource==null?1:viewSource.CompositionTarget.TransformToDevice.M11;
                    double factor=Math.Min(1,1440.0/Math.Max(width,height));var bitmap=new RenderTargetBitmap(Math.Max(1,(int)(width*factor)),Math.Max(1,(int)(height*factor)),96*dpi*factor,96*dpi*factor,PixelFormats.Pbgra32);bitmap.Render(content);bitmap.Freeze();texture=bitmap;
                }
                var presentation=PresentationSource.FromVisual(source);double scale=presentation==null?1:presentation.CompositionTarget.TransformToDevice.M11;
                double left=Math.Min(rect.Left,destination.Value.X)-3,top=Math.Min(rect.Top,destination.Value.Y)-3,right=Math.Max(rect.Right,destination.Value.X)+3,bottom=Math.Max(rect.Bottom,destination.Value.Y)+3;
                var start=new Rect((rect.Left-left)/scale,(rect.Top-top)/scale,width/scale,height/scale);var target=new Point((destination.Value.X-left)/scale,(destination.Value.Y-top)/scale);
                var monitor=System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point((int)destination.Value.X,(int)destination.Value.Y)).Bounds;bool verticalAxis=DockUsesVertical(destination.Value,new Rect(monitor.Left,monitor.Top,monitor.Width,monitor.Height));
                var surface=new GenieSurface(texture,start,target,Native.GlassRadius(source),(right-left)/scale,(bottom-top)/scale,verticalAxis);
                if(opening)surface.SetValue(GenieSurface.ProgressProperty,1.0);
                overlay=new Window{Title="MacOS · Transición al Dock",Width=(right-left)/scale,Height=(bottom-top)/scale,Left=left/scale,Top=top/scale,WindowStyle=WindowStyle.None,ResizeMode=ResizeMode.NoResize,AllowsTransparency=true,Background=Brushes.Transparent,ShowInTaskbar=false,ShowActivated=false,Topmost=true,IsHitTestVisible=false,Content=surface};
                var view=overlay;overlay.SourceInitialized+=delegate{Native.NoActivate(view);var handle=new WindowInteropHelper(view).Handle;Native.SetWindowLong(handle,-20,Native.GetWindowLong(handle,-20)|0x20);};
                var timeout=new DispatcherTimer(DispatcherPriority.Background){Interval=TimeSpan.FromMilliseconds(duration+160)};bool ended=false,counted=false,enteringNow=opening;int revision=0;EventHandler sourceClosed=null;DependencyPropertyChangedEventHandler hidden=null;
                Action stop=delegate{if(ended)return;ended=true;++revision;active.Remove(source);timeout.Stop();surface.BeginAnimation(GenieSurface.ProgressProperty,null);source.Closed-=sourceClosed;source.IsVisibleChanged-=hidden;if(WindowLife.Alive(view))view.Close();};
                abort=stop;sourceClosed=delegate{stop();};hidden=delegate{if(!source.IsVisible)stop();};source.Closed+=sourceClosed;source.IsVisibleChanged+=hidden;timeout.Tick+=delegate{stop();};overlay.Closed+=delegate{if(counted){ActiveCount--;counted=false;}if(!ended)stop();};
                overlay.Show();ActiveCount++;counted=true;Native.SetWindowPos(new WindowInteropHelper(overlay).Handle,new IntPtr(-1),(int)left,(int)top,(int)(right-left),(int)(bottom-top),0x50);
                var notify=Started;if(notify!=null)notify(source,surface);if(ended)return false;
                var transition=new Transition();transition.Retarget=delegate(bool entering,int milliseconds){if(ended)return;enteringNow=entering;int token=++revision;double current=surface.Progress,end=entering?0:1;surface.BeginAnimation(GenieSurface.ProgressProperty,null);surface.SetValue(GenieSurface.ProgressProperty,current);double length=Math.Max(90,milliseconds*Math.Abs(end-current));timeout.Stop();timeout.Interval=TimeSpan.FromMilliseconds(length+160);var motion=new DoubleAnimation(current,end,TimeSpan.FromMilliseconds(length)){FillBehavior=FillBehavior.HoldEnd};Timeline.SetDesiredFrameRate(motion,60);motion.Completed+=delegate{if(token!=revision||ended)return;if(enteringNow)Design.CompleteGenieOpen(source);stop();};surface.BeginAnimation(GenieSurface.ProgressProperty,motion);timeout.Start();};active[source]=transition;transition.Retarget(opening,duration);return true;
            }
            catch(Exception ex){if(abort!=null)abort();else if(overlay!=null&&overlay.IsVisible)overlay.Close();Log.Write("Transición curva no disponible: "+ex.Message);return false;}
        }
    }
    internal sealed class GenieSurface : Grid
    {
        internal static readonly DependencyProperty ProgressProperty=DependencyProperty.Register("Progress",typeof(double),typeof(GenieSurface),new PropertyMetadata(0.0,Changed));
        private readonly Rect start;private readonly Point target;private readonly Border image;private readonly ScaleTransform scale=new ScaleTransform(1,1);private readonly TranslateTransform shift=new TranslateTransform();private readonly bool vertical;
        internal double Progress{get{return (double)GetValue(ProgressProperty);}}
        internal Point Tail{get;private set;}
        internal double TopWidth{get;private set;}internal double TailWidth{get;private set;}
        internal double AspectRatioError{get{return Math.Abs(scale.ScaleX-scale.ScaleY);}}
        internal GenieSurface(BitmapSource texture,Rect start,Point target,double radius,double width,double height,bool? verticalAxis=null)
        {
            this.start=start;this.target=target;vertical=verticalAxis??(Math.Abs(target.Y-start.Top)>=Math.Abs(target.X-start.Left));
            image=new Border{Width=start.Width,Height=start.Height,CornerRadius=new CornerRadius(radius),HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top,Child=new Image{Source=texture,Stretch=Stretch.Fill},Clip=new RectangleGeometry(new Rect(0,0,start.Width,start.Height),radius,radius)};
            var transform=new TransformGroup();transform.Children.Add(scale);transform.Children.Add(shift);image.RenderTransformOrigin=new Point(.5,.5);image.RenderTransform=transform;Children.Add(image);Update(0);
        }
        private static void Changed(DependencyObject value,DependencyPropertyChangedEventArgs e){((GenieSurface)value).Update((double)e.NewValue);}
        private void Update(double progress)
        {
            double p=Motion.Smooth(progress),minimum=Math.Min(1,38/Math.Max(start.Width,start.Height)),size=1-(1-minimum)*p;
            scale.ScaleX=scale.ScaleY=size;double cx=start.Left+start.Width/2,cy=start.Top+start.Height/2;
            // A single smooth arc; the complete rectangle keeps its proportions.
            double x=cx+(target.X-cx)*p,y=cy+(target.Y-cy)*p;
            if(vertical)x+=Math.Sin(Math.PI*p)*(target.X-cx)*.06;else y+=Math.Sin(Math.PI*p)*(target.Y-cy)*.06;
            shift.X=x-start.Width/2;shift.Y=y-start.Height/2;image.Opacity=1-Motion.Smooth((progress-.76)/.24);
            TopWidth=TailWidth=(vertical?start.Width:start.Height)*size;Tail=new Point(x,y);
        }
    }
}

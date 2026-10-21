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
    // A short-lived texture of an OWN window, bent into a curved funnel.
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
                    var bitmap=new RenderTargetBitmap(width,height,96*dpi,96*dpi,PixelFormats.Pbgra32);bitmap.Render(content);bitmap.Freeze();texture=bitmap;
                }
                var presentation=PresentationSource.FromVisual(source);double scale=presentation==null?1:presentation.CompositionTarget.TransformToDevice.M11;
                double left=Math.Min(rect.Left,destination.Value.X)-3,top=Math.Min(rect.Top,destination.Value.Y)-3,right=Math.Max(rect.Right,destination.Value.X)+3,bottom=Math.Max(rect.Bottom,destination.Value.Y)+3;
                var start=new Rect((rect.Left-left)/scale,(rect.Top-top)/scale,width/scale,height/scale);var target=new Point((destination.Value.X-left)/scale,(destination.Value.Y-top)/scale);
                var monitor=System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point((int)destination.Value.X,(int)destination.Value.Y)).Bounds;bool verticalAxis=DockUsesVertical(destination.Value,new Rect(monitor.Left,monitor.Top,monitor.Width,monitor.Height));
                var surface=new GenieSurface(texture,start,target,Native.GlassRadius(source),(right-left)/scale,(bottom-top)/scale,verticalAxis);
                if(opening)surface.SetValue(GenieSurface.ProgressProperty,1.0);
                overlay=new Window{Title="MacFlow · Transición al Dock",Width=(right-left)/scale,Height=(bottom-top)/scale,Left=left/scale,Top=top/scale,WindowStyle=WindowStyle.None,ResizeMode=ResizeMode.NoResize,AllowsTransparency=true,Background=Brushes.Transparent,ShowInTaskbar=false,ShowActivated=false,Topmost=true,IsHitTestVisible=false,Content=surface};
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
        private readonly Rect start;private readonly Point target;private readonly MeshGeometry3D mesh;private readonly double canvasHeight;private readonly bool vertical,positive;
        internal double Progress{get{return (double)GetValue(ProgressProperty);}}
        internal Point Tail{get;private set;}
        internal double TopWidth{get;private set;}internal double TailWidth{get;private set;}
        internal GenieSurface(BitmapSource texture,Rect start,Point target,double radius,double width,double height,bool? verticalAxis=null)
        {
            this.start=start;this.target=target;canvasHeight=height;double dx=target.X-(start.Left+start.Width/2),dy=target.Y-(start.Top+start.Height/2);vertical=verticalAxis??(Math.Abs(dy)>=Math.Abs(dx));positive=vertical?dy>=0:dx>=0;
            // Shared vertices, one cached texture and ambient lighting. Eight
            // columns avoid diagonal texture kinks in curved, stretched text.
            var uv=new PointCollection(441);var indices=new Int32Collection(2304);
            for(int row=0;row<=48;row++){double along=row/48.0;for(int side=0;side<=8;side++)uv.Add(vertical?new Point(side/8.0,along):new Point(along,side/8.0));if(row<48)for(int col=0;col<8;col++){int n=row*9+col;indices.Add(n);indices.Add(n+1);indices.Add(n+9);indices.Add(n+1);indices.Add(n+10);indices.Add(n+9);}}
            uv.Freeze();indices.Freeze();mesh=new MeshGeometry3D{TextureCoordinates=uv,TriangleIndices=indices};
            var drawing=new DrawingGroup();drawing.ClipGeometry=new RectangleGeometry(new Rect(0,0,texture.PixelWidth,texture.PixelHeight),radius*texture.PixelWidth/start.Width,radius*texture.PixelHeight/start.Height);drawing.Children.Add(new ImageDrawing(texture,new Rect(0,0,texture.PixelWidth,texture.PixelHeight)));drawing.Freeze();
            var brush=new DrawingBrush(drawing){Stretch=Stretch.Fill};brush.Freeze();var material=new DiffuseMaterial(brush);material.Freeze();var model=new GeometryModel3D(mesh,material){BackMaterial=material};
            var viewport=new Viewport3D{ClipToBounds=false,IsHitTestVisible=false,Camera=new OrthographicCamera(new Point3D(width/2,height/2,10),new Vector3D(0,0,-1),new Vector3D(0,1,0),width)};viewport.Children.Add(new ModelVisual3D{Content=new AmbientLight(Colors.White)});viewport.Children.Add(new ModelVisual3D{Content=model});Children.Add(viewport);Update(0);
        }
        private static void Changed(DependencyObject value,DependencyPropertyChangedEventArgs e){((GenieSurface)value).Update((double)e.NewValue);}
        private static double Smooth(double value){value=Math.Max(0,Math.Min(1,value));return value*value*(3-2*value);}
        private void Update(double progress)
        {
            double pinch=Smooth(progress/.48),travel=Smooth((progress-.32)/.68);var points=new Point3DCollection(441);
            for(int row=0;row<=48;row++)
            {
                double along=row/48.0,t=positive?along:1-along,bend=pinch*Smooth(t),cross=(vertical?start.Width:start.Height)*(1-.985*bend)*(1-travel);
                double center=(vertical?start.Left+start.Width/2:start.Top+start.Height/2);double destination=vertical?target.X:target.Y;center+=(destination-center)*(bend+(1-bend)*travel);
                double axis=(vertical?start.Top:start.Left)+along*(vertical?start.Height:start.Width),tailPull=bend,axisTravel=tailPull+(1-tailPull)*travel;axis+=((vertical?target.Y:target.X)-axis)*axisTravel;
                for(int side=0;side<=8;side++){double across=center+(side/8.0-.5)*cross;double x=vertical?across:axis,y=vertical?axis:across;points.Add(new Point3D(x,canvasHeight-y,0));}
                if(t==0)TopWidth=cross;if(t==1){TailWidth=cross;Tail=vertical?new Point(center,axis):new Point(axis,center);}
            }
            mesh.Positions=points;
        }
    }
}

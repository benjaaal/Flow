using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
namespace MacFlow
{
    // Local light caustic over the compositor's blur, not optical refraction.
    // Geometry moves independently of layout; foreground controls stay sharp.
    internal sealed class LiquidDrop : Grid
    {
        private readonly Grid host;private readonly Path contour=new Path();
        private Rect current,target;
        private double alpha,wantedAlpha,previous=-1,stretch;
        private bool listening;
        internal static LiquidDrop Find(DependencyObject root)
        {var drop=root as LiquidDrop;if(drop!=null)return drop;for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){drop=Find(VisualTreeHelper.GetChild(root,i));if(drop!=null)return drop;}return null;}
        internal Rect CurrentBounds {get{return current;}}
        internal bool IsAnimating {get{return listening;}}
        internal void ProbeControl(DependencyObject root,string label)
        {
            var button=root as Button;if(button!=null&&String.Equals(button.Content as string,label,StringComparison.Ordinal)){var area=new Rect(button.TranslatePoint(new Point(),host),button.RenderSize);area.Inflate(3,3);Probe(area);return;}
            for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)ProbeControl(VisualTreeHelper.GetChild(root,i),label);
        }
        internal LiquidDrop(Grid layer)
        {
            host=layer;IsHitTestVisible=false;Opacity=0;contour.StrokeThickness=.65;Children.Add(contour);
            contour.Fill=new RadialGradientBrush(Color.FromArgb(24,255,255,255),Color.FromArgb(3,255,255,255)){Center=new Point(.35,.15),GradientOrigin=new Point(.35,.15),RadiusX=.8,RadiusY=.9};
            var edge=new LinearGradientBrush{StartPoint=new Point(.12,0),EndPoint=new Point(.85,1)};
            edge.GradientStops.Add(new GradientStop(Color.FromArgb(126,228,248,255),0));edge.GradientStops.Add(new GradientStop(Color.FromArgb(18,255,255,255),.24));edge.GradientStops.Add(new GradientStop(Color.FromArgb(32,191,215,255),.66));edge.GradientStops.Add(new GradientStop(Color.FromArgb(105,255,235,202),1));edge.Freeze();contour.Stroke=edge;
            host.MouseMove+=Move;host.MouseLeave+=delegate{wantedAlpha=0;Start();};host.Unloaded+=delegate{Stop();Opacity=alpha=wantedAlpha=0;current=Rect.Empty;};current=Rect.Empty;
        }
        private void Move(object sender,MouseEventArgs e)
        {
            DependencyObject item=e.OriginalSource as DependencyObject;Button button=null;
            while(item!=null&&item!=host){button=item as Button;if(button!=null)break;item=item is Visual?VisualTreeHelper.GetParent(item):LogicalTreeHelper.GetParent(item);}
            if(button==null){wantedAlpha=0;Start();return;}
            var point=button.TranslatePoint(new Point(),host);var area=new Rect(point,button.RenderSize);area.Inflate(3,3);Probe(area);
        }
        internal void Probe(Rect area)
        {
            area.Intersect(new Rect(0,0,Math.Max(0,host.ActualWidth),Math.Max(0,host.ActualHeight)));if(area.IsEmpty||area.Width<=0||area.Height<=0)return;
            target=area;wantedAlpha=1;if(current.IsEmpty){current=target;alpha=0;}Start();
        }
        private void Start(){if(listening||!host.IsLoaded)return;previous=-1;listening=true;CompositionTarget.Rendering+=Render;}
        private void Stop(){if(!listening)return;listening=false;CompositionTarget.Rendering-=Render;previous=-1;}
        private void Render(object sender,EventArgs e)
        {
            double now=((RenderingEventArgs)e).RenderingTime.TotalSeconds;if(previous>=0&&now<=previous)return;
            double dt=previous<0?1.0/60:Math.Min(.05,now-previous);previous=now;
            double fraction=Design.Animations?1-Math.Exp(-dt/.13):1;
            if(!current.IsEmpty){double travel=target.X-current.X;stretch+=(Math.Min(3,Math.Abs(travel)*.09)-stretch)*fraction;current=new Rect(current.X+(target.X-current.X)*fraction,current.Y+(target.Y-current.Y)*fraction,current.Width+(target.Width-current.Width)*fraction,current.Height+(target.Height-current.Height)*fraction);Draw();}
            alpha+=(wantedAlpha-alpha)*fraction;Opacity=alpha;
            if((current.IsEmpty||Math.Abs(current.X-target.X)+Math.Abs(current.Y-target.Y)+Math.Abs(current.Width-target.Width)+Math.Abs(current.Height-target.Height)<.06)&&Math.Abs(alpha-wantedAlpha)<.002&&stretch<.04){alpha=wantedAlpha;Opacity=alpha;stretch=0;if(!current.IsEmpty){current=target;Draw();}Stop();}
        }
        private void Draw()
        {
            double x=current.X,y=current.Y,w=current.Width,h=current.Height,r=Math.Min(h*.48,w*.48),s=Math.Min(stretch,r*.12);
            var shape=new StreamGeometry();using(var g=shape.Open())
            {
                g.BeginFigure(new Point(x+r,y),true,true);g.LineTo(new Point(x+w-r,y),true,false);g.BezierTo(new Point(x+w-r*.35,y-s),new Point(x+w,y+r*.35),new Point(x+w,y+r),true,true);g.LineTo(new Point(x+w,y+h-r),true,false);g.BezierTo(new Point(x+w,y+h-r*.35),new Point(x+w-r*.35,y+h),new Point(x+w-r,y+h),true,true);g.LineTo(new Point(x+r,y+h),true,false);g.BezierTo(new Point(x+r*.35,y+h+s),new Point(x,y+h-r*.35),new Point(x,y+h-r),true,true);g.LineTo(new Point(x,y+r),true,false);g.BezierTo(new Point(x,y+r*.35),new Point(x+r*.35,y),new Point(x+r,y),true,true);
            }shape.Freeze();contour.Data=shape;
        }
    }
}

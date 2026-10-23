using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
namespace MacFlow
{
    internal enum AssistantStage { Hidden, Ready, Appearance, Listening, Working, Answer, Speaking }

    // Original monochrome glass lens with a lower specular reflection.
    // Speaking is supplied by real playback, never by a visual timer.
    // No video playback, bitmap sequence, screen capture or Apple resources.
    internal sealed class AssistantVisual : FrameworkElement, IDisposable
    {
        private readonly Stopwatch elapsed=new Stopwatch(),phaseClock=Stopwatch.StartNew();
        private readonly Brush spectrum=Freeze(new LinearGradientBrush(new GradientStopCollection{new GradientStop(Color.FromArgb(155,98,135,255),0),new GradientStop(Color.FromArgb(210,124,110,246),.26),new GradientStop(Color.FromArgb(245,190,235,255),.48),new GradientStop(Color.FromArgb(210,248,196,133),.72),new GradientStop(Color.FromArgb(145,235,120,171),1)},new Point(0,0),new Point(1,0)));
        private static readonly DependencyProperty EnergyProperty=DependencyProperty.Register("Energy",typeof(double),typeof(AssistantVisual),new FrameworkPropertyMetadata(.08,FrameworkPropertyMetadataOptions.AffectsRender));
        private readonly Brush lensFill,lowerGlow,upperGlow,bandFill;
        private readonly Pen rim;
        private bool attached,disposed;private double lastFrame=-1;
        internal bool Ribbon;
        internal AssistantStage Stage{get;private set;}
        internal bool Rendering{get{return attached;}}
        internal AssistantVisual()
        {
            IsHitTestVisible=false;
            lensFill=Freeze(new RadialGradientBrush(new GradientStopCollection{new GradientStop(Color.FromRgb(32,34,39),0),new GradientStop(Color.FromRgb(4,5,7),.72),new GradientStop(Color.FromRgb(58,62,68),1)}){GradientOrigin=new Point(.35,.2),Center=new Point(.5,.5),RadiusX=.58,RadiusY=.58});
            bandFill=Freeze(new LinearGradientBrush(new GradientStopCollection{new GradientStop(Color.FromRgb(17,18,21),0),new GradientStop(Colors.Black,.45),new GradientStop(Color.FromRgb(8,9,11),1)},new Point(0,0),new Point(0,1)));
            lowerGlow=Freeze(new RadialGradientBrush(new GradientStopCollection{new GradientStop(Colors.White,0),new GradientStop(Color.FromArgb(80,215,224,234),.35),new GradientStop(Colors.Transparent,1)}));upperGlow=Freeze(new RadialGradientBrush(Color.FromArgb(64,255,255,255),Colors.Transparent));rim=new Pen(Design.Brush("#507A818B"),.65);rim.Freeze();
            Loaded+=delegate{UpdateClock();};Unloaded+=delegate{Detach();};IsVisibleChanged+=delegate{UpdateClock();};
        }
        private static Brush Freeze(Brush brush){brush.Freeze();return brush;}
        internal void Set(AssistantStage stage)
        {
            if(disposed)return;if(Stage!=stage){Stage=stage;elapsed.Restart();lastFrame=-1;Motion.To(this,EnergyProperty,stage==AssistantStage.Listening||stage==AssistantStage.Speaking?1:stage==AssistantStage.Working?.78:stage==AssistantStage.Answer?.45:.08,Motion.Panel);}UpdateClock();InvalidateVisual();
        }
        private void UpdateClock()
        {
            bool needs=!disposed&&IsLoaded&&IsVisible&&Stage!=AssistantStage.Hidden&&Stage!=AssistantStage.Ready&&Motion.Enabled&&(Stage!=AssistantStage.Answer||elapsed.Elapsed.TotalSeconds<1.6)&&(Stage!=AssistantStage.Appearance||elapsed.Elapsed.TotalSeconds<.8);
            if(needs&&!attached){attached=true;CompositionTarget.Rendering+=Frame;}else if(!needs)Detach();
        }
        private void Detach(){if(attached){CompositionTarget.Rendering-=Frame;attached=false;}}
        private void Frame(object sender,EventArgs args)
        {
            if(disposed||!IsVisible||!Motion.Enabled){Detach();return;}double time=elapsed.Elapsed.TotalSeconds;if(time-lastFrame<1.0/60)return;lastFrame=time;InvalidateVisual();if(Stage==AssistantStage.Answer&&time>1.6||Stage==AssistantStage.Appearance&&time>.8)Detach();
        }
        protected override void OnRender(DrawingContext dc)
        {
            if(Stage==AssistantStage.Hidden||ActualWidth<1||ActualHeight<1)return;
            if(Ribbon)
            {
                double h=Math.Max(1,ActualHeight-12),w=Math.Max(1,ActualWidth-12),radius=h/2,phase=Motion.Enabled?phaseClock.Elapsed.TotalSeconds:0;var rect=new Rect(6,6,w,h);double sphere=1-Math.Max(0,Math.Min(1,(w/h-1)/2));
                dc.DrawRoundedRectangle(bandFill,null,rect,radius,radius);dc.PushClip(new RectangleGeometry(rect,radius,radius));dc.PushOpacity(sphere);dc.DrawRectangle(lensFill,null,rect);dc.Pop();
                double breathe=Stage==AssistantStage.Listening||Stage==AssistantStage.Speaking?.5+.5*Math.Sin(phase*2.8):.4;
                double highlightX=ActualWidth/2+(Stage==AssistantStage.Working?Math.Sin(phase*1.6)*w*.18:Math.Sin(phase*.7)*w*.025),y=rect.Bottom-h*(.14+.045*Math.Sin(phase*1.3));
                dc.PushOpacity(.70+.22*breathe);dc.DrawEllipse(lowerGlow,null,new Point(highlightX,y),w*.48,h*.27);dc.Pop();
                var arc=new StreamGeometry();using(var c=arc.Open()){c.BeginFigure(new Point(rect.Left+w*.07,y-h*.04),false,false);c.BezierTo(new Point(rect.Left+w*.28,y+h*.13),new Point(rect.Left+w*.68,y+h*.13),new Point(rect.Left+w*.93,y-h*.05),true,false);}arc.Freeze();dc.DrawGeometry(null,new Pen(Design.Brush("#CDECF4FF"),.65+.55*breathe),arc);
                dc.PushOpacity(.45*sphere);dc.DrawEllipse(upperGlow,null,new Point(ActualWidth*.40,rect.Top+h*.18),w*.25,h*.13);dc.Pop();dc.Pop();return;
            }

            double size=Math.Min(ActualWidth,ActualHeight),r=size*.39;var center=new Point(ActualWidth/2,ActualHeight/2);double t=Motion.Enabled?phaseClock.Elapsed.TotalSeconds:0;
            double breath=Stage==AssistantStage.Listening||Stage==AssistantStage.Speaking?.5+.5*Math.Sin(t*3):.35;
            dc.DrawEllipse(lensFill,rim,center,r,r);dc.PushClip(new EllipseGeometry(center,r-.5,r-.5));
            double energy=(double)GetValue(EnergyProperty),wave=Math.Sin(t*1.8)*r*.055*energy;
            var core=new StreamGeometry();using(var c=core.Open()){c.BeginFigure(new Point(center.X-r*.94,center.Y+wave),true,true);c.BezierTo(new Point(center.X-r*.32,center.Y-r*(.05+.20*energy)),new Point(center.X+r*.28,center.Y-r*(.03+.16*energy)),new Point(center.X+r*.94,center.Y-wave),true,false);c.BezierTo(new Point(center.X+r*.32,center.Y+r*(.04+.14*energy)),new Point(center.X-r*.28,center.Y+r*(.05+.18*energy)),new Point(center.X-r*.94,center.Y+wave),true,false);}core.Freeze();dc.PushOpacity(.22+.66*energy);dc.DrawGeometry(spectrum,null,core);dc.Pop();
            double drift=Stage==AssistantStage.Working?Math.Sin(t*1.7)*r*.18:Math.Sin(t*.8)*r*.04;
            dc.PushOpacity(.67+.27*breath);dc.DrawEllipse(lowerGlow,null,new Point(center.X+drift,center.Y+r*.76),r*.95,r*.32);dc.Pop();
            var shine=new StreamGeometry();using(var p=shine.Open()){p.BeginFigure(new Point(center.X-r*.85,center.Y+r*.3),false,false);p.BezierTo(new Point(center.X-r*.3,center.Y+r*.88),new Point(center.X+r*.4,center.Y+r*.88),new Point(center.X+r*.88,center.Y+r*.25),true,false);}shine.Freeze();dc.DrawGeometry(null,new Pen(Design.Brush("#BDF5F8FF"),size*.013),shine);
            dc.DrawEllipse(upperGlow,null,new Point(center.X-r*.2,center.Y-r*.7),r*.54,r*.12);dc.Pop();
        }
        public void Dispose(){if(disposed)return;disposed=true;Detach();elapsed.Stop();phaseClock.Stop();BeginAnimation(EnergyProperty,null);}
    }
}

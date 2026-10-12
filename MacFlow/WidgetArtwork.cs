using System;
using System.Windows;
using System.Windows.Media;
namespace MacFlow
{
    internal sealed class WidgetBattery : FrameworkElement
    {
        private int level=-1;private bool charge;
        internal WidgetBattery(){Width=68;Height=68;}
        internal void Update(DeviceSnapshot data){int next=!data.PowerKnown||data.Power.BatteryLifePercent>100||data.Power.BatteryFlag==128?-1:data.Power.BatteryLifePercent;bool charging=data.PowerKnown&&data.Power.ACLineStatus==1;if(next==level&&charge==charging)return;level=next;charge=charging;InvalidateVisual();}
        protected override void OnRender(DrawingContext dc)
        {
            dc.PushTransform(new ScaleTransform(ActualWidth/68,ActualHeight/68));
            var ink=TryFindResource("GlassInk") as Brush??Design.Ink;dc.DrawEllipse(null,new Pen(Design.Brush("#258A9DB5"),4),new Point(34,34),29,29);
            if(level>0){double angle=Math.PI*2*Math.Min(99.95,level)/100;var arc=new StreamGeometry();using(var g=arc.Open()){g.BeginFigure(new Point(34,5),false,false);g.ArcTo(new Point(34+29*Math.Sin(angle),34-29*Math.Cos(angle)),new Size(29,29),0,angle>Math.PI,SweepDirection.Clockwise,true,false);}dc.DrawGeometry(null,new Pen(Design.Brush(level<=20?"#FF7973":"#6ED29D"),4){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round},arc);}
            dc.DrawRoundedRectangle(null,new Pen(ink,1.5),new Rect(21,23,26,17),3,3);dc.DrawLine(new Pen(ink,1.5),new Point(17,44),new Point(51,44));if(charge)dc.DrawGeometry(Design.Brush("#6ED29D"),null,Geometry.Parse("M36,25 L29,33 33,33 31,39 39,30 35,30 Z"));dc.Pop();
        }
    }
    internal sealed class WidgetActivity : FrameworkElement
    {
        private DeviceSnapshot data=new DeviceSnapshot();
        internal WidgetActivity(){Height=46;}
        internal void Update(DeviceSnapshot value){if(Object.ReferenceEquals(data,value))return;data=value;InvalidateVisual();}
        protected override void OnRender(DrawingContext dc)
        {
            double[] levels={data.Cpu.HasValue?data.Cpu.Value/100:0,data.TotalRam>0?(double)data.UsedRam/data.TotalRam:0,data.DiskTotal>0?1-(double)data.DiskFree/data.DiskTotal:0};string[] colors={"#8DAEFF","#C09DEC","#78CFAE"};
            for(int i=0;i<3;i++){dc.DrawRoundedRectangle(Design.Brush("#288596AD"),null,new Rect(0,3+i*14,Math.Max(1,ActualWidth),5),2.5,2.5);double width=Math.Max(0,ActualWidth*Math.Min(1,levels[i]));if(width>0)dc.DrawRoundedRectangle(Design.Brush(colors[i]),null,new Rect(0,3+i*14,width,5),2.5,2.5);}
        }
    }
}

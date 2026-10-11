using System;
using System.Windows;
using System.Windows.Media;
namespace MacFlow
{
    internal sealed class BatteryGlyph : FrameworkElement
    {
        internal static readonly DependencyProperty InkProperty=DependencyProperty.Register("Ink",typeof(Brush),typeof(BatteryGlyph),new FrameworkPropertyMetadata(Brushes.White,FrameworkPropertyMetadataOptions.AffectsRender));
        private int level=-1;private bool charging,saving;
        internal BatteryGlyph(){Width=27;Height=14;SnapsToDevicePixels=true;SetResourceReference(InkProperty,"GlassInk");}
        internal void Update(Native.POWER power)
        {int next=power.BatteryFlag==128||power.BatteryLifePercent>100?-1:power.BatteryLifePercent;bool charge=(power.BatteryFlag&8)!=0||power.ACLineStatus==1&&next>=0&&next<100;bool save=power.SystemStatusFlag==1;if(level==next&&charging==charge&&saving==save)return;level=next;charging=charge;saving=save;InvalidateVisual();}
        protected override void OnRender(DrawingContext drawing)
        {
            var ink=(Brush)GetValue(InkProperty);var outline=new Pen(ink,1.1);drawing.PushOpacity(.68);drawing.DrawRoundedRectangle(null,outline,new Rect(.7,1.3,22.8,11.4),3,3);drawing.DrawRoundedRectangle(ink,null,new Rect(25,4.2,1.8,5.6),.8,.8);drawing.Pop();
            if(level>=0&&level>0){var fill=charging?Brushes.LightGreen:level<=20?Design.Brush("#FF5B61"):saving?Design.Brush("#FFD761"):ink;drawing.DrawRoundedRectangle(fill,null,new Rect(2.5,3.1,19.2*level/100.0,7.8),1.7,1.7);}
            if(charging||level<0){var bolt=Geometry.Parse("M14,2.3 L9.5,7.2 L12,7.2 L10.5,11.8 L16,6 L13,6 Z");drawing.DrawGeometry(level<0?ink:Design.Brush("#1A3322"),null,bolt);}
        }
    }
}

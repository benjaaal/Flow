using System;
using System.Windows;
using System.Windows.Media;
namespace MacFlow
{
    internal sealed class BatteryGlyph : FrameworkElement
    {
        internal static readonly DependencyProperty InkProperty=DependencyProperty.Register("Ink",typeof(Brush),typeof(BatteryGlyph),new FrameworkPropertyMetadata(Brushes.White,FrameworkPropertyMetadataOptions.AffectsRender));
        private int level=-1;private bool charging,saving;
        internal BatteryGlyph(){Width=25;Height=13;SnapsToDevicePixels=true;SetResourceReference(InkProperty,"GlassInk");}
        internal void Update(Native.POWER power)
        {int next=power.BatteryFlag==128||power.BatteryLifePercent>100?-1:power.BatteryLifePercent;bool charge=(power.BatteryFlag&8)!=0||power.ACLineStatus==1&&next>=0&&next<100;bool save=power.SystemStatusFlag==1;if(level==next&&charging==charge&&saving==save)return;level=next;charging=charge;saving=save;InvalidateVisual();}
        protected override void OnRender(DrawingContext drawing)
        {
            var ink=(Brush)GetValue(InkProperty);var outline=new Pen(ink,1);drawing.PushOpacity(.65);drawing.DrawRoundedRectangle(null,outline,new Rect(.6,1.1,21.5,10.8),2.8,2.8);drawing.DrawRoundedRectangle(ink,null,new Rect(23.2,4,1.6,5),.7,.7);drawing.Pop();
            if(level>=0&&level>0){var fill=charging?Design.Brush("#67C893"):level<=20?Design.Brush("#FF6969"):saving?Design.Brush("#E9C469"):ink;drawing.DrawRoundedRectangle(fill,null,new Rect(2.4,2.9,17.9*level/100.0,7.2),1.5,1.5);}
            if(charging||level<0){var bolt=Geometry.Parse("M13.3,2 L9,6.8 L11.5,6.8 L10,11 L15.1,5.6 L12.6,5.6 Z");drawing.DrawGeometry(level<0?ink:Design.Brush("#143829"),null,bolt);}
        }
    }
}

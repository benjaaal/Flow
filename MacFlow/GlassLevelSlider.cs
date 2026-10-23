using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
namespace MacFlow
{
    // A real accessible Slider with a compact filled vertical track. No bitmap
    // or invisible decorative control is placed over the interactive surface.
    internal sealed class GlassLevelSlider : Slider
    {
        private readonly string symbol;
        internal GlassLevelSlider(string symbol)
        {
            this.symbol=symbol;Minimum=0;Maximum=100;Orientation=Orientation.Vertical;Width=72;Height=106;SmallChange=1;LargeChange=10;Focusable=true;
            Template=new ControlTemplate(typeof(Slider)){VisualTree=new FrameworkElementFactory(typeof(Grid))};
            ValueChanged+=delegate{InvalidateVisual();};IsEnabledChanged+=delegate{InvalidateVisual();};
            System.Windows.Automation.AutomationProperties.SetName(this,symbol=="sun"?"Brillo de pantalla":"Volumen");
        }
        protected override void OnRender(DrawingContext dc)
        {
            var bounds=new Rect(0,0,ActualWidth,ActualHeight);double radius=Math.Min(ActualWidth/2,32);var clip=new RectangleGeometry(bounds,radius,radius);dc.PushClip(clip);dc.DrawRectangle(Design.Brush("#20FFFFFF"),null,bounds);
            double fraction=(Value-Minimum)/Math.Max(1,Maximum-Minimum),height=ActualHeight*fraction;dc.DrawRectangle(Design.Brush(IsEnabled?"#ECFFFFFF":"#60FFFFFF"),null,new Rect(0,ActualHeight-height,ActualWidth,height));
            var ink=symbol=="sun"&&fraction>.26?Design.Brush("#F5CC32"):fraction>.26?Design.Brush("#303036"):Design.Ink;var pen=new Pen(ink,1.8){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round};double x=ActualWidth/2,y=ActualHeight-27;
            if(symbol=="sun"){dc.DrawEllipse(null,pen,new Point(x,y),6,6);for(int i=0;i<8;i++){double a=i*Math.PI/4;dc.DrawLine(pen,new Point(x+9*Math.Cos(a),y+9*Math.Sin(a)),new Point(x+12*Math.Cos(a),y+12*Math.Sin(a)));}}
            else{dc.PushTransform(new TranslateTransform(x-12,y-12));dc.DrawGeometry(ink,null,Geometry.Parse("M3,9 L7,9 13,5 13,19 7,15 3,15 Z"));dc.DrawGeometry(null,pen,Geometry.Parse("M17,8 C21,10 21,14 17,16"));dc.Pop();}
            dc.Pop();if(IsKeyboardFocused)dc.DrawRoundedRectangle(null,new Pen(Design.Brush(Design.Accent),1.5),bounds,radius,radius);
        }
        private void SetFrom(Point point){Value=Minimum+(Maximum-Minimum)*(1-Math.Max(0,Math.Min(1,point.Y/Math.Max(1,ActualHeight))));}
        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e){if(!IsEnabled)return;Focus();CaptureMouse();SetFrom(e.GetPosition(this));e.Handled=true;}
        protected override void OnPreviewMouseMove(MouseEventArgs e){if(IsMouseCaptured&&e.LeftButton==MouseButtonState.Pressed){SetFrom(e.GetPosition(this));e.Handled=true;}}
        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e){if(IsMouseCaptured){ReleaseMouseCapture();e.Handled=true;}}
    }
}

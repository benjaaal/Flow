using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace MacFlow
{
    internal static class MotionRenewalRegression
    {
        internal static void Run(List<string> results)
        {
            Action<bool,string> check=delegate(bool ok,string label){if(!ok)throw new Exception(label);results.Add("PASS 27.2.2: "+label);};
            foreach(double damping in new[]{.72,.82,1.0,1.25})
            {
                double exact=-42,vExact=13;Motion.Spring(ref exact,ref vExact,416,.8,22,damping);
                foreach(int hz in new[]{30,60,120,144})
                {double x=-42,v=13;for(int frame=0;frame<hz;frame++)Motion.Spring(ref x,ref v,416,.8/hz,22,damping);check(Math.Abs(x-exact)<1e-8&&Math.Abs(v-vExact)<1e-7,"física independiente de cuadros · amortiguación "+damping+" · "+hz+" Hz");}
            }
            double soft=0,vs=0,critical=0,vc=0;Motion.Spring(ref soft,ref vs,1,.1,22,.72);Motion.Spring(ref critical,ref vc,1,.1,22,1);
            check(Math.Abs(soft-critical)>.05,"el parámetro de amortiguación modifica la respuesta real");
            double value=152,velocity=0;Motion.Spring(ref value,ref velocity,416,.07,22,.82);double before=value,speed=velocity;Motion.Spring(ref value,ref velocity,152,.00001,22,.82);
            check(Math.Abs(value-before)<.1&&speed>0&&velocity>0,"una inversión conserva pose y velocidad antes de frenar");
            Motion.Spring(ref value,ref velocity,152,2,22,.82);check(Math.Abs(value-152)<.00001&&Math.Abs(velocity)<.0001,"un cuadro tardío converge sin pasos artificiales ni explosión física");
            var area=new DisplayArea("fixture",new Rect(-1920,-200,1920,1080),1.25){WorkingPixels=new Rect(-1920,-164,1920,1020)};
            check(Math.Abs(ShellGeometry.BottomInset(area,false)-27.2)<.001&&ShellGeometry.BottomInset(area,true)==8,"Dock calcula separación del área útil y DPI, sin margen fijo de 58 puntos");
            area.WorkingPixels=new Rect(-1860,-200,1860,1080);check(ShellGeometry.SideInset(area,true,false)==58&&ShellGeometry.BottomInset(area,false)==8,"Dock respeta una barra lateral y no inventa espacio inferior");
            foreach(double height in new[]{34.0,80,160,258})
            {var geometry=NotchSurface.Outline(416,height);check(geometry.IsFrozen&&!geometry.FillContains(new Point(0,0))&&geometry.FillContains(new Point(208,height/2)),"isla redondeada continua a altura "+height);}
            var texture=BitmapSource.Create(2,2,96,96,PixelFormats.Bgra32,null,new byte[]{255,0,0,255,0,255,0,255,0,0,255,255,255,255,255,255},8);texture.Freeze();
            var flight=new GenieSurface(texture,new Rect(40,20,480,320),new Point(620,640),20,700,700,true);flight.Measure(new Size(700,700));flight.Arrange(new Rect(0,0,700,700));
            foreach(double progress in new[]{0.0,.1,.25,.5,.75,1})
            {flight.SetValue(GenieSurface.ProgressProperty,progress);var image=(Border)flight.Children[0];var rect=image.TransformToAncestor(flight).TransformBounds(new Rect(0,0,image.ActualWidth,image.ActualHeight));check(Math.Abs(rect.Width/rect.Height-1.5)<1e-8,"trayectoria al Dock conserva proporciones reales · "+progress);}
            check((flight.Tail-new Point(620,640)).Length<.001,"minimizar alcanza exactamente el destino del Dock");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
namespace MacFlow
{
    // Original, scalable artwork. Shared frozen drawings, no icon fonts.
    internal static class FeatureIcons
    {
        private static readonly Dictionary<string,ImageSource> cache=new Dictionary<string,ImageSource>();
        private static SolidColorBrush Brush(string color){return new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));}
        private static LinearGradientBrush Gradient(string top,string bottom){return new LinearGradientBrush(Brush(top).Color,Brush(bottom).Color,90);}
        private static Pen Line(string color,double width){return new Pen(Brush(color),width){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round,LineJoin=PenLineJoin.Round};}
        private static void Plate(DrawingContext dc,string top,string bottom)
        {
            if(IconThemes.Dark&&Design.IconAppearance!="Original"){top="#303B4D";bottom="#111B29";}
            dc.DrawRoundedRectangle(Gradient(top,bottom),null,new Rect(3,3,58,58),14,14);
            dc.DrawRoundedRectangle(null,new Pen(Gradient("#50FFFFFF","#05FFFFFF"),.65),new Rect(3.5,3.5,57,57),13.5,13.5);
        }
        internal static ImageSource Get(string kind)
        {
            string cacheKey=kind+(IconThemes.Dark&&Design.IconAppearance!="Original"?":dark":":light");ImageSource image;if(cache.TryGetValue(cacheKey,out image))return image;
            var drawing=new DrawingGroup();using(var dc=drawing.Open())
            {
                dc.DrawRectangle(Brushes.Transparent,null,new Rect(0,0,64,64));
                if(kind=="folder")
                {
                    dc.DrawGeometry(Gradient("#90DDFF","#3BA7E8"),null,Geometry.Parse("M4,17 Q4,11 10,11 L24,11 Q28,11 31,17 L53,17 Q60,17 60,23 L60,48 Q60,54 54,54 L10,54 Q4,54 4,48 Z"));
                    dc.DrawRoundedRectangle(Gradient("#77D2FA","#239CDF"),null,new Rect(4,24,56,31),6,6);
                    dc.DrawLine(Line("#91E3FF",1),new Point(10,25),new Point(54,25));
                }
                else if(kind=="grid")
                {
                    Plate(dc,"#F1F3F8","#CFD6E4");
                    string[] colors={"#49A8F1","#FFAC5D","#8C7BE7","#55C5AD","#EC7D9C","#6BB7DC","#8793E8","#DDAB55","#76BE83"};
                    for(int y=0;y<3;y++)for(int x=0;x<3;x++)dc.DrawRoundedRectangle(Brush(colors[y*3+x]),null,new Rect(13+x*14,13+y*14,10,10),3,3);
                }
                else if(kind=="windows")
                {
                    Plate(dc,"#718EE4","#5268BE");dc.DrawRoundedRectangle(Brush("#C8D5F7"),null,new Rect(10,13,32,27),5,5);
                    dc.DrawRoundedRectangle(Brush("#F1F5FF"),null,new Rect(22,24,32,27),5,5);
                    dc.DrawRoundedRectangle(Gradient("#88ACE8","#6889C6"),null,new Rect(25,33,26,15),2,2);
                    dc.DrawEllipse(Brush("#ED8C91"),null,new Point(27,29),1.4,1.4);dc.DrawEllipse(Brush("#E6BD69"),null,new Point(32,29),1.4,1.4);dc.DrawEllipse(Brush("#76B894"),null,new Point(37,29),1.4,1.4);
                }
                else if(kind=="settings")
                {
                    Plate(dc,"#D7DBE1","#A4ADB9");
                    // A clear twelve-tooth gear with three machined rings.
                    var gear=new StreamGeometry();using(var path=gear.Open())
                    {
                        for(int i=0;i<72;i++)
                        {
                            double a=i*Math.PI/36-Math.PI/2,r=i%6==1||i%6==2||i%6==3?23:20;
                            var p=new Point(32+r*Math.Cos(a),32+r*Math.Sin(a));if(i==0)path.BeginFigure(p,true,true);else path.LineTo(p,true,false);
                        }
                    }
                    dc.DrawGeometry(IconThemes.Dark&&Design.IconAppearance!="Original"?Gradient("#B2C4DB","#657D9E"):Gradient("#636B76","#424B58"),null,gear);
                    dc.DrawEllipse(null,new Pen(Gradient("#F1F4F7","#C5CED9"),2.1),new Point(32,32),16.3,16.3);
                    dc.DrawEllipse(null,new Pen(Brush("#8793A2"),1.4),new Point(32,32),12.7,12.7);
                    for(int i=0;i<8;i++){double a=i*Math.PI/4;dc.DrawLine(Line("#AAB6C5",1.4),new Point(32+9.2*Math.Cos(a),32+9.2*Math.Sin(a)),new Point(32+11.6*Math.Cos(a),32+11.6*Math.Sin(a)));}
                    dc.DrawEllipse(Gradient("#728091","#3F4B5C"),new Pen(Brush("#DCE4EE"),1.8),new Point(32,32),7,7);
                }
                else if(kind=="focus")
                {
                    Plate(dc,"#A294DA","#7563B6");dc.DrawGeometry(Brush("#F8F5FF"),null,Geometry.Parse("M39,14 C25,10 15,23 18,36 C21,49 38,53 48,43 C32,46 24,28 39,14 Z"));
                }
                else if(kind=="preview")
                {
                    Plate(dc,"#D7E9F2","#A1C2D5");dc.PushTransform(new RotateTransform(-7,32,32));dc.DrawRoundedRectangle(Brush("#F4F8FA"),null,new Rect(13,13,35,36),4,4);
                    dc.DrawRoundedRectangle(Gradient("#83C8DF","#4F96B9"),null,new Rect(16,17,29,24),2,2);dc.DrawGeometry(Brush("#D6EEF5"),null,Geometry.Parse("M16,39 L25,27 32,33 37,29 45,39 Z"));dc.DrawEllipse(Brush("#FFE3AE"),null,new Point(38,23),2.8,2.8);dc.Pop();
                    dc.DrawEllipse(Brush("#55FFFFFF"),Line("#426B89",2.2),new Point(44,43),7.5,7.5);dc.DrawLine(Line("#426B89",3),new Point(49,49),new Point(55,55));
                }
                else
                {
                    Plate(dc,"#7ABCEB","#4379D6");dc.DrawGeometry(null,new Pen(Gradient("#F9FDFF","#D0E7FA"),5.5){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round},Geometry.Parse("M13,42 C19,42 18,22 25,22 C32,22 30,42 37,42 C44,42 44,23 51,22"));
                }
            }
            drawing.Freeze();image=new DrawingImage(drawing);image.Freeze();cache[cacheKey]=image;return image;
        }
    }
}

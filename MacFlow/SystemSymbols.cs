using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
namespace MacFlow
{
    // Original optical 24-point glyphs. Fixed bounds, shared frozen geometry,
    // filled status symbols and consistent rounded strokes; no font/emoji.
    internal sealed class SystemSymbol : FrameworkElement
    {
        internal static readonly DependencyProperty InkProperty=DependencyProperty.Register("Ink",typeof(Brush),typeof(SystemSymbol),new FrameworkPropertyMetadata(Brushes.White,FrameworkPropertyMetadataOptions.AffectsRender));
        private static readonly Dictionary<string,Geometry> cache=new Dictionary<string,Geometry>();
        private readonly string kind;
        internal SystemSymbol(string kind,double size,Brush brush)
        {
            this.kind=kind;Width=Height=size;IsHitTestVisible=false;SnapsToDevicePixels=false;SetValue(InkProperty,brush);
            var solid=brush as SolidColorBrush;if(solid!=null&&solid.Color==((SolidColorBrush)Design.Ink).Color)SetResourceReference(InkProperty,"GlassInk");else if(solid!=null&&solid.Color==((SolidColorBrush)Design.Muted).Color)SetResourceReference(InkProperty,"GlassMuted");
        }
        private static Geometry Shape(string data){Geometry value;if(!cache.TryGetValue(data,out value)){value=Geometry.Parse(data);value.Freeze();cache[data]=value;}return value;}
        protected override void OnRender(DrawingContext dc)
        {
            var ink=(Brush)GetValue(InkProperty);var pen=new Pen(ink,1.75){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round,LineJoin=PenLineJoin.Round};dc.PushTransform(new ScaleTransform(ActualWidth/24,ActualHeight/24));
            if(kind.StartsWith("wifi",StringComparison.Ordinal))
            {
                if(kind!="wifi-low")dc.DrawGeometry(ink,null,Shape("M2,7.8 C7.5,2.7 16.5,2.7 22,7.8 L20.3,9.5 C15.7,5.2 8.3,5.2 3.7,9.5 Z"));
                dc.DrawGeometry(ink,null,Shape("M5.2,11.4 C9,7.9 15,7.9 18.8,11.4 L17.1,13.1 C14.3,10.5 9.7,10.5 6.9,13.1 Z M8.5,15 C10.4,13.2 13.6,13.2 15.5,15 L13.8,16.7 C12.8,15.8 11.2,15.8 10.2,16.7 Z"));dc.DrawEllipse(ink,null,new Point(12,19),1.35,1.35);
                if(kind=="wifi-off"){var cut=new Pen(Design.Brush(Design.Dark?"#18212D":"#EDF2F7"),4){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round};dc.DrawLine(cut,new Point(4,3),new Point(21,20));dc.DrawLine(pen,new Point(4,3),new Point(21,20));}
            }
            else if(kind.StartsWith("volume",StringComparison.Ordinal))
            {
                dc.DrawGeometry(ink,null,Shape("M3,9 Q3,8.4 3.6,8.4 L7.5,8.4 12.3,4.8 Q13,4.3 13,5.2 L13,18.8 Q13,19.7 12.3,19.2 L7.5,15.6 3.6,15.6 Q3,15.6 3,15 Z"));
                if(kind=="volume-off")dc.DrawGeometry(null,pen,Shape("M17,9 L22,15 M22,9 L17,15"));
                else{dc.DrawGeometry(null,pen,Shape("M16.5,8.6 C19,10.2 19,13.8 16.5,15.4"));if(kind!="volume-low")dc.DrawGeometry(null,pen,Shape("M19.5,5.7 C24,8.6 24,15.4 19.5,18.3"));}
            }
            else if(kind=="control")
            {
                dc.DrawRoundedRectangle(null,pen,new Rect(3,4,18,6),3,3);dc.DrawRoundedRectangle(null,pen,new Rect(3,14,18,6),3,3);dc.DrawEllipse(ink,null,new Point(8,7),2,2);dc.DrawEllipse(ink,null,new Point(16,17),2,2);
            }
            else if(kind=="grid")
            {for(int y=0;y<2;y++)for(int x=0;x<2;x++)dc.DrawRoundedRectangle(null,pen,new Rect(3+x*12,3+y*12,6,6),1.6,1.6);}
            else if(kind=="settings")
            {
                dc.DrawEllipse(null,pen,new Point(12,12),6.3,6.3);dc.DrawEllipse(null,pen,new Point(12,12),2.5,2.5);
                for(int i=0;i<8;i++){double a=i*Math.PI/4;dc.DrawLine(pen,new Point(12+6.1*Math.Cos(a),12+6.1*Math.Sin(a)),new Point(12+8.8*Math.Cos(a),12+8.8*Math.Sin(a)));}
            }
            else
            {
                string data;
                switch(kind)
                {
                    case "bluetooth":data="M11.5,3 L17,7.5 7,16.5 M11.5,21 L17,16.5 7,7.5 M11.5,3 L11.5,21";break;
                    case "appearance":data="M12,3 A9,9 0 1 1 11.99,3 M12,3 L12,21 M12,3 A9,9 0 0 1 12,21 Z";break;
                    case "wallpaper":data="M5,4 L19,4 Q21,4 21,6 L21,18 Q21,20 19,20 L5,20 Q3,20 3,18 L3,6 Q3,4 5,4 Z M3,17 L9,10 14,15 17,12 21,17 M16.5,7 L16.5,7.1";break;
                    case "dock":data="M4,9 L20,9 Q22,9 22,11 L22,18 Q22,20 20,20 L4,20 Q2,20 2,18 L2,11 Q2,9 4,9 Z M6,13 L6,16 M12,12 L12,16 M18,13 L18,16";break;
                    case "topbar":data="M4,4 L20,4 Q22,4 22,6 L22,18 Q22,20 20,20 L4,20 Q2,20 2,18 L2,6 Q2,4 4,4 Z M2,9 L22,9 M5,6.5 L5.1,6.5 M17,6.5 L19,6.5";break;
                    case "performance":data="M4,18 A9,9 0 1 1 20,18 M5,18 L19,18 M12,15 L17,7 M6,10 L7,11 M12,5 L12,7";break;
                    case "network":data="M5,3 L19,3 L19,12 16,12 16,16 8,16 8,12 5,12 Z M12,16 L12,21 M6,21 L18,21 M8,6 L8,9 M12,6 L12,9 M16,6 L16,9";break;
                    case "search":data="M10.5,3.5 A7,7 0 1 1 10.49,3.5 M16,16 L21,21";break;
                    case "grid":data="M3,3 L9,3 9,9 3,9 Z M15,3 L21,3 21,9 15,9 Z M3,15 L9,15 9,21 3,21 Z M15,15 L21,15 21,21 15,21 Z";break;
                    case "display":data="M4,4 Q2.8,4 2.8,5.2 L2.8,15.8 Q2.8,17 4,17 L20,17 Q21.2,17 21.2,15.8 L21.2,5.2 Q21.2,4 20,4 Z M12,17 L12,21 M7,21 L17,21";break;
                    case "battery":data="M3,6 L18,6 Q20,6 20,8 L20,16 Q20,18 18,18 L3,18 Q1,18 1,16 L1,8 Q1,6 3,6 Z M22,10 L22,14 M5,9 L5,15 M8,9 L8,15 M11,9 L11,15";break;
                    case "camera":data="M4,7 L8,7 10,4 15,4 17,7 20,7 Q22,7 22,9 L22,18 Q22,20 20,20 L4,20 Q2,20 2,18 L2,9 Q2,7 4,7 Z M12,10 A3.5,3.5 0 1 1 11.99,10";break;
                    case "sun":data="M12,7 A5,5 0 1 1 11.99,7 M12,2 L12,3.5 M12,20.5 L12,22 M2,12 L3.5,12 M20.5,12 L22,12 M4.9,4.9 L6,6 M18,18 L19.1,19.1 M4.9,19.1 L6,18 M18,6 L19.1,4.9";break;
                    case "moon":data="M20.5,14.5 A9,9 0 1 1 9.5,3.5 A7.5,7.5 0 0 0 20.5,14.5 Z";break;
                    case "settings":data="M12,3 L14,3 14.6,5.2 16.1,6.1 18.2,5.5 20.2,8.8 18.6,10.3 18.6,12.4 20.2,13.9 18.2,17.2 16.1,16.6 14.6,17.5 14,19.7 10,19.7 9.4,17.5 7.9,16.6 5.8,17.2 3.8,13.9 5.4,12.4 5.4,10.3 3.8,8.8 5.8,5.5 7.9,6.1 9.4,5.2 10,3 Z M12,8 A3.4,3.4 0 1 1 11.99,8";break;
                    case "windows":data="M4,4 L17,4 17,15 4,15 Z M8,19 L21,19 21,8";break;
                    case "power":data="M12,2 L12,11 M6,5.5 A8.5,8.5 0 1 0 18,5.5";break;
                    case "play":data="M8,5.5 L18.5,12 8,18.5 Z";break;
                    case "folder":data="M3,6 Q3,4.5 4.5,4.5 L9,4.5 11,7 19.5,7 Q21,7 21,8.5 L21,18.5 Q21,20 19.5,20 L4.5,20 Q3,20 3,18.5 Z";break;
                    case "check":data="M5,12 L9.5,16.5 19.5,6.5";break;
                    case "close":data="M6,6 L18,18 M18,6 L6,18";break;
                    default:data="M12,3 A9,9 0 1 1 11.99,3 M12,8 L12,13 M12,16 L12,16.1";break;
                }
                dc.DrawGeometry(kind=="play"?ink:null,pen,Shape(data));
            }
            dc.Pop();
        }
    }
}

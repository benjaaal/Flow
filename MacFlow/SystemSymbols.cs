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
                // Original redraw inspired by Apple's compact optical Wi-Fi
                // proportions; no SF Symbols font or proprietary asset.
                var arc=new Pen(ink,2.25){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round};
                if(kind=="wifi-off")dc.PushClip(new CombinedGeometry(GeometryCombineMode.Exclude,new RectangleGeometry(new Rect(0,0,24,24)),Shape("M2,0 L24,22 22,24 0,2 Z")));
                if(kind!="wifi-low")dc.DrawGeometry(null,arc,Shape("M2.4,7.5 C7.8,2.8 16.2,2.8 21.6,7.5"));
                dc.DrawGeometry(null,arc,Shape("M5.7,11.3 C9.2,8.3 14.8,8.3 18.3,11.3 M9,15.1 C10.7,13.6 13.3,13.6 15,15.1"));dc.DrawEllipse(ink,null,new Point(12,19),1.4,1.4);
                if(kind=="wifi-off"){dc.Pop();dc.DrawLine(pen,new Point(3,2),new Point(21,20));}
            }
            else if(kind.StartsWith("volume",StringComparison.Ordinal))
            {
                dc.DrawGeometry(ink,null,Shape("M3,9 Q3,8.4 3.6,8.4 L7.5,8.4 12.3,4.8 Q13,4.3 13,5.2 L13,18.8 Q13,19.7 12.3,19.2 L7.5,15.6 3.6,15.6 Q3,15.6 3,15 Z"));
                if(kind=="volume-off")dc.DrawGeometry(null,pen,Shape("M17,9 L22,15 M22,9 L17,15"));
                else{dc.DrawGeometry(null,pen,Shape("M16.5,8.6 C19,10.2 19,13.8 16.5,15.4"));if(kind!="volume-low")dc.DrawGeometry(null,pen,Shape("M19.5,5.7 C24,8.6 24,15.4 19.5,18.3"));}
            }
            else if(kind=="chevron")dc.DrawGeometry(null,pen,Shape("M9,5 L16,12 9,19"));
            else if(kind=="bell")
            {
                dc.DrawGeometry(null,pen,Shape("M5,16 C7,14 6.3,12 7,8.6 C7.5,5.5 9.3,4 12,4 C14.7,4 16.5,5.5 17,8.6 C17.7,12 17,14 19,16 Z M10,20 C11,21 13,21 14,20 M12,2.4 L12,4"));
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
                    case "keyboard":data="M4,5 L20,5 Q22,5 22,7 L22,17 Q22,19 20,19 L4,19 Q2,19 2,17 L2,7 Q2,5 4,5 Z M6,9 L6.1,9 M10,9 L10.1,9 M14,9 L14.1,9 M18,9 L18.1,9 M6,12 L6.1,12 M10,12 L10.1,12 M14,12 L14.1,12 M18,12 L18.1,12 M7,16 L17,16";break;
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
                    case "timer":data="M12,6 A8,8 0 1 1 11.99,6 M9,2 L15,2 M12,2 L12,6 M18,5 L20,3 M12,10 L12,15 15,15";break;
                    case "music":data="M9,17 L9,5 20,3 20,15 M9,8 L20,6 M9,17 A3,2 0 1 1 8.99,17 M20,15 A3,2 0 1 1 19.99,15";break;
                    case "pause":data="M8,5 L8,19 M16,5 L16,19";break;
                    case "previous":data="M5,5 L5,19 M18,5 L8,12 18,19 Z";break;
                    case "next":data="M19,5 L19,19 M6,5 L16,12 6,19 Z";break;
                    case "up":data="M5,15 L12,8 19,15";break;
                    case "assistant":data="M12,3 C12,9 9,12 3,12 C9,12 12,15 12,21 C12,15 15,12 21,12 C15,12 12,9 12,3 Z";break;
                    case "mic":data="M9,5 A3,3 0 0 1 15,5 L15,12 A3,3 0 0 1 9,12 Z M6,11 L6,12 A6,6 0 0 0 18,12 L18,11 M12,18 L12,22 M8,22 L16,22";break;
                    case "calendar":data="M5,5 L19,5 Q21,5 21,7 L21,20 Q21,22 19,22 L5,22 Q3,22 3,20 L3,7 Q3,5 5,5 Z M3,10 L21,10 M8,2 L8,7 M16,2 L16,7 M8,14 L9,14 M15,14 L16,14 M8,18 L9,18 M15,18 L16,18";break;
                    case "restart":data="M19.5,8 A8,8 0 1 0 20,15 M20,3 L20,9 14,9";break;
                    case "lock":data="M7,10 L7,7 A5,5 0 0 1 17,7 L17,10 M6,10 L18,10 Q20,10 20,12 L20,20 Q20,22 18,22 L6,22 Q4,22 4,20 L4,12 Q4,10 6,10 Z M12,15 L12,18";break;
                    case "signout":data="M10,3 L4,3 Q3,3 3,4 L3,20 Q3,21 4,21 L10,21 M9,12 L22,12 M18,8 L22,12 18,16";break;
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

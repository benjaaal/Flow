using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
namespace MacFlow
{
    // Original vector icons; each frozen image is shared across the UI.
    internal static class FeatureIcons
    {
        private static readonly Dictionary<string, ImageSource> cache = new Dictionary<string, ImageSource>();
        private static SolidColorBrush Brush(string color) { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)); }
        private static LinearGradientBrush Gradient(string top, string bottom) { return new LinearGradientBrush(Brush(top).Color, Brush(bottom).Color, 90); }
        private static Pen Line(string color, double width) { return new Pen(Brush(color), width) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round, LineJoin = PenLineJoin.Round }; }
        internal static ImageSource Get(string kind)
        {
            ImageSource cached; if (cache.TryGetValue(kind, out cached)) return cached;
            var drawing = new DrawingGroup(); using (var dc = drawing.Open())
            {
                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, 64, 64));
                if (kind == "folder")
                {
                    dc.DrawGeometry(Gradient("#96DCFF", "#38A4ED"), null, Geometry.Parse("M4,16 Q4,10 10,10 L24,10 Q28,10 31,16 L53,16 Q60,16 60,23 L60,49 Q60,56 53,56 L11,56 Q4,56 4,49 Z"));
                    dc.DrawRoundedRectangle(Gradient("#70D0FF", "#0874DD"), null, new Rect(4, 23, 56, 33), 7, 7);
                    dc.DrawLine(Line("#7ED5FF", 1.4), new Point(11, 24), new Point(53, 24));
                }
                else if (kind == "grid")
                {
                    dc.DrawRoundedRectangle(Gradient("#EDCAFF", "#8562E4"), null, new Rect(3, 3, 58, 58), 14, 14);
                    for (int y = 0; y < 3; y++) for (int x = 0; x < 3; x++) dc.DrawRoundedRectangle(Brush("#EDFFFFFF"), null, new Rect(14 + x * 14, 14 + y * 14, 8, 8), 2.5, 2.5);
                }
                else if (kind == "windows")
                {
                    dc.DrawRoundedRectangle(Gradient("#8CE5E7", "#2794BD"), null, new Rect(3, 3, 58, 58), 14, 14);
                    dc.DrawRoundedRectangle(Brush("#A0E8FA"), null, new Rect(11, 12, 32, 29), 4, 4);
                    dc.DrawRoundedRectangle(Brush("#ECFFFFFF"), null, new Rect(21, 23, 32, 29), 4, 4);
                    dc.DrawRoundedRectangle(Brush("#6F91AD"), null, new Rect(25, 31, 24, 17), 2, 2);
                    dc.DrawEllipse(Brush("#F27E8B"), null, new Point(26, 27), 1.5, 1.5); dc.DrawEllipse(Brush("#F3CB77"), null, new Point(31, 27), 1.5, 1.5); dc.DrawEllipse(Brush("#72C9A9"), null, new Point(36, 27), 1.5, 1.5);
                }
                else if (kind == "settings")
                {
                    // Original iOS-inspired Settings artwork, rendered as a
                    // vector at every size, with quiet machined-metal lighting.
                    dc.DrawRoundedRectangle(Gradient("#C6CBD2", "#9198A3"), null, new Rect(3, 3, 58, 58), 14, 14);
                    dc.DrawRoundedRectangle(null, new Pen(Brush("#50FFFFFF"), .65), new Rect(3.6, 3.6, 56.8, 56.8), 13.4, 13.4);
                    var teeth = new StreamGeometry(); using (var path = teeth.Open())
                    {
                        for (int i = 0; i < 96; i++)
                        {
                            double angle = i * Math.PI / 48 - Math.PI / 2;
                            double radius = i % 6 == 1 || i % 6 == 2 || i % 6 == 3 ? 23 : 20;
                            var point = new Point(32 + radius * Math.Cos(angle), 32 + radius * Math.Sin(angle));
                            if (i == 0) path.BeginFigure(point, true, true); else path.LineTo(point, true, false);
                        }
                    }
                    teeth.Freeze(); dc.DrawGeometry(Gradient("#575D65", "#272C33"), new Pen(Brush("#8BDDE2E8"), .8), teeth);
                    dc.DrawEllipse(null, new Pen(Gradient("#F0F2F4", "#ABB2BC"), 2.2), new Point(32, 32), 17, 17);
                    dc.DrawEllipse(null, new Pen(Brush("#343A43"), 1.3), new Point(32, 32), 14.6, 14.6);
                    for (int i = 0; i < 10; i++)
                    {
                        double angle = i * Math.PI / 5;
                        dc.DrawLine(Line("#9FA7B2", 1.6), new Point(32 + 9.5 * Math.Cos(angle), 32 + 9.5 * Math.Sin(angle)), new Point(32 + 12.6 * Math.Cos(angle), 32 + 12.6 * Math.Sin(angle)));
                    }
                    dc.DrawEllipse(Gradient("#7B838E", "#333A44"), new Pen(Gradient("#E6EBF0", "#A0AAB7"), 2), new Point(32, 32), 7.6, 7.6);
                    dc.DrawEllipse(Brush("#303741"), new Pen(Brush("#89939F"), .7), new Point(32, 32), 4.8, 4.8);
                }
                else if (kind == "focus")
                {
                    dc.DrawRoundedRectangle(Gradient("#AE98ED", "#5C55AF"), null, new Rect(3, 3, 58, 58), 14, 14);
                    dc.DrawGeometry(Brush("#F5FFFFFF"), null, Geometry.Parse("M39,13 C25,10 14,22 17,36 C20,52 40,57 50,44 C32,48 21,28 39,13 Z"));
                }
                else if (kind == "preview")
                {
                    dc.DrawRoundedRectangle(Gradient("#B5CFEB", "#59769B"), null, new Rect(3, 3, 58, 58), 14, 14);
                    dc.DrawRoundedRectangle(Brush("#F0FFFFFF"), null, new Rect(13, 15, 38, 32), 5, 5);
                    dc.DrawGeometry(Gradient("#66C2E0", "#4D89B2"), null, Geometry.Parse("M17,42 L17,36 26,25 33,33 40,29 47,38 47,42 Z")); dc.DrawEllipse(Brush("#F2C17B"), null, new Point(40, 23), 3, 3);
                }
                else
                {
                    dc.DrawRoundedRectangle(Gradient("#80D3FF", "#3A63E4"), null, new Rect(3, 3, 58, 58), 14, 14);
                    dc.DrawGeometry(null, new Pen(Gradient("#F1FBFF", "#BEE7FF"), 7) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }, Geometry.Parse("M13,44 C20,13 27,10 33,32 C40,55 47,49 52,19"));
                }
            }
            drawing.Freeze(); var image = new DrawingImage(drawing); image.Freeze(); cache[kind] = image; return image;
        }
    }
}

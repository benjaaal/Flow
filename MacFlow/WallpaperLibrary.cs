using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;

namespace MacFlow
{
    public sealed class WallpaperChoice
    {
        public string Name, File;
        public BitmapSource Thumbnail;
    }
    public static class WallpaperLibrary
    {
        // Original vector compositions, generated once off the UI thread.
        // No downloaded assets or continuous desktop animation.
        public static List<WallpaperChoice> Load(string directory)
        {
            string folder=Path.Combine(directory,"Wallpapers");Directory.CreateDirectory(folder);
            var result=new List<WallpaperChoice>();
            string[] art={"Seda","Sierra","Aqua","Midnight"},labels={"Seda","Sierra","Aqua","Medianoche"};
            for(int i=0;i<art.Length;i++)
            {
                string file=Path.Combine(folder,"MacFlow-"+art[i]+"-1.png");
                if(!System.IO.File.Exists(file))
                {
                    using(var resource=typeof(WallpaperLibrary).Assembly.GetManifestResourceStream("MacFlow.Wallpaper."+art[i]+".png"))
                    {if(resource==null)throw new IOException("Falta el fondo integrado: "+art[i]);using(var output=new FileStream(file,FileMode.CreateNew,FileAccess.Write,FileShare.None))resource.CopyTo(output);}
                }
                result.Add(new WallpaperChoice{Name=labels[i],File=file,Thumbnail=Decode(file,380)});
            }
            return result;
        }
        public static BitmapSource Decode(string path,int width)
        {
            using(var stream=System.IO.File.OpenRead(path))
            {
                var image=new BitmapImage();image.BeginInit();image.CacheOption=BitmapCacheOption.OnLoad;image.DecodePixelWidth=width;image.StreamSource=stream;image.EndInit();image.Freeze();return image;
            }
        }
        private static void Create(string file,int style)
        {
            Color[] a={Color.FromArgb(12,9,31),Color.FromArgb(5,30,48),Color.FromArgb(241,223,211),Color.FromArgb(4,10,27)};
            Color[] b={Color.FromArgb(24,18,55),Color.FromArgb(7,59,68),Color.FromArgb(190,196,221),Color.FromArgb(16,25,65)};
            Color[] light={Color.FromArgb(183,100,247),Color.FromArgb(83,224,207),Color.FromArgb(255,192,146),Color.FromArgb(91,135,244)};
            Color[] second={Color.FromArgb(251,113,191),Color.FromArgb(46,145,208),Color.FromArgb(183,149,223),Color.FromArgb(137,107,229)};
            using(var bitmap=new Bitmap(1920,1080,PixelFormat.Format24bppRgb))
            using(var g=Graphics.FromImage(bitmap))
            {
                g.SmoothingMode=SmoothingMode.AntiAlias;
                using(var fill=new LinearGradientBrush(new Rectangle(0,0,1920,1080),a[style],b[style],35))g.FillRectangle(fill,0,0,1920,1080);
                using(var curve=new GraphicsPath())using(var other=new GraphicsPath())
                {
                    curve.AddBezier(-240,990,210,-720,1360,290,2040,1060);
                    other.AddBezier(-80,1200,970,820,560,-170,2050,-160);
                    Glow(g,curve,light[style],style==2?130:85);
                    Glow(g,other,second[style],style==2?150:105);
                }
                // Noise-free JPEG at native desktop resolution; decode thumbnails separately.
                bitmap.Save(file,ImageFormat.Jpeg);
            }
        }
        private static void Glow(Graphics g,GraphicsPath path,Color color,float width)
        {
            for(int i=48;i>=1;i--)
            {
                float spread=width*i/12;
                int alpha=Math.Max(1,(int)Math.Round(5*Math.Exp(-.5*Math.Pow(i/13.0,2))));
                using(var pen=new Pen(Color.FromArgb(alpha,color),spread)){pen.StartCap=pen.EndCap=LineCap.Round;g.DrawPath(pen,path);}
            }
        }
    }
}

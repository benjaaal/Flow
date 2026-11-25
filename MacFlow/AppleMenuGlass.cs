using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Threading.Tasks;

namespace MacFlow
{
    internal sealed class AppleMenuGlass : Grid
    {
        internal const double Radius=18;
        private readonly Border lensLayer;
        private readonly Image texture;
        private readonly Border plate;
        private bool unloaded;
        internal bool HasBackdrop {get{return texture.Source!=null;}}

        internal AppleMenuGlass(UIElement content)
        {
            var outline=new CornerRadius(Radius);
            var dark=Design.Dark;
            UseLayoutRounding=true;SnapsToDevicePixels=true;
            if(!SystemParameters.HighContrast)
            {
                Children.Add(new Border{CornerRadius=new CornerRadius(Radius+3),IsHitTestVisible=false,Background=Design.Brush("#0A101927"),Margin=new Thickness(2,2,2,2)});
                Children.Add(new Border{CornerRadius=new CornerRadius(Radius+1),IsHitTestVisible=false,Background=Design.Brush("#17101927"),Margin=new Thickness(4,3,4,4)});
            }
            var panel=new Grid{Margin=new Thickness(5,3,5,7)};
            Children.Add(panel);
            texture=new Image{Stretch=Stretch.Fill,IsHitTestVisible=false};
            RenderOptions.SetBitmapScalingMode(texture,BitmapScalingMode.Linear);
            lensLayer=new Border{Child=texture,IsHitTestVisible=false,Visibility=Visibility.Collapsed};
            lensLayer.SizeChanged+=delegate{
                double w=Math.Max(1,lensLayer.ActualWidth),h=Math.Max(1,lensLayer.ActualHeight);
                lensLayer.Clip=new RectangleGeometry(new Rect(0,0,w,h),Radius,Radius);
            };
            panel.Children.Add(lensLayer);
            plate=new Border{CornerRadius=outline,Padding=new Thickness(7),BorderThickness=new Thickness(1),Child=content,
                Background=SystemParameters.HighContrast?SystemColors.WindowBrush:Design.Brush(dark?"#F4242934":"#FBEEF3FA"),
                BorderBrush=SystemParameters.HighContrast?(Brush)SystemColors.WindowTextBrush:
                    Tint(dark?"#B7D3E4FF":"#F8FFFFFF",dark?"#3BBDD0F5":"#70FFFFFF",dark?"#77A6BBDF":"#AD92B1CF")};
            panel.Children.Add(plate);
            Unloaded+=delegate{unloaded=true;texture.Source=null;};
        }

        internal void Sample(int left,int top,int width,int height)
        {
            if(!LiquidMaterial.LiveBackdrop||SystemParameters.HighContrast||width<1||height<1)return;
            try
            {
                int w=Math.Max(1,width/3),h=Math.Max(1,height/3);
                bool dark=Design.Dark;
                byte[] pixels;
                int stride;
                using(var bitmap=new System.Drawing.Bitmap(width,height,System.Drawing.Imaging.PixelFormat.Format32bppRgb))
                using(var small=new System.Drawing.Bitmap(w,h,System.Drawing.Imaging.PixelFormat.Format32bppRgb))
                {
                    using(var graphics=System.Drawing.Graphics.FromImage(bitmap))
                        graphics.CopyFromScreen(left,top,0,0,bitmap.Size);
                    using(var graphics=System.Drawing.Graphics.FromImage(small))
                    {
                        graphics.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.Low;
                        graphics.DrawImage(bitmap,new System.Drawing.Rectangle(0,0,w,h));
                    }
                    var data=small.LockBits(new System.Drawing.Rectangle(0,0,w,h),System.Drawing.Imaging.ImageLockMode.ReadOnly,small.PixelFormat);
                    try
                    {
                        stride=data.Stride;pixels=new byte[stride*h];
                        System.Runtime.InteropServices.Marshal.Copy(data.Scan0,pixels,0,pixels.Length);
                    }
                    finally{small.UnlockBits(data);}
                }
                ApplyBackdrop(pixels,stride,w,h,dark);
            }
            catch(System.ComponentModel.Win32Exception ex){Log.Write("Vidrio del menú: "+ex.Message);}
            catch(System.Runtime.InteropServices.ExternalException ex){Log.Write("Vidrio del menú: "+ex.Message);}
        }

        private async void ApplyBackdrop(byte[] pixels,int stride,int width,int height,bool dark)
        {
            try
            {
                var image=await Task.Run(delegate{
                    var horizontal=new byte[pixels.Length];
                    var blurred=new byte[pixels.Length];
                    const int blur=3;
                    for(int y=0;y<height;y++)for(int x=0;x<width;x++)for(int channel=0;channel<3;channel++)
                    {
                        int total=0;
                        for(int step=-blur;step<=blur;step++)total+=pixels[y*stride+Math.Max(0,Math.Min(width-1,x+step))*4+channel];
                        horizontal[y*stride+x*4+channel]=(byte)(total/(blur*2+1));
                    }
                    for(int y=0;y<height;y++)for(int x=0;x<width;x++)
                    {
                        int dx=Math.Min(x,width-1-x),dy=Math.Min(y,height-1-y);
                        int sampleX=x,sampleY=y;
                        if(dx<dy&&dx<6)sampleX=x+(x<width/2?1:-1)*(6-dx)/2;
                        if(dy<=dx&&dy<6)sampleY=y+(y<height/2?1:-1)*(6-dy)/2;
                        sampleX=Math.Max(0,Math.Min(width-1,sampleX));
                        int output=y*stride+x*4;
                        for(int channel=0;channel<3;channel++)
                        {
                            int total=0;
                            for(int step=-blur;step<=blur;step++)total+=horizontal[Math.Max(0,Math.Min(height-1,sampleY+step))*stride+sampleX*4+channel];
                            int mean=total/(blur*2+1);
                            blurred[output+channel]=(byte)(dark?mean*.23:154+mean*.39);
                        }
                    }
                    var value=BitmapSource.Create(width,height,96,96,PixelFormats.Bgr32,null,blurred,stride);
                    value.Freeze();return value;
                });
                if(unloaded)return;
                texture.Source=image;lensLayer.Visibility=Visibility.Visible;
                plate.Background=dark?Tint("#6B252D42","#32202B3C","#59233244"):Tint("#63FBFCFF","#22F5F8FD","#46F3F7FF");
                var window=Window.GetWindow(this);
                if(window!=null)window.Tag="Liquid Glass · muestra local";
            }
            catch(Exception ex){Log.Write("Vidrio del menú: "+ex.Message);}
        }

        private static LinearGradientBrush Tint(string top,string middle,string bottom)
        {
            var brush=new LinearGradientBrush{StartPoint=new Point(0,0),EndPoint=new Point(.85,1)};
            brush.GradientStops.Add(new GradientStop(((SolidColorBrush)Design.Brush(top)).Color,0));
            brush.GradientStops.Add(new GradientStop(((SolidColorBrush)Design.Brush(middle)).Color,.55));
            brush.GradientStops.Add(new GradientStop(((SolidColorBrush)Design.Brush(bottom)).Color,1));
            brush.Freeze();return brush;
        }
    }
}

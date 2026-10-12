using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
namespace MacFlow
{
    internal static class WelcomeWallpaper
    {
        // Read Windows' chosen wallpaper. Never capture apps or change Windows.
        internal static Task<BitmapSource> Load()
        {
            return Task.Run<BitmapSource>(delegate
            {
                object instance=null;
                try
                {
                    instance=new DesktopWallpaperObject();var service=(IDesktopWallpaper)instance;string path;int result=service.GetWallpaper(null,out path);
                    if(result<0||String.IsNullOrWhiteSpace(path))
                    {uint count;Marshal.ThrowExceptionForHR(service.GetMonitorDevicePathCount(out count));if(count==0)return null;string monitor;Marshal.ThrowExceptionForHR(service.GetMonitorDevicePathAt(0,out monitor));Marshal.ThrowExceptionForHR(service.GetWallpaper(monitor,out path));}
                    if(String.IsNullOrWhiteSpace(path)||!File.Exists(path)||new FileInfo(path).Length>64*1024*1024)return null;
                    using(var stream=File.OpenRead(path))
                    {
                        var decoder=BitmapDecoder.Create(stream,BitmapCreateOptions.DelayCreation,BitmapCacheOption.None);var frame=decoder.Frames[0];int w=frame.PixelWidth,h=frame.PixelHeight;if(w<=0||h<=0||(long)w*h>200000000)return null;stream.Position=0;
                        var image=new BitmapImage();image.BeginInit();image.CacheOption=BitmapCacheOption.OnLoad;if((double)w/h>=1.6)image.DecodePixelWidth=Math.Min(w,1920);else image.DecodePixelHeight=Math.Min(h,1200);image.StreamSource=stream;image.EndInit();image.Freeze();return image;
                    }
                }
                catch(Exception ex){Log.Write("Fondo de bienvenida: "+ex.Message);return null;}
                finally{if(instance!=null)Marshal.ReleaseComObject(instance);}
            });
        }
    }
}

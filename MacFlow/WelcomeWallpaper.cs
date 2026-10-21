using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
namespace MacFlow
{
    internal static class WelcomeWallpaper
    {
        internal static Task<BitmapSource> Load(string selected=null)
        {
            return Task.Run<BitmapSource>(delegate
            {
                object instance=null;string systemPath=null;
                try
                {
                    instance=new DesktopWallpaperObject();var service=(IDesktopWallpaper)instance;int result=service.GetWallpaper(null,out systemPath);
                    if(result<0||String.IsNullOrWhiteSpace(systemPath)){uint count;Marshal.ThrowExceptionForHR(service.GetMonitorDevicePathCount(out count));if(count>0){string monitor;Marshal.ThrowExceptionForHR(service.GetMonitorDevicePathAt(0,out monitor));Marshal.ThrowExceptionForHR(service.GetWallpaper(monitor,out systemPath));}}
                }
                catch(Exception ex){Log.Write("Fondo de bienvenida: "+ex.Message);}finally{if(instance!=null)Marshal.ReleaseComObject(instance);}
                foreach(string path in new[]{selected,systemPath})try
                {
                    if(String.IsNullOrWhiteSpace(path)||!File.Exists(path)||new FileInfo(path).Length>64*1024*1024)continue;
                    using(var stream=File.OpenRead(path))
                    {
                        var decoder=BitmapDecoder.Create(stream,BitmapCreateOptions.DelayCreation,BitmapCacheOption.None);var frame=decoder.Frames[0];int w=frame.PixelWidth,h=frame.PixelHeight;if(w<=0||h<=0||(long)w*h>200000000)continue;stream.Position=0;
                        var image=new BitmapImage();image.BeginInit();image.CacheOption=BitmapCacheOption.OnLoad;if((double)w/h>=1.6)image.DecodePixelWidth=Math.Min(w,1920);else image.DecodePixelHeight=Math.Min(h,1200);image.StreamSource=stream;image.EndInit();image.Freeze();return image;
                    }
                }
                catch(Exception ex){Log.Write("Imagen de bienvenida: "+ex.Message);}
                // A missing/solid system wallpaper or unavailable COM service
                // must not leave a feature presentation on an empty black scene.
                using(var stream=typeof(WelcomeWallpaper).Assembly.GetManifestResourceStream("MacFlow.Wallpaper.Sierra.png"))
                {if(stream==null)return null;var fallback=new BitmapImage();fallback.BeginInit();fallback.CacheOption=BitmapCacheOption.OnLoad;fallback.DecodePixelWidth=1920;fallback.StreamSource=stream;fallback.EndInit();fallback.Freeze();return fallback;}
            });
        }
    }
}

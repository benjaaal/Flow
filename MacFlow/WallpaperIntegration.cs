using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
namespace MacFlow
{
    internal static class WallpaperIntegration
    {
        // Explicit developer test, isolated journal, always restores the actual
        // original wallpapers. Never touches the user's MacFlow preferences.
        internal static int Run(string directory)
        {
            directory=Path.GetFullPath(directory);Directory.CreateDirectory(directory);var output=new List<string>();var service=(IDesktopWallpaper)new DesktopWallpaperObject();bool changed=false;
            try
            {
                uint count;Marshal.ThrowExceptionForHR(service.GetMonitorDevicePathCount(out count));var before=new Dictionary<string,string>();for(uint i=0;i<count;i++){string monitor,path;Marshal.ThrowExceptionForHR(service.GetMonitorDevicePathAt(i,out monitor));Marshal.ThrowExceptionForHR(service.GetWallpaper(monitor,out path));before[monitor]=path;}
                if(count==0)throw new Exception("Sin pantallas");string image=WallpaperLibrary.Load(directory)[1].File;changed=true;Wallpapers.Apply(directory,image);
                foreach(var monitor in before.Keys){string actual=Wallpapers.ReadCurrent(monitor);if(!String.Equals(actual,image,StringComparison.OrdinalIgnoreCase))throw new Exception("Windows no aplicó el fondo");}
                output.Add("PASS Windows confirmó la imagen elegida en "+count+" pantalla(s).");Wallpapers.Restore(directory);changed=false;
                foreach(var pair in before){string actual=Wallpapers.ReadCurrent(pair.Key);if(!String.Equals(actual,pair.Value,StringComparison.OrdinalIgnoreCase))throw new Exception("Fondo original no restaurado");}
                output.Add("PASS Fondo original restaurado y verificado. Sin modificar preferencias de usuario.");File.WriteAllLines(Path.Combine(directory,"wallpaper-integration.txt"),output);return 0;
            }
            catch(Exception ex){output.Add("FAIL "+ex);File.WriteAllLines(Path.Combine(directory,"wallpaper-integration.txt"),output);return 1;}
            finally{if(changed)try{Wallpapers.Restore(directory);}catch(Exception ex){File.AppendAllText(Path.Combine(directory,"wallpaper-integration.txt"),"\nFAIL Restauración: "+ex);}Marshal.ReleaseComObject(service);}
        }
    }
}

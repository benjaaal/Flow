using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace MacFlow
{
    public static class Brightness
    {
        public static int? Get()
        {
            try { using(var search = new ManagementObjectSearcher("root\\WMI", "SELECT CurrentBrightness FROM WmiMonitorBrightness")) using(var rows=search.Get()) { foreach(ManagementObject row in rows) using(row) return Convert.ToInt32(row["CurrentBrightness"]); } } catch { } return null;
        }
        public static void Set(int value)
        {
            using(var search=new ManagementObjectSearcher("root\\WMI","SELECT * FROM WmiMonitorBrightnessMethods")) using(var rows=search.Get()) foreach(ManagementObject row in rows) using(row) row.InvokeMethod("WmiSetBrightness",new object[]{1u,(byte)Math.Max(0,Math.Min(100,value))});
        }
    }
    [ComImport,Guid("C2CF3110-460E-4FC1-B9D0-8A1C0C9CC4BD")] internal class DesktopWallpaperObject { }
    [ComImport,Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] internal interface IDesktopWallpaper
    {
        [PreserveSig] int SetWallpaper([MarshalAs(UnmanagedType.LPWStr)]string monitor,[MarshalAs(UnmanagedType.LPWStr)]string wallpaper);
        [PreserveSig] int GetWallpaper([MarshalAs(UnmanagedType.LPWStr)]string monitor,[MarshalAs(UnmanagedType.LPWStr)]out string wallpaper);
        [PreserveSig] int GetMonitorDevicePathAt(uint index,[MarshalAs(UnmanagedType.LPWStr)]out string monitor);
        [PreserveSig] int GetMonitorDevicePathCount(out uint count);
        [PreserveSig] int GetMonitorRECT([MarshalAs(UnmanagedType.LPWStr)]string monitor,out Native.RECT rect);
        [PreserveSig] int SetBackgroundColor(uint color);
        [PreserveSig] int GetBackgroundColor(out uint color);
    }
    [DataContract] public sealed class WallpaperSnapshot
    {
        [DataMember] public List<string> Monitors=new List<string>();
        [DataMember] public List<string> Paths=new List<string>();
        [DataMember] public string Applied;
    }
    public static class Wallpapers
    {
        public static void Apply(string directory,string image)
        {
            var service=(IDesktopWallpaper)new DesktopWallpaperObject();
            try {Apply(directory,image,delegate{uint count;Marshal.ThrowExceptionForHR(service.GetMonitorDevicePathCount(out count));var monitors=new List<string>();for(uint i=0;i<count;i++){string monitor;Marshal.ThrowExceptionForHR(service.GetMonitorDevicePathAt(i,out monitor));monitors.Add(monitor);}return monitors;},ReadCurrent,delegate(string monitor,string path){Marshal.ThrowExceptionForHR(service.SetWallpaper(monitor,path));});}
            finally {Marshal.ReleaseComObject(service);}
        }
        internal static string ReadCurrent(string monitor)
        {var service=(IDesktopWallpaper)new DesktopWallpaperObject();try{string path;Marshal.ThrowExceptionForHR(service.GetWallpaper(monitor,out path));return path;}finally{Marshal.ReleaseComObject(service);}}
        internal static void Apply(string directory,string image,Func<List<string>> readMonitors,Func<string,string> get,Action<string,string> set)
        {
            if(!File.Exists(image))throw new IOException("No se encontró la imagen.");WallpaperLibrary.Decode(image,64);image=Path.GetFullPath(image);
            string file=Path.Combine(directory,"wallpaper.restore");byte[] previous=File.Exists(file)?File.ReadAllBytes(file):null;var monitors=readMonitors();if(monitors.Count==0)throw new IOException("Windows no informó pantallas disponibles.");
            var before=new Dictionary<string,string>();foreach(var monitor in monitors)before[monitor]=get(monitor);
            WallpaperSnapshot snapshot;
            if(previous!=null){using(var stream=new MemoryStream(previous))snapshot=(WallpaperSnapshot)new DataContractJsonSerializer(typeof(WallpaperSnapshot)).ReadObject(stream);}else {snapshot=new WallpaperSnapshot();foreach(var monitor in monitors){snapshot.Monitors.Add(monitor);snapshot.Paths.Add(before[monitor]);}}
            snapshot.Applied=image;Directory.CreateDirectory(directory);using(var stream=new FileStream(file,FileMode.Create,FileAccess.Write,FileShare.Read)){new DataContractJsonSerializer(typeof(WallpaperSnapshot)).WriteObject(stream,snapshot);stream.Flush(true);}
            try{set(null,image);foreach(var monitor in monitors){string current=get(monitor);for(int attempt=0;attempt<20&&!String.Equals(current,image,StringComparison.OrdinalIgnoreCase);attempt++){System.Threading.Thread.Sleep(75);current=get(monitor);}if(!String.Equals(current,image,StringComparison.OrdinalIgnoreCase))throw new IOException("Windows no confirmó el fondo. Esperado: "+Path.GetFileName(image)+"; recibido: "+Path.GetFileName(current)+".");}}
            catch
            {
                bool recovered=true;foreach(var monitor in monitors)try{if(String.Equals(get(monitor),image,StringComparison.OrdinalIgnoreCase))set(monitor,before[monitor]);}catch{recovered=false;}
                if(recovered){if(previous==null)File.Delete(file);else File.WriteAllBytes(file,previous);}throw;
            }
        }
        public static void Restore(string directory)
        {
            string file=Path.Combine(directory,"wallpaper.restore");if(!File.Exists(file))return;WallpaperSnapshot snapshot;using(var stream=File.OpenRead(file))snapshot=(WallpaperSnapshot)new DataContractJsonSerializer(typeof(WallpaperSnapshot)).ReadObject(stream);
            var service=(IDesktopWallpaper)new DesktopWallpaperObject();try{for(int i=0;i<snapshot.Monitors.Count;i++){string current;if(service.GetWallpaper(snapshot.Monitors[i],out current)!=0)continue;if(String.Equals(current,snapshot.Applied,StringComparison.OrdinalIgnoreCase))Marshal.ThrowExceptionForHR(service.SetWallpaper(snapshot.Monitors[i],snapshot.Paths[i]));}File.Delete(file);}finally{Marshal.ReleaseComObject(service);}
        }
    }
}

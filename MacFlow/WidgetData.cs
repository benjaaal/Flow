using System;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading;
using System.Threading.Tasks;
namespace MacFlow
{
    internal sealed class DeviceSnapshot
    {internal double? Cpu;internal ulong TotalRam,UsedRam,DiskTotal,DiskFree;internal Native.POWER Power;internal bool PowerKnown,Ready,Network;internal bool? Internet;internal string Wifi="Consultando Wi-Fi…";}
    internal static class DeviceMetrics
    {
        [StructLayout(LayoutKind.Sequential)]private struct Memory{public uint Length,Load;public ulong TotalPhys,AvailPhys,TotalPage,AvailPage,TotalVirtual,AvailVirtual,Extended;}
        [DllImport("kernel32.dll")]private static extern bool GlobalMemoryStatusEx(ref Memory value);
        [DllImport("kernel32.dll")]private static extern bool GetSystemTimes(out long idle,out long kernel,out long user);
        private static long previousIdle,previousTotal;private static readonly object Gate=new object();
        internal static DeviceSnapshot Read()
        {
            var result=new DeviceSnapshot{Ready=true};result.PowerKnown=Native.GetSystemPowerStatus(out result.Power);result.Network=NetworkInterface.GetIsNetworkAvailable();
            try{var radios=NetworkInterface.GetAllNetworkInterfaces();var wifi=radios.Where(n=>n.NetworkInterfaceType==NetworkInterfaceType.Wireless80211).ToList();result.Wifi=wifi.Count==0?"Sin adaptador Wi-Fi":wifi.Any(n=>n.OperationalStatus==OperationalStatus.Up)?"Wi-Fi conectado":"Wi-Fi sin conexión";}catch{result.Wifi="Wi-Fi no disponible";}
            object network=null;try{network=Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("DCB00C01-570F-4A9B-8D69-199FDBA5723B")));result.Internet=(bool)((dynamic)network).IsConnectedToInternet;}catch{}finally{if(network!=null&&Marshal.IsComObject(network))Marshal.ReleaseComObject(network);}
            var memory=new Memory{Length=(uint)Marshal.SizeOf(typeof(Memory))};if(GlobalMemoryStatusEx(ref memory)){result.TotalRam=memory.TotalPhys;result.UsedRam=memory.TotalPhys-memory.AvailPhys;}
            long idle,kernel,user;if(GetSystemTimes(out idle,out kernel,out user))lock(Gate){long total=kernel+user,elapsed=total-previousTotal;if(previousTotal>0&&elapsed>0)result.Cpu=Math.Max(0,Math.Min(100,100.0*(elapsed-(idle-previousIdle))/elapsed));previousTotal=total;previousIdle=idle;}
            try{var drive=new DriveInfo(Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.Windows)));if(drive.IsReady){result.DiskTotal=(ulong)drive.TotalSize;result.DiskFree=(ulong)drive.AvailableFreeSpace;}}catch{}return result;
        }
    }
    [DataContract]internal sealed class GeoResult{[DataMember]public GeoCity[] results;}
    [DataContract]internal sealed class GeoCity
    {[DataMember]public string name,country,admin1;[DataMember]public double latitude,longitude;public override string ToString(){return name+", "+(admin1??country);}}
    [DataContract]internal sealed class WeatherCurrent{[DataMember]public double temperature_2m;[DataMember]public int weather_code;}
    [DataContract]internal sealed class WeatherDaily{[DataMember]public string[] time;[DataMember]public double[] temperature_2m_max,temperature_2m_min;[DataMember]public int[] weather_code;}
    [DataContract]internal sealed class WeatherResult{[DataMember]public WeatherCurrent current;[DataMember]public WeatherDaily daily;internal DateTime FetchedUtc;}
    internal static class WidgetWeather
    {
        private static async Task<T> Read<T>(string url,CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();ServicePointManager.SecurityProtocol=SecurityProtocolType.Tls12;
            var request=(HttpWebRequest)WebRequest.Create(url);request.UserAgent="MacFlow/"+Product.VersionText;request.Timeout=request.ReadWriteTimeout=10000;
            using(var deadline=CancellationTokenSource.CreateLinkedTokenSource(cancellation))
            {deadline.CancelAfter(10000);
            using(deadline.Token.Register(request.Abort))using(var response=await request.GetResponseAsync())using(var source=response.GetResponseStream())using(var memory=new MemoryStream())
            {var bytes=new byte[4096];int n;while((n=await source.ReadAsync(bytes,0,bytes.Length,cancellation))>0){if(memory.Length+n>131072)throw new IOException("Respuesta demasiado grande");memory.Write(bytes,0,n);}memory.Position=0;return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(memory);}
            }
        }
        internal static async Task<GeoCity[]> Search(string city,CancellationToken cancellation)
        {if(String.IsNullOrWhiteSpace(city)||city.Trim().Length<2)return new GeoCity[0];var value=await Read<GeoResult>("https://geocoding-api.open-meteo.com/v1/search?count=5&language=es&name="+Uri.EscapeDataString(city.Trim()),cancellation);return value.results??new GeoCity[0];}
        internal static async Task<WeatherResult> Forecast(double lat,double lon,CancellationToken cancellation)
        {
            if(Double.IsNaN(lat)||Double.IsNaN(lon)||Math.Abs(lat)>90||Math.Abs(lon)>180)throw new IOException("Elegí una ciudad válida");var culture=System.Globalization.CultureInfo.InvariantCulture;
            var result=await Read<WeatherResult>("https://api.open-meteo.com/v1/forecast?latitude="+lat.ToString(culture)+"&longitude="+lon.ToString(culture)+"&current=temperature_2m,weather_code&daily=temperature_2m_max,temperature_2m_min,weather_code&forecast_days=3&timezone=auto",cancellation);
            if(result.current==null||result.daily==null)throw new IOException("Pronóstico no disponible");result.FetchedUtc=DateTime.UtcNow;return result;
        }
        internal static string Description(int code){return code==0?"Despejado":code<=3?"Nubosidad":code<=48?"Niebla":code<=67?"Lluvia":code<=77?"Nieve":code<=82?"Chubascos":code<=86?"Nieve":"Tormenta";}
    }
}

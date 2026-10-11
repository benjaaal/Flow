using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
namespace MacFlow
{
    internal sealed class ConnectionItem
    {public string Name,Detail,Profile;public Guid Interface;public bool Connected,Secure,Connectable;public uint Signal,Bss;}
    internal sealed class ConnectionSnapshot
    {public readonly List<ConnectionItem> Items=new List<ConnectionItem>();public string Message="";}
    internal static class Connectivity
    {
        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]internal struct WifiInterface
        {public Guid Guid;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=256)]public string Description;public uint State;}
        [StructLayout(LayoutKind.Sequential)]internal struct Ssid
        {public uint Length;[MarshalAs(UnmanagedType.ByValArray,SizeConst=32)]public byte[] Bytes;}
        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]internal struct WifiNetwork
        {
            [MarshalAs(UnmanagedType.ByValTStr,SizeConst=256)]public string Profile;
            public Ssid Ssid;public uint Bss,Count;public int Connectable;public uint Reason,PhyCount;
            [MarshalAs(UnmanagedType.ByValArray,SizeConst=8)]public uint[] Phys;
            public int MorePhy;public uint Signal;public int Secure;public uint Auth,Cipher,Flags,Reserved;
        }
        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]private struct WifiConnection
        {public uint Mode;[MarshalAs(UnmanagedType.LPWStr)]public string Profile;public IntPtr Ssid,Bssids;public uint Bss,Flags;}
        [DllImport("wlanapi.dll")]private static extern uint WlanOpenHandle(uint version,IntPtr reserved,out uint negotiated,out IntPtr client);
        [DllImport("wlanapi.dll")]private static extern uint WlanCloseHandle(IntPtr client,IntPtr reserved);
        [DllImport("wlanapi.dll")]private static extern uint WlanEnumInterfaces(IntPtr client,IntPtr reserved,out IntPtr list);
        [DllImport("wlanapi.dll")]private static extern uint WlanGetAvailableNetworkList(IntPtr client,ref Guid id,uint flags,IntPtr reserved,out IntPtr list);
        [DllImport("wlanapi.dll")]private static extern uint WlanConnect(IntPtr client,ref Guid id,ref WifiConnection parameters,IntPtr reserved);
        [DllImport("wlanapi.dll")]private static extern void WlanFreeMemory(IntPtr memory);
        [StructLayout(LayoutKind.Sequential)]internal struct BluetoothSearch
        {public uint Size;public int Authenticated,Remembered,Unknown,Connected,Inquiry;public byte Timeout;public IntPtr Radio;}
        [StructLayout(LayoutKind.Sequential)]internal struct SystemTime
        {public ushort Year,Month,DayOfWeek,Day,Hour,Minute,Second,Milliseconds;}
        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]internal struct BluetoothDevice
        {
            public uint Size;public ulong Address;public uint Class;public int Connected,Remembered,Authenticated;
            public SystemTime LastSeen,LastUsed;
            [MarshalAs(UnmanagedType.ByValTStr,SizeConst=248)]public string Name;
        }
        [DllImport("bthprops.cpl",SetLastError=true)]private static extern IntPtr BluetoothFindFirstDevice(ref BluetoothSearch search,ref BluetoothDevice info);
        [DllImport("bthprops.cpl",SetLastError=true)]private static extern bool BluetoothFindNextDevice(IntPtr find,ref BluetoothDevice info);
        [DllImport("bthprops.cpl")]private static extern bool BluetoothFindDeviceClose(IntPtr find);
        internal static string DecodeSsid(Ssid ssid)
        {if(ssid.Bytes==null||ssid.Length>32||ssid.Length>ssid.Bytes.Length)return "";try{return new UTF8Encoding(false,true).GetString(ssid.Bytes,0,(int)ssid.Length).Replace("\0","");}catch(DecoderFallbackException){return Encoding.GetEncoding(1252).GetString(ssid.Bytes,0,(int)ssid.Length).Replace("\0","");}}
        private static IntPtr OpenWifi()
        {uint version;IntPtr handle;uint error=WlanOpenHandle(2,IntPtr.Zero,out version,out handle);if(error!=0)throw new Win32Exception((int)error);return handle;}
        internal static ConnectionSnapshot ReadWifi()
        {
            var snapshot=new ConnectionSnapshot();IntPtr client=IntPtr.Zero,list=IntPtr.Zero;
            try
            {
                client=OpenWifi();uint error=WlanEnumInterfaces(client,IntPtr.Zero,out list);if(error!=0)throw new Win32Exception((int)error);
                int count=Marshal.ReadInt32(list);if(count<0||count>64)throw new InvalidOperationException("Lista de adaptadores inválida");
                for(int i=0;i<count;i++)
                {
                    var iface=(WifiInterface)Marshal.PtrToStructure(IntPtr.Add(list,8+i*Marshal.SizeOf(typeof(WifiInterface))),typeof(WifiInterface));IntPtr networks=IntPtr.Zero;
                    try
                    {
                        var id=iface.Guid;error=WlanGetAvailableNetworkList(client,ref id,0,IntPtr.Zero,out networks);
                        if(error!=0){snapshot.Message=error==5?"Windows no permitió leer las redes. Podés administrarlas en su configuración.":"El adaptador no ofrece redes en este momento.";continue;}
                        int length=Marshal.ReadInt32(networks);if(length<0||length>2048)throw new InvalidOperationException("Lista de redes inválida");
                        for(int j=0;j<length;j++)
                        {
                            var net=(WifiNetwork)Marshal.PtrToStructure(IntPtr.Add(networks,8+j*Marshal.SizeOf(typeof(WifiNetwork))),typeof(WifiNetwork));var name=DecodeSsid(net.Ssid);if(String.IsNullOrWhiteSpace(name))continue;
                            snapshot.Items.Add(new ConnectionItem{Name=name,Profile=net.Profile,Interface=id,Bss=net.Bss,Signal=Math.Min(100,net.Signal),Secure=net.Secure!=0,Connected=(net.Flags&1)!=0,Connectable=net.Connectable!=0});
                        }
                    }
                    finally{if(networks!=IntPtr.Zero)WlanFreeMemory(networks);}
                }
                var unique=snapshot.Items.GroupBy(n=>n.Interface+"|"+n.Name+"|"+n.Secure).Select(g=>g.OrderByDescending(n=>n.Connected).ThenByDescending(n=>!String.IsNullOrEmpty(n.Profile)).ThenByDescending(n=>n.Signal).First()).OrderByDescending(n=>n.Connected).ThenByDescending(n=>n.Signal).ToList();snapshot.Items.Clear();snapshot.Items.AddRange(unique);
                if(count==0)snapshot.Message="No se encontró un adaptador Wi-Fi.";else if(snapshot.Items.Count==0&&snapshot.Message=="")snapshot.Message="No hay redes disponibles. Revisá si Wi-Fi está encendido.";
            }
            catch(Exception ex){snapshot.Message=ex is Win32Exception?"Wi-Fi no está disponible o Windows no permitió consultarlo.":"No se pudieron consultar las redes.";}
            finally{if(list!=IntPtr.Zero)WlanFreeMemory(list);if(client!=IntPtr.Zero)WlanCloseHandle(client,IntPtr.Zero);}
            return snapshot;
        }
        internal static void ConnectSaved(ConnectionItem item)
        {
            if(item==null||item.Interface==Guid.Empty||String.IsNullOrWhiteSpace(item.Profile)||!item.Connectable)throw new InvalidOperationException("Esta red necesita configurarse en Windows.");
            var handle=OpenWifi();try{var id=item.Interface;var request=new WifiConnection{Mode=0,Profile=item.Profile,Bss=item.Bss};uint error=WlanConnect(handle,ref id,ref request,IntPtr.Zero);if(error!=0)throw new Win32Exception((int)error);}finally{WlanCloseHandle(handle,IntPtr.Zero);}
        }
        internal static ConnectionSnapshot ReadBluetooth()
        {
            var snapshot=new ConnectionSnapshot();IntPtr find=IntPtr.Zero;
            try
            {
                var search=new BluetoothSearch{Size=(uint)Marshal.SizeOf(typeof(BluetoothSearch)),Authenticated=1,Remembered=1,Connected=1,Unknown=0,Inquiry=0};var device=new BluetoothDevice{Size=(uint)Marshal.SizeOf(typeof(BluetoothDevice))};find=BluetoothFindFirstDevice(ref search,ref device);
                int seen=0;if(find!=IntPtr.Zero)do{seen++;if(!String.IsNullOrWhiteSpace(device.Name))snapshot.Items.Add(new ConnectionItem{Name=device.Name,Connected=device.Connected!=0,Detail=device.Connected!=0?"Conectado":"Guardado"});device.Size=(uint)Marshal.SizeOf(typeof(BluetoothDevice));}while(seen<200&&BluetoothFindNextDevice(find,ref device));
            }
            catch{snapshot.Message="No se pudo consultar el adaptador Bluetooth.";}
            finally{if(find!=IntPtr.Zero)BluetoothFindDeviceClose(find);}
            try
            {
                // Windows inventory also exposes registered low-energy devices.
                // Inventory status does not imply an active Bluetooth connection.
                using(var query=new ManagementObjectSearcher("SELECT Name,PNPDeviceID FROM Win32_PnPEntity WHERE PNPClass='Bluetooth'"))using(var devices=query.Get())foreach(ManagementObject device in devices)
                {using(device){var id=Convert.ToString(device["PNPDeviceID"]);var name=Convert.ToString(device["Name"]);if(id.StartsWith("BTHLEDEVICE\\",StringComparison.OrdinalIgnoreCase)&&!String.IsNullOrWhiteSpace(name)&&!snapshot.Items.Any(n=>String.Equals(n.Name,name,StringComparison.OrdinalIgnoreCase)))snapshot.Items.Add(new ConnectionItem{Name=name,Detail="Registrado en Windows"});}}
            }
            catch{}
            if(snapshot.Items.Count==0&&snapshot.Message=="")snapshot.Message="No hay dispositivos guardados disponibles. Podés agregar uno desde Windows.";
            snapshot.Items.Sort((a,b)=>a.Connected==b.Connected?String.Compare(a.Name,b.Name,StringComparison.CurrentCultureIgnoreCase):a.Connected?-1:1);return snapshot;
        }
    }
}

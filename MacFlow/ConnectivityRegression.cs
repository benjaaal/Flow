using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace MacFlow
{
    internal static class ConnectivityRegression
    {
        internal static System.Threading.Tasks.Task<RadioSnapshot> RadioFixture(bool wifi){return System.Threading.Tasks.Task.FromResult(new RadioSnapshot{Available=true,On=true,CanChange=true});}
        internal static ConnectionSnapshot Sample(bool wifi)
        {
            var result=new ConnectionSnapshot();
            if(wifi){result.Items.Add(new ConnectionItem{Name="Red de prueba · Casa",Connected=true,Secure=true,Signal=94});result.Items.Add(new ConnectionItem{Name="Red de prueba · Estudio",Secure=true,Signal=78,Connectable=true,Profile="test-profile",Interface=Guid.NewGuid()});result.Items.Add(new ConnectionItem{Name="Red de prueba · Invitados",Signal=59,Connectable=true});}
            else{result.Items.Add(new ConnectionItem{Name="Auriculares de prueba",Connected=true,Detail="Conectado"});result.Items.Add(new ConnectionItem{Name="Teclado de prueba",Detail="Guardado"});result.Items.Add(new ConnectionItem{Name="Mouse de prueba",Detail="Registrado en Windows"});}return result;
        }
        internal static void Run(string directory,List<string> results)
        {
            Check(Marshal.SizeOf(typeof(Connectivity.WifiInterface))==532&&Marshal.SizeOf(typeof(Connectivity.Ssid))==36&&Marshal.SizeOf(typeof(Connectivity.WifiNetwork))==628,"Wi-Fi: estructuras nativas x64 y campos correctos",results);
            Check(Marshal.SizeOf(typeof(Connectivity.BluetoothSearch))==40&&Marshal.SizeOf(typeof(Connectivity.BluetoothDevice))==560&&Marshal.OffsetOf(typeof(Connectivity.BluetoothDevice),"Name").ToInt32()==64,"Bluetooth: alineación nativa x64 y tamaño de nombre",results);
            var bytes=Encoding.UTF8.GetBytes("Café 雪");Check(Connectivity.DecodeSsid(new Connectivity.Ssid{Length=(uint)bytes.Length,Bytes=bytes})=="Café 雪"&&Connectivity.DecodeSsid(new Connectivity.Ssid{Length=33,Bytes=new byte[32]})=="","Wi-Fi: SSID Unicode y rechazo de longitud inválida",results);
            Check(Connectivity.DecodeSsid(new Connectivity.Ssid{Length=1,Bytes=new byte[]{0xE9}})=="é","Wi-Fi: nombres heredados sin fallo de decodificación",results);
            bool rejected=false;try{Connectivity.ConnectSaved(new ConnectionItem{Name="Unknown"});}catch(InvalidOperationException){rejected=true;}Check(rejected,"Wi-Fi: no intenta conectar sin perfil válido",results);
            Design.InstallTheme();int wifiSettings=0,btSettings=0,connectCalls=0;
            var panel=new ConnectivityPanel(true,Sample,mode=>{if(mode)wifiSettings++;else btSettings++;},item=>{connectCalls++;});
            panel.ShowActivated=false;panel.ShowInTaskbar=false;panel.Left=-2000;panel.Top=-2000;panel.Show();Wait(()=>panel.Ready);
            Check(panel.ItemCount==3&&panel.FirstName=="Red de prueba · Casa","Conexiones: lista funcional con proveedor aislado",results);
            panel.Choose(new ConnectionItem{Name="Nueva",Connectable=true});Check(wifiSettings==1&&connectCalls==0,"Wi-Fi: una red nueva dirige a Windows sin inventar un perfil",results);
            panel.Choose(new ConnectionItem{Name="Guardada",Profile="test-profile",Connectable=true,Interface=Guid.NewGuid()});Wait(()=>panel.Ready);
            Check(connectCalls==1&&panel.ItemCount==3,"Wi-Fi: conectar una red guardada actualiza el estado consultado",results);
            panel.SwitchMode(false);Wait(()=>panel.Ready);panel.Choose(new ConnectionItem{Name="Teclado"});Check(panel.FirstName=="Auriculares de prueba"&&btSettings==1&&wifiSettings==1,"Bluetooth: pestaña y gestión sin mezclar redes Wi-Fi",results);panel.Close();
            using(var release=new ManualResetEvent(false))
            {
                var late=new ConnectivityPanel(true,mode=>{if(mode)release.WaitOne(3000);return Sample(mode);},mode=>{},item=>{});
                late.ShowActivated=false;late.ShowInTaskbar=false;late.Left=-2000;late.Top=-2000;late.Show();late.SwitchMode(false);release.Set();Wait(()=>late.Ready);
                Check(!late.WifiMode&&late.FirstName=="Auriculares de prueba","Conexiones: descarta la respuesta Wi-Fi al cambiar a Bluetooth",results);late.Close();
            }
            using(var release=new ManualResetEvent(false))
            {
                var late=new ConnectivityPanel(true,mode=>{release.WaitOne(3000);return Sample(mode);},mode=>{},item=>{});late.ShowActivated=false;late.ShowInTaskbar=false;late.Left=-2000;late.Top=-2000;late.Show();late.Close();release.Set();Pump(200);
                Check(!late.IsVisible&&!late.Ready&&late.ItemCount==0,"Conexiones: cierre durante la carga evita actualizar una ventana cerrada",results);
            }
            var empty=new ConnectivityPanel(true,mode=>new ConnectionSnapshot{Message="Permiso no disponible"},mode=>{},item=>{});empty.ShowActivated=false;empty.ShowInTaskbar=false;empty.Left=-2000;empty.Top=-2000;empty.Show();Wait(()=>empty.Ready);Check(empty.ItemCount==0,"Conexiones: sin adaptador o permiso conserva un panel utilizable",results);empty.Close();
            bool radioOn=true;int radioChanges=0;bool deny=false;
            var radioPanel=new ConnectivityPanel(true,Sample,mode=>{},item=>{},mode=>System.Threading.Tasks.Task.FromResult(new RadioSnapshot{Available=true,On=radioOn,CanChange=true}),(mode,on,cancel)=>{radioChanges++;if(!deny)radioOn=on;return System.Threading.Tasks.Task.FromResult(new RadioChange{Success=!deny,Message=deny?"Cambio rechazado":"Cambio confirmado"});});
            radioPanel.ShowActivated=false;radioPanel.ShowInTaskbar=false;radioPanel.Left=-2000;radioPanel.Top=-2000;radioPanel.Show();Wait(()=>radioPanel.Ready);
            Check(radioPanel.RadioOn&&radioPanel.RadioEnabled&&radioChanges==0,"Radios: consultar y actualizar el interruptor no cambia el hardware",results);
            radioPanel.ChangeRadio(false);Wait(()=>radioPanel.Ready);Check(!radioPanel.RadioOn&&radioPanel.ItemCount==0&&radioChanges==1,"Radios: apagar Wi-Fi confirma el estado y vacía redes disponibles",results);
            deny=true;radioPanel.ChangeRadio(true);Wait(()=>radioPanel.Ready);Check(!radioPanel.RadioOn&&radioPanel.ItemCount==0,"Radios: un cambio rechazado no se presenta como encendido",results);radioPanel.Close();
            bool changedAfterClose=false;var cancelled=new ConnectivityPanel(true,Sample,mode=>{},item=>{},mode=>System.Threading.Tasks.Task.FromResult(new RadioSnapshot{Available=true,On=true,CanChange=true}),async (mode,on,cancel)=>{try{await System.Threading.Tasks.Task.Delay(500,cancel);changedAfterClose=true;}catch(OperationCanceledException){}return new RadioChange();});cancelled.ShowActivated=false;cancelled.ShowInTaskbar=false;cancelled.Left=-2000;cancelled.Top=-2000;cancelled.Show();Wait(()=>cancelled.Ready);cancelled.ChangeRadio(false);cancelled.Close();Pump(650);Check(!changedAfterClose,"Radios: cerrar el panel cancela una solicitud pendiente",results);
        }
        private static void Check(bool condition,string label,List<string> results){if(!condition)throw new Exception(label);results.Add("PASS "+label);}
        private static void Wait(Func<bool> ready){var end=DateTime.UtcNow.AddSeconds(6);while(!ready()&&DateTime.UtcNow<end)Pump(25);if(!ready())throw new Exception("La consulta aislada no terminó");}
        private static void Pump(int ms){var frame=new DispatcherFrame();var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(ms)};timer.Tick+=delegate{timer.Stop();frame.Continue=false;};timer.Start();Dispatcher.PushFrame(frame);}
    }
}

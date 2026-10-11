using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Radios;
using Windows.Foundation;
namespace MacFlow
{
    internal sealed class RadioSnapshot
    {public bool Available,On,CanChange;public string Message="";}
    internal sealed class RadioChange
    {public bool Success;public string Message="";}
    internal static class RadioControls
    {
        private static RadioAccessStatus? access;
        private static bool changing;
        private static async Task<T> Result<T>(IAsyncOperation<T> operation,CancellationToken cancellation,int seconds)
        {
            var until=DateTime.UtcNow.AddSeconds(seconds);
            try
            {
                while(operation.Status==AsyncStatus.Started){cancellation.ThrowIfCancellationRequested();if(DateTime.UtcNow>=until)throw new TimeoutException();await Task.Delay(40,cancellation);}
                cancellation.ThrowIfCancellationRequested();return operation.GetResults();
            }
            finally{try{if(operation.Status==AsyncStatus.Started)operation.Cancel();operation.Close();}catch{}}
        }
        internal static async Task<RadioSnapshot> ReadAsync(bool wifi)
        {
            try
            {
                var radios=await Result(Radio.GetRadiosAsync(),CancellationToken.None,10);var kind=wifi?RadioKind.WiFi:RadioKind.Bluetooth;var selected=radios.Where(r=>r.Kind==kind).ToList();
                if(selected.Count==0)return new RadioSnapshot{Message="No se encontró un radio "+(wifi?"Wi-Fi":"Bluetooth")+"."};
                return new RadioSnapshot{Available=true,On=selected.Any(r=>r.State==RadioState.On),CanChange=selected.All(r=>r.State==RadioState.On||r.State==RadioState.Off),Message=selected.Any(r=>r.State==RadioState.Disabled)?"Bloqueado por el hardware o Windows.":""};
            }
            catch{return new RadioSnapshot{Message="Windows no permite consultar este radio."};}
        }
        internal static async Task<RadioChange> SetAsync(bool wifi,bool on,CancellationToken cancellation)
        {
            if(changing)return new RadioChange{Message="Hay otro cambio en curso."};changing=true;
            try
            {
                cancellation.ThrowIfCancellationRequested();var radios=await Result(Radio.GetRadiosAsync(),cancellation,10);var kind=wifi?RadioKind.WiFi:RadioKind.Bluetooth;var selected=radios.Where(r=>r.Kind==kind).ToList();
                if(selected.Count==0)return new RadioChange{Message="Este equipo no ofrece ese radio."};
                if(selected.Any(r=>r.State==RadioState.Disabled))return new RadioChange{Message="El radio está bloqueado por el hardware o Windows."};
                // Called only by the user's switch on the UI thread. Never request
                // consent while enumerating or during startup/tests.
                if(!access.HasValue)access=await Result(Radio.RequestAccessAsync(),cancellation,120);
                if(access.Value!=RadioAccessStatus.Allowed)return new RadioChange{Message="Windows no autorizó el cambio. Podés administrarlo en Configuración."};
                var desired=on?RadioState.On:RadioState.Off;
                foreach(var radio in selected){cancellation.ThrowIfCancellationRequested();if(radio.State==desired)continue;var response=await Result(radio.SetStateAsync(desired),cancellation,10);if(response!=RadioAccessStatus.Allowed){access=null;return new RadioChange{Message="Windows rechazó el cambio del radio."};}}
                for(int i=0;i<20&&!selected.All(r=>r.State==desired);i++)await Task.Delay(100,cancellation);
                return selected.All(r=>r.State==desired)?new RadioChange{Success=true,Message=(wifi?"Wi-Fi":"Bluetooth")+(on?" encendido.":" apagado.")}:new RadioChange{Message="El radio no cambió de estado. Revisá el equipo y Configuración de Windows."};
            }
            catch(OperationCanceledException){return new RadioChange{Message="Cambio cancelado."};}
            catch{return new RadioChange{Message="No se pudo cambiar el radio. Abrí Configuración de Windows."};}
            finally{changing=false;}
        }
    }
}

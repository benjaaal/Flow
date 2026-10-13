using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
namespace MacFlow
{
    internal interface IControlBackend : IDisposable
    {
        AudioState Sound();void Volume(double value);void Mute(bool value);
        Task<int?> ReadBrightness();Task SetBrightness(int value);
        Task<RadioSnapshot> Radio(bool wifi);Task<RadioChange> SwitchRadio(bool wifi,bool on,CancellationToken token);
        Native.POWER Power();void Settings(string uri);
    }
    internal sealed class AudioState{internal bool Available,Muted;internal double Volume;}
    internal sealed class ControlBackend : IControlBackend
    {
        private Audio audio;
        public AudioState Sound(){try{if(audio!=null)audio.Dispose();audio=new Audio();return new AudioState{Available=audio.Available,Muted=audio.Muted,Volume=audio.Volume*100};}catch{return new AudioState();}}
        public void Volume(double value){if(audio==null)audio=new Audio();audio.Volume=(float)(value/100);}
        public void Mute(bool value){if(audio==null)audio=new Audio();audio.Muted=value;}
        public Task<int?> ReadBrightness(){return Task.Run((Func<int?>)Brightness.Get);}
        public Task SetBrightness(int value){return Task.Run(delegate{Brightness.Set(value);});}
        public Task<RadioSnapshot> Radio(bool wifi){return RadioControls.ReadAsync(wifi);}
        public Task<RadioChange> SwitchRadio(bool wifi,bool on,CancellationToken token){return RadioControls.SetAsync(wifi,on,token);}
        public Native.POWER Power(){Native.POWER value;Native.GetSystemPowerStatus(out value);return value;}
        public void Settings(string uri){Process.Start(new ProcessStartInfo(uri){UseShellExecute=true});}
        public void Dispose(){if(audio!=null)audio.Dispose();audio=null;}
    }
    internal sealed class ControlFixture : IControlBackend
    {
        internal readonly AudioState State=new AudioState{Available=true,Volume=45};internal int VolumeWrites;internal int Bright=60;internal bool Wifi=true,Bluetooth=true;
        public AudioState Sound(){return State;}public void Volume(double value){VolumeWrites++;State.Volume=value;}public void Mute(bool value){State.Muted=value;}
        public Task<int?> ReadBrightness(){return Task.FromResult((int?)Bright);}public Task SetBrightness(int value){Bright=value;return Task.FromResult(0);}
        public Task<RadioSnapshot> Radio(bool wifi){return Task.FromResult(new RadioSnapshot{Available=true,On=wifi?Wifi:Bluetooth,CanChange=true});}
        public Task<RadioChange> SwitchRadio(bool wifi,bool on,CancellationToken token){token.ThrowIfCancellationRequested();if(wifi)Wifi=on;else Bluetooth=on;return Task.FromResult(new RadioChange{Success=true});}
        public Native.POWER Power(){return new Native.POWER{BatteryLifePercent=74,ACLineStatus=1};}public void Settings(string uri){}public void Dispose(){}
    }
}

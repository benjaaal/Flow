using System;
using System.Windows.Threading;
namespace MacFlow
{
    internal static class WakePhrase
    {
        internal static bool Accept(string text,float confidence){string value=LocalCommands.Normalize(text);return confidence>=.78f&&(value=="hey siri"||value=="oye siri");}
    }
    internal sealed class VoiceWake : IDisposable
    {
        private readonly OfflineVoice voice;private readonly Action activate;private readonly Action<bool,string> status;private bool wanted,disposed,listening;private DateTime lastActivation,nextRetry;
        internal VoiceWake(Dispatcher dispatcher,Action activate,Action<bool,string> status,OfflineVoice service){voice=service;this.activate=activate;this.status=status;voice.Wake+=Detected;voice.Status+=Changed;}
        internal void Update(bool enabled){if(disposed||wanted==enabled&&(!enabled||listening||DateTime.UtcNow<nextRetry))return;wanted=enabled;nextRetry=DateTime.UtcNow.AddSeconds(10);voice.SetWake(enabled);}
        private void Changed(bool active,string text){listening=active;if(!disposed)status(wanted&&active,text);}
        private void Detected(){if(disposed||!wanted||DateTime.UtcNow-lastActivation<TimeSpan.FromSeconds(3))return;lastActivation=DateTime.UtcNow;Update(false);activate();}
        public void Dispose(){if(disposed)return;disposed=true;wanted=false;voice.Wake-=Detected;voice.Status-=Changed;voice.SetWake(false);status(false,"Micrófono apagado");}
    }
}

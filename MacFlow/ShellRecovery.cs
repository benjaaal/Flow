using System;
namespace MacFlow
{
    // Logon can start MacFlow before Explorer has created its taskbar. Keep the
    // user's choice and retry instead of permanently disabling replacement.
    internal sealed class ShellRecovery
    {
        private DateTime nextAttempt=DateTime.MinValue;
        internal bool Acquired {get;private set;}
        internal bool Attempt(bool requested,bool replacementReady,bool shellReady,DateTime now,Action acquire,Action restore)
        {
            if(!requested){if(Acquired)restore();Acquired=false;return false;}
            if(Acquired)return true;
            if(!replacementReady||!shellReady||now<nextAttempt)return false;
            nextAttempt=now.AddSeconds(1);
            try{acquire();Acquired=true;return true;}
            catch(Exception ex){Acquired=false;restore();Log.Write("Inicio: se conserva Windows y se reintentará MacFlow. "+ex.Message);return false;}
        }
        internal void Reset(Action restore){restore();Acquired=false;nextAttempt=DateTime.MinValue;}
    }
}

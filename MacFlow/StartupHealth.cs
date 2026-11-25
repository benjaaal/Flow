using System;
using System.IO;
using System.Runtime.Serialization;
namespace MacFlow
{
    [DataContract] internal sealed class StartupHealthState{[DataMember]public bool Pending;[DataMember]public int ConsecutiveFailures;[DataMember]public string LastStartUtc;}
    internal sealed class StartupHealth
    {
        private readonly string file;private readonly StartupHealthState state;
        internal StartupHealth(string directory){file=Path.Combine(directory,"startup-health.json");state=ModuleStore.Read<StartupHealthState>(file);}
        internal bool Begin(){if(state.Pending)state.ConsecutiveFailures++;if(state.ConsecutiveFailures>=3){state.Pending=false;ModuleStore.Write(file,state);return false;}state.Pending=true;state.LastStartUtc=DateTime.UtcNow.ToString("o");ModuleStore.Write(file,state);return true;}
        internal void Reset(){state.Pending=false;state.ConsecutiveFailures=0;ModuleStore.Write(file,state);}
        internal void End(bool success){state.Pending=!success;if(success)state.ConsecutiveFailures=0;ModuleStore.Write(file,state);}
    }
}

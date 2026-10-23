using System;
using System.Threading.Tasks;
using System.Windows.Threading;
using Windows.Devices.Sensors;
using Windows.Foundation;
namespace MacFlow
{
    internal sealed class HingeTracker : IDisposable
    {
        private readonly Dispatcher dispatcher;private readonly Action<double> changed;
        private HingeAngleSensor sensor;private bool disposed,queued;private double latest;private readonly object gate=new object();
        private bool subscribed;
        internal bool Available{get{return sensor!=null&&subscribed&&!disposed;}}
        internal string Status{get;private set;}
        internal HingeTracker(Dispatcher target,Action<double> callback){dispatcher=target;changed=callback;Status="Comprobando sensor de ángulo…";Initialize();}
        private async void Initialize()
        {
            IAsyncOperation<HingeAngleSensor> operation=null;
            try
            {
                operation=HingeAngleSensor.GetDefaultAsync();var deadline=DateTime.UtcNow.AddSeconds(3);
                while(!disposed&&operation.Status==AsyncStatus.Started&&DateTime.UtcNow<deadline)await Task.Delay(40);
                if(disposed)return;if(operation.Status!=AsyncStatus.Completed){Status="Sin sensor de ángulo disponible; sólo avisos de apertura/cierre.";return;}
                sensor=operation.GetResults();if(sensor==null){Status="Windows no ofrece un sensor de ángulo en este equipo.";return;}
                sensor.ReportThresholdInDegrees=Math.Max(sensor.MinReportThresholdInDegrees,1.0);sensor.ReadingChanged+=Reading;subscribed=true;Status="Sensor de ángulo conectado; sigue el movimiento de la tapa.";
            }
            catch{Status="Windows no permite leer un sensor de ángulo; se usan avisos de tapa.";}
            finally{if(operation!=null)try{if(operation.Status==AsyncStatus.Started)operation.Cancel();operation.Close();}catch{}}
        }
        private void Reading(HingeAngleSensor sender,HingeAngleSensorReadingChangedEventArgs args)
        {
            if(disposed)return;lock(gate){latest=args.Reading.AngleInDegrees;if(queued)return;queued=true;}
            dispatcher.BeginInvoke(DispatcherPriority.Render,new Action(delegate{double angle;lock(gate){queued=false;angle=latest;}if(!disposed)changed(angle);}));
        }
        public void Dispose(){if(disposed)return;disposed=true;if(sensor!=null){if(subscribed)sensor.ReadingChanged-=Reading;sensor=null;}}
    }
}

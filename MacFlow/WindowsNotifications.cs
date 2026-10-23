using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Threading;
using Windows.Foundation;
using Windows.UI.Notifications;
using Windows.UI.Notifications.Management;
namespace MacFlow
{
    // Official, consent-based listener. No notification database reads, toast
    // interception, hooks, permission bypass or automatic dismissals in Windows.
    internal sealed class WindowsNotifications : IDisposable
    {
        [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]private static extern int GetCurrentPackageFullName(ref uint length,System.Text.StringBuilder name);
        internal static bool HasIdentity{get{uint size=0;int result=GetCurrentPackageFullName(ref size,null);return result==122&&size>0;}}
        private readonly Dispatcher dispatcher;private readonly ActivityLog history;private readonly Action<string,string> show;
        private UserNotificationListener listener;private bool disposed,reading,pending,enabled,primed;
        private HashSet<uint> known=new HashSet<uint>();
        internal string Status{get;private set;}
        internal WindowsNotifications(Dispatcher dispatcher,ActivityLog history,Action<string,string> show){this.dispatcher=dispatcher;this.history=history;this.show=show;Status=HasIdentity?"Necesita permiso de Windows.":"El EXE clásico no tiene identidad de paquete. Requiere una distribución MSIX firmada con la capacidad userNotificationListener.";}
        internal async Task<string> RequestAccess()
        {
            if(disposed)return "El lector está cerrado.";if(!HasIdentity)return Status;
            try{var access=await Result(UserNotificationListener.Current.RequestAccessAsync(),120);Status=access==UserNotificationListenerAccessStatus.Allowed?"Acceso permitido por Windows.":"Windows no autorizó leer las notificaciones.";if(access==UserNotificationListenerAccessStatus.Allowed)SetEnabled(true);return Status;}
            catch(Exception ex){Status="No se pudo habilitar el acceso: "+ex.Message;return Status;}
        }
        internal void SetEnabled(bool value)
        {
            if(disposed)return;enabled=value;
            if(!value){Detach();history.SetExternal(new List<ActivityItem>());return;}if(!HasIdentity)return;
            try{var source=UserNotificationListener.Current;if(source.GetAccessStatus()!=UserNotificationListenerAccessStatus.Allowed){Status="Falta permiso de Windows. Elegí Autorizar acceso.";return;}if(listener==null){listener=source;listener.NotificationChanged+=Changed;}Read();}catch(Exception ex){Status="Lector no disponible: "+ex.Message;Detach();}
        }
        private void Changed(UserNotificationListener sender,UserNotificationChangedEventArgs args){if(disposed)return;dispatcher.BeginInvoke(new Action(Read));}
        internal async void Read()
        {
            if(disposed||!enabled||listener==null)return;if(reading){pending=true;return;}reading=true;
            try
            {
                var source=listener;if(source.GetAccessStatus()!=UserNotificationListenerAccessStatus.Allowed){Status="Acceso revocado en Windows.";Detach();history.SetExternal(new List<ActivityItem>());return;}
                var values=await Result(source.GetNotificationsAsync(NotificationKinds.Toast),10);if(disposed||!enabled)return;
                var next=new HashSet<uint>();var records=new List<ActivityItem>();
                foreach(var notice in values.OrderByDescending(n=>n.CreationTime).Take(40))
                {
                    next.Add(notice.Id);try{var binding=notice.Notification.Visual.GetBinding(KnownNotificationBindings.ToastGeneric);if(binding==null)continue;var lines=binding.GetTextElements().Select(t=>t.Text??"").Where(t=>!String.IsNullOrWhiteSpace(t)).ToArray();if(lines.Length==0)continue;string app=notice.AppInfo.DisplayInfo.DisplayName,title=Limit(app+" · "+lines[0],180),detail=Limit(String.Join("\n",lines.Skip(1)),1200);records.Add(new ActivityItem{Title=title,Detail=detail,CreatedUtc=notice.CreationTime.UtcDateTime,External=true,ExternalKey=notice.Id.ToString()});if(primed&&!known.Contains(notice.Id)&&DateTime.UtcNow-notice.CreationTime.UtcDateTime<TimeSpan.FromSeconds(30)&&show!=null)show(title,detail);}catch(Exception ex){Log.Write("Aviso omitido: "+ex.GetType().Name);}
                }
                known=next;primed=true;history.SetExternal(records);Status="Avisos de Windows conectados · "+records.Count;
            }
            catch(Exception ex){Status="No se pudieron consultar avisos: "+ex.Message;}
            finally{reading=false;if(pending&&!disposed){pending=false;Read();}}
        }
        private async Task<T> Result<T>(IAsyncOperation<T> operation,int seconds)
        {var until=DateTime.UtcNow.AddSeconds(seconds);try{while(operation.Status==AsyncStatus.Started){if(disposed)throw new OperationCanceledException();if(DateTime.UtcNow>=until)throw new TimeoutException();await Task.Delay(50);}if(disposed)throw new OperationCanceledException();return operation.GetResults();}finally{try{if(operation.Status==AsyncStatus.Started)operation.Cancel();operation.Close();}catch{}}}
        private static string Limit(string value,int length){return value.Length>length?value.Substring(0,length):value;}
        private void Detach(){if(listener!=null){listener.NotificationChanged-=Changed;listener=null;}known.Clear();primed=false;}
        public void Dispose(){if(disposed)return;disposed=true;enabled=false;Detach();}
    }
    // Armed only by a real suspend/resume pair. Logon alone never opens a curtain.
    internal sealed class ResumePresentationGate
    {
        private bool sleeping,pending;
        internal void Suspend(){sleeping=true;pending=false;}
        internal void Resume(){if(sleeping){sleeping=false;pending=true;}}
        internal bool Take(bool sessionUnlocked,bool featureEnabled){if(!featureEnabled){pending=false;return false;}if(!pending||!sessionUnlocked)return false;pending=false;return true;}
    }
}

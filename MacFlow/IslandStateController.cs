using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;
namespace MacFlow
{
    internal enum IslandState {idle,compact,expanded,notification,media,voiceListening,voiceThinking,voiceSpeaking,timer,transfer,error}
    internal sealed class IslandEvent
    {
        internal string Key,Text,Symbol,Detail;internal IslandState State;internal int Priority;internal DateTime Expires,Created;
    }
    internal sealed class IslandStateController : IDisposable
    {
        private readonly List<IslandEvent> events=new List<IslandEvent>();
        private readonly DispatcherTimer expiry=new DispatcherTimer(DispatcherPriority.Background){Interval=TimeSpan.FromSeconds(1)};
        private IMediaSource provider;private bool reading,pending,disposed;
        internal MediaState Media=new MediaState();internal event Action Changed;
        internal IslandStateController(){expiry.Tick+=delegate{Prune();Raise();if(events.Count==0&&!Media.Playing)expiry.Stop();};}
        internal int Count{get{Prune();return events.Count;}}
        internal IslandEvent Current{get{Prune();return events.OrderByDescending(e=>e.Priority).ThenByDescending(e=>e.Created).FirstOrDefault();}}
        internal void Publish(string key,string text,string symbol,IslandState state,int priority,TimeSpan lifetime,string detail=null)
        {
            if(disposed)return;Prune();var item=events.FirstOrDefault(e=>e.Key==key);
            if(item==null){if(events.Count>=32)events.Remove(events.OrderBy(e=>e.Priority).ThenBy(e=>e.Created).First());item=new IslandEvent{Key=key,Created=DateTime.UtcNow};events.Add(item);}
            item.Text=text;item.Detail=detail;item.Created=DateTime.UtcNow;item.Symbol=symbol;item.State=state;item.Priority=Math.Max(0,Math.Min(100,priority));item.Expires=DateTime.UtcNow+lifetime;expiry.Start();Raise();
        }
        internal void Remove(string key){events.RemoveAll(e=>e.Key==key);if(events.Count==0&&!Media.Playing)expiry.Stop();Raise();}
        private void Prune(){events.RemoveAll(e=>e.Expires<=DateTime.UtcNow);}
        internal void Attach(IMediaSource source){if(Object.ReferenceEquals(provider,source))return;if(provider!=null){provider.Changed-=Read;provider.Dispose();}provider=source;provider.Changed+=Read;Read();}
        private async void Read()
        {
            if(disposed||provider==null)return;if(!expiry.Dispatcher.CheckAccess()){expiry.Dispatcher.BeginInvoke(new Action(Read));return;}if(reading){pending=true;return;}reading=true;
            try{do{pending=false;try{var value=await provider.Read();if(!disposed)Media=value??new MediaState();}catch(Exception ex){Log.Write("Multimedia: "+ex.GetType().Name);if(!disposed)Media=new MediaState();}if(!disposed){if(Media.Playing||events.Count>0)expiry.Start();else expiry.Stop();Raise();}}while(pending&&!disposed);}finally{reading=false;}
        }
        internal async System.Threading.Tasks.Task Command(MediaCommand command){if(provider!=null){await provider.Command(command);Read();}}
        internal async System.Threading.Tasks.Task<bool> Seek(double seconds){var seek=provider as ISeekMediaSource;if(seek==null||!Media.CanSeek)return false;bool result=await seek.Seek(TimeSpan.FromSeconds(seconds));Read();return result;}
        internal void Raise(){if(!disposed&&Changed!=null)Changed();}
        public void Dispose(){if(disposed)return;disposed=true;expiry.Stop();if(provider!=null){provider.Changed-=Read;provider.Dispose();}events.Clear();Changed=null;}
    }
}

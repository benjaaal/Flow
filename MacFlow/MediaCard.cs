using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Windows.Foundation;
using Windows.Media.Control;

namespace MacFlow
{
    internal enum MediaCommand { Previous, Toggle, Next }
    internal sealed class MediaState
    {
        internal string Title="Sin reproducción",Artist="Abrí tu reproductor";
        internal bool Playing,CanToggle,CanPrevious,CanNext;
    }
    internal interface IMediaSource : IDisposable
    {
        event Action Changed;
        Task<MediaState> Read();
        Task<bool> Command(MediaCommand command);
    }
    // Windows reports only sessions whose players expose system media controls.
    // No keyboard interception, injected keys, album downloads or idle polling.
    internal sealed class WindowsMediaSource : IMediaSource
    {
        private GlobalSystemMediaTransportControlsSessionManager manager;
        private GlobalSystemMediaTransportControlsSession session;
        private readonly CancellationTokenSource stop=new CancellationTokenSource();
        private bool disposed;
        public event Action Changed;
        private void Notify(){var callback=Changed;if(!disposed&&callback!=null)callback();}
        private void CurrentChanged(GlobalSystemMediaTransportControlsSessionManager sender,CurrentSessionChangedEventArgs args){Notify();}
        private void PropertiesChanged(GlobalSystemMediaTransportControlsSession sender,MediaPropertiesChangedEventArgs args){Notify();}
        private void PlaybackChanged(GlobalSystemMediaTransportControlsSession sender,PlaybackInfoChangedEventArgs args){Notify();}
        private async Task<T> Result<T>(IAsyncOperation<T> operation)
        {
            var until=DateTime.UtcNow.AddSeconds(6);
            try{while(operation.Status==AsyncStatus.Started){stop.Token.ThrowIfCancellationRequested();if(DateTime.UtcNow>until)throw new TimeoutException();await Task.Delay(40,stop.Token);}stop.Token.ThrowIfCancellationRequested();return operation.GetResults();}
            finally{try{if(operation.Status==AsyncStatus.Started)operation.Cancel();operation.Close();}catch{}}
        }
        public async Task<MediaState> Read()
        {
            if(disposed)return new MediaState();
            if(manager==null){var created=await Result(GlobalSystemMediaTransportControlsSessionManager.RequestAsync());if(disposed)return new MediaState();manager=created;manager.CurrentSessionChanged+=CurrentChanged;}
            var current=manager.GetCurrentSession();
            if(current!=session){Detach();session=current;if(session!=null){session.MediaPropertiesChanged+=PropertiesChanged;session.PlaybackInfoChanged+=PlaybackChanged;}}
            if(session==null)return new MediaState();
            var selected=session;var properties=await Result(selected.TryGetMediaPropertiesAsync());
            var playback=selected.GetPlaybackInfo();var controls=playback.Controls;
            return new MediaState {Title=String.IsNullOrWhiteSpace(properties.Title)?"Multimedia":properties.Title,Artist=String.IsNullOrWhiteSpace(properties.Artist)?"Reproductor de Windows":properties.Artist,Playing=playback.PlaybackStatus==GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing,CanToggle=controls.IsPlayPauseToggleEnabled,CanPrevious=controls.IsPreviousEnabled,CanNext=controls.IsNextEnabled};
        }
        public async Task<bool> Command(MediaCommand command)
        {
            if(disposed||manager==null||session==null||manager.GetCurrentSession()!=session)return false;
            var selected=session;
            return await Result(command==MediaCommand.Previous?selected.TrySkipPreviousAsync():command==MediaCommand.Next?selected.TrySkipNextAsync():selected.TryTogglePlayPauseAsync());
        }
        private void Detach(){if(session==null)return;session.MediaPropertiesChanged-=PropertiesChanged;session.PlaybackInfoChanged-=PlaybackChanged;session=null;}
        public void Dispose(){if(disposed)return;disposed=true;stop.Cancel();Detach();if(manager!=null)manager.CurrentSessionChanged-=CurrentChanged;manager=null;Changed=null;}
    }
    internal sealed class MediaCard : StackPanel,IDisposable
    {
        private readonly IMediaSource source;
        private readonly TextBlock title,artist,status;
        internal readonly Button Previous,Toggle,Next;
        internal string TrackTitle {get{return title.Text;}}
        private bool disposed,busy,pending,commanding;
        internal MediaCard(IMediaSource provider)
        {
            source=provider;VerticalAlignment=VerticalAlignment.Center;
            Children.Add(new Border {Width=32,Height=32,CornerRadius=new CornerRadius(10),Background=Design.Brush(Design.Dark?"#15FFFFFF":"#18000000"),Child=Design.Text("♫",20,true),HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,0,0,7)});
            title=Design.Text("Sin reproducción",12,true);title.TextTrimming=TextTrimming.CharacterEllipsis;title.TextWrapping=TextWrapping.NoWrap;Children.Add(title);
            artist=Design.Text("Abrí tu reproductor",10,false);artist.SetResourceReference(TextBlock.ForegroundProperty,"GlassMuted");artist.TextTrimming=TextTrimming.CharacterEllipsis;artist.TextWrapping=TextWrapping.NoWrap;artist.Margin=new Thickness(0,3,0,5);Children.Add(artist);
            var buttons=new StackPanel {Orientation=Orientation.Horizontal};Previous=Button("‹",MediaCommand.Previous,"Pista anterior");Toggle=Button("▷",MediaCommand.Toggle,"Reproducir / pausar");Next=Button("›",MediaCommand.Next,"Pista siguiente");buttons.Children.Add(Previous);buttons.Children.Add(Toggle);buttons.Children.Add(Next);Children.Add(buttons);
            status=Design.Text("",9,false);status.SetResourceReference(TextBlock.ForegroundProperty,"GlassMuted");Children.Add(status);
            source.Changed+=Changed;Loaded+=delegate{Refresh();};Apply(new MediaState());
        }
        private Button Button(string symbol,MediaCommand command,string hint)
        {
            var button=Design.Button(symbol,async delegate
            {
                if(disposed||commanding)return;commanding=true;Previous.IsEnabled=Toggle.IsEnabled=Next.IsEnabled=false;
                try{if(!await source.Command(command)&&!disposed)status.Text="El reproductor no aceptó la acción";}catch{if(!disposed)status.Text="Reproductor no disponible";}
                finally{commanding=false;if(!disposed)Refresh();}
            },false);
            button.Width=30;button.Height=30;button.Padding=new Thickness(0);button.Margin=new Thickness(2);button.Background=Brushes.Transparent;button.BorderThickness=new Thickness(0);button.FontSize=21;button.ToolTip=hint;return button;
        }
        private void Changed(){if(disposed||Dispatcher.HasShutdownStarted)return;try{Dispatcher.BeginInvoke(new Action(Refresh));}catch(InvalidOperationException){}}
        private async void Refresh()
        {
            if(disposed)return;if(busy){pending=true;return;}busy=true;
            try{do{pending=false;MediaState state;try{state=await source.Read();}catch{state=new MediaState {Artist="Información no disponible"};}if(!disposed)Apply(state);}while(pending&&!disposed);}
            finally{busy=false;}
        }
        private void Apply(MediaState state)
        {
            title.Text=state.Title;title.ToolTip=state.Title;artist.Text=state.Artist;artist.ToolTip=state.Artist;Toggle.Content=state.Playing?"Ⅱ":"▷";
            Previous.IsEnabled=!commanding&&state.CanPrevious;Toggle.IsEnabled=!commanding&&state.CanToggle;Next.IsEnabled=!commanding&&state.CanNext;
        }
        public void Dispose(){if(disposed)return;disposed=true;source.Changed-=Changed;source.Dispose();}
    }
}

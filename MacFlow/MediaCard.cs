using System;
using System.Linq;using System.Threading;using System.IO;using System.Windows.Media.Imaging;using Windows.Storage.Streams;
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
        internal TimeSpan Position,Duration;internal bool CanSeek;
        internal bool Playing,CanToggle,CanPrevious,CanNext;internal BitmapSource Artwork;
    }
    internal interface ISeekMediaSource{Task<bool> Seek(TimeSpan position);}
    internal interface IMediaSource : IDisposable
    {
        event Action Changed;
        Task<MediaState> Read();
        Task<bool> Command(MediaCommand command);
    }
    // Windows reports only sessions whose players expose system media controls.
    // No keyboard interception, injected keys, album downloads or idle polling.
    internal sealed class WindowsMediaSource : IMediaSource,ISeekMediaSource
    {
        private GlobalSystemMediaTransportControlsSessionManager manager;
        private GlobalSystemMediaTransportControlsSession session;private readonly System.Collections.Generic.List<GlobalSystemMediaTransportControlsSession> watched=new System.Collections.Generic.List<GlobalSystemMediaTransportControlsSession>();
        private readonly CancellationTokenSource stop=new CancellationTokenSource();
        private bool disposed;private string artworkKey;private BitmapSource artwork;
        public event Action Changed;
        private void Notify(){var callback=Changed;if(!disposed&&callback!=null)callback();}
        private void SessionsChanged(GlobalSystemMediaTransportControlsSessionManager sender,SessionsChangedEventArgs args){WatchSessions();Notify();}
        private void WatchSessions(){lock(watched){foreach(var item in watched)try{item.PlaybackInfoChanged-=PlaybackChanged;}catch{}watched.Clear();if(disposed||manager==null)return;try{foreach(var item in manager.GetSessions().Take(32)){watched.Add(item);item.PlaybackInfoChanged+=PlaybackChanged;}}catch(Exception ex){Log.Write("Sesiones multimedia: "+ex.GetType().Name);}}}
        private GlobalSystemMediaTransportControlsSession SelectSession(){var current=manager.GetCurrentSession();if(current!=null)try{if(current.GetPlaybackInfo().PlaybackStatus==GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)return current;}catch{}foreach(var candidate in manager.GetSessions())try{if(candidate.GetPlaybackInfo().PlaybackStatus==GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)return candidate;}catch{}return current;}
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
            if(manager==null){var created=await Result(GlobalSystemMediaTransportControlsSessionManager.RequestAsync());if(disposed)return new MediaState();manager=created;manager.CurrentSessionChanged+=CurrentChanged;manager.SessionsChanged+=SessionsChanged;WatchSessions();}
            var current=SelectSession();
            if(current!=session){Detach();session=current;if(session!=null){session.MediaPropertiesChanged+=PropertiesChanged;}}
            if(session==null)return new MediaState();
            var selected=session;var properties=await Result(selected.TryGetMediaPropertiesAsync());
            string key=properties.Title+"|"+properties.Artist;if(key!=artworkKey){artworkKey=key;artwork=null;try{if(properties.Thumbnail!=null)using(var stream=await Result(properties.Thumbnail.OpenReadAsync())){if(stream.Size>0&&stream.Size<=4194304)using(var reader=new DataReader(stream)){await Result(reader.LoadAsync((uint)stream.Size));var bytes=new byte[(int)stream.Size];reader.ReadBytes(bytes);using(var memory=new MemoryStream(bytes)){var bitmap=new BitmapImage();bitmap.BeginInit();bitmap.CacheOption=BitmapCacheOption.OnLoad;bitmap.DecodePixelWidth=256;bitmap.StreamSource=memory;bitmap.EndInit();bitmap.Freeze();artwork=bitmap;}}}}catch{artwork=null;}}
            var playback=selected.GetPlaybackInfo();var controls=playback.Controls;var timeline=selected.GetTimelineProperties();
            return new MediaState {Position=timeline.Position,Duration=timeline.EndTime,CanSeek=controls.IsPlaybackPositionEnabled,Artwork=artwork,Title=String.IsNullOrWhiteSpace(properties.Title)?"Multimedia":properties.Title,Artist=String.IsNullOrWhiteSpace(properties.Artist)?"Reproductor de Windows":properties.Artist,Playing=playback.PlaybackStatus==GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing,CanToggle=controls.IsPlayPauseToggleEnabled,CanPrevious=controls.IsPreviousEnabled,CanNext=controls.IsNextEnabled};
        }
        public async Task<bool> Command(MediaCommand command)
        {
            if(disposed||manager==null||session==null||SelectSession()!=session)return false;
            var selected=session;
            return await Result(command==MediaCommand.Previous?selected.TrySkipPreviousAsync():command==MediaCommand.Next?selected.TrySkipNextAsync():selected.TryTogglePlayPauseAsync());
        }
        public async Task<bool> Seek(TimeSpan position){if(disposed||session==null||SelectSession()!=session)return false;return await Result(session.TryChangePlaybackPositionAsync(Math.Max(0,position.Ticks)));}
        private void Detach(){if(session==null)return;session.MediaPropertiesChanged-=PropertiesChanged;session=null;artworkKey=null;artwork=null;}
        public void Dispose(){if(disposed)return;disposed=true;stop.Cancel();Detach();lock(watched){foreach(var item in watched)try{item.PlaybackInfoChanged-=PlaybackChanged;}catch{}watched.Clear();}if(manager!=null){manager.CurrentSessionChanged-=CurrentChanged;manager.SessionsChanged-=SessionsChanged;}manager=null;Changed=null;}
    }
    internal sealed class MediaCard : StackPanel,IDisposable
    {
        private readonly IMediaSource source;private readonly Border artwork;private bool spacious;
        private readonly TextBlock title,artist,status;
        internal readonly Button Previous,Toggle,Next;
        internal string TrackTitle {get{return title.Text;}}
        private bool lockPlayer;private bool hideWhenEmpty;private MediaState lastState=new MediaState();
        internal bool HideWhenEmpty{get{return hideWhenEmpty;}set{hideWhenEmpty=value;Visibility=value&&!HasMedia(lastState)?Visibility.Collapsed:Visibility.Visible;}}
        internal event Action MediaChanged;internal event Action ExpandRequested;
        internal bool Expanded{get{return spacious;}}
        internal bool HasActiveSession{get{return HasMedia(lastState);}}
        internal bool ArtworkToggleOnly;
        internal FrameworkElement ArtworkElement{get{return artwork;}}
        internal bool Playing{get{return lastState.Playing;}}
        internal ImageSource Artwork{get{return lastState.Artwork;}}
        internal event Action ArtworkPressed;
        private readonly Button expand;private bool compactPreferred,compact;private StackPanel compactButtons;
        private readonly System.Windows.Threading.DispatcherTimer mediaClock=new System.Windows.Threading.DispatcherTimer{Interval=TimeSpan.FromSeconds(1)};
        private readonly Slider position=new Slider{Minimum=0,Height=18,Visibility=Visibility.Collapsed};private readonly TextBlock timing=Design.Text("",10,false);private bool seeking;
        internal static bool HasMedia(MediaState state){return state!=null&&(state.CanToggle||state.CanNext||state.CanPrevious||state.Artwork!=null||!String.IsNullOrWhiteSpace(state.Title)&&state.Title!="Sin reproducción");}
        private bool disposed,busy,pending,commanding;
        internal MediaCard(IMediaSource provider,bool spacious=false)
        {
            source=provider;this.spacious=spacious;VerticalAlignment=VerticalAlignment.Center;
            artwork=new Border{Width=spacious?72:38,Height=spacious?72:38,CornerRadius=new CornerRadius(spacious?16:10),Background=Design.Brush("#20FFFFFF"),HorizontalAlignment=spacious?HorizontalAlignment.Center:HorizontalAlignment.Left,Margin=new Thickness(0,0,0,10),ClipToBounds=true};Children.Add(artwork);
            title=Design.Text("Sin reproducción",spacious?19:12,true);title.TextTrimming=TextTrimming.CharacterEllipsis;title.TextWrapping=TextWrapping.NoWrap;Children.Add(title);
            artist=Design.Text("Abrí tu reproductor",spacious?14:10,false);artist.SetResourceReference(TextBlock.ForegroundProperty,"GlassMuted");artist.TextTrimming=TextTrimming.CharacterEllipsis;artist.TextWrapping=TextWrapping.NoWrap;artist.Margin=new Thickness(0,3,0,5);Children.Add(artist);if(spacious){title.TextAlignment=artist.TextAlignment=TextAlignment.Center;}
            var buttons=new StackPanel {Orientation=Orientation.Horizontal,HorizontalAlignment=spacious?HorizontalAlignment.Center:HorizontalAlignment.Left};Previous=Button("‹",MediaCommand.Previous,"Pista anterior");Toggle=Button("▷",MediaCommand.Toggle,"Reproducir / pausar");Next=Button("›",MediaCommand.Next,"Pista siguiente");buttons.Children.Add(Previous);buttons.Children.Add(Toggle);buttons.Children.Add(Next);Children.Add(buttons);
            status=Design.Text("",9,false);status.SetResourceReference(TextBlock.ForegroundProperty,"GlassMuted");Children.Add(status);
            expand=Design.Button("Ampliar",delegate{var callback=ExpandRequested;if(callback!=null)callback();},false);expand.Background=Brushes.Transparent;expand.FontSize=10;expand.HorizontalAlignment=HorizontalAlignment.Right;expand.Margin=new Thickness(0);Children.Add(expand);expand.Loaded+=delegate{expand.Visibility=ArtworkToggleOnly||lockPlayer||ExpandRequested==null?Visibility.Collapsed:Visibility.Visible;};
            position.PreviewMouseLeftButtonDown+=delegate{seeking=true;};position.PreviewMouseLeftButtonUp+=async delegate{if(!seeking)return;seeking=false;var seek=source as ISeekMediaSource;if(seek!=null)try{await seek.Seek(TimeSpan.FromSeconds(position.Value));}catch{status.Text="No se pudo mover la reproducción";}};Children.Insert(3,position);Children.Insert(4,timing);timing.Visibility=Visibility.Collapsed;mediaClock.Tick+=delegate{Refresh();};Unloaded+=delegate{mediaClock.Stop();};IsVisibleChanged+=delegate{if(!IsVisible)mediaClock.Stop();else if((spacious||lockPlayer||privacyCompact)&&lastState.Playing)mediaClock.Start();};
            artwork.SizeChanged+=delegate{artwork.Clip=new RectangleGeometry(new Rect(0,0,artwork.ActualWidth,artwork.ActualHeight),Math.Min(16,artwork.ActualWidth*.22),Math.Min(16,artwork.ActualHeight*.22));};artwork.Tag="MediaArtwork";artwork.Cursor=System.Windows.Input.Cursors.Hand;System.Windows.Automation.AutomationProperties.SetName(artwork,"Ampliar carátula");artwork.MouseLeftButtonUp+=delegate(object sender,System.Windows.Input.MouseButtonEventArgs args){var handler=ArtworkPressed;if(handler!=null){handler();args.Handled=true;}};
            source.Changed+=Changed;Loaded+=delegate{Refresh();};Apply(new MediaState());
        }
        internal void CompactLayout()
        {
            if(compact)return;compact=true;compactPreferred=true;var buttons=(StackPanel)Toggle.Parent;compactButtons=buttons;buttons.HorizontalAlignment=HorizontalAlignment.Left;Children.Clear();artwork.Width=artwork.Height=46;artwork.Margin=new Thickness(0,0,12,0);artwork.VerticalAlignment=VerticalAlignment.Top;
            var layout=new Grid();layout.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(58)});layout.ColumnDefinitions.Add(new ColumnDefinition());layout.RowDefinitions.Add(new RowDefinition());layout.RowDefinitions.Add(new RowDefinition());
            layout.Children.Add(artwork);Grid.SetRowSpan(artwork,2);var labels=new StackPanel();labels.Children.Add(title);labels.Children.Add(artist);Grid.SetColumn(labels,1);layout.Children.Add(labels);
            var actions=new DockPanel();DockPanel.SetDock(expand,Dock.Right);actions.Children.Add(expand);actions.Children.Add(buttons);Grid.SetColumn(actions,1);Grid.SetRow(actions,1);layout.Children.Add(actions);Children.Add(layout);Children.Add(status);
        }
        private void FullLayout()
        {
            if(!compact)return;foreach(var item in new FrameworkElement[]{artwork,title,artist,compactButtons,expand}){var parent=item.Parent as Panel;if(parent!=null)parent.Children.Remove(item);}Children.Clear();Children.Add(artwork);Children.Add(title);Children.Add(artist);Children.Add(position);Children.Add(timing);Children.Add(compactButtons);compactButtons.HorizontalAlignment=HorizontalAlignment.Center;Children.Add(status);Children.Add(expand);compact=false;
        }
        internal void ControlLayout(double artworkSize=32)
        {
            FullLayout();ArtworkToggleOnly=true;artwork.Width=artwork.Height=artworkSize;artwork.Margin=new Thickness(0,0,0,6);artwork.HorizontalAlignment=HorizontalAlignment.Left;title.FontSize=12;artist.FontSize=10;title.TextAlignment=artist.TextAlignment=TextAlignment.Left;expand.Visibility=Visibility.Collapsed;compactButtons.HorizontalAlignment=HorizontalAlignment.Center;foreach(var button in new[]{Previous,Toggle,Next}){button.Width=30;button.Height=28;button.Margin=new Thickness(2,3,2,0);button.MinHeight=0;}Apply(lastState);
        }
        private bool privacyCompact;
        internal void PrivacyLayout()
        {
            CompactLayout();FullLayout();privacyCompact=true;compact=true;compactPreferred=true;Children.Clear();
            artwork.Width=artwork.Height=34;artwork.Margin=new Thickness(0,0,10,0);artwork.VerticalAlignment=VerticalAlignment.Center;artwork.Visibility=Visibility.Visible;
            title.FontSize=12;artist.FontSize=10;title.TextAlignment=artist.TextAlignment=TextAlignment.Left;title.Margin=new Thickness(0);artist.Margin=new Thickness(0,3,0,0);
            var row=new Grid{MinHeight=36};row.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(44)});row.ColumnDefinitions.Add(new ColumnDefinition());row.Children.Add(artwork);
            var labels=new StackPanel{VerticalAlignment=VerticalAlignment.Center};labels.Children.Add(title);labels.Children.Add(artist);Grid.SetColumn(labels,1);row.Children.Add(labels);Children.Add(row);
            ActivityIsland.StyleTimeline(position);position.Height=12;position.Margin=new Thickness(0,5,0,0);Children.Add(position);
            compactButtons.HorizontalAlignment=HorizontalAlignment.Center;compactButtons.VerticalAlignment=VerticalAlignment.Center;foreach(var button in new[]{Previous,Toggle,Next}){button.Width=36;button.Height=28;button.MinHeight=0;button.Margin=new Thickness(5,1,5,0);}
            Previous.Content=ActivityIsland.Transport("previous",16);Next.Content=ActivityIsland.Transport("next",16);Children.Add(compactButtons);Children.Add(status);Apply(lastState);
        }
        internal void LockPlayerLayout(bool value)
        {
            lockPlayer=value;if(value){privacyCompact=false;FullLayout();artwork.Visibility=Visibility.Collapsed;title.FontSize=18;artist.FontSize=14;title.TextAlignment=artist.TextAlignment=TextAlignment.Center;expand.Visibility=Visibility.Collapsed;compactButtons.HorizontalAlignment=HorizontalAlignment.Center;foreach(var button in new[]{Previous,Toggle,Next}){button.Width=64;button.Height=42;button.MinHeight=0;button.Margin=new Thickness(6,4,6,0);}Previous.Content=ActivityIsland.Transport("previous",30);Next.Content=ActivityIsland.Transport("next",30);ActivityIsland.StyleTimeline(position);position.Height=18;}
            else{artwork.Visibility=Visibility.Visible;title.FontSize=12;artist.FontSize=10;title.TextAlignment=artist.TextAlignment=TextAlignment.Left;expand.Visibility=ArtworkToggleOnly?Visibility.Collapsed:Visibility.Visible;foreach(var button in new[]{Previous,Toggle,Next}){button.Width=30;button.Height=30;button.Margin=new Thickness(2);}Previous.Content="‹";Next.Content="›";PrivacyLayout();}Apply(lastState);
        }
        private Button Button(string symbol,MediaCommand command,string hint)
        {
            var button=Design.Button(symbol,async delegate
            {
                if(disposed||commanding)return;commanding=true;Previous.IsEnabled=Toggle.IsEnabled=Next.IsEnabled=false;
                try{if(!await source.Command(command)&&!disposed)status.Text="El reproductor no aceptó la acción";}catch{if(!disposed)status.Text="Reproductor no disponible";}
                finally{commanding=false;if(!disposed)Refresh();}
            },false);
            button.Width=spacious?58:30;button.Height=spacious?42:30;button.Padding=new Thickness(0);button.Margin=new Thickness(2);button.Background=Brushes.Transparent;button.BorderThickness=new Thickness(0);button.FontSize=spacious?32:21;button.ToolTip=hint;return button;
        }
        private void Changed(){if(disposed||Dispatcher.HasShutdownStarted)return;try{Dispatcher.BeginInvoke(new Action(Refresh));}catch(InvalidOperationException){}}
        private async void Refresh()
        {
            if(disposed)return;if(busy){pending=true;return;}busy=true;
            try{do{pending=false;MediaState state;try{state=await source.Read();}catch{state=new MediaState {Artist="Información no disponible"};}if(!disposed)Apply(state);}while(pending&&!disposed);}
            finally{busy=false;}
        }
        internal void SetExpanded(bool value,double artworkSize=260){if(value)FullLayout();spacious=value;expand.Content=value?"Reducir":"Ampliar";artwork.Width=artwork.Height=value?Math.Max(100,Math.Min(300,artworkSize)):38;artwork.HorizontalAlignment=value?HorizontalAlignment.Center:HorizontalAlignment.Left;title.FontSize=value?19:12;artist.FontSize=value?14:10;title.TextAlignment=artist.TextAlignment=value?TextAlignment.Center:TextAlignment.Left;if(!value&&compactPreferred)CompactLayout();Apply(lastState);}
        private void Apply(MediaState state)
        {
            lastState=state;bool timeline=(spacious||lockPlayer||privacyCompact)&&state.Duration.TotalSeconds>0;position.Visibility=timing.Visibility=timeline?Visibility.Visible:Visibility.Collapsed;position.IsEnabled=state.CanSeek&&source is ISeekMediaSource;if(!seeking){position.Maximum=Math.Max(1,state.Duration.TotalSeconds);position.Value=Math.Max(0,Math.Min(position.Maximum,state.Position.TotalSeconds));}if(privacyCompact)timing.Visibility=Visibility.Collapsed;timing.Text=state.Position.ToString(@"m\:ss")+" / "+state.Duration.ToString(@"m\:ss");if((spacious||lockPlayer||privacyCompact)&&state.Playing&&IsVisible)mediaClock.Start();else mediaClock.Stop();Visibility=HideWhenEmpty&&!HasMedia(state)?Visibility.Collapsed:Visibility.Visible;
            artwork.Child=state.Artwork!=null?(UIElement)new Image{Source=state.Artwork,Stretch=System.Windows.Media.Stretch.UniformToFill}:Design.Symbol("music",spacious?32:20,Design.Ink);title.Text=state.Title;title.ToolTip=state.Title;artist.Text=state.Artist;artist.ToolTip=state.Artist;Toggle.Content=lockPlayer||privacyCompact?(object)ActivityIsland.Transport(state.Playing?"pause":"play",privacyCompact?19:32):state.Playing?"Ⅱ":"▷";
            Previous.IsEnabled=!commanding&&state.CanPrevious;Toggle.IsEnabled=!commanding&&state.CanToggle;Next.IsEnabled=!commanding&&state.CanNext;var changed=MediaChanged;if(changed!=null)changed();
        }
        public void Dispose(){if(disposed)return;disposed=true;mediaClock.Stop();source.Changed-=Changed;source.Dispose();}
    }
}

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
namespace MacFlow
{
    internal sealed class ActivityIsland : IDisposable
    {
        private readonly Desktop desktop;private readonly Window anchor;private readonly bool preview;
        private readonly Border plate;private readonly Border compactIcon=new Border{Width=18,Height=18,VerticalAlignment=VerticalAlignment.Center};private readonly ActivityWave compactWave=new ActivityWave();private readonly Border expandedArt=new Border{Width=44,Height=44,HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(0,0,0,8)};private readonly StackPanel body,commands;
        private readonly Button compactButton;private readonly Slider progress;
        private ShellTopBar host;private bool expanded,disposed,shelfMode,autoExpanded,wasExpanded;private string noticeKey;private double targetWidth=-1,targetHeight=-1;
        private readonly System.Windows.Threading.DispatcherTimer hover=new System.Windows.Threading.DispatcherTimer{Interval=TimeSpan.FromMilliseconds(450)};
        internal IslandState State{get;private set;}
        internal Window View{get{return anchor;}}internal FrameworkElement Surface{get{return plate;}}
        internal bool Expanded{get{return expanded;}}internal bool PlaybackVisible{get{return desktop.IslandState.Media.Playing&&plate.IsVisible;}}
        internal bool CompactTrackNameVisible{get{return false;}}internal Slider PositionControl{get{return progress;}}
        internal bool ShelfVisible{get{return false;}}internal double Expansion{get{return Math.Max(0,Math.Min(1,(plate.ActualHeight-24)/226));}}
        internal double ExpandedContentOpacity{get{return expanded?body.Opacity:0;}}
        internal ActivityIsland(Desktop owner,IMediaSource provider,bool preview=false,Window anchor=null)
        {
            desktop=owner;this.anchor=anchor??owner.TopBar;this.preview=preview;
            var grid=new Grid();
            compactButton=Design.Button("",Toggle,false);var compactRow=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center};compactRow.Children.Add(compactIcon);compactRow.Children.Add(compactWave);compactButton.Content=compactRow;compactButton.Height=24;compactButton.MinHeight=0;compactButton.Padding=new Thickness(12,0,12,0);compactButton.Margin=new Thickness(0);compactButton.Background=Brushes.Transparent;compactButton.VerticalAlignment=VerticalAlignment.Top;
            System.Windows.Automation.AutomationProperties.SetName(compactButton,"Isla dinámica de MacFlow · expandir actividad");grid.Children.Add(compactButton);
            body=new StackPanel{Margin=new Thickness(16,36,16,12),Visibility=Visibility.Collapsed};body.Children.Add(expandedArt);
            progress=new Slider{Minimum=0,Maximum=1,Height=24};System.Windows.Automation.AutomationProperties.SetName(progress,"Posición de reproducción");StyleTimeline(progress);body.Children.Add(progress);progress.PreviewMouseLeftButtonUp+=async delegate{await SeekPosition(progress.Value);};progress.PreviewKeyUp+=async delegate{await SeekPosition(progress.Value);};
            commands=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center};body.Children.Add(commands);grid.Children.Add(body);
            plate=new Border{Background=Brushes.Black,CornerRadius=new CornerRadius(12),Width=96,Height=24,VerticalAlignment=VerticalAlignment.Top,HorizontalAlignment=HorizontalAlignment.Center,Child=grid,ClipToBounds=true};
            System.Windows.Automation.AutomationProperties.SetName(plate,"Isla dinámica integrada en la barra superior");
            plate.SizeChanged+=delegate{if(host!=null)host.RefreshRegion();if(Math.Abs(targetWidth-plate.ActualWidth)<1)body.Opacity=1;};
            compactButton.PreviewKeyDown+=delegate(object sender,KeyEventArgs e){if(e.Key==Key.Escape){expanded=false;Refresh();e.Handled=true;}};
            plate.PreviewKeyDown+=delegate(object sender,KeyEventArgs e){if(e.Key==Key.Escape){expanded=false;Refresh();compactButton.Focus();e.Handled=true;}};
            hover.Tick+=delegate{hover.Stop();if(!disposed){expanded=true;Refresh();}};
            plate.MouseEnter+=delegate{if(desktop.Preferences.NotchHover&&!expanded)hover.Start();};plate.MouseLeave+=delegate{hover.Stop();};
            plate.AllowDrop=true;plate.DragOver+=delegate(object sender,DragEventArgs e){e.Effects=e.Data.GetDataPresent(DataFormats.FileDrop)&&desktop.Preferences.NotchShelf?DragDropEffects.Copy:DragDropEffects.None;e.Handled=true;};
            plate.Drop+=delegate(object sender,DragEventArgs e){if(desktop.Preferences.NotchShelf&&e.Data.GetDataPresent(DataFormats.FileDrop)){desktop.OpenShelf((string[])e.Data.GetData(DataFormats.FileDrop));}e.Handled=true;};
            desktop.IslandState.Changed+=Refresh;desktop.IslandState.Attach(provider);Refresh();
        }
        internal static UIElement Transport(string symbol,double size)
        {
            string path=symbol=="previous"?"M6,7 L6,25 M25,6 L9,16 25,26 Z":symbol=="next"?"M26,7 L26,25 M7,6 L23,16 7,26 Z":symbol=="pause"?"M8,6 L12,6 12,26 8,26 Z M20,6 L24,6 24,26 20,26 Z":symbol=="play"?"M8,5 L26,16 8,27 Z":symbol=="stop"?"M8,8 L24,8 24,24 8,24 Z":symbol=="close"?"M8,8 L24,24 M24,8 L8,24":symbol=="up"?"M8,20 L16,12 24,20":symbol=="folder"?"M4,10 L13,10 16,13 28,13 27,26 5,26 Z M4,10 L4,7 13,7 16,10":symbol=="keyboard"?"M3,8 L29,8 29,25 3,25 Z M7,12 L9,12 M13,12 L15,12 M19,12 L21,12 M25,12 L26,12 M7,17 L9,17 M13,17 L15,17 M19,17 L21,17 M8,21 L24,21":symbol=="timer"?"M16,6 A11,11 0 1 1 15.9,6 M16,10 L16,17 21,20 M12,2 L20,2":symbol=="mic"?"M12,6 A4,4 0 0 1 20,6 L20,17 A4,4 0 0 1 12,17 Z M8,15 L8,17 A8,8 0 0 0 24,17 L24,15 M16,25 L16,30 M11,30 L21,30":symbol=="music"?"M13,24 L13,7 27,4 27,20 M13,11 L27,8 M13,24 A4,3 0 1 1 9,21 A4,3 0 0 1 13,24 M27,20 A4,3 0 1 1 23,17 A4,3 0 0 1 27,20":symbol=="error"?"M16,3 L30,28 2,28 Z M16,11 L16,19 M16,23 L16,24":symbol=="open"?"M17,5 L27,5 27,15 M27,5 L14,18 M11,8 L5,8 5,27 24,27 24,21":"M10,23 L10,13 A6,6 0 0 1 22,13 L22,23 26,25 6,25 Z M13,28 A3,3 0 0 0 19,28";
            bool filled=symbol=="play"||symbol=="pause"||symbol=="stop";
            return new System.Windows.Shapes.Path{Data=Geometry.Parse(path),Width=size,Height=size,Stretch=Stretch.Uniform,Stroke=Brushes.White,StrokeThickness=1.8,StrokeStartLineCap=PenLineCap.Round,StrokeEndLineCap=PenLineCap.Round,Fill=filled?Brushes.White:Brushes.Transparent};
        }
        internal static void StyleTimeline(Slider slider){slider.IsMoveToPointEnabled=true;}
        internal void Flash(string text,string symbol){desktop.IslandState.Publish(symbol+":"+text,text,symbol,IslandState.notification,Priority("notification",60),TimeSpan.FromSeconds(3));}
        internal void Toggle(){if(disposed)return;hover.Stop();autoExpanded=false;expanded=!expanded;if(!expanded)shelfMode=false;Refresh();}
        internal void Remove(){desktop.SetIslandEnabled(false);Hide();}
        internal void ShowShelf(){if(desktop.Preferences.NotchShelf)desktop.OpenShelf();}
        internal void ShowActivity(){shelfMode=false;expanded=true;Refresh();}
        internal async System.Threading.Tasks.Task<bool> SeekPosition(double value){try{return await desktop.IslandState.Seek(Math.Max(0,Math.Min(desktop.IslandState.Media.Duration.TotalSeconds,value)));}catch(Exception ex){desktop.IslandState.Publish("media-error",ex.Message,"volume",IslandState.error,100,TimeSpan.FromSeconds(4));return false;}}
        private int Priority(string state,int fallback){int value;return desktop.Preferences.IslandPriorities!=null&&desktop.Preferences.IslandPriorities.TryGetValue(state,out value)?Math.Max(0,Math.Min(100,value)):fallback;}
        private void Button(string text,Action action)
        {
            string symbol=text=="Anterior"?"previous":text=="Siguiente"?"next":text=="Pausar"?"pause":text=="Reproducir"?"play":text=="Escribir"?"keyboard":text=="Detener"?"stop":text=="Shelf"?"folder":text=="Contraer"?"up":text=="Descartar"?"close":text=="Abrir"?"open":"bell";
            var button=Design.Button("",action,false);button.Content=Transport(symbol,19);button.Width=button.Height=36;button.Background=Design.Brush("#303034");button.Margin=new Thickness(3);button.Padding=new Thickness(8);button.MinHeight=36;System.Windows.Automation.AutomationProperties.SetName(button,text);commands.Children.Add(button);
        }
        private async void Command(MediaCommand command){try{await desktop.IslandState.Command(command);}catch(Exception ex){desktop.IslandState.Publish("media-error",ex.Message,"volume",IslandState.error,100,TimeSpan.FromSeconds(4));}}
        internal void Refresh()
        {
            if(disposed||anchor.Dispatcher.HasShutdownStarted)return;
            if(!anchor.Dispatcher.CheckAccess()){anchor.Dispatcher.BeginInvoke(new Action(Refresh));return;}
            var currentHost=ShellTopBar.For(anchor);if(currentHost==null)return;if(!Object.ReferenceEquals(host,currentHost)){if(host!=null)host.Unmount(plate);host=currentHost;host.Mount(plate);}
            if(!preview&&(!desktop.ShellVisible||!desktop.Preferences.ShowTopBar||desktop.Preferences.ShowActivityIsland==false||desktop.FullscreenOn(DisplaySpace.For(anchor)))){Hide();return;}
            plate.Visibility=Visibility.Visible;var media=desktop.IslandState.Media;var notice=desktop.IslandState.Current;
            bool notify=notice!=null&&(notice.State==IslandState.notification||notice.State==IslandState.error);if(notify&&notice.Key!=noticeKey){if(!autoExpanded)wasExpanded=expanded;autoExpanded=true;noticeKey=notice.Key;expanded=true;}else if(!notify){if(autoExpanded)expanded=wasExpanded;autoExpanded=false;noticeKey=null;}
            var timer=desktop.Preferences.NotchTimers&&desktop.Widgets!=null?desktop.Widgets.State.Items.FirstOrDefault(t=>(t.Kind=="Timer"||t.Kind=="Stopwatch")&&t.Running):null;
            bool voice=desktop.AssistantMode!=AssistantStage.Hidden&&(notice==null||notice.Priority<Priority("voice",90));
            State=voice?(desktop.AssistantMode==AssistantStage.Listening?IslandState.voiceListening:desktop.AssistantMode==AssistantStage.Speaking?IslandState.voiceSpeaking:IslandState.voiceThinking):notice!=null?notice.State:timer!=null?IslandState.timer:desktop.Preferences.NotchMedia&&MediaCard.HasMedia(media)?IslandState.media:expanded?IslandState.expanded:IslandState.idle;
            string heading=voice?(desktop.AssistantMessage??"Asistente de MacFlow"):notice!=null?notice.Text:timer!=null?"Temporizador en curso":State==IslandState.media?media.Title:"Sin actividades en curso";
            commands.Children.Clear();if(expanded){
                if(voice){Button("Escribir",desktop.ShowAssistantInput);Button("Detener",desktop.StopAssistantVoice);}
                else if(timer!=null){Button("Pausar",delegate{double seconds=(DateTime.UtcNow-timer.StartedUtc).TotalSeconds;timer.Seconds+=Math.Max(0,seconds);timer.Running=false;desktop.Widgets.Save();Refresh();});}
                else if(State==IslandState.media){if(media.CanPrevious)Button("Anterior",delegate{Command(MediaCommand.Previous);});if(media.CanToggle)Button(media.Playing?"Pausar":"Reproducir",delegate{Command(MediaCommand.Toggle);});if(media.CanNext)Button("Siguiente",delegate{Command(MediaCommand.Next);});}
                else if(State==IslandState.notification||State==IslandState.error){Button("Ver avisos",desktop.OpenActivity);Button("Descartar",delegate{if(notice!=null)desktop.IslandState.Remove(notice.Key);});}
                else if(desktop.Preferences.NotchShelf)Button("Shelf",ShowShelf);
                Button("Contraer",delegate{expanded=false;shelfMode=false;Refresh();});
            }
            progress.Visibility=expanded&&(State==IslandState.media&&media.Duration.TotalSeconds>0||State==IslandState.timer)?Visibility.Visible:Visibility.Collapsed;progress.Maximum=Math.Max(1,media.Duration.TotalSeconds);if(!progress.IsMouseCaptureWithin&&!progress.IsKeyboardFocusWithin)progress.Value=Math.Min(progress.Maximum,media.Position.TotalSeconds);progress.IsEnabled=State==IslandState.media&&media.CanSeek;if(State==IslandState.timer&&timer!=null){progress.Maximum=Math.Max(1,timer.TimerMinutes*60);progress.Value=Math.Max(0,Math.Min(progress.Maximum,timer.Seconds+(DateTime.UtcNow-timer.StartedUtc).TotalSeconds));}
            string glyph=voice?"mic":State==IslandState.media?"music":State==IslandState.timer?"timer":State==IslandState.error?"error":State==IslandState.transfer?"folder":"bell";compactIcon.Child=State==IslandState.media&&media.Artwork!=null?(UIElement)new Image{Source=media.Artwork,Stretch=Stretch.UniformToFill}:Transport(glyph,18);compactIcon.Visibility=State==IslandState.idle?Visibility.Collapsed:Visibility.Visible;compactWave.Set(media.Playing||voice||State==IslandState.transfer);compactWave.Visibility=media.Playing||voice||State==IslandState.transfer?Visibility.Visible:Visibility.Collapsed;expandedArt.Child=State==IslandState.media&&media.Artwork!=null?(UIElement)new Image{Source=media.Artwork,Stretch=Stretch.Uniform}:Transport(glyph,38);expandedArt.Visibility=expanded&&State!=IslandState.idle?Visibility.Visible:Visibility.Collapsed;
            double available=host.AvailableCenter;bool narrow=available<180;body.Visibility=expanded&&!shelfMode?Visibility.Visible:Visibility.Collapsed;
            if(expanded&&narrow){commands.Children.Clear();Button("Abrir",voice?(Action)desktop.ShowAssistantInput:desktop.OpenActivity);progress.Visibility=Visibility.Collapsed;}
            double width=Math.Min(available,expanded?(State==IslandState.media?290:State==IslandState.notification||State==IslandState.error?196:220):State==IslandState.idle?80:108),height=expanded?(State==IslandState.media?176:State==IslandState.idle?92:136):24;
            Resize(width,height);System.Windows.Automation.AutomationProperties.SetName(compactButton,"Isla dinámica · "+State+" · "+heading);
        }
        private static string TimerText(WidgetSpec timer){if(timer==null)return "";double elapsed=timer.Seconds+(timer.Running?(DateTime.UtcNow-timer.StartedUtc).TotalSeconds:0);double seconds=timer.Kind=="Timer"?Math.Max(0,timer.TimerMinutes*60-elapsed):elapsed;return TimeSpan.FromSeconds(seconds).ToString(@"hh\:mm\:ss");}
        private void Resize(double width,double height)
        {
            if(Math.Abs(targetWidth-width)<.1&&Math.Abs(targetHeight-height)<.1)return;targetWidth=width;targetHeight=height;
            Animate(FrameworkElement.WidthProperty,width);Animate(FrameworkElement.HeightProperty,height);body.Opacity=1;
        }
        private void Animate(DependencyProperty property,double target){double current=(double)plate.GetValue(property);plate.BeginAnimation(property,null);plate.SetValue(property,target);if(!Motion.Enabled)return;var animation=new DoubleAnimation(current,target,TimeSpan.FromMilliseconds(240)){EasingFunction=new CubicEase{EasingMode=EasingMode.EaseOut},FillBehavior=FillBehavior.Stop};plate.BeginAnimation(property,animation);}
        internal void Hide(){compactWave.Set(false);hover.Stop();expanded=false;shelfMode=false;autoExpanded=false;noticeKey=null;plate.BeginAnimation(FrameworkElement.WidthProperty,null);plate.BeginAnimation(FrameworkElement.HeightProperty,null);plate.Height=24;plate.Visibility=Visibility.Collapsed;if(host!=null)host.RefreshRegion();}
        public void Dispose(){if(disposed)return;disposed=true;compactWave.Dispose();hover.Stop();desktop.IslandState.Changed-=Refresh;if(host!=null)host.Unmount(plate);plate.BeginAnimation(FrameworkElement.WidthProperty,null);plate.BeginAnimation(FrameworkElement.HeightProperty,null);}
    }
}

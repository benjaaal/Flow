using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
namespace MacFlow
{
    // A quiet black capsule, rather than a scaled-down dialog. Idle has no
    // invented task. Media and timers use their actual Windows/local providers.
    internal sealed class ActivityIsland : IDisposable
    {
        private readonly Desktop desktop;private readonly IMediaSource media;private readonly Window window,anchor;private readonly bool preview;
        private readonly Border plate,compactIcon,art;private readonly TextBlock compactTitle,title,subtitle;private readonly StackPanel commands;private readonly Grid compact,expandedBody;
        private readonly Grid assistantLayer;private readonly AssistantVisual assistantVisual;private readonly TextBlock assistantText;private readonly StackPanel assistantActions;
        private readonly DispatcherTimer flashTimer;private MediaState state=new MediaState();private WidgetSpec timer;private bool focus,disposed,busy,pending,expanded,hiding;private int generation;private string flash,flashSymbol,actionSignature;
        internal Window View{get{return window;}}internal bool Expanded{get{return expanded;}}
        internal ActivityIsland(Desktop controller,IMediaSource provider,bool preview=false,Window anchor=null)
        {
            desktop=controller;media=provider;this.preview=preview;this.anchor=anchor??controller.TopBar;expanded=preview;
            window=new Window{Title="MacFlow · Actividad en vivo",WindowStyle=WindowStyle.None,ResizeMode=ResizeMode.NoResize,Background=Brushes.Black,ShowInTaskbar=false,ShowActivated=false,Topmost=true,Width=preview?366:126,Height=preview?174:30};
            window.Resources["GlassInk"]=Brushes.White;window.Resources["GlassMuted"]=Design.Brush("#96969E");
            var layers=new Grid();compact=new Grid{Margin=new Thickness(12,0,12,0)};compact.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(22)});compact.ColumnDefinitions.Add(new ColumnDefinition());compact.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(18)});
            compactIcon=new Border{Width=18,Height=18,VerticalAlignment=VerticalAlignment.Center};compact.Children.Add(compactIcon);compactTitle=Label("",11,true);compactTitle.Margin=new Thickness(7,0,5,0);Grid.SetColumn(compactTitle,1);compact.Children.Add(compactTitle);var dot=new Border{Width=4,Height=4,CornerRadius=new CornerRadius(2),Background=Design.Brush("#5CD8A0"),VerticalAlignment=VerticalAlignment.Center,HorizontalAlignment=HorizontalAlignment.Right};Grid.SetColumn(dot,2);compact.Children.Add(dot);
            var compactButton=Design.Button("",Toggle,false);compactButton.Padding=compactButton.Margin=new Thickness(0);compactButton.Background=Brushes.Transparent;compactButton.HorizontalContentAlignment=HorizontalAlignment.Stretch;compactButton.VerticalContentAlignment=VerticalAlignment.Stretch;compactButton.Content=compact;System.Windows.Automation.AutomationProperties.SetName(compactButton,"Expandir actividad en vivo");layers.Children.Add(compactButton);
            expandedBody=new Grid{Margin=new Thickness(20,18,20,12)};expandedBody.RowDefinitions.Add(new RowDefinition{Height=new GridLength(64)});expandedBody.RowDefinitions.Add(new RowDefinition());
            var info=new Grid();info.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(58)});info.ColumnDefinitions.Add(new ColumnDefinition());info.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(26)});
            art=new Border{Width=46,Height=46,CornerRadius=new CornerRadius(13),Background=Design.Brush("#202023"),HorizontalAlignment=HorizontalAlignment.Left};info.Children.Add(art);var text=new StackPanel{VerticalAlignment=VerticalAlignment.Center};title=Label("",16,true);subtitle=Label("",12,false);subtitle.Margin=new Thickness(0,6,0,0);subtitle.Foreground=Design.Brush("#A5A5AD");text.Children.Add(title);text.Children.Add(subtitle);Grid.SetColumn(text,1);info.Children.Add(text);
            var remove=Control("close","Quitar isla dinámica",delegate{Remove();},26);Grid.SetColumn(remove,2);remove.VerticalAlignment=VerticalAlignment.Top;info.Children.Add(remove);expandedBody.Children.Add(info);
            commands=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center};Grid.SetRow(commands,1);expandedBody.Children.Add(commands);layers.Children.Add(expandedBody);
            assistantLayer=new Grid{Visibility=Visibility.Collapsed};assistantVisual=new AssistantVisual();assistantLayer.Children.Add(assistantVisual);assistantText=Label("",12,false);assistantText.Margin=new Thickness(59,0,64,0);assistantText.VerticalAlignment=VerticalAlignment.Center;assistantLayer.Children.Add(assistantText);assistantActions=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(0,0,9,0)};assistantActions.Children.Add(Control("keyboard","Escribir una orden",desktop.ShowAssistantInput,25));assistantActions.Children.Add(Control("close","Detener y cerrar asistente",desktop.StopAssistantVoice,25));foreach(Button button in assistantActions.Children){button.Margin=new Thickness(1);button.Padding=new Thickness(6);}assistantLayer.Children.Add(assistantActions);assistantLayer.MouseLeftButtonUp+=delegate{desktop.ShowAssistantInput();};layers.Children.Add(assistantLayer);
            plate=new Border{CornerRadius=new CornerRadius(24),Background=Design.Brush("#FF030304"),Child=layers};window.Content=plate;
            window.SourceInitialized+=delegate{Native.NoActivate(window);Native.RemoveGlassFrame(window);Native.SetGlassClip(window,1);};window.SizeChanged+=delegate{if(!preview&&!hiding)CenterOnBar();Native.SetGlassClip(window,1);};
            window.KeyDown+=delegate(object s,System.Windows.Input.KeyEventArgs e){if(e.Key==System.Windows.Input.Key.Escape){expanded=false;Refresh();}};
            flashTimer=new DispatcherTimer(DispatcherPriority.Background){Interval=TimeSpan.FromSeconds(2.2)};flashTimer.Tick+=delegate{flashTimer.Stop();flash=null;Refresh();};media.Changed+=MediaChanged;if(preview)window.Loaded+=delegate{ReadMedia();};else ReadMedia();
        }
        private static TextBlock Label(string value,double size,bool bold){var text=Design.Text(value,size,bold);text.Foreground=Brushes.White;text.TextTrimming=TextTrimming.CharacterEllipsis;return text;}
        private static Button Control(string symbol,string name,Action action,double size=40)
        {var button=Design.Button("",action,false);button.Width=button.Height=size;button.Margin=new Thickness(9,0,9,0);button.Padding=new Thickness(7);button.Background=Design.Brush("#121212");button.Foreground=Brushes.White;button.Content=Design.Symbol(symbol,size>30?23:14,Brushes.White);button.ToolTip=name;System.Windows.Automation.AutomationProperties.SetName(button,name);return button;}
        private void CenterOnBar(){var bar=anchor;if(bar!=null)window.Left=bar.Left+(bar.Width-window.Width)/2;}
        private void MediaChanged(){if(disposed||window.Dispatcher.HasShutdownStarted)return;window.Dispatcher.BeginInvoke(new Action(ReadMedia));}
        private async void ReadMedia(){if(disposed)return;if(busy){pending=true;return;}busy=true;try{do{pending=false;try{state=await media.Read();}catch{state=new MediaState();}if(!disposed)Refresh();}while(pending&&!disposed);}finally{busy=false;}}
        internal void Flash(string text,string symbol){if(disposed)return;flash=text;flashSymbol=symbol;flashTimer.Stop();flashTimer.Start();Refresh();}
        internal void Toggle(){expanded=!expanded;if(!preview&&!desktop.IsTesting)SoundCue.Play("open");Refresh();}
        internal void Remove(){desktop.SetIslandEnabled(false);Hide();}
        internal void Refresh()
        {
            if(disposed)return;bool assistantActive=desktop.AssistantOn(anchor);if(!preview&&(!desktop.ShellVisible||!desktop.FeedbackAllowedOn(anchor)&&!assistantActive||desktop.Preferences.ShowActivityIsland==false&&!assistantActive||!desktop.Preferences.ShowTopBar&&!assistantActive)){Hide();return;}
            bool voice=assistantActive&&(desktop.AssistantListening||desktop.AssistantSpeaking);timer=desktop.Widgets==null?null:desktop.Widgets.State.Items.FirstOrDefault(t=>(t.Kind=="Timer"||t.Kind=="Stopwatch")&&t.Running);focus=timer==null&&desktop.Focus.Session.State=="Running";bool active=voice||flash!=null||timer!=null||focus||state.Playing||(expanded&&state.CanToggle);
            string symbol=voice?"mic":flash!=null?flashSymbol:timer!=null?"timer":focus?"moon":state.CanToggle?"music":"apple";
            string current=voice?(desktop.AssistantListening?"Escuchando…":"Leyendo en voz alta…"):flash??(timer!=null?Format(timer.Kind=="Timer"?timer.Remaining(DateTime.UtcNow):timer.Elapsed(DateTime.UtcNow)):focus?Format(desktop.Focus.Session.Remaining(DateTime.UtcNow)):state.CanToggle?state.Title:"Sin actividades en curso");
            compactIcon.Child=active?Design.Symbol(symbol,18,Design.Brush(timer!=null||focus?"#FFA62B":"#F0F0F5")):null;compactTitle.Text=active?current:"";compact.Visibility=expanded||!active?Visibility.Collapsed:Visibility.Visible;expandedBody.Visibility=expanded?Visibility.Visible:Visibility.Collapsed;
            title.Text=voice?"Asistente de MacFlow":timer!=null?WidgetCatalog.Name(timer.Kind):focus?"Concentración":flash!=null?"MacFlow":state.CanToggle?state.Title:"Tu actividad";subtitle.Text=voice||timer!=null||focus?current:flash??(state.CanToggle?state.Artist:"No hay música ni temporizadores activos");
            art.Child=Design.Symbol(symbol,26,Design.Brush(timer!=null||focus?"#FFA62B":"#E8E8EF"));
            string signature=voice+":"+expanded+":"+(flash!=null)+":"+(timer==null?"":timer.Id)+":"+focus+":"+state.Playing+":"+state.CanToggle+":"+state.CanPrevious+":"+state.CanNext;
            if(signature!=actionSignature){actionSignature=signature;commands.Children.Clear();if(expanded){if(voice){commands.Children.Add(Control("close","Detener voz",desktop.StopAssistantVoice));}else if(timer!=null||focus){commands.Children.Add(Control("pause","Pausar",delegate{if(timer!=null){timer.Toggle(DateTime.UtcNow);desktop.Widgets.Save();}else desktop.Focus.Toggle();Refresh();}));commands.Children.Add(Control("grid","Abrir actividad",delegate{if(timer!=null)desktop.OpenWidgets(timer.Id);else desktop.OpenFocus();}));}else if(flash==null&&state.CanToggle){var previous=Control("previous","Anterior",delegate{Command(MediaCommand.Previous);});previous.IsEnabled=state.CanPrevious;commands.Children.Add(previous);commands.Children.Add(Control(state.Playing?"pause":"play",state.Playing?"Pausar":"Reproducir",delegate{Command(MediaCommand.Toggle);}));var next=Control("next","Siguiente",delegate{Command(MediaCommand.Next);});next.IsEnabled=state.CanNext;commands.Children.Add(next);}else{commands.Children.Add(Control("timer","Iniciar concentración",desktop.OpenFocus));commands.Children.Add(Control("control","Centro de control",desktop.OpenControl));}commands.Children.Add(Control("up","Contraer actividad",delegate{expanded=false;Refresh();}));}}
            assistantLayer.Visibility=assistantActive?Visibility.Visible:Visibility.Collapsed;assistantVisual.Set(assistantActive?desktop.AssistantMode:AssistantStage.Hidden);if(assistantActive){compact.Visibility=expandedBody.Visibility=Visibility.Collapsed;assistantText.Text=desktop.AssistantMessage??"";bool appearance=desktop.AssistantMode==AssistantStage.Appearance;assistantText.Visibility=assistantActions.Visibility=appearance?Visibility.Collapsed:Visibility.Visible;assistantText.Margin=new Thickness(desktop.AssistantMode==AssistantStage.Answer?18:59,0,64,0);plate.CornerRadius=new CornerRadius(appearance?44:30);}else plate.CornerRadius=new CornerRadius(24);
            ++generation;hiding=false;window.BeginAnimation(Window.TopProperty,null);window.BeginAnimation(UIElement.OpacityProperty,null);window.Opacity=1;
            if(!preview){window.Top=anchor.Top+4;CenterOnBar();}
            if(!window.IsVisible){window.Show();if(!preview){var area=DisplaySpace.For(anchor);DisplaySpace.Place(window,area,(area.Width-window.Width)/2,7);}Motion.Fade(window,0,1,220);}
            bool lens=assistantActive&&desktop.AssistantMode==AssistantStage.Appearance;Animate(Window.WidthProperty,assistantActive?(lens?84:366):expanded?366:active?190:126);Animate(Window.HeightProperty,assistantActive?(lens?84:58):expanded?174:32);Native.SetGlassClip(window,1);window.ToolTip=assistantActive?desktop.AssistantMessage:active?current:"Actividad en vivo · clic para abrir";
        }
        private static string Format(double seconds){var value=TimeSpan.FromSeconds(Math.Max(0,Math.Ceiling(seconds)));return value.TotalHours>=1?((int)value.TotalHours).ToString("00")+value.ToString(@"\:mm\:ss"):value.ToString(@"mm\:ss");}
        private async void Command(MediaCommand command){try{await media.Command(command);}catch{}if(!disposed)ReadMedia();}
        private void Animate(DependencyProperty property,double value){double previous=(double)window.GetValue(property);if(Math.Abs(previous-value)<1)return;window.BeginAnimation(property,null);window.SetValue(property,value);if(!Motion.Enabled)return;var animation=new DoubleAnimation(previous,value,TimeSpan.FromMilliseconds(380)){EasingFunction=new SpringEase{EasingMode=EasingMode.EaseIn},FillBehavior=FillBehavior.Stop};Timeline.SetDesiredFrameRate(animation,60);window.BeginAnimation(property,animation);}
        internal void Hide()
        {
            assistantVisual.Set(AssistantStage.Hidden);if(!window.IsVisible||hiding)return;expanded=false;hiding=true;int token=++generation;if(!Motion.Enabled){window.Hide();hiding=false;return;}
            double top=window.Top,end=(anchor==null?0:DisplaySpace.UpperEdge(anchor))-window.Height-8;var slide=new DoubleAnimationUsingKeyFrames{FillBehavior=FillBehavior.Stop};slide.KeyFrames.Add(new EasingDoubleKeyFrame(top,KeyTime.FromTimeSpan(TimeSpan.Zero)));slide.KeyFrames.Add(new EasingDoubleKeyFrame(top+4,KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(65)),Motion.Ease()));slide.KeyFrames.Add(new EasingDoubleKeyFrame(end-4,KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(310)),new CubicEase{EasingMode=EasingMode.EaseInOut}));slide.KeyFrames.Add(new EasingDoubleKeyFrame(end,KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(370)),Motion.Ease()));Timeline.SetDesiredFrameRate(slide,60);slide.Completed+=delegate{if(!disposed&&token==generation){window.Hide();hiding=false;}};window.BeginAnimation(Window.TopProperty,slide);Motion.Fade(window,window.Opacity,0,350);
        }
        public void Dispose(){if(disposed)return;disposed=true;flashTimer.Stop();assistantVisual.Dispose();media.Changed-=MediaChanged;media.Dispose();window.Close();}
    }
}

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
    // Event-driven media plus existing timer ticks. Nothing is shown for an
    // invented task, recording or device. The capsule is intentionally black.
    internal sealed class ActivityIsland : IDisposable
    {
        private readonly Desktop desktop;private readonly IMediaSource media;private readonly Window window;private readonly TextBlock label,detail;private readonly Border icon;private readonly StackPanel actions;private readonly DispatcherTimer flashTimer;private readonly bool preview;private string actionSignature;
        private MediaState state=new MediaState();private bool disposed,busy,pending,expanded,hiding;private int generation;private string flash,flashSymbol;private WidgetSpec timer;private bool focus;private readonly Button remove;
        internal Window View{get{return window;}}internal bool Expanded{get{return expanded;}}
        internal ActivityIsland(Desktop desktop,IMediaSource media,bool preview=false)
        {
            this.desktop=desktop;this.media=media;this.preview=preview;expanded=preview;window=new Window{Title="MacFlow · Actividad en vivo",WindowStyle=WindowStyle.None,ResizeMode=ResizeMode.NoResize,Background=Brushes.Black,ShowInTaskbar=false,ShowActivated=false,Topmost=true,Width=preview?354:244,Height=preview?145:34};
            window.Resources["GlassInk"]=Brushes.White;window.Resources["GlassMuted"]=Design.Brush("#BBC0CC");
            var root=new DockPanel{Margin=new Thickness(13,0,13,0)};var head=new DockPanel{Height=34};icon=new Border{Width=20,Margin=new Thickness(0,0,10,0),VerticalAlignment=VerticalAlignment.Center};head.Children.Add(icon);remove=Design.Button("×",Remove,false);remove.Width=24;remove.Height=24;remove.Padding=new Thickness(0);remove.Margin=new Thickness(4,0,0,0);remove.Background=Brushes.Transparent;remove.Foreground=Brushes.White;remove.ToolTip="Quitar isla · Reactivar desde Configuración → Barra superior";System.Windows.Automation.AutomationProperties.SetName(remove,"Quitar isla dinámica");DockPanel.SetDock(remove,Dock.Right);head.Children.Add(remove);label=Design.Text("",12,true);label.Foreground=Brushes.White;label.TextTrimming=TextTrimming.CharacterEllipsis;label.VerticalAlignment=VerticalAlignment.Center;head.Children.Add(label);
            var toggle=Design.Button("",Toggle,false);toggle.Content=head;toggle.Background=Brushes.Transparent;toggle.BorderThickness=new Thickness(0);toggle.Padding=new Thickness(0);toggle.Margin=new Thickness(0);toggle.HorizontalContentAlignment=HorizontalAlignment.Stretch;System.Windows.Automation.AutomationProperties.SetName(toggle,"Expandir actividad en vivo");head.Children.Remove(remove);remove.VerticalAlignment=VerticalAlignment.Center;var headerBar=new DockPanel{Height=34};DockPanel.SetDock(remove,Dock.Right);headerBar.Children.Add(remove);headerBar.Children.Add(toggle);DockPanel.SetDock(headerBar,Dock.Top);root.Children.Add(headerBar);
            var body=new StackPanel();detail=Design.Text("",13,false);detail.Foreground=Design.Brush("#D3D5DB");detail.TextWrapping=TextWrapping.Wrap;detail.Margin=new Thickness(0,8,0,10);body.Children.Add(detail);actions=new StackPanel{Orientation=Orientation.Horizontal};body.Children.Add(actions);root.Children.Add(body);window.Content=new Border{CornerRadius=new CornerRadius(24),Background=Design.Brush("#FF050506"),Child=root};
            window.SourceInitialized+=delegate{Native.NoActivate(window);Native.RemoveGlassFrame(window);Native.SetGlassClip(window,1);};window.SizeChanged+=delegate{if(!preview)window.Left=SystemParameters.PrimaryScreenWidth/2-window.Width/2;Native.SetGlassClip(window,1);};window.KeyDown+=delegate(object s,System.Windows.Input.KeyEventArgs e){if(e.Key==System.Windows.Input.Key.Escape){expanded=false;Refresh();}};
            flashTimer=new DispatcherTimer(DispatcherPriority.Background){Interval=TimeSpan.FromSeconds(2.2)};flashTimer.Tick+=delegate{flashTimer.Stop();flash=null;Refresh();};media.Changed+=MediaChanged;if(preview)window.Loaded+=delegate{ReadMedia();};else ReadMedia();
        }
        private void MediaChanged(){if(disposed||window.Dispatcher.HasShutdownStarted)return;window.Dispatcher.BeginInvoke(new Action(ReadMedia));}
        private async void ReadMedia()
        {
            if(disposed)return;if(busy){pending=true;return;}busy=true;
            try{do{pending=false;try{state=await media.Read();}catch{state=new MediaState();}if(!disposed)Refresh();}while(pending&&!disposed);}finally{busy=false;}
        }
        internal void Flash(string text,string symbol){if(disposed)return;flash=text;flashSymbol=symbol;flashTimer.Stop();flashTimer.Start();Refresh();}
        internal void Toggle(){expanded=!expanded;Refresh();}
        internal void Remove(){desktop.Preferences.ShowActivityIsland=false;desktop.Save();Hide();}
        internal void Refresh()
        {
            if(disposed)return;timer=desktop.Widgets==null?null:desktop.Widgets.State.Items.FirstOrDefault(t=>(t.Kind=="Timer"||t.Kind=="Stopwatch")&&t.Running);focus=timer==null&&desktop.Focus.Session.State=="Running";
            bool active=flash!=null||timer!=null||focus||state.Playing||(expanded&&state.CanToggle);
            if(!active||(!preview&&(!desktop.LiveFeedbackAllowed||desktop.Preferences.ShowActivityIsland==false||!desktop.Preferences.ShowTopBar))){Hide();return;}
            string symbol=flash!=null?flashSymbol:timer!=null?"performance":focus?"moon":"play";
            icon.Child=Design.Symbol(symbol,18,Design.Brush(timer!=null||focus?"#FFB23F":"#EAEAFF"));
            string text=flash??(timer!=null?(timer.Kind=="Timer"?"Temporizador · ":"Cronómetro · ")+Format(timer.Kind=="Timer"?timer.Remaining(DateTime.UtcNow):timer.Elapsed(DateTime.UtcNow)):focus?"Concentración · "+Format(desktop.Focus.Session.Remaining(DateTime.UtcNow)):state.Title);
            if(label.Text!=text)label.Text=text;detail.Text=flash??(timer!=null?WidgetCatalog.Name(timer.Kind):focus?desktop.Focus.Session.Label:state.Artist);
            string signature=(flash!=null)+":"+expanded+":"+(timer==null?"":timer.Id)+":"+focus+":"+state.Playing+":"+state.CanToggle+":"+state.CanPrevious+":"+state.CanNext;if(signature!=actionSignature){actionSignature=signature;actions.Children.Clear();if(expanded){if(flash!=null){actions.Children.Add(ActionButton(flashSymbol=="grid"?"Ver ventanas":"Centro de control",flashSymbol=="grid"?(Action)desktop.OpenOverview:desktop.OpenControl));}else if(timer!=null||focus){actions.Children.Add(ActionButton("Pausar",delegate{if(timer!=null){timer.Toggle(DateTime.UtcNow);desktop.Widgets.Save();}else desktop.Focus.Toggle();expanded=false;Refresh();}));actions.Children.Add(ActionButton("Abrir",delegate{if(timer!=null)desktop.OpenWidgets(timer.Id);else desktop.OpenFocus();}));}else if(state.CanToggle){actions.Children.Add(ActionButton("Anterior",delegate{Command(MediaCommand.Previous);},state.CanPrevious));actions.Children.Add(ActionButton(state.Playing?"Pausar":"Reproducir",delegate{Command(MediaCommand.Toggle);}));actions.Children.Add(ActionButton("Siguiente",delegate{Command(MediaCommand.Next);},state.CanNext));}else actions.Children.Add(ActionButton("Centro de control",desktop.OpenControl));}}
            remove.Visibility=expanded?Visibility.Visible:Visibility.Collapsed;++generation;hiding=false;window.BeginAnimation(Window.TopProperty,null);if(!preview)window.Top=desktop.TopBar.Top+3;window.BeginAnimation(UIElement.OpacityProperty,null);window.Opacity=1;
            if(!window.IsVisible){window.Top=desktop.TopBar.Top+3;window.Show();Motion.Fade(window,0,1,220);}
            Resize(expanded?354:244,expanded?145:34);window.ToolTip=text;
        }
        private static string Format(double seconds){var value=TimeSpan.FromSeconds(Math.Max(0,Math.Ceiling(seconds)));return value.TotalHours>=1?((int)value.TotalHours).ToString("00")+value.ToString(@"\:mm\:ss"):value.ToString(@"mm\:ss");}
        private Button ActionButton(string text,Action action,bool enabled=true){var b=Design.Button(text,action,false);b.Foreground=Brushes.White;b.Background=Design.Brush("#202025");b.Padding=new Thickness(11,7,11,7);b.FontSize=11;b.IsEnabled=enabled;return b;}
        private async void Command(MediaCommand command){try{await media.Command(command);}catch{}if(!disposed)ReadMedia();}
        private void Resize(double width,double height){Animate(Window.WidthProperty,width);Animate(Window.HeightProperty,height);}
        private void Animate(DependencyProperty property,double value){double previous=(double)window.GetValue(property);if(Math.Abs(previous-value)<1)return;window.BeginAnimation(property,null);window.SetValue(property,value);if(!Motion.Enabled)return;var animation=new DoubleAnimation(previous,value,TimeSpan.FromMilliseconds(360)){EasingFunction=new SpringEase{EasingMode=EasingMode.EaseIn},FillBehavior=FillBehavior.Stop};Timeline.SetDesiredFrameRate(animation,60);window.BeginAnimation(property,animation);}
        internal void Hide()
        {
            if(!window.IsVisible||hiding)return;expanded=false;hiding=true;int token=++generation;
            if(!Motion.Enabled){window.Hide();hiding=false;return;}
            double top=window.Top;var slide=new DoubleAnimationUsingKeyFrames{FillBehavior=FillBehavior.Stop};
            slide.KeyFrames.Add(new EasingDoubleKeyFrame(top,KeyTime.FromTimeSpan(TimeSpan.Zero)));
            slide.KeyFrames.Add(new EasingDoubleKeyFrame(top+3,KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(65)),Motion.Ease()));
            slide.KeyFrames.Add(new EasingDoubleKeyFrame(-window.Height-10,KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(300)),new CubicEase{EasingMode=EasingMode.EaseInOut}));
            slide.KeyFrames.Add(new EasingDoubleKeyFrame(-window.Height-4,KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(360)),Motion.Ease()));
            Timeline.SetDesiredFrameRate(slide,60);slide.Completed+=delegate{if(!disposed&&token==generation){window.Hide();hiding=false;}};window.BeginAnimation(Window.TopProperty,slide);Motion.Fade(window,window.Opacity,0,340);
        }
        public void Dispose(){if(disposed)return;disposed=true;flashTimer.Stop();media.Changed-=MediaChanged;media.Dispose();window.Close();}
    }
}

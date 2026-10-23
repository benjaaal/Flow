using System;
using System.Windows.Media.Animation;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
namespace MacFlow
{
    internal sealed class PrivacyProfile
    {
        private readonly string path;
        internal string Salt,Hash,Face;
        internal int Failures;internal DateTime RetryUtc;
        internal PrivacyProfile(string directory)
        {
            path=Path.Combine(directory,"privacy.dat");
            try{if(File.Exists(path)){var lines=Encoding.UTF8.GetString(ProtectedData.Unprotect(File.ReadAllBytes(path),null,DataProtectionScope.CurrentUser)).Split('\n');if(lines.Length>=3){Salt=lines[0];Hash=lines[1];Face=lines[2];}if(lines.Length>=5){Int32.TryParse(lines[3],out Failures);long time;if(Int64.TryParse(lines[4],out time))RetryUtc=new DateTime(time,DateTimeKind.Utc);}}}catch(Exception ex){Log.Write("Perfil de privacidad no disponible: "+ex.GetType().Name);}
        }
        internal bool Configured{get{return !String.IsNullOrEmpty(Salt)&&!String.IsNullOrEmpty(Hash);}}
        internal static bool ValidPin(string pin){return pin!=null&&pin.Length>=6&&pin.Length<=12&&pin.All(c=>c>='0'&&c<='9');}
        private static string Derive(string pin,string salt){using(var kdf=new Rfc2898DeriveBytes(pin,Convert.FromBase64String(salt),150000))return Convert.ToBase64String(kdf.GetBytes(32));}
        internal void SetPin(string pin){if(!ValidPin(pin))throw new ArgumentException("Usá entre 6 y 12 números.");var salt=new byte[24];using(var random=RandomNumberGenerator.Create())random.GetBytes(salt);Salt=Convert.ToBase64String(salt);Hash=Derive(pin,Salt);Failures=0;RetryUtc=DateTime.MinValue;Save();}
        internal bool Verify(string pin)
        {if(!Configured||DateTime.UtcNow<RetryUtc)return false;var a=Convert.FromBase64String(Hash);var b=Convert.FromBase64String(Derive(pin,Salt));int mismatch=0;for(int i=0;i<a.Length;i++)mismatch|=a[i]^b[i];if(mismatch==0){Failures=0;RetryUtc=DateTime.MinValue;Save();return true;}if(++Failures>=5){RetryUtc=DateTime.UtcNow.AddSeconds(30);Failures=0;}Save();return false;}
        internal void Save(){Directory.CreateDirectory(Path.GetDirectoryName(path));var data=Encoding.UTF8.GetBytes(Salt+"\n"+Hash+"\n"+(Face??"")+"\n"+Failures+"\n"+RetryUtc.Ticks);var temp=path+".new";File.WriteAllBytes(temp,ProtectedData.Protect(data,null,DataProtectionScope.CurrentUser));if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}
    }

    // Recoverable privacy curtain; Windows Hello owns Windows authentication.
    internal sealed class PrivacyScreen : Window
    {
        private readonly Desktop desktop;private readonly PrivacyProfile profile;
        private readonly Image wallpaper=new Image{Stretch=Stretch.UniformToFill};
        private readonly Grid backgroundScene=new Grid();private readonly EarthLockScene earth;
        private readonly Grid cover,access;private Border musicFace;private StackPanel lower,notices;private Grid musicScene;private Border albumFace;private ImageSource normalWallpaper;private Image musicBackground,albumImage;private Border musicShade;private readonly DispatcherTimer sceneSettle=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(390)};private Action settleScene;private TextBlock musicDate;private bool musicExpanded;private readonly System.Collections.Generic.HashSet<string> dismissed=new System.Collections.Generic.HashSet<string>();private readonly System.Collections.Generic.Dictionary<string,LockNotice> noticeViews=new System.Collections.Generic.Dictionary<string,LockNotice>();private readonly TranslateTransform slide=new TranslateTransform();
        private readonly PrivacyClock time;private readonly TextBlock date,message;private readonly PasswordBox pin,confirmation;
        private readonly Button submit;private readonly MediaCard media;private readonly PlayerFooter playerFooter;
        private readonly DispatcherTimer clock=new DispatcherTimer{Interval=TimeSpan.FromSeconds(1)};
        private readonly System.Collections.Generic.List<Window> curtains=new System.Collections.Generic.List<Window>();
        private readonly bool notificationView;private readonly double quickTop;private int quickGeneration;private readonly DispatcherTimer quickFallback=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(600)};private readonly bool afterResume;private readonly ScrollViewer noticeScroll;private readonly Grid sceneRoot;private bool closed,allowed,working,leaving;private Point? drag;private double dragScreenY,lastDragDistance,dragVelocity;private long lastDragTime;
        internal bool CameraRunning{get{return false;}}
        internal bool AuthenticationVisible{get{return access.Visibility==Visibility.Visible;}}
        internal static bool AcceptSwipe(double start,double end,double height){return start>=height*.72&&start-end>=Math.Min(80,height*.12);}
        internal PrivacyScreen(Desktop owner,bool demonstration=false,IMediaSource provider=null,Preferences appearance=null,bool afterResume=false,bool notificationView=false)
        {
            this.notificationView=notificationView;this.afterResume=afterResume;appearance=appearance??owner.Preferences;appearance.Validate();desktop=owner;profile=new PrivacyProfile(owner.Store.DirectoryPath);Title=notificationView?"MacOS · Notificaciones y reloj":"MacOS · Privacidad";WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;ShowInTaskbar=false;Topmost=true;Background=Brushes.Black;
            var bounds=owner.IsTesting?new Rect(60,30,Math.Min(1100,SystemParameters.PrimaryScreenWidth-100),Math.Min(850,SystemParameters.PrimaryScreenHeight-60)):(notificationView?DisplaySpace.Current:DisplaySpace.Of(System.Windows.Forms.Screen.PrimaryScreen)).Dips;
            Left=bounds.Left;Top=bounds.Top;quickTop=bounds.Top;Width=bounds.Width;Height=bounds.Height;WindowLife.Track(this);
            Resources["GlassInk"]=Brushes.White;Resources["GlassMuted"]=Design.Brush("#CCFFFFFF");
            var root=new Grid{ClipToBounds=true};sceneRoot=root;wallpaper.Source=owner.PresentationWallpaper;backgroundScene.Children.Add(wallpaper);if(appearance.PrivacyAstronomy){earth=new EarthLockScene(DateTime.Now);backgroundScene.Children.Add(earth);}root.Children.Add(backgroundScene);root.Children.Add(new Border{Background=Design.Brush("#28000000"),IsHitTestVisible=false});
            cover=new Grid{RenderTransform=slide};cover.RowDefinitions.Add(new RowDefinition{Height=new GridLength(2,GridUnitType.Star)});cover.RowDefinitions.Add(new RowDefinition{Height=new GridLength(3,GridUnitType.Star)});cover.RowDefinitions.Add(new RowDefinition{Height=new GridLength(90)});
            var heading=new StackPanel{VerticalAlignment=VerticalAlignment.Top,HorizontalAlignment=HorizontalAlignment.Center};date=Label("",14,true);date.TextAlignment=TextAlignment.Center;heading.Children.Add(date);time=new PrivacyClock{Width=440,Height=120,Margin=new Thickness(0,4,0,0)};time.AttachWallpaper(backgroundScene);time.Appearance(appearance.PrivacyClockWeight,appearance.PrivacyClockColor);heading.Children.Add(time);cover.Children.Add(heading);
            lower=new StackPanel{Width=520,VerticalAlignment=VerticalAlignment.Bottom,Margin=new Thickness(0,0,0,26)};Grid.SetRow(lower,1);
            media=new MediaCard(provider??(owner.IsTesting?(IMediaSource)new MediaFixture():new WindowsMediaSource()));media.ArtworkToggleOnly=true;media.HideWhenEmpty=true;media.PrivacyLayout();playerFooter=new PlayerFooter(owner.IsTesting){Visibility=Visibility.Collapsed};var playerBody=new StackPanel();playerBody.Children.Add(media);playerBody.Children.Add(playerFooter);var musicCard=new Border{Width=360,HorizontalAlignment=HorizontalAlignment.Center,CornerRadius=new CornerRadius(18),Background=Brushes.Transparent,BorderThickness=new Thickness(0),Child=new WallpaperGlass(backgroundScene,playerBody,22,new Thickness(14),true)};musicFace=musicCard;musicCard.Visibility=Visibility.Collapsed;lower.Children.Add(musicCard);notices=new StackPanel{Margin=new Thickness(0,10,0,0)};noticeScroll=new ScrollViewer{Content=notices,MaxHeight=280,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};lower.Children.Add(noticeScroll);cover.Children.Add(lower);media.ExpandRequested+=delegate{ToggleMusic();};media.ArtworkPressed+=ToggleMusic;media.MediaChanged+=RefreshMedia;desktop.Activity.Changed+=RefreshNotices;RefreshNotices();
            var hint=Label(notificationView?"Deslizá hacia arriba para volver al escritorio":"Deslizá hacia arriba o presioná una tecla",11,false);hint.Opacity=.7;hint.HorizontalAlignment=HorizontalAlignment.Center;hint.VerticalAlignment=VerticalAlignment.Bottom;hint.Margin=new Thickness(0,0,0,26);Grid.SetRow(hint,2);cover.Children.Add(hint);root.Children.Add(cover);
            musicScene=new Grid{Visibility=Visibility.Collapsed,ClipToBounds=true};musicScene.RowDefinitions.Add(new RowDefinition{Height=new GridLength(48)});musicScene.RowDefinitions.Add(new RowDefinition());musicScene.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});musicDate=Label("",21,true);musicDate.HorizontalAlignment=HorizontalAlignment.Center;musicDate.VerticalAlignment=VerticalAlignment.Center;musicScene.Children.Add(musicDate);albumImage=new Image{Stretch=Stretch.UniformToFill};albumFace=new Border{CornerRadius=new CornerRadius(30),Child=albumImage,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,ClipToBounds=true,Cursor=Cursors.Hand};albumFace.Tag="MediaArtwork";albumFace.MouseLeftButtonUp+=delegate(object sender,MouseButtonEventArgs e){ToggleMusic();e.Handled=true;};albumFace.SizeChanged+=delegate{albumFace.Clip=new RectangleGeometry(new Rect(0,0,albumFace.ActualWidth,albumFace.ActualHeight),30,30);};Grid.SetRow(albumFace,1);musicScene.Children.Add(albumFace);musicBackground=new Image{Opacity=0,Stretch=Stretch.UniformToFill,IsHitTestVisible=false,Effect=new System.Windows.Media.Effects.BlurEffect{Radius=32,RenderingBias=System.Windows.Media.Effects.RenderingBias.Performance},CacheMode=new BitmapCache{RenderAtScale=.5}};Grid.SetRowSpan(musicBackground,3);Panel.SetZIndex(musicBackground,-2);musicScene.Children.Add(musicBackground);musicShade=new Border{Opacity=0,Background=new LinearGradientBrush(Color.FromArgb(60,0,0,0),Color.FromArgb(150,0,0,0),90),IsHitTestVisible=false};Grid.SetRowSpan(musicShade,3);Panel.SetZIndex(musicShade,-1);musicScene.Children.Add(musicShade);musicDate.Margin=new Thickness(0,18,0,0);root.Children.Add(musicScene);
            SizeChanged+=delegate{heading.Margin=new Thickness(0,Math.Max(32,ActualHeight*.095),0,0);time.Width=Math.Max(180,Math.Min(520,ActualWidth*.46))*appearance.PrivacyClockScale;time.Height=Math.Max(70,Math.Min(132,ActualHeight*.19))*appearance.PrivacyClockScale;lower.Width=Math.Max(280,Math.Min(560,ActualWidth-48));LayoutMusic();LayoutNotices();};
            access=new Grid{Visibility=Visibility.Collapsed,Opacity=0};access.Children.Add(new Border{Background=Design.Brush("#48000000")});var form=new StackPanel{Width=300,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center};
            form.Children.Add(new Border{Width=72,Height=72,CornerRadius=new CornerRadius(36),Background=Design.Brush("#30FFFFFF"),Child=new Viewbox{Margin=new Thickness(23),Child=Design.Symbol("lock",22,Brushes.White)},HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(0,0,0,18)});
            var account=Label(Environment.UserName,24,true);account.TextAlignment=TextAlignment.Center;form.Children.Add(account);
            message=Label(profile.Configured?"Ingresá tu PIN de MacOS":"Creá un PIN de MacOS",12,false);message.TextWrapping=TextWrapping.Wrap;message.TextAlignment=TextAlignment.Center;message.Margin=new Thickness(0,10,0,18);form.Children.Add(message);
            pin=Password("PIN de MacOS");confirmation=Password("Repetir PIN de MacOS");confirmation.Visibility=profile.Configured?Visibility.Collapsed:Visibility.Visible;form.Children.Add(pin);form.Children.Add(confirmation);
            submit=Design.Button(profile.Configured?"Desbloquear":"Guardar PIN",SubmitPin,true);submit.Height=36;submit.Margin=new Thickness(0,12,0,8);form.Children.Add(submit);
            var back=Design.Button("Volver",HideAccess,false);back.Background=Brushes.Transparent;back.Foreground=Brushes.White;form.Children.Add(back);var help=Label("PIN local · 6 a 12 números\nRecuperar MacOS: Ctrl + Windows + Alt",10,false);help.TextAlignment=TextAlignment.Center;help.Margin=new Thickness(0,20,0,0);form.Children.Add(help);access.Children.Add(form);root.Children.Add(access);
            if(notificationView){var backToDesktop=Design.Button("↑",delegate{CompleteQuick(false);},false);backToDesktop.Foreground=Brushes.White;backToDesktop.Background=Design.Brush("#26000000");backToDesktop.HorizontalAlignment=HorizontalAlignment.Left;backToDesktop.VerticalAlignment=VerticalAlignment.Top;backToDesktop.Width=backToDesktop.Height=34;backToDesktop.ToolTip="Volver al escritorio";backToDesktop.Margin=new Thickness(24,22,0,0);System.Windows.Automation.AutomationProperties.SetName(backToDesktop,"Cerrar notificaciones y volver al escritorio");root.Children.Add(backToDesktop);}
            Content=root;quickFallback.Tick+=delegate{quickFallback.Stop();if(notificationView&&leaving&&!closed){allowed=true;Close();}};
            if(owner.IsTesting){ShowActivated=false;IsHitTestVisible=false;SourceInitialized+=delegate{Native.NoActivate(this);};}
            SourceInitialized+=delegate{Native.OpaquePresentation(this);};Loaded+=async delegate{Tick();clock.Start();if(earth!=null)earth.Enter();if(!owner.IsTesting&&!demonstration&&!notificationView){var physical=System.Windows.Forms.Screen.PrimaryScreen.Bounds;Native.SetWindowPos(new System.Windows.Interop.WindowInteropHelper(this).Handle,new IntPtr(-1),physical.Left,physical.Top,physical.Width,physical.Height,0x40);CoverOtherDisplays();}if(wallpaper.Source==null||!String.IsNullOrWhiteSpace(appearance.PrivacyWallpaperPath)){var image=await WelcomeWallpaper.Load(String.IsNullOrWhiteSpace(appearance.PrivacyWallpaperPath)?appearance.WallpaperPath:appearance.PrivacyWallpaperPath);if(!closed){if(musicExpanded)normalWallpaper=image;else wallpaper.Source=image;}}};clock.Tick+=delegate{Tick();};sceneSettle.Tick+=delegate{sceneSettle.Stop();var action=settleScene;settleScene=null;if(!closed&&action!=null)action();};
            PreviewMouseLeftButtonDown+=delegate(object s,MouseButtonEventArgs e){var point=e.GetPosition(this);if(!AuthenticationVisible&&!Interactive(e.OriginalSource as DependencyObject)&&point.Y>=ActualHeight*.72){BeginDrag(point,PointToScreen(point).Y);CaptureMouse();e.Handled=true;}};
            PreviewMouseMove+=delegate(object s,MouseEventArgs e){if(drag.HasValue&&IsMouseCaptured){MoveDrag(PointToScreen(e.GetPosition(this)).Y);}};
            PreviewMouseLeftButtonUp+=delegate(object s,MouseButtonEventArgs e){if(drag.HasValue){bool accept=notificationView?QuickShouldClose(lastDragDistance,dragVelocity,ActualHeight):AcceptSwipe(drag.Value.Y,e.GetPosition(this).Y,ActualHeight);drag=null;ReleaseMouseCapture();if(notificationView)CompleteQuick(!accept,-dragVelocity);else if(accept)RevealAccess();else ResetCover();e.Handled=true;}};
            PreviewTouchDown+=delegate(object s,TouchEventArgs e){var point=e.GetTouchPoint(this).Position;if(!AuthenticationVisible&&!Interactive(e.OriginalSource as DependencyObject)&&point.Y>=ActualHeight*.72){BeginDrag(point,PointToScreen(point).Y);CaptureTouch(e.TouchDevice);e.Handled=true;}};
            PreviewTouchMove+=delegate(object s,TouchEventArgs e){if(drag.HasValue){MoveDrag(PointToScreen(e.GetTouchPoint(this).Position).Y);e.Handled=true;}};
            PreviewTouchUp+=delegate(object s,TouchEventArgs e){if(drag.HasValue){bool accept=notificationView?QuickShouldClose(lastDragDistance,dragVelocity,ActualHeight):AcceptSwipe(drag.Value.Y,e.GetTouchPoint(this).Position.Y,ActualHeight);drag=null;ReleaseTouchCapture(e.TouchDevice);if(notificationView)CompleteQuick(!accept,-dragVelocity);else if(accept)RevealAccess();else ResetCover();e.Handled=true;}};
            LostMouseCapture+=delegate{if(drag.HasValue){drag=null;ResetCover();}};LostTouchCapture+=delegate{if(drag.HasValue){drag=null;ResetCover();}};
            PreviewKeyDown+=delegate(object s,KeyEventArgs e){if(e.Key==Key.Escape&&notificationView){CompleteQuick(false);e.Handled=true;}else if(e.Key==Key.Escape&&demonstration){Recover();e.Handled=true;}else if(e.Key==Key.Escape&&AuthenticationVisible){HideAccess();e.Handled=true;}else if(e.Key==Key.Enter){if(AuthenticationVisible)SubmitPin();else RevealAccess();e.Handled=true;}else if(!AuthenticationVisible&&(e.Key==Key.Space||e.Key==Key.Up)){RevealAccess();e.Handled=true;}};
            Closing+=delegate(object s,System.ComponentModel.CancelEventArgs e){if(!allowed&&!demonstration&&!owner.IsTesting&&!afterResume&&!notificationView)e.Cancel=true;};Closed+=delegate{closed=true;quickFallback.Stop();sceneSettle.Stop();settleScene=null;clock.Stop();if(earth!=null)earth.Stop();desktop.Activity.Changed-=RefreshNotices;media.Dispose();playerFooter.Dispose();foreach(var curtain in curtains)WindowLife.Close(curtain);curtains.Clear();};
        }
        private void BeginDrag(Point point,double screenY)
        {drag=point;dragScreenY=screenY;lastDragDistance=dragVelocity=0;lastDragTime=Stopwatch.GetTimestamp();}
        private void MoveDrag(double screenY)
        {
            var source=PresentationSource.FromVisual(this);double dpi=source==null?1:source.CompositionTarget.TransformToDevice.M22;
            double distance=Math.Max(0,(dragScreenY-screenY)/dpi);long now=Stopwatch.GetTimestamp();double dt=(now-lastDragTime)/(double)Stopwatch.Frequency;
            if(dt>.001){double measured=(distance-lastDragDistance)/dt;dragVelocity=dragVelocity*.3+Math.Max(-5000,Math.Min(5000,measured))*.7;lastDragTime=now;lastDragDistance=distance;}
            if(notificationView){PullQuick(1-distance/Math.Max(1,Height));return;}
            slide.BeginAnimation(TranslateTransform.YProperty,null);slide.Y=-distance;if(earth!=null)earth.Gesture(distance/Math.Max(1,ActualHeight*.5));cover.Opacity=Math.Max(.2,1-distance/Math.Max(1,ActualHeight*.65));
        }
        internal static bool QuickShouldClose(double distance,double velocity,double height)
        {return velocity>=650&&distance>14||velocity>=-250&&distance>=Math.Min(100,height*.18);}
        internal double QuickCornerRadius{get;private set;}
        protected override void OnLocationChanged(EventArgs e)
        {
            base.OnLocationChanged(e);if(!notificationView||sceneRoot==null||!IsLoaded)return;
            double remaining=Math.Max(0,Math.Min(1,(quickTop-Top)/Math.Max(1,Height)));
            QuickCornerRadius=Math.Min(40,remaining*Height*.42);
            Native.SetPresentationClip(this,QuickCornerRadius);
        }
        internal bool NotificationView{get{return notificationView;}}
        internal void PullQuick(double progress)
        {if(!notificationView||closed)return;quickGeneration++;quickFallback.Stop();leaving=false;double top=quickTop-Height*(1-Math.Max(0,Math.Min(1,progress)));BeginAnimation(TopProperty,null);Top=top;}
        internal void CompleteQuick(bool show,double velocity=0)
        {
            if(!notificationView||closed)return;int token=++quickGeneration;leaving=!show;quickFallback.Stop();
            if(!show)quickFallback.Start();Motion.To(this,TopProperty,show?quickTop:quickTop-Height,show?400:420,false,delegate{if(!closed&&token==quickGeneration&&!show){quickFallback.Stop();allowed=true;Close();}},velocity);
        }
        private static TextBlock Label(string text,double size,bool bold){var result=Design.Text(text,size,bold);result.Foreground=Brushes.White;return result;}
        private static PasswordBox Password(string name)
        {
            var field=new PasswordBox{MaxLength=12,FontSize=20,Height=44,Padding=new Thickness(14,9,14,9),Margin=new Thickness(0,5,0,0),HorizontalContentAlignment=HorizontalAlignment.Center,Foreground=Brushes.White,Background=Design.Brush("#32FFFFFF"),BorderBrush=Design.Brush("#50FFFFFF"),BorderThickness=new Thickness(.6)};
            var template=new ControlTemplate(typeof(PasswordBox));var border=new FrameworkElementFactory(typeof(Border));border.SetValue(Border.CornerRadiusProperty,new CornerRadius(14));border.SetValue(Border.BackgroundProperty,new TemplateBindingExtension(Control.BackgroundProperty));border.SetValue(Border.BorderBrushProperty,new TemplateBindingExtension(Control.BorderBrushProperty));border.SetValue(Border.BorderThicknessProperty,new TemplateBindingExtension(Control.BorderThicknessProperty));border.SetValue(Border.PaddingProperty,new TemplateBindingExtension(Control.PaddingProperty));var host=new FrameworkElementFactory(typeof(ScrollViewer));host.Name="PART_ContentHost";border.AppendChild(host);template.VisualTree=border;field.Template=template;System.Windows.Automation.AutomationProperties.SetName(field,name);return field;
        }
        internal bool ClockRefractive{get{return time.Refractive;}}
        internal bool MusicExpanded{get{return musicExpanded;}}
        internal bool PlanetVisible{get{return earth!=null&&earth.Visibility==Visibility.Visible;}}
        internal double PlayerAspect{get{return musicFace.ActualWidth/Math.Max(1,musicFace.ActualHeight);}}
        internal ImageSource BackgroundImage{get{return wallpaper.Source;}}
        internal bool MusicVisible{get{return musicFace.Visibility==Visibility.Visible;}}
        internal int NotificationCount{get{return notices.Children.Count;}}
        internal static string NoticeDetail(ActivityItem item,bool previews){return item.External&&!previews?"Nueva notificación":item.Detail;}
        private Rect ArtworkBounds(FrameworkElement element){try{return element.TransformToVisual(this).TransformBounds(new Rect(0,0,element.ActualWidth,element.ActualHeight));}catch{return new Rect(ActualWidth/2-20,ActualHeight*.65,40,40);}}
        private void MoveArtwork(Rect from,Rect to)
        {
            albumFace.RenderTransform=Transform.Identity;UpdateLayout();Rect basis=ArtworkBounds(albumFace);if(basis.Width<1||basis.Height<1)return;var scale=new ScaleTransform(Math.Max(.01,from.Width/basis.Width),Math.Max(.01,from.Height/basis.Height));var shift=new TranslateTransform(from.Left-basis.Left,from.Top-basis.Top);var group=new TransformGroup();group.Children.Add(scale);group.Children.Add(shift);albumFace.RenderTransformOrigin=new Point(0,0);albumFace.RenderTransform=group;
            Motion.To(scale,ScaleTransform.ScaleXProperty,Math.Max(.01,to.Width/basis.Width),360);Motion.To(scale,ScaleTransform.ScaleYProperty,Math.Max(.01,to.Height/basis.Height),360);Motion.To(shift,TranslateTransform.XProperty,to.Left-basis.Left,360);Motion.To(shift,TranslateTransform.YProperty,to.Top-basis.Top,360);
        }
        internal void ToggleMusic()
        {
            if(closed||(!musicExpanded&&(!media.Playing||media.Artwork==null)))return;
            Rect from=ArtworkBounds(musicExpanded?(FrameworkElement)albumFace:media.ArtworkElement);sceneSettle.Stop();settleScene=null;musicExpanded=!musicExpanded;musicScene.RenderTransform=Transform.Identity;musicScene.Opacity=1;
            if(musicExpanded)
            {
                if(normalWallpaper==null)normalWallpaper=wallpaper.Source;if(earth!=null)Motion.Fade(earth,earth.Opacity,0,350);lower.Children.Remove(musicFace);media.LockPlayerLayout(true);playerFooter.Visibility=Visibility.Visible;musicFace.Width=420;musicFace.Margin=new Thickness(24,0,24,42);musicFace.HorizontalAlignment=HorizontalAlignment.Center;Grid.SetRow(musicFace,2);musicScene.Children.Add(musicFace);albumImage.Source=musicBackground.Source=media.Artwork;cover.Visibility=musicScene.Visibility=Visibility.Visible;albumFace.RenderTransform=Transform.Identity;LayoutMusic();UpdateLayout();Rect target=ArtworkBounds(albumFace);MoveArtwork(from,target);Motion.Fade(musicBackground,musicBackground.Opacity,1,350);Motion.Fade(musicShade,musicShade.Opacity,1,350);Motion.Fade(musicFace,0,1,300);Motion.Fade(cover,cover.Opacity,0,320);
                settleScene=delegate{if(musicExpanded){cover.Visibility=Visibility.Collapsed;if(earth!=null)earth.Visibility=Visibility.Collapsed;wallpaper.Source=media.Artwork;albumFace.RenderTransform=Transform.Identity;}};
            }
            else
            {
                musicScene.Children.Remove(musicFace);wallpaper.Source=normalWallpaper;if(earth!=null){earth.Visibility=Visibility.Visible;Motion.Fade(earth,earth.Opacity,1,340);}musicFace.Margin=new Thickness(0);media.LockPlayerLayout(false);playerFooter.Visibility=Visibility.Collapsed;musicFace.Width=Math.Max(240,Math.Min(360,ActualWidth-48));lower.Children.Insert(0,musicFace);cover.Visibility=Visibility.Visible;media.ArtworkElement.Opacity=0;UpdateLayout();Rect target=ArtworkBounds(media.ArtworkElement);MoveArtwork(from,target);Motion.Fade(musicBackground,musicBackground.Opacity,0,340);Motion.Fade(musicShade,musicShade.Opacity,0,340);Motion.Fade(cover,cover.Opacity,1,320);
                settleScene=delegate{if(!musicExpanded){musicScene.Visibility=Visibility.Collapsed;media.ArtworkElement.Opacity=1;albumFace.RenderTransform=Transform.Identity;}};
            }
            if(Motion.Enabled)sceneSettle.Start();else{var action=settleScene;settleScene=null;if(action!=null)action();}
        }
        private void LayoutMusic()
        {
            if(albumFace==null||ActualWidth<1||ActualHeight<1)return;
            if(!musicExpanded)musicFace.Width=Math.Max(240,Math.Min(360,ActualWidth-48));double playerWidth=Math.Max(240,Math.Min(640,ActualWidth-48));musicFace.MaxWidth=Math.Max(240,ActualWidth-32);
            double size=Math.Min(640,Math.Min(ActualWidth-48,ActualHeight*.62));
            if(musicExpanded){musicFace.Width=playerWidth;musicFace.Margin=new Thickness(16,12,16,26);musicFace.Measure(new Size(playerWidth,Double.PositiveInfinity));size=Math.Max(100,Math.Min(size,ActualHeight-48-musicFace.DesiredSize.Height-44));}
            albumFace.Width=albumFace.Height=size;
        }
        private void RefreshMedia(){if(musicFace==null)return;if(!media.Playing&&musicExpanded)ToggleMusic();musicFace.Visibility=media.Playing?Visibility.Visible:Visibility.Collapsed;if(albumImage!=null)albumImage.Source=media.Artwork;if(musicBackground!=null)musicBackground.Source=media.Artwork;if(musicExpanded&&!sceneSettle.IsEnabled)wallpaper.Source=media.Artwork;}
        private void LayoutNotices()
        {
            if(noticeScroll==null||sceneRoot==null)return;bool wide=ActualWidth>=1000;
            if(wide&&noticeScroll.Parent==lower){lower.Children.Remove(noticeScroll);sceneRoot.Children.Add(noticeScroll);}
            else if(!wide&&noticeScroll.Parent==sceneRoot){sceneRoot.Children.Remove(noticeScroll);lower.Children.Add(noticeScroll);}
            noticeScroll.Width=wide?340:Double.NaN;noticeScroll.HorizontalAlignment=wide?HorizontalAlignment.Right:HorizontalAlignment.Stretch;noticeScroll.VerticalAlignment=wide?VerticalAlignment.Top:VerticalAlignment.Stretch;noticeScroll.Margin=wide?new Thickness(0,Math.Max(250,ActualHeight*.36),36,0):new Thickness(0);noticeScroll.MaxHeight=wide?ActualHeight*.54:Math.Max(60,Math.Min(250,(ActualHeight-90)*.6-150));
        }
        private static string NoticeKey(ActivityItem item){return item.ExternalKey??(item.CreatedUtc.Ticks+"|"+item.Title);}
        private void RefreshNotices()
        {
            if(closed||notices==null)return;
            var items=desktop.Activity.Items.Where(n=>!dismissed.Contains(NoticeKey(n))&&DateTime.UtcNow-n.CreatedUtc<TimeSpan.FromDays(1)).Take(8).ToList();
            var positions=new System.Collections.Generic.Dictionary<string,double>();
            foreach(var pair in noticeViews)try{positions[pair.Key]=pair.Value.TransformToAncestor(notices).Transform(new Point()).Y;}catch(InvalidOperationException){}
            var visible=new System.Collections.Generic.HashSet<string>();notices.Children.Clear();
            foreach(var item in items)
            {
                string key=NoticeKey(item);visible.Add(key);LockNotice view;bool fresh=!noticeViews.TryGetValue(key,out view);
                if(fresh){var captured=item;view=new LockNotice(backgroundScene,delegate{dismissed.Add(NoticeKey(captured));RefreshNotices();});noticeViews[key]=view;}
                view.Update(item,NoticeDetail(item,desktop.Preferences.LockNotificationPreviews));notices.Children.Add(view);
            }
            foreach(var key in noticeViews.Keys.Where(k=>!visible.Contains(k)).ToList())noticeViews.Remove(key);
            notices.UpdateLayout();
            foreach(var key in visible)
            {
                var view=noticeViews[key];double old;if(positions.TryGetValue(key,out old)){double next=view.TransformToAncestor(notices).Transform(new Point()).Y;view.Shift.Y+=old-next;Motion.To(view.Shift,TranslateTransform.YProperty,0,320);}
                else if(IsLoaded){view.Shift.Y=-8;Motion.To(view.Shift,TranslateTransform.YProperty,0,320);Motion.Fade(view,0,1,210);}
            }
        }
        private void Tick(){if(closed)return;if(earth!=null&&earth.IsVisible)earth.UpdateTime(DateTime.Now);playerFooter.Refresh();time.Set(DateTime.Now.ToString("HH:mm"));string label=DateTime.Now.ToString("dddd d 'de' MMMM",CultureInfo.GetCultureInfo("es-AR"));date.Text=Char.ToUpper(label[0])+label.Substring(1);musicDate.Text=DateTime.Now.ToString("HH:mm   ddd d MMM",CultureInfo.GetCultureInfo("es-AR"));}
        private void ResetCover(){if(notificationView){CompleteQuick(true,-dragVelocity);return;}if(earth!=null)earth.Settle(false);double from=slide.Y;slide.BeginAnimation(TranslateTransform.YProperty,null);slide.Y=0;if(Motion.Enabled){var restore=new DoubleAnimation(from,0,TimeSpan.FromMilliseconds(310)){EasingFunction=Motion.Ease(),FillBehavior=FillBehavior.Stop};slide.BeginAnimation(TranslateTransform.YProperty,restore);Motion.Fade(cover,cover.Opacity,1,240);}else cover.Opacity=1;}
        internal void RevealAccess()
        {
            if(notificationView){CompleteQuick(false);return;}
            if(closed||AuthenticationVisible||leaving)return;if(afterResume){Unlock(true);return;}if(earth!=null)earth.Settle(true);if(musicExpanded)ToggleMusic();access.Visibility=Visibility.Visible;wallpaper.Effect=Motion.Enabled?new System.Windows.Media.Effects.BlurEffect{Radius=16,RenderingBias=System.Windows.Media.Effects.RenderingBias.Performance}:null;
            if(Motion.Enabled){slide.BeginAnimation(TranslateTransform.YProperty,new System.Windows.Media.Animation.DoubleAnimation(slide.Y,-ActualHeight,TimeSpan.FromMilliseconds(360)){EasingFunction=new System.Windows.Media.Animation.CubicEase{EasingMode=System.Windows.Media.Animation.EasingMode.EaseOut}});Motion.Fade(access,0,1,260);}else{cover.Visibility=Visibility.Collapsed;access.Opacity=1;}
            if(!desktop.IsTesting)pin.Focus();
        }
        private void HideAccess(){if(working)return;pin.Clear();confirmation.Clear();access.Visibility=Visibility.Collapsed;access.Opacity=0;wallpaper.Effect=null;cover.Visibility=Visibility.Visible;ResetCover();}
        private async void SubmitPin()
        {
            if(closed||working)return;string value=pin.Password,again=confirmation.Password;pin.Clear();confirmation.Clear();working=true;submit.IsEnabled=false;
            try{if(!profile.Configured){if(!PrivacyProfile.ValidPin(value)||value!=again){message.Text="Usá 6 a 12 números y repetí el mismo PIN.";return;}await Task.Run(()=>profile.SetPin(value));if(!closed)Unlock(true);}else{bool ok=await Task.Run(()=>profile.Verify(value));if(!closed){if(ok)Unlock(true);else message.Text=DateTime.UtcNow<profile.RetryUtc?"Esperá 30 segundos antes de volver a intentar.":"PIN incorrecto. Intentá de nuevo.";}}}
            catch(Exception ex){if(!closed)message.Text="No se pudo leer o guardar tu PIN.";Log.Write("Privacidad: "+ex.GetType().Name);}finally{working=false;if(!closed)submit.IsEnabled=true;}
        }
        private void CoverOtherDisplays()
        {
            foreach(var screen in System.Windows.Forms.Screen.AllScreens)
            {
                if(screen.Primary)continue;var area=DisplaySpace.Of(screen);var scene=new Grid();var picture=new Image{Stretch=Stretch.UniformToFill};picture.Source=new DrawingImage(new GeometryDrawing(new VisualBrush(backgroundScene){Stretch=Stretch.UniformToFill},null,new RectangleGeometry(new Rect(0,0,Width,Height))));scene.Children.Add(picture);scene.Children.Add(new Border{Background=Design.Brush("#20000000"),IsHitTestVisible=false});var note=Label("Continuá en la pantalla principal · MacOS",13,false);note.HorizontalAlignment=HorizontalAlignment.Center;note.VerticalAlignment=VerticalAlignment.Bottom;note.Margin=new Thickness(0,0,0,44);scene.Children.Add(note);
                var curtain=new Window{Title="MacOS · Privacidad secundaria",WindowStyle=WindowStyle.None,ResizeMode=ResizeMode.NoResize,ShowInTaskbar=false,Topmost=true,ShowActivated=false,Left=area.Dips.Left,Top=area.Dips.Top,Width=area.Width,Height=area.Height,Background=Brushes.Black,Content=scene};curtain.SourceInitialized+=delegate{Native.NoActivate(curtain);};curtain.Show();Native.SetWindowPos(new System.Windows.Interop.WindowInteropHelper(curtain).Handle,new IntPtr(-1),(int)area.Pixels.Left,(int)area.Pixels.Top,(int)area.Pixels.Width,(int)area.Pixels.Height,0x50);curtains.Add(curtain);
            }
        }
        private static bool Interactive(DependencyObject node)
        {
            while(node!=null){var element=node as FrameworkElement;if(element!=null&&Equals(element.Tag,"MediaArtwork"))return true;if(node is System.Windows.Controls.Primitives.ButtonBase||node is Slider||node is TextBox||node is PasswordBox)return true;node=node is Visual?VisualTreeHelper.GetParent(node):LogicalTreeHelper.GetParent(node);}return false;
        }
        internal void Recover(){Unlock();}
        private void Unlock(bool animate=false){if(closed||leaving)return;allowed=true;if(!animate||!Motion.Enabled){Close();return;}leaving=true;IsHitTestVisible=false;if(earth!=null)earth.Settle(true);var exit=new DoubleAnimation(Opacity,0,TimeSpan.FromMilliseconds(430)){EasingFunction=Motion.Ease(),FillBehavior=FillBehavior.Stop};exit.Completed+=delegate{if(!closed)Close();};BeginAnimation(OpacityProperty,exit);foreach(var curtain in curtains)Motion.Fade(curtain,curtain.Opacity,0,310);Motion.To(slide,TranslateTransform.YProperty,-45,330);}
    }
}

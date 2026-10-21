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
        private readonly Grid cover,access;private Border musicFace;private StackPanel lower,notices;private Grid musicScene;private Border albumFace;private ImageSource normalWallpaper;private Image musicBackground,albumImage;private TextBlock musicDate;private bool musicExpanded;private readonly System.Collections.Generic.HashSet<ActivityItem> dismissed=new System.Collections.Generic.HashSet<ActivityItem>();private readonly TranslateTransform slide=new TranslateTransform();
        private readonly PrivacyClock time;private readonly TextBlock date,message;private readonly PasswordBox pin,confirmation;
        private readonly Button submit;private readonly MediaCard media;private readonly PlayerFooter playerFooter;
        private readonly DispatcherTimer clock=new DispatcherTimer{Interval=TimeSpan.FromSeconds(1)};
        private readonly System.Collections.Generic.List<Window> curtains=new System.Collections.Generic.List<Window>();
        private bool closed,allowed,working;private Point? drag;
        internal bool CameraRunning{get{return false;}}
        internal bool AuthenticationVisible{get{return access.Visibility==Visibility.Visible;}}
        internal static bool AcceptSwipe(double start,double end,double height){return start>=height*.72&&start-end>=Math.Min(80,height*.12);}
        internal PrivacyScreen(Desktop owner,bool demonstration=false,IMediaSource provider=null,Preferences appearance=null)
        {
            appearance=appearance??owner.Preferences;appearance.Validate();desktop=owner;profile=new PrivacyProfile(owner.Store.DirectoryPath);Title="MacFlow · Privacidad";WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;ShowInTaskbar=false;Topmost=true;Background=Brushes.Black;
            var bounds=owner.IsTesting?new Rect(60,30,Math.Min(1100,SystemParameters.PrimaryScreenWidth-100),Math.Min(850,SystemParameters.PrimaryScreenHeight-60)):DisplaySpace.Of(System.Windows.Forms.Screen.PrimaryScreen).Dips;
            Left=bounds.Left;Top=bounds.Top;Width=bounds.Width;Height=bounds.Height;WindowLife.Track(this);
            Resources["GlassInk"]=Brushes.White;Resources["GlassMuted"]=Design.Brush("#CCFFFFFF");
            var root=new Grid{ClipToBounds=true};wallpaper.Source=owner.PresentationWallpaper;root.Children.Add(wallpaper);root.Children.Add(new Border{Background=Design.Brush("#28000000"),IsHitTestVisible=false});
            cover=new Grid{RenderTransform=slide};cover.RowDefinitions.Add(new RowDefinition{Height=new GridLength(2,GridUnitType.Star)});cover.RowDefinitions.Add(new RowDefinition{Height=new GridLength(3,GridUnitType.Star)});cover.RowDefinitions.Add(new RowDefinition{Height=new GridLength(90)});
            var heading=new StackPanel{VerticalAlignment=VerticalAlignment.Top,HorizontalAlignment=HorizontalAlignment.Center};date=Label("",16,true);date.TextAlignment=TextAlignment.Center;heading.Children.Add(date);time=new PrivacyClock{Width=420,Height=132,Margin=new Thickness(0,4,0,0)};time.Appearance(appearance.PrivacyClockWeight,appearance.PrivacyClockColor);heading.Children.Add(time);cover.Children.Add(heading);
            lower=new StackPanel{Width=360,VerticalAlignment=VerticalAlignment.Bottom,Margin=new Thickness(0,0,0,26)};Grid.SetRow(lower,1);
            media=new MediaCard(provider??(owner.IsTesting?(IMediaSource)new MediaFixture():new WindowsMediaSource()));media.ArtworkToggleOnly=true;media.HideWhenEmpty=true;media.CompactLayout();playerFooter=new PlayerFooter(owner.IsTesting){Visibility=Visibility.Collapsed};var playerBody=new StackPanel();playerBody.Children.Add(media);playerBody.Children.Add(playerFooter);var musicCard=new Border{CornerRadius=new CornerRadius(26),Background=Brushes.Transparent,BorderThickness=new Thickness(0),Child=new WallpaperGlass(wallpaper,playerBody,26,new Thickness(18),true)};musicFace=musicCard;musicCard.Visibility=Visibility.Collapsed;lower.Children.Add(musicCard);notices=new StackPanel{Margin=new Thickness(0,10,0,0)};lower.Children.Add(new ScrollViewer{Content=notices,MaxHeight=280,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled});cover.Children.Add(lower);media.ExpandRequested+=delegate{ToggleMusic();};media.ArtworkPressed+=ToggleMusic;media.MediaChanged+=RefreshMedia;desktop.Activity.Changed+=RefreshNotices;RefreshNotices();
            var handle=new Border{Width=132,Height=5,CornerRadius=new CornerRadius(3),Background=Design.Brush("#E8FFFFFF"),HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Bottom,Margin=new Thickness(0,0,0,20)};Grid.SetRow(handle,2);cover.Children.Add(handle);root.Children.Add(cover);
            musicScene=new Grid{Visibility=Visibility.Collapsed,ClipToBounds=true};musicScene.RowDefinitions.Add(new RowDefinition{Height=new GridLength(48)});musicScene.RowDefinitions.Add(new RowDefinition());musicScene.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});musicDate=Label("",21,true);musicDate.HorizontalAlignment=HorizontalAlignment.Center;musicDate.VerticalAlignment=VerticalAlignment.Center;musicScene.Children.Add(musicDate);albumImage=new Image{Stretch=Stretch.UniformToFill};albumFace=new Border{CornerRadius=new CornerRadius(30),Child=albumImage,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,ClipToBounds=true,Cursor=Cursors.Hand};albumFace.Tag="MediaArtwork";albumFace.MouseLeftButtonUp+=delegate(object sender,MouseButtonEventArgs e){ToggleMusic();e.Handled=true;};albumFace.SizeChanged+=delegate{albumFace.Clip=new RectangleGeometry(new Rect(0,0,albumFace.ActualWidth,albumFace.ActualHeight),30,30);};Grid.SetRow(albumFace,1);musicScene.Children.Add(albumFace);musicBackground=new Image{Stretch=Stretch.UniformToFill,IsHitTestVisible=false,Effect=new System.Windows.Media.Effects.BlurEffect{Radius=32,RenderingBias=System.Windows.Media.Effects.RenderingBias.Performance},CacheMode=new BitmapCache{RenderAtScale=.5}};Grid.SetRowSpan(musicBackground,3);Panel.SetZIndex(musicBackground,-2);musicScene.Children.Add(musicBackground);var musicShade=new Border{Background=new LinearGradientBrush(Color.FromArgb(60,0,0,0),Color.FromArgb(150,0,0,0),90),IsHitTestVisible=false};Grid.SetRowSpan(musicShade,3);Panel.SetZIndex(musicShade,-1);musicScene.Children.Add(musicShade);musicDate.Margin=new Thickness(0,18,0,0);root.Children.Add(musicScene);
            SizeChanged+=delegate{heading.Margin=new Thickness(0,Math.Max(24,ActualHeight*.065),0,0);time.Width=Math.Max(180,Math.Min(440,ActualWidth*.65))*appearance.PrivacyClockScale;time.Height=Math.Max(70,Math.Min(150,ActualHeight*.20))*appearance.PrivacyClockScale;lower.Width=Math.Max(280,Math.Min(380,ActualWidth-48));LayoutMusic();((ScrollViewer)notices.Parent).MaxHeight=Math.Max(60,Math.Min(280,(ActualHeight-90)*.6-150));};
            access=new Grid{Visibility=Visibility.Collapsed,Opacity=0};access.Children.Add(new Border{Background=Design.Brush("#48000000")});var form=new StackPanel{Width=300,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center};
            form.Children.Add(new Border{Width=72,Height=72,CornerRadius=new CornerRadius(36),Background=Design.Brush("#30FFFFFF"),Child=new Viewbox{Margin=new Thickness(23),Child=Design.Symbol("lock",22,Brushes.White)},HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(0,0,0,18)});
            var account=Label(Environment.UserName,24,true);account.TextAlignment=TextAlignment.Center;form.Children.Add(account);
            message=Label(profile.Configured?"Ingresá tu PIN de MacFlow":"Creá un PIN de MacFlow",12,false);message.TextWrapping=TextWrapping.Wrap;message.TextAlignment=TextAlignment.Center;message.Margin=new Thickness(0,10,0,18);form.Children.Add(message);
            pin=Password("PIN de MacFlow");confirmation=Password("Repetir PIN de MacFlow");confirmation.Visibility=profile.Configured?Visibility.Collapsed:Visibility.Visible;form.Children.Add(pin);form.Children.Add(confirmation);
            submit=Design.Button(profile.Configured?"Desbloquear":"Guardar PIN",SubmitPin,true);submit.Height=36;submit.Margin=new Thickness(0,12,0,8);form.Children.Add(submit);
            var back=Design.Button("Volver",HideAccess,false);back.Background=Brushes.Transparent;back.Foreground=Brushes.White;form.Children.Add(back);var help=Label("PIN local · 6 a 12 números\nRecuperar MacFlow: Ctrl + Windows + Alt",10,false);help.TextAlignment=TextAlignment.Center;help.Margin=new Thickness(0,20,0,0);form.Children.Add(help);access.Children.Add(form);root.Children.Add(access);Content=root;
            if(owner.IsTesting){ShowActivated=false;IsHitTestVisible=false;SourceInitialized+=delegate{Native.NoActivate(this);};}
            SourceInitialized+=delegate{Native.OpaquePresentation(this);};Loaded+=async delegate{Tick();clock.Start();if(!owner.IsTesting&&!demonstration){var physical=System.Windows.Forms.Screen.PrimaryScreen.Bounds;Native.SetWindowPos(new System.Windows.Interop.WindowInteropHelper(this).Handle,new IntPtr(-1),physical.Left,physical.Top,physical.Width,physical.Height,0x40);CoverOtherDisplays();}if(wallpaper.Source==null||!String.IsNullOrWhiteSpace(appearance.PrivacyWallpaperPath)){var image=await WelcomeWallpaper.Load(String.IsNullOrWhiteSpace(appearance.PrivacyWallpaperPath)?appearance.WallpaperPath:appearance.PrivacyWallpaperPath);if(!closed){if(musicExpanded)normalWallpaper=image;else wallpaper.Source=image;}}};clock.Tick+=delegate{Tick();};
            PreviewMouseLeftButtonDown+=delegate(object s,MouseButtonEventArgs e){var point=e.GetPosition(this);if(!AuthenticationVisible&&!Interactive(e.OriginalSource as DependencyObject)&&point.Y>=ActualHeight*.72){drag=point;CaptureMouse();e.Handled=true;}};
            PreviewMouseMove+=delegate(object s,MouseEventArgs e){if(drag.HasValue&&IsMouseCaptured){double distance=Math.Max(0,drag.Value.Y-e.GetPosition(this).Y);slide.Y=-distance;cover.Opacity=Math.Max(.2,1-distance/Math.Max(1,ActualHeight*.65));}};
            PreviewMouseLeftButtonUp+=delegate(object s,MouseButtonEventArgs e){if(drag.HasValue){bool accept=AcceptSwipe(drag.Value.Y,e.GetPosition(this).Y,ActualHeight);drag=null;ReleaseMouseCapture();if(accept)RevealAccess();else ResetCover();e.Handled=true;}};
            PreviewTouchDown+=delegate(object s,TouchEventArgs e){var point=e.GetTouchPoint(this).Position;if(!AuthenticationVisible&&!Interactive(e.OriginalSource as DependencyObject)&&point.Y>=ActualHeight*.72){drag=point;CaptureTouch(e.TouchDevice);e.Handled=true;}};
            PreviewTouchMove+=delegate(object s,TouchEventArgs e){if(drag.HasValue){double distance=Math.Max(0,drag.Value.Y-e.GetTouchPoint(this).Position.Y);slide.Y=-distance;cover.Opacity=Math.Max(.2,1-distance/Math.Max(1,ActualHeight*.65));e.Handled=true;}};
            PreviewTouchUp+=delegate(object s,TouchEventArgs e){if(drag.HasValue){bool accept=AcceptSwipe(drag.Value.Y,e.GetTouchPoint(this).Position.Y,ActualHeight);drag=null;ReleaseTouchCapture(e.TouchDevice);if(accept)RevealAccess();else ResetCover();e.Handled=true;}};
            LostMouseCapture+=delegate{if(drag.HasValue){drag=null;ResetCover();}};LostTouchCapture+=delegate{if(drag.HasValue){drag=null;ResetCover();}};
            PreviewKeyDown+=delegate(object s,KeyEventArgs e){if(e.Key==Key.Escape&&demonstration){Recover();e.Handled=true;}else if(e.Key==Key.Escape&&AuthenticationVisible){HideAccess();e.Handled=true;}else if(e.Key==Key.Enter){if(AuthenticationVisible)SubmitPin();else RevealAccess();e.Handled=true;}else if(!AuthenticationVisible&&(e.Key==Key.Space||e.Key==Key.Up)){RevealAccess();e.Handled=true;}};
            Closing+=delegate(object s,System.ComponentModel.CancelEventArgs e){if(!allowed&&!demonstration&&!owner.IsTesting)e.Cancel=true;};Closed+=delegate{closed=true;clock.Stop();desktop.Activity.Changed-=RefreshNotices;media.Dispose();playerFooter.Dispose();foreach(var curtain in curtains)WindowLife.Close(curtain);curtains.Clear();};
        }
        private static TextBlock Label(string text,double size,bool bold){var result=Design.Text(text,size,bold);result.Foreground=Brushes.White;return result;}
        private static PasswordBox Password(string name)
        {
            var field=new PasswordBox{MaxLength=12,FontSize=20,Height=44,Padding=new Thickness(14,9,14,9),Margin=new Thickness(0,5,0,0),HorizontalContentAlignment=HorizontalAlignment.Center,Foreground=Brushes.White,Background=Design.Brush("#32FFFFFF"),BorderBrush=Design.Brush("#50FFFFFF"),BorderThickness=new Thickness(.6)};
            var template=new ControlTemplate(typeof(PasswordBox));var border=new FrameworkElementFactory(typeof(Border));border.SetValue(Border.CornerRadiusProperty,new CornerRadius(14));border.SetValue(Border.BackgroundProperty,new TemplateBindingExtension(Control.BackgroundProperty));border.SetValue(Border.BorderBrushProperty,new TemplateBindingExtension(Control.BorderBrushProperty));border.SetValue(Border.BorderThicknessProperty,new TemplateBindingExtension(Control.BorderThicknessProperty));border.SetValue(Border.PaddingProperty,new TemplateBindingExtension(Control.PaddingProperty));var host=new FrameworkElementFactory(typeof(ScrollViewer));host.Name="PART_ContentHost";border.AppendChild(host);template.VisualTree=border;field.Template=template;System.Windows.Automation.AutomationProperties.SetName(field,name);return field;
        }
        internal bool MusicExpanded{get{return musicExpanded;}}
        internal ImageSource BackgroundImage{get{return wallpaper.Source;}}
        internal bool MusicVisible{get{return musicFace.Visibility==Visibility.Visible;}}
        internal int NotificationCount{get{return notices.Children.Count;}}
        internal void ToggleMusic()
        {
            if(closed||(!musicExpanded&&(!media.Playing||media.Artwork==null)))return;
            musicExpanded=!musicExpanded;
            if(musicExpanded){normalWallpaper=wallpaper.Source;lower.Children.Remove(musicFace);media.LockPlayerLayout(true);playerFooter.Visibility=Visibility.Visible;musicFace.Width=420;musicFace.Margin=new Thickness(24,0,24,42);wallpaper.Source=media.Artwork;musicFace.HorizontalAlignment=HorizontalAlignment.Center;Grid.SetRow(musicFace,2);musicScene.Children.Add(musicFace);albumImage.Source=musicBackground.Source=media.Artwork;cover.Visibility=Visibility.Collapsed;musicScene.Visibility=Visibility.Visible;LayoutMusic();if(Motion.Enabled){var zoom=new ScaleTransform();musicScene.RenderTransformOrigin=new Point(.5,.5);musicScene.RenderTransform=zoom;var grow=new DoubleAnimation(.94,1,TimeSpan.FromMilliseconds(360)){EasingFunction=Motion.Ease(),FillBehavior=FillBehavior.Stop};zoom.BeginAnimation(ScaleTransform.ScaleXProperty,grow);zoom.BeginAnimation(ScaleTransform.ScaleYProperty,grow);Motion.Fade(musicScene,0,1,260);}}
            else{musicScene.Children.Remove(musicFace);wallpaper.Source=normalWallpaper;musicFace.Margin=new Thickness(0);media.LockPlayerLayout(false);playerFooter.Visibility=Visibility.Collapsed;musicFace.ClearValue(FrameworkElement.WidthProperty);lower.Children.Insert(0,musicFace);musicScene.Visibility=Visibility.Collapsed;cover.Visibility=Visibility.Visible;Motion.Fade(cover,0,1,220);}
        }
        private void LayoutMusic(){if(albumFace==null||ActualWidth<1||ActualHeight<1)return;musicFace.MaxWidth=Math.Max(220,ActualWidth-32);double size=Math.Min(640,ActualWidth-48);if(musicExpanded){musicFace.Margin=new Thickness(16,18,16,30);for(int i=0;i<2;i++){musicFace.Width=Math.Max(280,size);musicFace.Measure(new Size(Math.Max(280,size),Double.PositiveInfinity));size=Math.Max(120,Math.Min(size,ActualHeight-48-musicFace.DesiredSize.Height-16));}musicFace.Width=Math.Max(280,size);}albumFace.Width=albumFace.Height=size;}
        private void RefreshMedia(){if(musicFace==null)return;if(!media.Playing&&musicExpanded)ToggleMusic();musicFace.Visibility=media.Playing?Visibility.Visible:Visibility.Collapsed;if(albumImage!=null)albumImage.Source=media.Artwork;if(musicBackground!=null)musicBackground.Source=media.Artwork;if(musicExpanded)wallpaper.Source=media.Artwork;}
        private void RefreshNotices()
        {
            if(closed||notices==null)return;notices.Children.Clear();
            foreach(var item in desktop.Activity.Items.Where(n=>!dismissed.Contains(n)&&DateTime.UtcNow-n.CreatedUtc<TimeSpan.FromDays(1)).Take(3))
            {
                var row=new DockPanel();var close=Design.Button("×",delegate{dismissed.Add(item);RefreshNotices();},false);close.Width=26;close.Height=26;close.Background=Brushes.Transparent;close.Foreground=Brushes.White;close.Padding=new Thickness(0);System.Windows.Automation.AutomationProperties.SetName(close,"Descartar aviso de MacFlow");DockPanel.SetDock(close,Dock.Right);row.Children.Add(close);var text=new StackPanel();text.Children.Add(Label(item.Title,13,true));var detail=Label(item.Detail,12,false);detail.TextWrapping=TextWrapping.Wrap;detail.MaxHeight=36;detail.TextTrimming=TextTrimming.CharacterEllipsis;text.Children.Add(detail);row.Children.Add(text);notices.Children.Add(new Border{Margin=new Thickness(0,0,0,8),Child=new WallpaperGlass(wallpaper,row,22,new Thickness(16),true)});
            }
        }
        private void Tick(){if(closed)return;playerFooter.Refresh();time.Set(DateTime.Now.ToString("HH:mm"));date.Text=DateTime.Now.ToString("dddd d 'de' MMMM",CultureInfo.GetCultureInfo("es-AR"));musicDate.Text=DateTime.Now.ToString("HH:mm   ddd d MMM",CultureInfo.GetCultureInfo("es-AR"));}
        private void ResetCover(){double from=slide.Y;slide.BeginAnimation(TranslateTransform.YProperty,null);slide.Y=0;if(Motion.Enabled){var restore=new DoubleAnimation(from,0,TimeSpan.FromMilliseconds(310)){EasingFunction=Motion.Ease(),FillBehavior=FillBehavior.Stop};slide.BeginAnimation(TranslateTransform.YProperty,restore);Motion.Fade(cover,cover.Opacity,1,240);}else cover.Opacity=1;}
        internal void RevealAccess()
        {
            if(closed||AuthenticationVisible)return;if(musicExpanded)ToggleMusic();access.Visibility=Visibility.Visible;wallpaper.Effect=Motion.Enabled?new System.Windows.Media.Effects.BlurEffect{Radius=16,RenderingBias=System.Windows.Media.Effects.RenderingBias.Performance}:null;
            if(Motion.Enabled){slide.BeginAnimation(TranslateTransform.YProperty,new System.Windows.Media.Animation.DoubleAnimation(slide.Y,-ActualHeight,TimeSpan.FromMilliseconds(360)){EasingFunction=new System.Windows.Media.Animation.CubicEase{EasingMode=System.Windows.Media.Animation.EasingMode.EaseOut}});Motion.Fade(access,0,1,260);}else{cover.Visibility=Visibility.Collapsed;access.Opacity=1;}
            if(!desktop.IsTesting)pin.Focus();
        }
        private void HideAccess(){if(working)return;pin.Clear();confirmation.Clear();access.Visibility=Visibility.Collapsed;access.Opacity=0;wallpaper.Effect=null;cover.Visibility=Visibility.Visible;ResetCover();}
        private async void SubmitPin()
        {
            if(closed||working)return;string value=pin.Password,again=confirmation.Password;pin.Clear();confirmation.Clear();working=true;submit.IsEnabled=false;
            try{if(!profile.Configured){if(!PrivacyProfile.ValidPin(value)||value!=again){message.Text="Usá 6 a 12 números y repetí el mismo PIN.";return;}await Task.Run(()=>profile.SetPin(value));if(!closed)Unlock();}else{bool ok=await Task.Run(()=>profile.Verify(value));if(!closed){if(ok)Unlock();else message.Text=DateTime.UtcNow<profile.RetryUtc?"Esperá 30 segundos antes de volver a intentar.":"PIN incorrecto. Intentá de nuevo.";}}}
            catch(Exception ex){if(!closed)message.Text="No se pudo leer o guardar tu PIN.";Log.Write("Privacidad: "+ex.GetType().Name);}finally{working=false;if(!closed)submit.IsEnabled=true;}
        }
        private void CoverOtherDisplays()
        {
            foreach(var screen in System.Windows.Forms.Screen.AllScreens)
            {
                if(screen.Primary)continue;var area=DisplaySpace.Of(screen);var scene=new Grid();var picture=new Image{Stretch=Stretch.UniformToFill};picture.SetBinding(Image.SourceProperty,new System.Windows.Data.Binding("Source"){Source=wallpaper});scene.Children.Add(picture);scene.Children.Add(new Border{Background=Design.Brush("#20000000"),IsHitTestVisible=false});var note=Label("Continuá en la pantalla principal · MacFlow",13,false);note.HorizontalAlignment=HorizontalAlignment.Center;note.VerticalAlignment=VerticalAlignment.Bottom;note.Margin=new Thickness(0,0,0,44);scene.Children.Add(note);
                var curtain=new Window{Title="MacFlow · Privacidad secundaria",WindowStyle=WindowStyle.None,ResizeMode=ResizeMode.NoResize,ShowInTaskbar=false,Topmost=true,ShowActivated=false,Left=area.Dips.Left,Top=area.Dips.Top,Width=area.Width,Height=area.Height,Background=Brushes.Black,Content=scene};curtain.SourceInitialized+=delegate{Native.NoActivate(curtain);};curtain.Show();Native.SetWindowPos(new System.Windows.Interop.WindowInteropHelper(curtain).Handle,new IntPtr(-1),(int)area.Pixels.Left,(int)area.Pixels.Top,(int)area.Pixels.Width,(int)area.Pixels.Height,0x50);curtains.Add(curtain);
            }
        }
        private static bool Interactive(DependencyObject node)
        {
            while(node!=null){var element=node as FrameworkElement;if(element!=null&&Equals(element.Tag,"MediaArtwork"))return true;if(node is System.Windows.Controls.Primitives.ButtonBase||node is Slider||node is TextBox||node is PasswordBox)return true;node=node is Visual?VisualTreeHelper.GetParent(node):LogicalTreeHelper.GetParent(node);}return false;
        }
        internal void Recover(){Unlock();}
        private void Unlock(){if(closed)return;allowed=true;Close();}
    }
}

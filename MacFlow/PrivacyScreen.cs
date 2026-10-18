using System;
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
    // An explicitly recoverable privacy curtain, not a Windows credential provider.
    // Camera and inference exist only in the short-lived child process.
    internal sealed class PrivacyScreen : Window
    {
        [DllImport("kernel32.dll")]private static extern uint SetThreadExecutionState(uint flags);
        private readonly Desktop desktop;private readonly PrivacyProfile profile;
        private readonly Image wallpaper=new Image{Stretch=Stretch.UniformToFill},camera=new Image{Stretch=Stretch.UniformToFill};
        private readonly TextBlock date,message;private readonly PrivacyClock time;private readonly StackPanel pinPanel,notices;private readonly PasswordBox pin,confirmation;private readonly Border cameraShell;private readonly Button submit,faceButton;
        private readonly DispatcherTimer clock=new DispatcherTimer{Interval=TimeSpan.FromSeconds(1)};
        private readonly MediaCard media;private Process worker;private bool closed,allowed,enrollRequested,canEnroll,working;private int generation;
        internal bool CameraRunning{get{return worker!=null;}}
        internal PrivacyScreen(Desktop owner,bool demonstration=false)
        {
            desktop=owner;profile=new PrivacyProfile(owner.Store.DirectoryPath);Title="MacFlow · Privacidad";WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;ShowInTaskbar=false;Topmost=true;Background=Brushes.Black;
            var bounds=owner.IsTesting?new Rect(60,30,Math.Min(1100,SystemParameters.PrimaryScreenWidth-100),Math.Min(850,SystemParameters.PrimaryScreenHeight-60)):DisplaySpace.Of(System.Windows.Forms.Screen.PrimaryScreen).Dips;Left=bounds.Left;Top=bounds.Top;Width=bounds.Width;Height=bounds.Height;WindowLife.Track(this);
            Resources["GlassInk"]=Brushes.White;Resources["GlassMuted"]=Design.Brush("#D0FFFFFF");wallpaper.Source=owner.PresentationWallpaper;
            var root=new Grid();root.Children.Add(wallpaper);root.Children.Add(new Border{Background=Design.Brush("#54000000"),IsHitTestVisible=false});
            var layout=new Grid{Width=760,Height=860};layout.RowDefinitions.Add(new RowDefinition{Height=new GridLength(320)});layout.RowDefinitions.Add(new RowDefinition());layout.RowDefinitions.Add(new RowDefinition{Height=new GridLength(92)});
            var heading=new StackPanel{VerticalAlignment=VerticalAlignment.Center};date=Label("",22,true);date.HorizontalAlignment=HorizontalAlignment.Center;heading.Children.Add(date);time=new PrivacyClock{Width=650,Height=230,HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(0,18,0,0)};heading.Children.Add(time);layout.Children.Add(heading);
            var body=new StackPanel{Width=460};Grid.SetRow(body,1);layout.Children.Add(body);
            cameraShell=new Border{Width=190,Height=190,CornerRadius=new CornerRadius(95),ClipToBounds=true,Background=Design.Brush("#30000000"),BorderThickness=new Thickness(2),BorderBrush=Design.Brush("#80FFFFFF"),Child=camera,Visibility=Visibility.Collapsed,HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(0,0,0,14)};cameraShell.SizeChanged+=delegate{cameraShell.Clip=new EllipseGeometry(new Rect(0,0,cameraShell.ActualWidth,cameraShell.ActualHeight));};body.Children.Add(cameraShell);
            message=Label(profile.Configured?"Deslizá hacia arriba o usá tu PIN":"Creá tu PIN de MacFlow",16,true);message.FontSize=12;message.TextWrapping=TextWrapping.Wrap;message.TextAlignment=TextAlignment.Center;message.Margin=new Thickness(10,0,10,10);body.Children.Add(message);
            pinPanel=new StackPanel{Width=280,Visibility=profile.Configured?Visibility.Collapsed:Visibility.Visible};pin=new PasswordBox{MaxLength=12,FontSize=22,Padding=new Thickness(12),HorizontalContentAlignment=HorizontalAlignment.Center};System.Windows.Automation.AutomationProperties.SetName(pin,"PIN de MacFlow");pinPanel.Children.Add(pin);confirmation=new PasswordBox{MaxLength=12,FontSize=22,Padding=new Thickness(12),Margin=new Thickness(0,8,0,0),HorizontalContentAlignment=HorizontalAlignment.Center,Visibility=profile.Configured?Visibility.Collapsed:Visibility.Visible};System.Windows.Automation.AutomationProperties.SetName(confirmation,"Repetir PIN de MacFlow");pinPanel.Children.Add(confirmation);submit=Design.Button(profile.Configured?"Desbloquear":"Guardar PIN",SubmitPin,true);submit.Margin=new Thickness(0,10,0,6);pinPanel.Children.Add(submit);var hint=Label("6 a 12 números · Este PIN pertenece a MacFlow",10,false);hint.TextAlignment=TextAlignment.Center;pinPanel.Children.Add(hint);body.Children.Add(pinPanel);
            var actions=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center};faceButton=ActionButton("Reconocer rostro",delegate{StartFace(false);});faceButton.IsEnabled=profile.Configured&&!String.IsNullOrEmpty(profile.Face);actions.Children.Add(faceButton);actions.Children.Add(ActionButton("Usar PIN",delegate{StopFace();pinPanel.Visibility=Visibility.Visible;pin.Focus();}));actions.Children.Add(ActionButton("Registrar rostro",delegate{if(!profile.Configured){message.Text="Primero creá un PIN de recuperación.";return;}if(canEnroll)StartFace(true);else{enrollRequested=true;message.Text="Ingresá tu PIN para registrar o cambiar el rostro.";pinPanel.Visibility=Visibility.Visible;pin.Focus();}}));var bottomActions=new StackPanel();bottomActions.Children.Add(actions);Grid.SetRow(bottomActions,2);layout.Children.Add(bottomActions);
            media=new MediaCard(owner.IsTesting?(IMediaSource)new MediaFixture():new WindowsMediaSource(),true);var music=new Border{CornerRadius=new CornerRadius(26),Background=Design.Brush("#700E1822"),BorderBrush=Design.Brush("#38FFFFFF"),BorderThickness=new Thickness(1),Padding=new Thickness(24,18,24,18),Margin=new Thickness(0,16,0,0),Child=media};body.Children.Add(music);
            notices=new StackPanel{Margin=new Thickness(0,10,0,0)};body.Children.Add(notices);
            var footer=Label("Privacidad de MacFlow · Música y tareas siguen activas\nRecuperar: Ctrl + Windows + Alt",11,false);footer.TextAlignment=TextAlignment.Center;footer.HorizontalAlignment=HorizontalAlignment.Center;footer.Margin=new Thickness(0,8,0,0);bottomActions.Children.Add(footer);root.Children.Add(new Viewbox{Child=layout,Stretch=Stretch.Uniform,StretchDirection=StretchDirection.DownOnly,Margin=new Thickness(22)});Content=root;
            SourceInitialized+=delegate{Native.OpaquePresentation(this);};Loaded+=async delegate{Tick();clock.Start();if(!demonstration&&!owner.IsTesting){var physical=System.Windows.Forms.Screen.PrimaryScreen.Bounds;Native.SetWindowPos(new System.Windows.Interop.WindowInteropHelper(this).Handle,new IntPtr(-1),physical.Left,physical.Top,physical.Width,physical.Height,0x40);SetThreadExecutionState(0x80000003);}if(wallpaper.Source==null){var image=await WelcomeWallpaper.Load(owner.Preferences.WallpaperPath);if(!closed)wallpaper.Source=image;}};clock.Tick+=delegate{Tick();};owner.Activity.Changed+=RefreshNotices;RefreshNotices();
            Point? swipe=null;PreviewMouseLeftButtonDown+=delegate(object s,MouseButtonEventArgs e){swipe=e.GetPosition(this);};PreviewMouseLeftButtonUp+=delegate(object s,MouseButtonEventArgs e){if(swipe.HasValue&&swipe.Value.Y-e.GetPosition(this).Y>70){StopFace();pinPanel.Visibility=Visibility.Visible;pin.Focus();}swipe=null;};
            PreviewKeyDown+=delegate(object s,KeyEventArgs e){if(e.Key==Key.Enter&&pinPanel.Visibility==Visibility.Visible){e.Handled=true;SubmitPin();}};
            Closing+=delegate(object s,System.ComponentModel.CancelEventArgs e){if(!allowed&&!demonstration&&!owner.IsTesting)e.Cancel=true;};Closed+=delegate{closed=true;clock.Stop();StopFace();media.Dispose();owner.Activity.Changed-=RefreshNotices;if(!demonstration&&!owner.IsTesting)SetThreadExecutionState(0x80000000);};
        }
        private static TextBlock Label(string text,double size,bool bold){var label=Design.Text(text,size,bold);label.Foreground=Brushes.White;return label;}
        private static Button ActionButton(string text,Action action){var b=Design.Button(text,action,false);b.Foreground=Brushes.White;b.Background=Design.Brush("#24FFFFFF");b.FontSize=11;b.Margin=new Thickness(3,8,3,0);return b;}
        private void Tick(){if(closed)return;var now=DateTime.Now;string current=now.ToString("HH:mm");time.Set(current);date.Text=now.ToString("dddd d 'de' MMMM",CultureInfo.GetCultureInfo("es-AR"));}
        private void RefreshNotices(){if(closed)return;notices.Children.Clear();foreach(var item in desktop.Activity.Items.Take(2)){var text=Label(item.Title+" · "+item.Detail,12,false);text.TextWrapping=TextWrapping.Wrap;text.MaxHeight=40;notices.Children.Add(new Border{Background=Design.Brush("#650E1822"),CornerRadius=new CornerRadius(18),Padding=new Thickness(16,10,16,10),Margin=new Thickness(0,3,0,3),Child=text});}}
        private async void SubmitPin()
        {
            if(closed||working)return;string value=pin.Password,again=confirmation.Password;pin.Clear();confirmation.Clear();working=true;submit.IsEnabled=false;
            try{if(!profile.Configured){if(!PrivacyProfile.ValidPin(value)||value!=again){message.Text="Usá 6 a 12 números y repetí el mismo PIN.";return;}await Task.Run(()=>profile.SetPin(value));if(closed)return;canEnroll=true;confirmation.Visibility=Visibility.Collapsed;pinPanel.Visibility=Visibility.Collapsed;faceButton.IsEnabled=false;submit.Content="Desbloquear";message.Text="PIN guardado. Podés registrar tu rostro o volver al escritorio.";var done=ActionButton("Volver al escritorio",Unlock);pinPanel.Children.Add(done);pinPanel.Visibility=Visibility.Visible;}
                else{bool ok=await Task.Run(()=>profile.Verify(value));if(closed)return;if(ok){if(enrollRequested){enrollRequested=false;canEnroll=true;pinPanel.Visibility=Visibility.Collapsed;StartFace(true);}else Unlock();}else message.Text=DateTime.UtcNow<profile.RetryUtc?"Esperá 30 segundos antes de volver a intentar.":"PIN incorrecto. Intentá de nuevo.";}}
            catch(Exception ex){if(!closed)message.Text="No se pudo leer o guardar el perfil de privacidad.";Log.Write("Privacidad: "+ex.GetType().Name);}finally{working=false;if(!closed)submit.IsEnabled=true;}
        }
        internal void Recover(){Unlock();}
        private void Unlock(){if(closed)return;allowed=true;StopFace();Close();}
        private void StopFace(){++generation;var child=worker;worker=null;if(child!=null){try{if(!child.HasExited)child.Kill();}catch(InvalidOperationException){}finally{child.Dispose();}}camera.Source=null;cameraShell.Visibility=Visibility.Collapsed;}
        private async void StartFace(bool enroll)
        {
            if(!File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"MacFlow.Face.exe"))){message.Text="El reconocimiento facial se incluye en 26.5.0. Por ahora usá tu PIN y actualizá desde Configuración → Sistema.";return;}
            if(closed||!profile.Configured||enroll&&!canEnroll)return;if(!enroll&&String.IsNullOrEmpty(profile.Face)){message.Text="Registrá tu rostro después de verificar el PIN.";return;}StopFace();int token=generation;cameraShell.Visibility=Visibility.Visible;pinPanel.Visibility=Visibility.Collapsed;message.Text=enroll?"Registro local · mirá la cámara y mové suavemente la cabeza":"Buscando tu rostro…";
            try
            {
                var child=new Process{StartInfo=new ProcessStartInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"MacFlow.Face.exe"),enroll?"--enroll":"--verify"){UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true}};worker=child;
                child.OutputDataReceived+=delegate(object s,DataReceivedEventArgs e){if(e.Data==null||Dispatcher.HasShutdownStarted)return;string line=e.Data;Dispatcher.BeginInvoke(DispatcherPriority.Background,new Action(delegate{if(closed||token!=generation)return;try{if(line.StartsWith("FRAME:")){using(var stream=new MemoryStream(Convert.FromBase64String(line.Substring(6)))){var image=new BitmapImage();image.BeginInit();image.CacheOption=BitmapCacheOption.OnLoad;image.StreamSource=stream;image.EndInit();image.Freeze();camera.Source=image;}}else if(line.StartsWith("ENROLLED:")){string data=line.Substring(9);if(Convert.FromBase64String(data).Length!=1536)throw new InvalidDataException();profile.Face=data;profile.Save();faceButton.IsEnabled=true;message.Text="Rostro registrado en este equipo. No se guardaron fotos.";StopFace();}else if(line=="VERIFIED")Unlock();else if(line.StartsWith("STATUS:")||line.StartsWith("ERROR:"))message.Text=line.Substring(line.IndexOf(':')+1);}catch(Exception ex){message.Text="No se completó el reconocimiento. Usá tu PIN.";Log.Write("Respuesta facial: "+ex.GetType().Name);StopFace();}}));};
                child.Start();child.BeginOutputReadLine();child.BeginErrorReadLine();if(!enroll)child.StandardInput.WriteLine(profile.Face);child.StandardInput.Close();await Task.Delay(33000);if(!closed&&token==generation){StopFace();message.Text="Usá tu PIN o volvé a intentar el reconocimiento.";}
            }
            catch(Exception ex){if(!closed&&token==generation){StopFace();message.Text="No se pudo iniciar la cámara o el motor facial. Usá tu PIN.";}Log.Write("Motor facial: "+ex.GetType().Name);}
        }
    }
}

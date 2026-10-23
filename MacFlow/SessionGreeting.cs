using System;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
namespace MacFlow
{
    // Reuse the update welcome after Windows sign-in. Never replace Winlogon.
    internal sealed class SessionGreeting : WelcomeWindow
    {
        private readonly DispatcherTimer timeout=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(3600)};
        internal SessionGreeting(BitmapSource wallpaper,System.Drawing.Rectangle screen,Action finished):base(screen,false,wallpaper)
        {
            Title="MacOS · Inicio de sesión";ShowActivated=false;Loaded+=delegate{timeout.Start();};timeout.Tick+=delegate{timeout.Stop();Finish();};Closed+=delegate{timeout.Stop();if(finished!=null)finished();};
        }
        internal void Finish(){timeout.Stop();DismissWelcome();}
    }
}
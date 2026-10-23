using System;
using System.IO;
using System.Windows;
namespace MacFlow
{
    internal static class AssistantLive
    {
        internal static int Run(string folder)
        {
            folder=Path.GetFullPath(folder);Directory.CreateDirectory(folder);Log.DirectoryPath=folder;
            var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};Desktop desktop=null;
            app.Startup+=delegate{Design.InstallTheme();new SettingsStore(folder).Save(new Preferences{AutoHideTaskbar=false,VoiceWakeEnabled=false,CheckUpdatesAutomatically=false,ShowTopBar=false,ShowWidget=false,HideDockInApps=false,HideDockInCodex=false});desktop=new Desktop(folder,true,false);desktop.Dock.Hide();desktop.TopBar.Hide();var assistant=new AssistantWindow(desktop,null);assistant.Closed+=delegate{app.Shutdown();};assistant.ShowInput();};
            app.Exit+=delegate{if(desktop!=null)desktop.Dispose();};return app.Run();
        }
    }
}

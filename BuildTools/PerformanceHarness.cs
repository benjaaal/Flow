using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using MacFlow;

// Isolated integration probe: no changes to taskbar, wallpaper, startup or radios.
public static class PerformanceHarness
{
    [STAThread] public static int Main(string[] args)
    {
        string folder=Path.GetFullPath(args[0]);Directory.CreateDirectory(folder);Log.DirectoryPath=folder;
        var watch=Stopwatch.StartNew();var process=Process.GetCurrentProcess();
        var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};Desktop desktop=null;MotionProbe motion=null;
        app.Startup+=delegate
        {
            new SettingsStore(Path.Combine(folder,"profile")).Save(new Preferences{ShowTopBar=true,ShowWidget=false,HideDockInApps=false,HideDockInCodex=false,HideInFullscreen=false,ContainedDockApplied=true,IconSize=36,LowPower=true});
            desktop=new Desktop(Path.Combine(folder,"profile"),true,true);
            desktop.Dock.ContentRendered+=delegate{File.WriteAllText(Path.Combine(folder,"performance.txt"),"Version: "+typeof(Desktop).Assembly.GetName().Version+"\nFirst Dock frame ms: "+watch.Elapsed.TotalMilliseconds.ToString("F1")+"\n");};
            int stage=0;TimeSpan cpu=TimeSpan.Zero;double started=0;var timer=new DispatcherTimer(DispatcherPriority.Background){Interval=TimeSpan.FromSeconds(6)};
            timer.Tick+=delegate
            {
                if(stage==0){process.Refresh();cpu=process.TotalProcessorTime;started=watch.Elapsed.TotalSeconds;timer.Interval=TimeSpan.FromSeconds(8);}
                else if(stage==1)
                {
                    process.Refresh();double elapsed=watch.Elapsed.TotalSeconds-started;
                    File.AppendAllText(Path.Combine(folder,"performance.txt"),"Idle seconds: "+elapsed.ToString("F2")+"\nIdle CPU machine percent: "+(100*(process.TotalProcessorTime-cpu).TotalSeconds/elapsed/Environment.ProcessorCount).ToString("F3")+"\nPrivate MB: "+(process.PrivateMemorySize64/1048576.0).ToString("F1")+"\nWorking set MB: "+(process.WorkingSet64/1048576.0).ToString("F1")+"\nDetected applications: "+desktop.AppCount+"\nRender tier: "+(System.Windows.Media.RenderCapability.Tier>>16)+"\n");
                    motion=new MotionProbe(desktop);timer.Interval=TimeSpan.FromSeconds(4);
                }
                else {timer.Stop();File.AppendAllText(Path.Combine(folder,"performance.txt"),motion.Report());motion.Dispose();desktop.Dispose();app.Shutdown();}stage++;
            };timer.Start();
        };
        app.DispatcherUnhandledException+=delegate(object s,DispatcherUnhandledExceptionEventArgs e){File.AppendAllText(Path.Combine(folder,"performance.txt"),"\nFAIL: "+e.Exception);e.Handled=true;app.Shutdown(1);};
        return app.Run();
    }
}

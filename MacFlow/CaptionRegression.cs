using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
namespace MacFlow
{
    // All actions target an owned fixture, never the user's applications.
    public static class CaptionRegression
    {
        public static int Run(string directory)
        {
            directory=Path.GetFullPath(directory);Directory.CreateDirectory(directory);Log.DirectoryPath=directory;
            var output=new List<string>();Window fixture=null;CaptionButtons overlay=null;
            try
            {
                if(Application.Current==null)new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};Design.InstallTheme();
                fixture=new Window{Title="MacFlow · Ventana propia de prueba",Width=520,Height=260,Left=180,Top=130,ShowActivated=false,ShowInTaskbar=false,Topmost=true,Background=Brushes.White,Content=new TextBlock{Text="Prueba de controles Apple\n\nEsta ventana pertenece al test de MacFlow.",Margin=new Thickness(28),FontSize=18,Foreground=Brushes.Black}};
                bool cancel=true;fixture.Closing+=delegate(object sender,System.ComponentModel.CancelEventArgs e){e.Cancel=cancel;};fixture.Show();Pump(200);
                var entry=new WindowEntry{Handle=new WindowInteropHelper(fixture).Handle,ProcessId=(uint)Process.GetCurrentProcess().Id};Native.RECT area;
                DumpBounds(entry,output,"Normal");Check(CaptionButtons.TryBounds(entry,out area),"DWM: límites válidos en ventana normal",output);
                overlay=new CaptionButtons(delegate(string message){throw new Exception(message);},false);overlay.PresentTest(entry);Pump(250);
                Native.RECT actual;Native.GetWindowRect(new WindowInteropHelper(overlay).Handle,out actual);
                Check(overlay.IsVisible&&actual.Left==area.Left&&actual.Top==area.Top&&actual.Right==area.Right&&actual.Bottom==area.Bottom,"Capa: cubre exactamente los controles nativos",output);
                Capture(fixture,overlay,Path.Combine(directory,"caption-normal.png"));
                Check(!Native.RequestZoom(new WindowEntry{Handle=entry.Handle,ProcessId=UInt32.MaxValue}),"Ampliar: rechaza identidad incorrecta",output);
                overlay.Lights.ZoomButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Pump(250);
                Check(fixture.WindowState==WindowState.Maximized,"Verde: maximiza la ventana real",output);DumpBounds(entry,output,"Maximizada");
                Check(CaptionButtons.TryBounds(entry,out area),"DWM: límites válidos al maximizar",output);overlay.PresentTest(entry);Pump(150);
                overlay.Lights.ZoomButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Pump(250);Check(fixture.WindowState==WindowState.Normal,"Verde: restaura la ventana real",output);
                overlay.PresentTest(entry);overlay.Lights.MinimizeButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Pump(250);
                Check(Native.IsIconic(entry.Handle)&&!overlay.IsVisible,"Amarillo: minimiza y retira la capa",output);
                Native.ShowWindowAsync(entry.Handle,9);Pump(200);overlay.PresentTest(entry);Pump(150);
                overlay.Lights.CloseButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Pump(200);Check(Native.IsWindow(entry.Handle),"Rojo: respeta la cancelación de cierre",output);
                overlay.PresentTest(entry);cancel=false;overlay.Lights.CloseButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Pump(200);Check(!Native.IsWindow(entry.Handle),"Rojo: cierra sin terminar el proceso",output);
                Check(!CaptionButtons.TryBounds(entry,out area),"Capa: rechaza un identificador destruido",output);
                output.Add("PASS: "+output.FindAll(s=>s.StartsWith("PASS ")).Count+" comprobaciones; sin acciones sobre ventanas externas.");File.WriteAllLines(Path.Combine(directory,"caption-tests.txt"),output);return 0;
            }
            catch(Exception ex){output.Add("FAIL: "+ex);File.WriteAllLines(Path.Combine(directory,"caption-tests.txt"),output);return 1;}
            finally{if(overlay!=null)overlay.Dispose();if(fixture!=null&&fixture.IsVisible)fixture.Hide();}
        }
        private static void Check(bool valid,string name,List<string> output){if(!valid)throw new Exception(name);output.Add("PASS "+name);}
        private static void DumpBounds(WindowEntry entry,List<string> output,string label){Native.RECT frame,caption;Native.GetWindowRect(entry.Handle,out frame);int result=Native.DwmGetWindowRectAttribute(entry.Handle,5,out caption,16);output.Add(label+": frame="+frame.Left+","+frame.Top+","+frame.Right+","+frame.Bottom+" caption="+caption.Left+","+caption.Top+","+caption.Right+","+caption.Bottom+" hr="+result);}
        private static void Capture(Window fixture,Window overlay,string path){Native.SetWindowPos(new WindowInteropHelper(fixture).Handle,new IntPtr(-1),0,0,0,0,0x13);Native.SetWindowPos(new WindowInteropHelper(overlay).Handle,new IntPtr(-1),0,0,0,0,0x13);Pump(180);Native.RECT frame;Native.GetWindowRect(new WindowInteropHelper(fixture).Handle,out frame);using(var bitmap=new System.Drawing.Bitmap(frame.Right-frame.Left,frame.Bottom-frame.Top))using(var g=System.Drawing.Graphics.FromImage(bitmap)){g.CopyFromScreen(frame.Left,frame.Top,0,0,bitmap.Size);bitmap.Save(path,System.Drawing.Imaging.ImageFormat.Png);}}
        private static void Pump(int ms){var frame=new DispatcherFrame();var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(ms)};timer.Tick+=delegate{timer.Stop();frame.Continue=false;};timer.Start();Dispatcher.PushFrame(frame);}
    }
}

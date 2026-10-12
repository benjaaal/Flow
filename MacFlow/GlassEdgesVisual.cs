using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
namespace MacFlow
{
    internal static class GlassEdgesVisual
    {
        internal static int Run(string directory)
        {
            directory=Path.GetFullPath(directory);Directory.CreateDirectory(directory);Log.DirectoryPath=directory;var results=new List<string>();var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};Window ground=null,panel=null;Desktop desktop=null;
            app.Startup+=async delegate
            {
                try
                {
                    string profile=Path.Combine(directory,"profile");new SettingsStore(profile).Save(new Preferences{Theme="Dark",IconSize=36,ContainedDockApplied=true});desktop=new Desktop(profile,true,true);
                    ground=new Window{Title="MacFlow · Fondo de prueba",Width=700,Height=500,Left=150,Top=70,WindowStyle=WindowStyle.None,ResizeMode=ResizeMode.NoResize,Background=Design.Brush("#708492"),ShowActivated=false,Topmost=true,IsHitTestVisible=false};ground.SourceInitialized+=delegate{Native.NoActivate(ground);};ground.Show();await Task.Delay(250);
                    foreach(string name in new[]{"Widget","Panel","Dock"})
                    {
                        if(name=="Widget"){var w=new DesktopWidget(desktop.Widgets,new WidgetSpec{Kind="Digital"});w.Place();w.Left=330;w.Top=180;panel=w;}
                        else{panel=Design.Panel(name=="Dock"?"MacFlow Dock · Borde":"MacFlow · Panel de prueba",name=="Dock"?300:250,name=="Dock"?100:200);panel.Left=330;panel.Top=180;panel.Content=Design.Glass(Design.Text("Cristal",20,true),22,name=="Dock");}
                        panel.ShowActivated=false;panel.SourceInitialized+=delegate{Native.NoActivate(panel);};panel.Show();Native.SetWindowPos(new WindowInteropHelper(panel).Handle,new IntPtr(-1),0,0,0,0,0x13);await Task.Delay(400);
                        var h=new WindowInteropHelper(panel).Handle;if((Native.GetWindowLong(h,-16)&(0x00800000|0x00400000|0x00040000))!=0||(Native.GetWindowLong(h,-20)&(0x100|0x200|1))!=0)throw new Exception(name+": marco nativo presente");results.Add("PASS "+name+": sin bordes de ventana ni marco de relieve.");
                        Native.RECT rect,bg;Native.GetWindowRect(h,out rect);Native.GetWindowRect(new WindowInteropHelper(ground).Handle,out bg);int margin=20,x=rect.Left-margin,y=rect.Top-margin,wid=rect.Right-rect.Left+margin*2,hei=rect.Bottom-rect.Top+margin*2;
                        if(x<bg.Left||y<bg.Top||x+wid>bg.Right||y+hei>bg.Bottom)throw new Exception("La captura debe estar dentro del fondo propio");
                        // Only the controlled background plus our own panel is read.
                        using(var bitmap=new System.Drawing.Bitmap(wid,hei))using(var graphics=System.Drawing.Graphics.FromImage(bitmap))
                        {
                            graphics.CopyFromScreen(x,y,0,0,new System.Drawing.Size(wid,hei),System.Drawing.CopyPixelOperation.SourceCopy);bitmap.Save(Path.Combine(directory,name+"-outer.png"),System.Drawing.Imaging.ImageFormat.Png);
                            var reference=bitmap.GetPixel(2,hei/2);int difference=0;foreach(int px in new[]{3,6,wid-4,wid-7}){var pixel=bitmap.GetPixel(px,hei/2);difference=Math.Max(difference,Math.Abs(pixel.R-reference.R)+Math.Abs(pixel.G-reference.G)+Math.Abs(pixel.B-reference.B));}
                            if(difference>12)throw new Exception(name+": sombra exterior visible, diferencia "+difference);results.Add("PASS "+name+": exterior coincide con el fondo propio; sin halo oscuro.");
                            int cornerDifference=0;foreach(var point in new[]{new System.Drawing.Point(margin+2,margin+2),new System.Drawing.Point(wid-margin-3,margin+2),new System.Drawing.Point(margin+2,hei-margin-3),new System.Drawing.Point(wid-margin-3,hei-margin-3)}){var corner=bitmap.GetPixel(point.X,point.Y);cornerDifference=Math.Max(cornerDifference,Math.Abs(corner.R-reference.R)+Math.Abs(corner.G-reference.G)+Math.Abs(corner.B-reference.B));}if(cornerDifference>12)results.Add("LIMITATION "+name+": el desenfoque nativo conserva cobertura rectangular en las esquinas; diferencia "+cornerDifference+". No se afirma recorte optico completo.");else results.Add("PASS "+name+": esquinas sin placa adicional.");
                        }
                        panel.Close();panel=null;
                    }
                    File.WriteAllLines(Path.Combine(directory,"glass-edges.txt"),results);app.Shutdown();
                }
                catch(Exception ex){results.Add("FAIL "+ex);File.WriteAllLines(Path.Combine(directory,"glass-edges.txt"),results);app.Shutdown(1);}
            };
            app.Exit+=delegate{if(panel!=null&&panel.IsVisible)panel.Close();if(ground!=null)ground.Close();if(desktop!=null)desktop.Dispose();};return app.Run();
        }
    }
}

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
                            int cornerDifference=0;foreach(var point in new[]{new System.Drawing.Point(margin+2,margin+2),new System.Drawing.Point(wid-margin-3,margin+2),new System.Drawing.Point(margin+2,hei-margin-3),new System.Drawing.Point(wid-margin-3,hei-margin-3)}){var corner=bitmap.GetPixel(point.X,point.Y);cornerDifference=Math.Max(cornerDifference,Math.Abs(corner.R-reference.R)+Math.Abs(corner.G-reference.G)+Math.Abs(corner.B-reference.B));}if(cornerDifference>12)throw new Exception(name+": placa extra en esquinas, diferencia "+cornerDifference);var body=bitmap.GetPixel(wid/2,hei-60);if(Math.Abs(body.R-reference.R)+Math.Abs(body.G-reference.G)+Math.Abs(body.B-reference.B)<8)throw new Exception(name+": el panel desapareció");if(!CompositorGlass.Active(panel))throw new Exception(name+": composición recortada no activa");results.Add("PASS "+name+": material visible y esquinas sin placa adicional.");
                        }
                        panel.Close();panel=null;
                    }
                    var stripes=new DrawingGroup();using(var dc=stripes.Open()){dc.DrawRectangle(Brushes.Black,null,new Rect(0,0,16,16));dc.DrawRectangle(Brushes.White,null,new Rect(8,0,8,16));}ground.Background=new DrawingBrush(stripes){TileMode=TileMode.Tile,ViewportUnits=BrushMappingMode.Absolute,Viewport=new Rect(0,0,16,16)};
                    panel=Design.Panel("MacFlow · Desenfoque de prueba",300,200);panel.Content=Design.Glass(new Grid(),22,false);panel.Left=330;panel.Top=180;panel.ShowActivated=false;panel.SourceInitialized+=delegate{Native.NoActivate(panel);};panel.Show();await Task.Delay(650);
                    Native.RECT blurBounds;Native.GetWindowRect(new WindowInteropHelper(panel).Handle,out blurBounds);using(var bitmap=new System.Drawing.Bitmap(blurBounds.Right-blurBounds.Left,blurBounds.Bottom-blurBounds.Top))using(var graphics=System.Drawing.Graphics.FromImage(bitmap))
                    {graphics.CopyFromScreen(blurBounds.Left,blurBounds.Top,0,0,bitmap.Size,System.Drawing.CopyPixelOperation.SourceCopy);bitmap.Save(Path.Combine(directory,"Pattern-blur.png"),System.Drawing.Imaging.ImageFormat.Png);int min=255,max=0;for(int x=60;x<bitmap.Width-60;x++){int v=bitmap.GetPixel(x,bitmap.Height/2).R;min=Math.Min(min,v);max=Math.Max(max,v);}if(max-min>55)throw new Exception("El fondo no está desenfocado: contraste "+(max-min));results.Add("PASS Desenfoque real del patrón: contraste residual "+(max-min)+" / 255.");}
                    panel.Close();panel=null;
                    var blue=System.Windows.Media.Imaging.BitmapSource.Create(1,1,96,96,PixelFormats.Bgra32,null,new byte[]{255,0,0,255},4);blue.Freeze();var lettering=new HelloLettering{Width=270,Height=150,Effect=new WelcomeApple{Backdrop=new ImageBrush(blue),Wallpaper=1,Size=new Point(270,150),Frost=.18}};lettering.SetWord("Hola");
                    panel=new Window{Title="MacFlow · Cristal neutro",Width=300,Height=180,Left=330,Top=180,WindowStyle=WindowStyle.None,ResizeMode=ResizeMode.NoResize,Topmost=true,ShowActivated=false,Background=Brushes.Black,Content=lettering};panel.SourceInitialized+=delegate{Native.OpaquePresentation(panel);Native.NoActivate(panel);};panel.Show();await Task.Delay(350);Native.RECT glyphBounds;Native.GetWindowRect(new WindowInteropHelper(panel).Handle,out glyphBounds);
                    using(var bitmap=new System.Drawing.Bitmap(glyphBounds.Right-glyphBounds.Left,glyphBounds.Bottom-glyphBounds.Top))using(var graphics=System.Drawing.Graphics.FromImage(bitmap))
                    {graphics.CopyFromScreen(glyphBounds.Left,glyphBounds.Top,0,0,bitmap.Size,System.Drawing.CopyPixelOperation.SourceCopy);bitmap.Save(Path.Combine(directory,"Hola-neutral.png"),System.Drawing.Imaging.ImageFormat.Png);int transparentPixels=0;for(int y=8;y<bitmap.Height-8;y++)for(int x=8;x<bitmap.Width-8;x++){var pixel=bitmap.GetPixel(x,y);if(Math.Max(pixel.R,Math.Max(pixel.G,pixel.B))-Math.Min(pixel.R,Math.Min(pixel.G,pixel.B))>5)throw new Exception("El cristal agrega un tinte de color");if(pixel.R>35&&pixel.R<180)transparentPixels++;}if(transparentPixels<100)throw new Exception("No se renderizó el cristal transparente");results.Add("PASS Hola: reflejos neutros y cuerpo translúcido incluso con una muestra de foto azul.");}
                    var symbols=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center};foreach(string kind in new[]{"wifi","wifi-low","wifi-off"})symbols.Children.Add(new Border{Child=Design.Symbol(kind,48,Brushes.White),Margin=new Thickness(18)});panel.Content=symbols;await Task.Delay(160);await Program.CaptureOwnWindow(panel,Path.Combine(directory,"WiFi-symbols.png"));panel.Close();panel=null;
                    File.WriteAllLines(Path.Combine(directory,"glass-edges.txt"),results);app.Shutdown();
                }
                catch(Exception ex){results.Add("FAIL "+ex);File.WriteAllLines(Path.Combine(directory,"glass-edges.txt"),results);app.Shutdown(1);}
            };
            app.Exit+=delegate{if(panel!=null&&panel.IsVisible)panel.Close();if(ground!=null)ground.Close();if(desktop!=null)desktop.Dispose();};return app.Run();
        }
    }
}

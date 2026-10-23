using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
namespace MacFlow
{
    // Astronomy belongs exclusively to MacFlow's own privacy scene. This code
    // never writes the Windows wallpaper or subscribes to an idle render loop.
    internal static class AstronomyWallpaper
    {
        internal static bool IsEarth(string file)
        {
            string name=Path.GetFileName(file);
            return String.Equals(name,"MacFlow-Tierra-1.png",StringComparison.OrdinalIgnoreCase)||String.Equals(name,"MacFlow-Tierra-2.png",StringComparison.OrdinalIgnoreCase)||String.Equals(name,"Tierra-hour-0.jpg",StringComparison.OrdinalIgnoreCase)||String.Equals(name,"Tierra-hour-1.jpg",StringComparison.OrdinalIgnoreCase);
        }
        internal static double Angle(DateTime local){return local.TimeOfDay.TotalHours*15-84;}
        // Local-time art direction, not geolocation or astronomical ephemerides.
        internal static Vector3D SunDirection(DateTime local)
        {double hour=(local.TimeOfDay.TotalHours-12)*Math.PI/12;return new Vector3D(-Math.Sin(hour)*1.2,-.25,-Math.Cos(hour));}
        internal static Task RenderAsync(string file,DateTime local,int width,int height)
        {
            var completion=new TaskCompletionSource<bool>();var worker=new Thread(delegate(){try{Render(file,local,width,height);completion.SetResult(true);}catch(Exception ex){completion.SetException(ex);}});worker.IsBackground=true;worker.Name="MacOS.LockEarth";worker.SetApartmentState(ApartmentState.STA);worker.Start();return completion.Task;
        }
        internal static void Render(string file,DateTime local,int width,int height)
        {
            width=Math.Max(160,Math.Min(2560,width));height=Math.Max(90,Math.Min(1440,height));var scene=new EarthLockScene(local){Width=width,Height=height};scene.Measure(new Size(width,height));scene.Arrange(new Rect(0,0,width,height));scene.UpdateLayout();var frame=new RenderTargetBitmap(width,height,96,96,PixelFormats.Pbgra32);frame.Render(scene);BitmapEncoder encoder=String.Equals(Path.GetExtension(file),".png",StringComparison.OrdinalIgnoreCase)?(BitmapEncoder)new PngBitmapEncoder():new JpegBitmapEncoder{QualityLevel=92};encoder.Frames.Add(BitmapFrame.Create(frame));Directory.CreateDirectory(Path.GetDirectoryName(file));using(var output=File.Create(file))encoder.Save(output);
        }
    }

    internal sealed class EarthLockScene : Grid
    {
        private static readonly object cacheLock=new object();private static BitmapSource texture;private static MeshGeometry3D sphere;
        private readonly AxisAngleRotation3D rotation;private readonly DirectionalLight sunlight;private readonly OrthographicCamera camera;
        private readonly ScaleTransform zoom=new ScaleTransform(1,1);private readonly TranslateTransform drift=new TranslateTransform();private DateTime updated;
        internal EarthLockScene(DateTime local)
        {
            Background=Brushes.Black;IsHitTestVisible=false;ClipToBounds=true;EnsureAssets();
            var brush=new ImageBrush(texture);brush.Freeze();var material=new DiffuseMaterial(brush);material.Freeze();var planet=new GeometryModel3D(sphere,material);planet.BackMaterial=material;
            rotation=new AxisAngleRotation3D(new Vector3D(0,1,0),0);var transforms=new Transform3DGroup();transforms.Children.Add(new RotateTransform3D(rotation));transforms.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0,0,1),-12)));planet.Transform=transforms;
            sunlight=new DirectionalLight(Color.FromRgb(232,239,255),new Vector3D(-1,-.25,-.7));var group=new Model3DGroup();group.Children.Add(new AmbientLight(Color.FromRgb(58,67,85)));group.Children.Add(sunlight);group.Children.Add(planet);
            camera=new OrthographicCamera(new Point3D(0,0,4),new Vector3D(0,0,-1),new Vector3D(0,1,0),4.7);var viewport=new Viewport3D{IsHitTestVisible=false,Camera=camera};viewport.Children.Add(new ModelVisual3D{Content=group});
            var body=new Grid();var atmosphere=new System.Windows.Shapes.Ellipse{Fill=new RadialGradientBrush(new GradientStopCollection{new GradientStop(Colors.Transparent,.91),new GradientStop(Color.FromArgb(100,45,143,255),.965),new GradientStop(Colors.Transparent,1)})};body.Children.Add(atmosphere);body.Children.Add(viewport);var pose=new TransformGroup();var placement=new TranslateTransform();pose.Children.Add(zoom);pose.Children.Add(drift);pose.Children.Add(placement);body.RenderTransformOrigin=new Point(.5,.56);body.RenderTransform=pose;Children.Add(body);
            SizeChanged+=delegate{if(ActualHeight<1)return;camera.Width=3.15*ActualWidth/ActualHeight;atmosphere.Width=atmosphere.Height=ActualHeight*.652;placement.Y=ActualHeight*.11;};UpdateTime(local);
        }
        private static void EnsureAssets()
        {
            lock(cacheLock)
            {
                if(texture!=null)return;BitmapImage image;using(var stream=typeof(EarthLockScene).Assembly.GetManifestResourceStream("MacFlow.Wallpaper.EarthSurface-NASA.jpg")){if(stream==null)throw new IOException("Falta la textura de la Tierra.");image=new BitmapImage();image.BeginInit();image.CacheOption=BitmapCacheOption.OnLoad;image.DecodePixelWidth=2048;image.StreamSource=stream;image.EndInit();image.Freeze();}
                const int latitude=48,longitude=96;var mesh=new MeshGeometry3D();for(int y=0;y<=latitude;y++){double v=(double)y/latitude,phi=Math.PI*v;for(int x=0;x<=longitude;x++){double u=(double)x/longitude,theta=2*Math.PI*u;var p=new Point3D(Math.Sin(phi)*Math.Sin(theta),Math.Cos(phi),Math.Sin(phi)*Math.Cos(theta));mesh.Positions.Add(p);mesh.Normals.Add(new Vector3D(p.X,p.Y,p.Z));mesh.TextureCoordinates.Add(new Point(u,v));}}
                for(int y=0;y<latitude;y++)for(int x=0;x<longitude;x++){int a=y*(longitude+1)+x,b=a+longitude+1;foreach(int i in new[]{a,b,a+1,a+1,b,b+1})mesh.TriangleIndices.Add(i);}mesh.Freeze();sphere=mesh;texture=image;
            }
        }
        internal void UpdateTime(DateTime local){if(local.Date==updated.Date&&local.Hour==updated.Hour&&local.Minute==updated.Minute)return;updated=local;rotation.Angle=AstronomyWallpaper.Angle(local);sunlight.Direction=AstronomyWallpaper.SunDirection(local);}
        internal double Zoom{get{return zoom.ScaleX;}}
        internal void Gesture(double progress){progress=Math.Max(0,Math.Min(1,progress));Stop();zoom.ScaleX=zoom.ScaleY=1+progress*.16;drift.Y=-progress*ActualHeight*.06;}
        internal void Enter(){if(!Motion.Enabled)return;zoom.ScaleX=zoom.ScaleY=1.12;drift.Y=ActualHeight*.025;Animate(1,0,650);}
        internal void Settle(bool exit){Animate(exit?1.24:1,exit?-ActualHeight*.08:0,exit?430:330);}
        private void Animate(double scale,double offset,int ms){Tween(zoom,ScaleTransform.ScaleXProperty,scale,ms);Tween(zoom,ScaleTransform.ScaleYProperty,scale,ms);Tween(drift,TranslateTransform.YProperty,offset,ms);}
        private static void Tween(Animatable target,DependencyProperty property,double value,int ms)
        {
            // Keep the visible pose as the base value until WPF starts its clock.
            // Setting the destination first exposes one frame of the endpoint.
            double current=(double)target.GetValue(property);target.BeginAnimation(property,null);target.SetValue(property,current);if(!Motion.Enabled){target.SetValue(property,value);return;}
            var animation=new DoubleAnimation(current,value,TimeSpan.FromMilliseconds(ms)){EasingFunction=Motion.Ease(),FillBehavior=FillBehavior.Stop};Timeline.SetDesiredFrameRate(animation,60);animation.Completed+=delegate{target.SetValue(property,value);};target.BeginAnimation(property,animation);
        }
        internal void Stop(){double scale=zoom.ScaleX,y=drift.Y;zoom.BeginAnimation(ScaleTransform.ScaleXProperty,null);zoom.BeginAnimation(ScaleTransform.ScaleYProperty,null);drift.BeginAnimation(TranslateTransform.YProperty,null);zoom.ScaleX=zoom.ScaleY=scale;drift.Y=y;}
    }
}

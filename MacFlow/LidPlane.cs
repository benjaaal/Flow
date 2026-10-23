using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Media.Imaging;
namespace MacFlow
{
    // Perspective fallback also works when shader model 3 is unavailable.
    internal sealed class LidPlane : Grid
    {
        internal static readonly DependencyProperty ProgressProperty=DependencyProperty.Register("Progress",typeof(double),typeof(LidPlane),new PropertyMetadata(0.0,delegate(DependencyObject d,DependencyPropertyChangedEventArgs e){((LidPlane)d).Update();}));
        internal double Progress{get{return (double)GetValue(ProgressProperty);}set{SetValue(ProgressProperty,value);}}
        private readonly MeshGeometry3D mesh=new MeshGeometry3D();private readonly PerspectiveCamera camera=new PerspectiveCamera{Position=new Point3D(0,0,4),LookDirection=new Vector3D(0,0,-4),UpDirection=new Vector3D(0,1,0)};
        internal LidPlane(BitmapSource image)
        {Background=Brushes.Black;ClipToBounds=true;mesh.TextureCoordinates=new PointCollection{new Point(0,1),new Point(1,1),new Point(1,0),new Point(0,0)};mesh.TriangleIndices=new Int32Collection{0,1,2,0,2,3};var material=new DiffuseMaterial(new ImageBrush(image));var model=new GeometryModel3D(mesh,material){BackMaterial=material};var group=new Model3DGroup();group.Children.Add(new AmbientLight(Colors.White));group.Children.Add(model);var viewport=new Viewport3D{Camera=camera};viewport.Children.Add(new ModelVisual3D{Content=group});Children.Add(viewport);SizeChanged+=delegate{Update();};Update();}
        private void Update(){double ratio=ActualWidth/Math.Max(1,ActualHeight),angle=Math.Max(0,Math.Min(1,Progress))*1.44;camera.FieldOfView=Math.Max(1,Math.Min(170,2*Math.Atan(ratio/4)*180/Math.PI));mesh.Positions=new Point3DCollection{new Point3D(-ratio,-1,0),new Point3D(ratio,-1,0),new Point3D(ratio,-1+2*Math.Cos(angle),-2*Math.Sin(angle)),new Point3D(-ratio,-1+2*Math.Cos(angle),-2*Math.Sin(angle))};}
    }
}

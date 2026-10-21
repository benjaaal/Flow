using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace MacFlow
{
    // Optical lighting is separate from the compositor backdrop and UI content.
    // No screen sampling, capture exclusion, perpetual render loop or bitmap UI.
    internal sealed class CrystalLighting : ShaderEffect
    {
        private static readonly PixelShader shader=Load();
        private static PixelShader Load(){var value=new PixelShader();using(var stream=typeof(CrystalLighting).Assembly.GetManifestResourceStream("MacFlow.CrystalLight.ps"))value.SetStreamSource(stream);value.Freeze();return value;}
        public static readonly DependencyProperty SizeProperty=DependencyProperty.Register("Size",typeof(Point),typeof(CrystalLighting),new UIPropertyMetadata(new Point(400,200),PixelShaderConstantCallback(0)));
        public static readonly DependencyProperty RadiusProperty=DependencyProperty.Register("Radius",typeof(double),typeof(CrystalLighting),new UIPropertyMetadata(22.0,PixelShaderConstantCallback(1)));
        public static readonly DependencyProperty PointerProperty=DependencyProperty.Register("Pointer",typeof(Point),typeof(CrystalLighting),new UIPropertyMetadata(new Point(.3,.2),PixelShaderConstantCallback(2)));
        public static readonly DependencyProperty ActivityProperty=DependencyProperty.Register("Activity",typeof(double),typeof(CrystalLighting),new UIPropertyMetadata(0.0,PixelShaderConstantCallback(3)));
        public static readonly DependencyProperty IntensityProperty=DependencyProperty.Register("Intensity",typeof(double),typeof(CrystalLighting),new UIPropertyMetadata(1.0,PixelShaderConstantCallback(4)));
        public Point Size{get{return (Point)GetValue(SizeProperty);}set{SetValue(SizeProperty,value);}}
        public double Radius{get{return (double)GetValue(RadiusProperty);}set{SetValue(RadiusProperty,value);}}
        public Point Pointer{get{return (Point)GetValue(PointerProperty);}set{SetValue(PointerProperty,value);}}
        public double Activity{get{return (double)GetValue(ActivityProperty);}set{SetValue(ActivityProperty,value);}}
        public double Intensity{get{return (double)GetValue(IntensityProperty);}set{SetValue(IntensityProperty,value);}}
        internal CrystalLighting(){PixelShader=shader;UpdateShaderValue(SizeProperty);UpdateShaderValue(RadiusProperty);UpdateShaderValue(PointerProperty);UpdateShaderValue(ActivityProperty);UpdateShaderValue(IntensityProperty);}
    }
    internal sealed class CrystalLight : FrameworkElement
    {
        private readonly CrystalLighting lighting;
        private double radius;
        private readonly Brush fallback;
        private readonly Pen upperEdge,lowerEdge;
        private readonly double strength;
        private static readonly DependencyProperty GleamProperty=DependencyProperty.Register("Gleam",typeof(double),typeof(CrystalLight),new FrameworkPropertyMetadata(0.0,FrameworkPropertyMetadataOptions.AffectsRender));
        private FrameworkElement host;
        private DateTime lastMove;
        internal CrystalLighting Lighting {get{return lighting;}}
        internal void SetCorner(double value){radius=value;if(lighting!=null)lighting.Radius=value;InvalidateVisual();}
        internal CrystalLight(double corner,double strength=1)
        {
            radius=corner;this.strength=strength;IsHitTestVisible=false;
            fallback=new LinearGradientBrush(Color.FromArgb(15,255,255,255),Colors.Transparent,90);fallback.Freeze();
            var upper=new LinearGradientBrush{StartPoint=new Point(0,0),EndPoint=new Point(.7,1)};upper.GradientStops.Add(new GradientStop(Color.FromArgb(70,255,255,255),0));upper.GradientStops.Add(new GradientStop(Color.FromArgb(18,255,255,255),.18));upper.GradientStops.Add(new GradientStop(Colors.Transparent,.52));upper.Freeze();upperEdge=new Pen(upper,.65);upperEdge.Freeze();
            var lower=new LinearGradientBrush{StartPoint=new Point(0,0),EndPoint=new Point(0,1)};lower.GradientStops.Add(new GradientStop(Colors.Transparent,.78));lower.GradientStops.Add(new GradientStop(Color.FromArgb(16,255,255,255),1));lower.Freeze();lowerEdge=new Pen(lower,.5);lowerEdge.Freeze();
            // The economy profile keeps the real GPU backdrop but uses vector
            // specular light, avoiding an off-screen shader surface per tile.
            // Vector highlights retain the real compositor backdrop without an off-screen shader per surface.
            SizeChanged+=delegate{if(lighting!=null)lighting.Size=new Point(Math.Max(1,ActualWidth),Math.Max(1,ActualHeight));};
            Loaded+=delegate{host=Parent as FrameworkElement;if(host!=null){host.MouseMove+=Move;host.MouseEnter+=Enter;host.MouseLeave+=Leave;}};
            Unloaded+=delegate{if(host!=null){host.MouseMove-=Move;host.MouseEnter-=Enter;host.MouseLeave-=Leave;host=null;}if(lighting!=null){lighting.BeginAnimation(CrystalLighting.PointerProperty,null);lighting.BeginAnimation(CrystalLighting.ActivityProperty,null);lighting.Activity=0;}};
        }
        protected override void OnRender(DrawingContext dc)
        {
            if(ActualWidth<=0||ActualHeight<=0)return;
            // A transparent primitive gives the GPU an output surface; content
            // stays in a sibling layer and cannot be blurred by this effect.
            if(lighting!=null){dc.DrawRoundedRectangle(Brushes.Transparent,null,new Rect(0,0,ActualWidth,ActualHeight),radius,radius);return;}
            if(SystemParameters.HighContrast)return;
            var bounds=new Rect(.6,.6,Math.Max(0,ActualWidth-1.2),Math.Max(0,ActualHeight-1.2));dc.PushOpacity(strength*(.72+(double)GetValue(GleamProperty)*.28));dc.DrawRoundedRectangle(null,upperEdge,bounds,radius,radius);dc.DrawRoundedRectangle(null,lowerEdge,bounds,radius,radius);dc.Pop();
        }
        private void Enter(object sender,System.Windows.Input.MouseEventArgs e){Activity(1);Move(sender,e);}
        private void Leave(object sender,System.Windows.Input.MouseEventArgs e){Activity(0);}
        private void Activity(double target)
        {
            if(lighting==null){double current=(double)GetValue(GleamProperty);BeginAnimation(GleamProperty,null);SetValue(GleamProperty,target);if(Motion.Enabled)BeginAnimation(GleamProperty,new DoubleAnimation(current,target,TimeSpan.FromMilliseconds(Motion.Hover)){EasingFunction=Motion.Ease(),FillBehavior=FillBehavior.Stop});return;}
            if(!Motion.Enabled){lighting.BeginAnimation(CrystalLighting.ActivityProperty,null);lighting.Activity=target;return;}
            Motion.To(lighting,CrystalLighting.ActivityProperty,target,Motion.Hover);
        }
        private void Move(object sender,System.Windows.Input.MouseEventArgs e)
        {
            if(lighting==null||ActualWidth<=0||ActualHeight<=0||DateTime.UtcNow-lastMove<TimeSpan.FromMilliseconds(32))return;lastMove=DateTime.UtcNow;
            var p=e.GetPosition(this);var target=new Point(Math.Max(0,Math.Min(1,p.X/ActualWidth)),Math.Max(0,Math.Min(1,p.Y/ActualHeight)));
            if(!Motion.Enabled){lighting.BeginAnimation(CrystalLighting.PointerProperty,null);lighting.Pointer=target;return;}
            lighting.BeginAnimation(CrystalLighting.PointerProperty,null);lighting.Pointer=target;
        }
    }
}

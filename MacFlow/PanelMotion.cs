using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
namespace MacFlow
{
    internal sealed class SpringEase : EasingFunctionBase
    {
        internal static double Progress(double time){return Motion.Arrival(time,.78);}
        protected override double EaseInCore(double time){return Progress(time);}
        protected override Freezable CreateInstanceCore(){return new SpringEase();}
    }
    // Live windows keep their aspect ratio. A close is a quiet dismissal;
    // only minimize/restore follows the corresponding Dock icon.
    internal static class PanelMotion
    {
        private static readonly DependencyProperty BaseTop=DependencyProperty.RegisterAttached("BaseTop",typeof(double),typeof(PanelMotion),new PropertyMetadata(Double.NaN));
        private static readonly DependencyProperty Closing=DependencyProperty.RegisterAttached("Closing",typeof(bool),typeof(PanelMotion),new PropertyMetadata(false));
        private static readonly DependencyProperty Opening=DependencyProperty.RegisterAttached("Opening",typeof(bool),typeof(PanelMotion),new PropertyMetadata(false));
        private static readonly DependencyProperty OriginalTransform=DependencyProperty.RegisterAttached("OriginalTransform",typeof(Transform),typeof(PanelMotion),new PropertyMetadata(null));
        private static readonly DependencyProperty OriginalOrigin=DependencyProperty.RegisterAttached("OriginalOrigin",typeof(Point),typeof(PanelMotion),new PropertyMetadata(new Point()));
        private static readonly DependencyProperty ExitProgress=DependencyProperty.RegisterAttached("ExitProgress",typeof(double),typeof(PanelMotion),new PropertyMetadata(0.0,Changed));
        internal static readonly DependencyProperty DockFlight=DependencyProperty.RegisterAttached("DockFlight",typeof(bool),typeof(PanelMotion),new PropertyMetadata(false));
        internal static readonly DependencyProperty UseDockEntrance=DependencyProperty.RegisterAttached("UseDockEntrance",typeof(bool),typeof(PanelMotion),new PropertyMetadata(false));
        internal static readonly DependencyProperty DockKey=DependencyProperty.RegisterAttached("DockKey",typeof(string),typeof(PanelMotion),new PropertyMetadata(null));
        internal static readonly DependencyProperty DockPoint=DependencyProperty.RegisterAttached("DockPoint",typeof(Point),typeof(PanelMotion),new PropertyMetadata(new Point(Double.NaN,Double.NaN)));
        internal static event Action<Window> Frame;
        internal static Func<Window,Point?> DockDestination;
        internal static bool IsClosing(Window window){return (bool)window.GetValue(Closing);}
        internal static bool IsTransitioning(Window window){return IsClosing(window)||(bool)window.GetValue(Opening);}
        internal static bool FromTop(Window window)
        {return window.Title=="MacOS · Centro de control"||window.Title=="MacOS · Conexiones"||window.Title=="MacOS · Widgets"||window.Title=="MacOS · Actividad"||window.Title=="MacOS · Stage Manager · Grupos"||window.Title=="MacOS · Menú";}
        internal static Point? IconDestination(Window source,Window dock,Native.RECT resting)
        {
            string key=source.GetValue(DockKey) as string;if(String.IsNullOrEmpty(key))return null;var icon=FindIcon(dock.Content as DependencyObject,key);if(icon==null||!icon.IsLoaded)return null;
            Native.RECT current;Native.GetWindowRect(new System.Windows.Interop.WindowInteropHelper(dock).Handle,out current);
            var point=icon.PointToScreen(new Point(icon.ActualWidth/2,icon.ActualHeight/2));return new Point(point.X+resting.Left-current.Left,point.Y+resting.Top-current.Top);
        }
        private static FrameworkElement FindIcon(DependencyObject node,string key)
        {if(node==null)return null;var item=node as System.Windows.Controls.Border;if(item!=null&&String.Equals(item.Tag as string,key,StringComparison.OrdinalIgnoreCase))return item;for(int i=0;i<VisualTreeHelper.GetChildrenCount(node);i++){var found=FindIcon(VisualTreeHelper.GetChild(node,i),key);if(found!=null)return found;}return null;}
        private static void Prepare(Window window)
        {
            var content=window.Content as FrameworkElement;if(content==null||window.GetValue(OriginalTransform)!=null)return;
            window.SetValue(OriginalTransform,content.RenderTransform??Transform.Identity);window.SetValue(OriginalOrigin,content.RenderTransformOrigin);
            var transforms=new TransformGroup();transforms.Children.Add((Transform)window.GetValue(OriginalTransform));transforms.Children.Add(new ScaleTransform(1,1));content.RenderTransformOrigin=new Point(.5,.5);content.RenderTransform=transforms;
        }
        private static void Changed(DependencyObject target,DependencyPropertyChangedEventArgs args)
        {
            var window=(Window)target;if(!IsTransitioning(window)||!WindowLife.Alive(window))return;double fraction=1-Math.Max(0,Math.Min(.06,(double)args.NewValue));
            var content=window.Content as FrameworkElement;var group=content==null?null:content.RenderTransform as TransformGroup;
            if(group!=null&&group.Children.Count>1){var scale=group.Children[group.Children.Count-1] as ScaleTransform;if(scale!=null)scale.ScaleX=scale.ScaleY=fraction;}
            Native.SetGlassClip(window,fraction);var frame=Frame;if(frame!=null)frame(window);
        }
        internal static void Open(Window window)
        {
            if(window.WindowState!=WindowState.Normal||Double.IsNaN(window.Top)||!Motion.Enabled)return;
            bool continuing=IsClosing(window);if(!continuing)Reset(window);double top=window.Top;window.SetValue(BaseTop,top);window.SetValue(Opening,true);window.SetValue(Closing,false);Prepare(window);
            if(!continuing)window.SetValue(ExitProgress,FromTop(window)?.018:.035);
            Motion.To(window,ExitProgress,0,FromTop(window)?Motion.Menu:Motion.Panel,false,delegate{if(WindowLife.Alive(window)&&(bool)window.GetValue(Opening))Reset(window);});
            window.Top=top+(FromTop(window)?-10:10);Motion.To(window,Window.TopProperty,top,FromTop(window)?Motion.Menu:Motion.Panel);
        }
        internal static void Close(Window window)
        {
            if(window.WindowState==WindowState.Minimized)return;double top=window.Top;if(Double.IsNaN((double)window.GetValue(BaseTop)))window.SetValue(BaseTop,top);
            window.SetValue(Opening,false);window.SetValue(Closing,true);Prepare(window);Motion.To(window,ExitProgress,FromTop(window)?.022:.045,Motion.WindowClose,true);
            if(window.WindowState==WindowState.Normal)Motion.To(window,Window.TopProperty,top+(FromTop(window)?-8:8),Motion.WindowClose,true);
        }
        internal static void CompleteClose(Window window)
        {if(IsClosing(window)&&window.IsVisible){window.BeginAnimation(ExitProgress,null);window.SetValue(ExitProgress,FromTop(window)?.022:.045);}}
        internal static void Reset(Window window)
        {
            window.SetValue(Closing,false);window.SetValue(Opening,false);window.BeginAnimation(ExitProgress,null);window.SetValue(ExitProgress,0.0);
            var content=window.Content as FrameworkElement;var previous=window.GetValue(OriginalTransform) as Transform;
            if(content!=null&&previous!=null){content.RenderTransform=previous;content.RenderTransformOrigin=(Point)window.GetValue(OriginalOrigin);window.ClearValue(OriginalTransform);window.ClearValue(OriginalOrigin);}
            double top=(double)window.GetValue(BaseTop);window.BeginAnimation(Window.TopProperty,null);if(window.WindowState==WindowState.Normal&&!Double.IsNaN(top))window.Top=top;window.SetValue(BaseTop,Double.NaN);Native.SetGlassClip(window,1);
        }
    }
}

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace MacFlow
{
    // One HWND contains the bar and its expanding center. The native region is
    // the union of the bar and capsule; transparent space never eats clicks.
    internal sealed class ShellTopBar : Grid,IDisposable
    {
        private static readonly DependencyProperty HostProperty=DependencyProperty.RegisterAttached("Host",typeof(ShellTopBar),typeof(ShellTopBar),new PropertyMetadata(null));
        internal static ShellTopBar For(Window window){return window.GetValue(HostProperty) as ShellTopBar;}
        private readonly Window owner;
        private readonly FrameworkElement right;
        private readonly ContentPresenter center=new ContentPresenter{HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Top,Margin=new Thickness(4,2,4,2)};
        private readonly SizeChangedEventHandler changed;
        private bool syncing,disposed;
        internal double AvailableCenter {get{return Math.Max(76,ActualWidth-2*Math.Max(135,Math.Min(right.DesiredSize.Width+20,ActualWidth*.4))-16);}}
        private ShellTopBar(Window window,FrameworkElement left,FrameworkElement indicators)
        {
            owner=window;owner.SetValue(HostProperty,this);right=indicators;VerticalAlignment=VerticalAlignment.Top;
            ColumnDefinitions.Add(new ColumnDefinition());ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});ColumnDefinitions.Add(new ColumnDefinition());
            var face=Design.Glass(new Grid(),10,true);face.Background=SystemParameters.HighContrast?SystemColors.WindowBrush:Design.Brush(Design.Dark?"#F01E1E20":"#F5F5F5F7");face.Height=Desktop.TopBarHeight;face.VerticalAlignment=VerticalAlignment.Top;face.Padding=new Thickness(0);SetColumnSpan(face,3);Children.Add(face);
            left.Height=right.Height=Desktop.TopBarHeight;left.VerticalAlignment=right.VerticalAlignment=VerticalAlignment.Top;left.Margin=new Thickness(10,0,0,0);right.Margin=new Thickness(0,0,10,0);left.ClipToBounds=right.ClipToBounds=true;
            Children.Add(left);SetColumn(center,1);Children.Add(center);SetColumn(right,2);Children.Add(right);
            changed=delegate{Synchronize();};SizeChanged+=changed;owner.SizeChanged+=changed;
            owner.SetValue(Native.CustomShellRegion,new Action(UpdateRegion));
        }
        internal static void Install(Window owner,FrameworkElement left,FrameworkElement right)
        {
            var previous=For(owner);if(previous!=null)previous.Dispose();
            owner.Content=new ShellTopBar(owner,left,right);
        }
        internal void Mount(FrameworkElement view)
        {
            if(Object.ReferenceEquals(center.Content,view))return;
            var parent=LogicalTreeHelper.GetParent(view) as ContentPresenter;if(parent!=null)parent.Content=null;
            center.Content=view;Synchronize();
        }
        internal void Unmount(FrameworkElement view){if(Object.ReferenceEquals(center.Content,view)){center.Content=null;Synchronize();}}
        internal void RefreshRegion(){Synchronize();}
        private void Synchronize()
        {
            if(syncing||disposed)return;syncing=true;
            try{var view=center.Content as FrameworkElement;double h=view==null||view.Visibility==Visibility.Collapsed?0:view.ActualHeight+4;double height=Math.Max(Desktop.TopBarHeight,h);if(Math.Abs(owner.Height-height)>.1)owner.Height=height;UpdateRegion();}finally{syncing=false;}
        }
        private void UpdateRegion()
        {
            if(disposed)return;var view=center.Content as FrameworkElement;Rect capsule=Rect.Empty;
            if(view!=null&&view.Visibility==Visibility.Visible&&view.ActualWidth>0&&view.ActualHeight>0){try{capsule=new Rect(view.TranslatePoint(new Point(),owner),new Size(view.ActualWidth,view.ActualHeight));}catch(InvalidOperationException){}}
            Native.SetIntegratedBarRegion(owner,Desktop.TopBarHeight,capsule);
        }
        public void Dispose(){if(disposed)return;disposed=true;SizeChanged-=changed;owner.SizeChanged-=changed;center.Content=null;owner.ClearValue(Native.CustomShellRegion);}
    }
}

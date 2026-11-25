using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
namespace MacFlow
{
    public sealed class MacWindowChrome : DockPanel
    {
        public readonly WindowControls Controls;
        public MacWindowChrome(Window window,string title,Action close)
        {
            LastChildFill=true;Margin=new Thickness(0,0,0,16);
            Controls=new WindowControls(window,close);SetDock(Controls,Dock.Left);Children.Add(Controls);
            var label=Design.Text(title,13,true);label.VerticalAlignment=VerticalAlignment.Center;label.Margin=new Thickness(12,0,0,0);Children.Add(label);
            MouseLeftButtonDown+=delegate(object sender,MouseButtonEventArgs e){
                var node=e.OriginalSource as DependencyObject;
                while(node!=null&&node!=this){if(node is Button||node is TextBox||node is ComboBox)return;node=node is Visual?VisualTreeHelper.GetParent(node):LogicalTreeHelper.GetParent(node);}
                if(e.ClickCount==2){if(window.ResizeMode!=ResizeMode.NoResize&&window.ResizeMode!=ResizeMode.CanMinimize)window.WindowState=window.WindowState==WindowState.Maximized?WindowState.Normal:WindowState.Maximized;}
                else if(e.LeftButton==MouseButtonState.Pressed){try{window.DragMove();}catch(InvalidOperationException){}}
                e.Handled=true;
            };
        }
    }
}

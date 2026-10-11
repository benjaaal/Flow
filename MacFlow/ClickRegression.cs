using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
namespace MacFlow
{
    internal static class ClickRegression
    {
        internal static void Run(List<string> results)
        {
            int clicks=0;var button=Design.Button("Iniciar",delegate{clicks++;},false);button.Width=170;button.Height=46;button.Margin=new Thickness(0);
            var window=new Window{Width=210,Height=90,Left=-2000,Top=-2000,ShowInTaskbar=false,ShowActivated=false,WindowStyle=WindowStyle.None,Content=button};
            try
            {
                window.Show();window.UpdateLayout();button.ApplyTemplate();CheckBounds(button);
                button.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice,0){RoutedEvent=Mouse.MouseEnterEvent});Pump(270);CheckBounds(button);
                button.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice,0,MouseButton.Left){RoutedEvent=UIElement.PreviewMouseLeftButtonDownEvent});Pump(270);CheckBounds(button);
                var scale=(ScaleTransform)button.RenderTransform;var face=(Border)button.Template.FindName("AnimatedFace",button);
                if(Math.Abs(scale.ScaleX-1)>.001||((ScaleTransform)face.RenderTransform).ScaleX>=1)throw new Exception("La animación sigue cambiando la zona de clic");
                button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));if(clicks!=1)throw new Exception("El botón no ejecuta una única acción");
                results.Add("PASS Clics: los cuatro extremos siguen dentro del botón durante hover y pulsación; sólo se anima la cara, acción una vez.");
            }
            finally{window.Close();}
        }
        private static void CheckBounds(Button button)
        {
            foreach(var point in new[]{new Point(.25,.25),new Point(button.ActualWidth-.25,.25),new Point(.25,button.ActualHeight-.25),new Point(button.ActualWidth-.25,button.ActualHeight-.25)})
            {var hit=button.InputHitTest(point) as DependencyObject;while(hit!=null&&hit!=button)hit=VisualTreeHelper.GetParent(hit);if(hit!=button)throw new Exception("La zona de clic se encogió al animar el botón");}
        }
        private static void Pump(int ms){var frame=new DispatcherFrame();var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(ms)};timer.Tick+=delegate{timer.Stop();frame.Continue=false;};timer.Start();Dispatcher.PushFrame(frame);}
    }
}

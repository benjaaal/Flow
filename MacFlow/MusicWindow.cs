using System;
using System.Windows;
using System.Windows.Controls;
namespace MacFlow
{
    internal static class MusicWindow
    {
        private static Window current;
        internal static void CloseCurrent(){if(current!=null)WindowLife.Close(current);current=null;}
        internal static void Open(Desktop desktop)
        {
            if(WindowLife.Alive(current)){if(!desktop.IsTesting)current.Activate();return;}
            var panel=Design.Panel("MacFlow · Música",380,540);current=panel;
            var media=new MediaCard(desktop.IsTesting?(IMediaSource)new MediaFixture():new WindowsMediaSource()){HideWhenEmpty=true};media.SetExpanded(true);
            var root=new DockPanel();var close=Design.Button("Cerrar",delegate{WindowLife.Close(panel);},false);close.HorizontalAlignment=HorizontalAlignment.Right;DockPanel.SetDock(close,Dock.Top);root.Children.Add(close);root.Children.Add(media);panel.Content=Design.Glass(root,26,false);
            media.MediaChanged+=delegate{if(!media.HasActiveSession)WindowLife.Close(panel);};panel.Closed+=delegate{media.Dispose();if(current==panel)current=null;};
            if(desktop.IsTesting){panel.ShowActivated=false;panel.IsHitTestVisible=false;panel.SourceInitialized+=delegate{Native.NoActivate(panel);};}
            DisplaySpace.Center(panel,.18);panel.Show();Design.Reveal(panel);
        }
    }
}

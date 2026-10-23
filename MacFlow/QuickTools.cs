using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace MacFlow
{
    internal static class QuickTools
    {
        private static Window current;
        internal static void Close(){WindowLife.Close(current);current=null;}
        internal static void Open(Desktop desktop)
        {
            if(WindowLife.Alive(current)){current.Activate();return;}
            var view=Design.Panel("MacOS · Emojis y portapapeles",430,440);current=view;var root=new DockPanel();
            var header=new DockPanel();var close=Design.Button("Cerrar",()=>Design.Dismiss(view),false);DockPanel.SetDock(close,Dock.Right);header.Children.Add(close);header.Children.Add(Design.Text("Emojis y texto",19,true));DockPanel.SetDock(header,Dock.Top);root.Children.Add(header);
            var status=Design.Text("Elegí un emoji para copiarlo. Pegalo con Ctrl+V.",11,false);status.TextWrapping=TextWrapping.Wrap;status.Margin=new Thickness(0,12,0,12);DockPanel.SetDock(status,Dock.Bottom);root.Children.Add(status);
            var items=new WrapPanel();string[] symbols={"😀","😊","🥰","😎","🤔","🙌","👍","👏","🙏","❤️","✨","🎉","🔥","💡","📌","✅","🌿","☀️","🌙","🎧","🎵","💻","📁","🗓️","📝","🚀","☕","🏠","🌎","💬"};
            foreach(string symbol in symbols){string emoji=symbol;var b=Design.Button(emoji,delegate{try{Clipboard.SetText(emoji);status.Text="Copiado · "+emoji;}catch(Exception ex){status.Text=ex.Message;}},false);b.FontFamily=new FontFamily("Segoe UI Emoji");b.FontSize=24;b.Width=49;b.Height=47;b.Padding=new Thickness(4);b.ToolTip="Copiar "+emoji;items.Children.Add(b);}
            var content=new StackPanel();content.Children.Add(items);var capture=Design.Button("Leer texto actual del portapapeles",delegate{try{string text=Clipboard.ContainsText()?Clipboard.GetText():"No contiene texto.";status.Text=text.Length>400?text.Substring(0,400)+"…":text;}catch(Exception ex){status.Text=ex.Message;}},false);capture.Margin=new Thickness(0,15,0,0);content.Children.Add(capture);root.Children.Add(new ScrollViewer{Content=content,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});view.Content=Design.Glass(root,22,false);view.Closed+=delegate{if(current==view)current=null;};view.Show();DisplaySpace.Center(view,.18);Design.Reveal(view);
        }
    }
}

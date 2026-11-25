using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MacFlow
{
    internal sealed class BuiltInApp
    {
        internal readonly string Id, Name, Description, Icon;
        internal readonly Action<Desktop> Launch;
        internal BuiltInApp(string id,string name,string description,string icon,Action<Desktop> launch)
        { Id="macflow-app-"+id;Name=name;Description=description;Icon=icon;Launch=launch; }
        internal AppEntry Entry(){return new AppEntry{Name=Name,Target=Id,IconPath="macflow-glyph-"+Icon};}
    }

    public sealed partial class Desktop
    {
        internal static readonly BuiltInApp[] BuiltInApps={
            new BuiltInApp("finder","Finder","Explorar carpetas y gestionar archivos","folder",d=>d.OpenFinder()),
            new BuiltInApp("settings","Configuración de MacFlow","Apariencia, accesibilidad y recuperación","settings",d=>d.OpenSettings()),
            new BuiltInApp("calculator","Calculadora","Operaciones, potencias y paréntesis","calculator",d=>d.OpenBuiltInPanel("calculator",()=>new CalculatorPanel())),
            new BuiltInApp("calendar","Calendario","Agenda local por fecha","calendar",d=>d.OpenBuiltInPanel("calendar",()=>new CalendarPanel(d.Store.DirectoryPath))),
            new BuiltInApp("notes","Notas","Bloc persistente del perfil","notes",d=>d.OpenBuiltInPanel("notes",()=>new NotesPanel(d.Store.DirectoryPath))),
            new BuiltInApp("activity","Monitor de actividad","Procesos y memoria en uso","performance",d=>d.OpenBuiltInPanel("activity",()=>new ProcessPanel(d))),
            new BuiltInApp("pictures","Visualizador de imágenes","Abrir una imagen en Vista rápida","preview",d=>d.ChooseImage()),
            new BuiltInApp("trash","Papelera de Windows","Consultar y restaurar elementos eliminados","trash",d=>d.OpenRecycleBin()),
            new BuiltInApp("terminal","Terminal de Windows","Abrir un terminal interactivo de la sesión","terminal",d=>d.OpenTerminal()),
            new BuiltInApp("catalog","Biblioteca de MacFlow","Abrir aplicaciones integradas y fijarlas en el Dock","grid",d=>d.OpenBuiltInPanel("catalog",()=>new AppLibrary(d)))
        };

        private readonly Dictionary<string,FeaturePanel> builtInPanels=new Dictionary<string,FeaturePanel>();
        internal static BuiltInApp FindBuiltIn(string target)
        { return BuiltInApps.FirstOrDefault(a=>String.Equals(a.Id,target,StringComparison.OrdinalIgnoreCase)); }

        private void OpenBuiltInPanel(string id,Func<FeaturePanel> create)
        {
            if(disposed)return;
            FeaturePanel window;
            if(builtInPanels.TryGetValue(id,out window)){RestoreFeature(window);return;}
            window=create();
            window.SetValue(PanelMotion.DockKey,"macflow-app-"+id);
            builtInPanels.Add(id,window);
            window.Closed+=delegate{builtInPanels.Remove(id);};
            ShowModule(window);
        }

        private void ChooseImage()
        {
            var picker=new Microsoft.Win32.OpenFileDialog{CheckFileExists=true,Filter="Imágenes|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff;*.ico"};
            if(picker.ShowDialog()==true)OpenPreview(picker.FileName);
        }

        private void OpenTerminal()
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("wt.exe"){UseShellExecute=true});
            }
            catch(System.ComponentModel.Win32Exception)
            {
                OpenUri(Path.Combine(Environment.SystemDirectory,@"WindowsPowerShell\v1.0\powershell.exe"));
            }
        }
    }

    internal sealed class AppLibrary : FeaturePanel
    {
        internal AppLibrary(Desktop desktop):base("Biblioteca",680,610)
        {
            var rows=new StackPanel();
            foreach(var app in Desktop.BuiltInApps.Where(a=>a.Id!="macflow-app-catalog"))
            {
                var captured=app;
                var entry=captured.Entry();
                var row=new DockPanel();
                var actions=new StackPanel{Orientation=Orientation.Horizontal};
                var pin=Design.Button(PinText(desktop,entry),null,false);
                pin.Click+=delegate{desktop.Pin(entry);pin.Content=PinText(desktop,entry);};
                actions.Children.Add(Design.Button("Abrir",delegate{desktop.Launch(entry);},true));
                actions.Children.Add(pin);
                DockPanel.SetDock(actions,Dock.Right);row.Children.Add(actions);
                var icon=new Image{Source=FeatureIcons.Get(captured.Icon),Width=34,Height=34,Margin=new Thickness(0,0,12,0)};
                DockPanel.SetDock(icon,Dock.Left);row.Children.Add(icon);
                var text=new StackPanel();text.Children.Add(Design.Text(captured.Name,14,true));text.Children.Add(Caption(captured.Description));row.Children.Add(text);
                rows.Children.Add(Design.Card(row));
            }
            Body.Children.Add(new ScrollViewer{Content=rows,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});
        }
        private static string PinText(Desktop desktop,AppEntry entry)
        {return desktop.Preferences.Pins.Any(p=>p.Key==entry.Key)?"Quitar del Dock":"Fijar en el Dock";}
    }
}

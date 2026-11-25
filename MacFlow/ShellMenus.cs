using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
namespace MacFlow
{
    internal sealed class ShelfPanel : FeaturePanel
    {
        private readonly Desktop desktop;private NotchShelfView view;
        internal ShelfPanel(Desktop desktop):base("Shelf",460,350){this.desktop=desktop;Reload();}
        internal void Reload(){Body.Children.Clear();Body.Background=Design.Brush("#18181B");view=new NotchShelfView(desktop);Body.Children.Add(view);}internal void Add(string[] paths){if(paths!=null)view.Add(paths);}
    }
    public sealed partial class Desktop
    {
        private void AddApplicationMenus(StackPanel bar)
        {
            foreach(string label in new[]{"Archivo","Edición","Ver","Ventana","Ayuda"}){string menu=label;var button=Design.Button(label,delegate{ShowApplicationMenu(menu);},false);button.ToolTip="Menú de MacFlow · las aplicaciones Windows conservan su propio menú";bar.Children.Add(button);}
        }
        private void ShowApplicationMenu(string menu)
        {
            var commands=new List<GlassCommand>();bool finder=Finder!=null&&Finder.IsActive;
            if(menu=="Archivo"){
                commands.Add(new GlassCommand("Abrir Finder",OpenFinder));if(finder){commands.Add(new GlassCommand("Nueva carpeta",delegate{Finder.ExecuteCommand("Nueva carpeta");}));commands.Add(new GlassCommand("Renombrar selección",delegate{Finder.ExecuteCommand("Renombrar");}));}commands.Add(new GlassCommand("Ajustes de MacFlow",OpenSettings));
            }else if(menu=="Edición"){
                if(finder){commands.Add(new GlassCommand("Copiar a carpeta…",delegate{Finder.ExecuteCommand("Copiar");}));commands.Add(new GlassCommand("Mover a carpeta…",delegate{Finder.ExecuteCommand("Mover");}));commands.Add(new GlassCommand("Enviar a la Papelera…",delegate{Finder.ExecuteCommand("Papelera");}));}
                else {var active=Application.Current.Windows.Cast<Window>().FirstOrDefault(w=>w.IsActive);var input=active==null?null:FocusManager.GetFocusedElement(active) as TextBox;if(input!=null){commands.Add(new GlassCommand("Copiar",input.Copy));commands.Add(new GlassCommand("Pegar",input.Paste));commands.Add(new GlassCommand("Seleccionar todo",input.SelectAll));}else commands.Add(new GlassCommand("Abrir Finder para editar archivos",OpenFinder));}
            }else if(menu=="Ver"){
                if(finder){commands.Add(new GlassCommand("Lista",delegate{Finder.SetView(0);}));commands.Add(new GlassCommand("Iconos",delegate{Finder.SetView(1);}));commands.Add(new GlassCommand("Galería",delegate{Finder.SetView(2);}));}commands.Add(new GlassCommand("Ventanas abiertas de Windows",OpenOverview));commands.Add(new GlassCommand(Preferences.ShowActivityIsland==false?"Mostrar isla dinámica":"Ocultar isla dinámica",delegate{SetIslandEnabled(Preferences.ShowActivityIsland==false);}));
            }else if(menu=="Ventana"){OpenOwnWindowMenu();return;}
            else{commands.Add(new GlassCommand("Acerca de MacFlow",delegate{MessageBox.Show("MacFlow "+Product.VersionText+"\nEntorno para Windows, con recursos originales.\nMenú MacFlow → Salir restaura Windows.\nWin+Tab administra los escritorios de Windows.\nLas aplicaciones externas mantienen sus controles nativos.","Acerca de MacFlow");}));commands.Add(new GlassCommand("Ajustes y recuperación",OpenSettings));}
            GlassMenu.Open(TopBar,menu,commands,IsTesting);
        }
        internal void OpenOwnWindowMenu()
        {
            var commands=new List<GlassCommand>();foreach(var window in Application.Current.Windows.Cast<Window>().Where(w=>w!=Dock&&w!=TopBar&&(w is FeaturePanel||w==Settings||w.WindowState==WindowState.Minimized)).ToArray()){var target=window;commands.Add(new GlassCommand(target.Title,delegate{RestoreFeature(target);}));}
            commands.Add(new GlassCommand("Abrir Finder",OpenFinder));commands.Add(new GlassCommand("Ajustes",OpenSettings));GlassMenu.Open(Dock,"Ventanas de MacFlow",commands,IsTesting);
        }
        private ShelfPanel shelfPanel;
        internal void OpenShelf(){OpenShelf(null);}internal void OpenShelf(string[] paths){if(shelfPanel!=null){shelfPanel.Reload();RestoreFeature(shelfPanel);shelfPanel.Add(paths);return;}shelfPanel=new ShelfPanel(this);shelfPanel.Closed+=delegate{shelfPanel=null;};ShowFeature(shelfPanel,.12);shelfPanel.Add(paths);}
        private void OpenRecycleBin(){try{System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe","shell:RecycleBinFolder"){UseShellExecute=true});}catch(Exception ex){Notify(ex.Message);}}
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
namespace MacFlow
{
    internal static class NewFeaturesVisual
    {
        internal static int Run(string directory)
        {
            directory = Path.GetFullPath(directory); Directory.CreateDirectory(directory); Log.DirectoryPath = directory;
            var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown }; Desktop desktop = null; VisualTestBackground background = null; var results = new List<string>();
            app.Startup += async delegate
            {
                try
                {
                    string profile = Path.Combine(directory, "profile"); new SettingsStore(profile).Save(new Preferences { Theme = "Dark", ContainedDockApplied = true, IconSize = 36 }); desktop = new Desktop(profile, true, true);
                    background = new VisualTestBackground { WindowState = WindowState.Normal, ShowActivated = false, IsHitTestVisible = false, Topmost = true }; background.SourceInitialized += delegate { Native.NoActivate(background); }; background.Show();
                    desktop.OpenFocus(); var focus = Field<FocusWindow>(desktop, "focusWindow"); await Capture(focus, background, directory, "concentracion"); desktop.Focus.Start(25, "Crear algo hermoso");
                    focus.MinimizeButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await Task.Delay(450); if (focus.WindowState != WindowState.Minimized) throw new Exception("No minimiza Concentración"); desktop.OpenFocus(); await Task.Delay(450); if (Field<FocusWindow>(desktop, "focusWindow") != focus || focus.WindowState != WindowState.Normal) throw new Exception("No restaura la misma sesión"); focus.CloseButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await Task.Delay(450); if (Field<FocusWindow>(desktop, "focusWindow") != null || desktop.Focus.Session.State != "Running") throw new Exception("Cerrar detuvo la sesión"); results.Add("PASS Concentración: minimizar, restaurar y cerrar conserva la sesión; controles reales."); desktop.Focus.Reset();
                    string folder = Path.Combine(directory, "files"); Directory.CreateDirectory(folder); Directory.CreateDirectory(Path.Combine(folder, "Proyecto MacFlow")); File.WriteAllText(Path.Combine(folder, "Ideas.txt"), "Crear. Concentrarse. Disfrutar.\n\nUna nueva forma de sentir tu escritorio."); File.WriteAllText(Path.Combine(folder, "Notas de la versión.md"), "# MacFlow 1.1.5\nPilas de carpetas y vista rápida.");
                    desktop.OpenStack(folder); var stack = Field<FolderStack>(desktop, "stacks"); await Task.Delay(550); if (!stack.Ready || stack.ItemCount != 3) throw new Exception("Pila no cargó sus archivos"); await Capture(stack, background, directory, "pilas");
                    stack.MinimizeButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await Task.Delay(450); desktop.OpenStack(folder); await Task.Delay(450); if (Field<FolderStack>(desktop, "stacks") != stack || stack.WindowState != WindowState.Normal) throw new Exception("No restaura la pila"); stack.CloseButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await Task.Delay(450); if (Field<FolderStack>(desktop, "stacks") != null) throw new Exception("No cierra la pila"); results.Add("PASS Pilas: carga real, minimizar, restaurar y cierre animado.");
                    desktop.OpenPreview(Path.Combine(folder, "Ideas.txt")); var preview = Field<FilePreviewWindow>(desktop, "filePreview"); await Task.Delay(500); if (!preview.Ready) throw new Exception("Vista rápida no lista"); await Capture(preview, background, directory, "vista-texto"); preview.CloseButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await Task.Delay(450); if (Field<FilePreviewWindow>(desktop, "filePreview") != null) throw new Exception("Vista rápida no cierra");
                    var picture = WallpaperLibrary.Load(Path.Combine(directory, "wallpapers"))[0].File; desktop.OpenPreview(picture); preview = Field<FilePreviewWindow>(desktop, "filePreview"); await Task.Delay(650); if (!preview.Ready) throw new Exception("Vista de imagen no lista"); await Capture(preview, background, directory, "vista-imagen"); preview.Close(); results.Add("PASS Vista rápida: texto e imagen reales, descarga de recursos y cierre.");
                    desktop.OpenDesktopMenu(); await Capture(GlassMenu.Current, background, directory, "menu-escritorio"); GlassMenu.Current.Close(); results.Add("PASS Menú de cristal propio; Explorer no se modifica.");
                    desktop.Preferences.Theme = "Light"; desktop.ApplyPreferences(desktop.Preferences, null); desktop.OpenFocus(); await Capture(Field<FocusWindow>(desktop, "focusWindow"), background, directory, "concentracion-claro");
                    File.WriteAllLines(Path.Combine(directory, "new-features-visual.txt"), results); app.Shutdown();
                }
                catch (Exception ex) { File.WriteAllText(Path.Combine(directory, "new-features-visual.txt"), "FAIL " + ex); app.Shutdown(1); }
            };
            app.Exit += delegate { if (desktop != null) desktop.Dispose(); if (background != null) background.Close(); }; return app.Run();
        }
        private static T Field<T>(Desktop desktop, string name) { return (T)typeof(Desktop).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(desktop); }
        private static async Task Capture(Window panel, Window background, string directory, string name)
        { background.Left = panel.Left; background.Top = panel.Top; background.Width = panel.Width; background.Height = panel.Height; var handle = new System.Windows.Interop.WindowInteropHelper(panel).Handle; Native.SetWindowPos(handle, new IntPtr(-1), 0, 0, 0, 0, 0x13); Native.SetWindowPos(new System.Windows.Interop.WindowInteropHelper(background).Handle, handle, 0, 0, 0, 0, 0x13); await Task.Delay(500); await Program.CaptureOwnWindow(panel, Path.Combine(directory, name + ".png")); }
    }
}

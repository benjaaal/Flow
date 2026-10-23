using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
namespace MacFlow
{
    internal sealed class StackEntry { internal string Path, Name; internal bool Folder; internal DateTime Modified; internal long Size; }
    internal static class FolderSnapshot
    {
        internal static List<StackEntry> Read(string path, CancellationToken stop)
        {
            var result = new List<StackEntry>(); if (!Directory.Exists(path)) throw new IOException("La carpeta ya no está disponible.");
            foreach (var entry in new DirectoryInfo(path).EnumerateFileSystemInfos())
            {
                stop.ThrowIfCancellationRequested(); if (result.Count >= 200) break;
                try { if ((entry.Attributes & (FileAttributes.Hidden | FileAttributes.System)) != 0) continue; bool folder = (entry.Attributes & FileAttributes.Directory) != 0; result.Add(new StackEntry { Path = entry.FullName, Name = entry.Name, Folder = folder, Modified = entry.LastWriteTimeUtc, Size = folder ? 0 : ((FileInfo)entry).Length }); }
                catch (IOException) { } catch (UnauthorizedAccessException) { }
            }
            return result.OrderByDescending(e => e.Folder).ThenByDescending(e => e.Modified).ThenBy(e => e.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
        }
    }
    public sealed class FolderStack : FeaturePanel
    {
        private readonly Desktop desktop;
        private readonly ListBox list;
        private readonly TextBlock heading, status;
        private readonly Button back, preview, pin;
        private CancellationTokenSource scan;
        private int generation;
        private bool closed;
        public string FolderPath { get; private set; }
        internal int ItemCount { get { return list.Items.Count; } }
        internal bool Ready { get; private set; }
        public FolderStack(Desktop owner, string path) : base("Pilas de carpetas", 530, 550)
        {
            desktop = owner;
            var shortcuts = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 14) };
            AddPlace(shortcuts, "Descargas", Downloads()); AddPlace(shortcuts, "Documentos", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)); AddPlace(shortcuts, "Escritorio", Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
            shortcuts.Children.Add(Design.Button("Elegir…", Choose, false)); DockPanel.SetDock(shortcuts, System.Windows.Controls.Dock.Top); Body.Children.Add(shortcuts);
            var nav = new DockPanel { Margin = new Thickness(0, 0, 0, 10) }; back = Design.Button("‹", delegate { var parent = Directory.GetParent(FolderPath); if (parent != null) Navigate(parent.FullName); }, false); back.ToolTip = "Carpeta anterior"; DockPanel.SetDock(back, System.Windows.Controls.Dock.Left); nav.Children.Add(back); heading = Design.Text("", 21, true); heading.VerticalAlignment = VerticalAlignment.Center; heading.TextTrimming = TextTrimming.CharacterEllipsis; nav.Children.Add(heading); DockPanel.SetDock(nav, System.Windows.Controls.Dock.Top); Body.Children.Add(nav);
            var footer = new StackPanel { Margin = new Thickness(0, 12, 0, 0) }; status = Caption("Cargando…"); footer.Children.Add(status);
            var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 10, 0, 0) };
            pin = Design.Button("Fijar carpeta", delegate { if (!desktop.Preferences.Pins.Any(p => String.Equals(p.Target, FolderPath, StringComparison.OrdinalIgnoreCase))) desktop.Pin(new AppEntry { Name = heading.Text, Target = FolderPath }); UpdatePin(); }, false); actions.Children.Add(pin);
            actions.Children.Add(Design.Button("Explorador", delegate { OpenFile(FolderPath); }, false)); preview = Design.Button("Vista rápida", Preview, true); actions.Children.Add(preview); footer.Children.Add(actions); DockPanel.SetDock(footer, System.Windows.Controls.Dock.Bottom); Body.Children.Add(footer);
            list = new ListBox { Background = Brushes.Transparent, BorderThickness = new Thickness(0), HorizontalContentAlignment = HorizontalAlignment.Stretch }; ScrollViewer.SetHorizontalScrollBarVisibility(list, ScrollBarVisibility.Disabled); VirtualizingStackPanel.SetIsVirtualizing(list, true); VirtualizingStackPanel.SetVirtualizationMode(list, VirtualizationMode.Recycling); Body.Children.Add(list);
            list.SelectionChanged += delegate { var selected = Selected; preview.IsEnabled = selected != null && !selected.Folder; };
            list.MouseDoubleClick += delegate { var selected = Selected; if (selected == null) return; if (selected.Folder) Navigate(selected.Path); else OpenFile(selected.Path); };
            list.PreviewKeyDown += delegate(object sender, KeyEventArgs e) { if (e.Key == Key.Space) { Preview(); e.Handled = true; } else if (e.Key == Key.Enter) { var selected = Selected; if (selected != null) { if (selected.Folder) Navigate(selected.Path); else OpenFile(selected.Path); } e.Handled = true; } };
            Closed += delegate { closed = true; generation++; if (scan != null) { scan.Cancel(); scan.Dispose(); } }; Navigate(path);
        }
        public static string Downloads()
        {
            // User Shell Folders preserves relocated Downloads, including another drive.
            try { using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\User Shell Folders")) { var value = key == null ? null : key.GetValue("{374DE290-123F-4565-9164-39C4925E467B}") as string; if (!String.IsNullOrWhiteSpace(value)) return Environment.ExpandEnvironmentVariables(value); } } catch { }
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        }
        private StackEntry Selected { get { var item = list.SelectedItem as ListBoxItem; return item == null ? null : item.Tag as StackEntry; } }
        private void AddPlace(StackPanel row, string name, string path) { var button = Design.Button(name, delegate { Navigate(path); }, false); button.IsEnabled = Directory.Exists(path); button.Padding = new Thickness(8, 7, 8, 7); row.Children.Add(button); }
        private void Choose() { using (var picker = new System.Windows.Forms.FolderBrowserDialog { Description = "Elegí una carpeta para abrir como pila", ShowNewFolderButton = false, SelectedPath = Directory.Exists(FolderPath) ? FolderPath : "" }) if (picker.ShowDialog() == System.Windows.Forms.DialogResult.OK) Navigate(picker.SelectedPath); }
        public async void Navigate(string path)
        {
            int request = ++generation; if (scan != null) { scan.Cancel(); scan.Dispose(); } scan = new CancellationTokenSource(); var stop = scan.Token; Ready = false; list.Items.Clear(); preview.IsEnabled = false; status.Text = "Cargando…";
            try
            {
                path = Path.GetFullPath(path); FolderPath = path; heading.Text = new DirectoryInfo(path).Name; if (String.IsNullOrEmpty(heading.Text)) heading.Text = path; heading.ToolTip = path; back.IsEnabled = Directory.GetParent(path) != null; UpdatePin();
                var entries = await Task.Run(delegate { return FolderSnapshot.Read(path, stop); }, stop); if (closed || request != generation) return;
                for (int i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i]; var row = new DockPanel { Margin = new Thickness(5, 8, 5, 8) };
                    row.Children.Add(new Border { Width = 36, Height = 36, CornerRadius = new CornerRadius(10), Background = Design.Brush(entry.Folder ? "#2277BAFF" : "#168DAEFF"), Child = Design.Symbol(entry.Folder ? "folder" : "display", 19, Design.Ink), Margin = new Thickness(0, 0, 12, 0) });
                    var text = new StackPanel(); var name = Design.Text(entry.Name, 13, true); name.TextTrimming = TextTrimming.CharacterEllipsis; text.Children.Add(name); text.Children.Add(Caption(entry.Folder ? "Carpeta" : FilePreview.FormatSize(entry.Size) + " · " + entry.Modified.ToLocalTime().ToString("dd MMM"))); row.Children.Add(text);
                    var item = new ListBoxItem { Content = row, Tag = entry }; System.Windows.Automation.AutomationProperties.SetName(item, entry.Name); list.Items.Add(item);
                    if (Design.Animations && i < 8) { row.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220)) { BeginTime = TimeSpan.FromMilliseconds(i * 18), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } }); }
                }
                if (entries.Count > 0) list.SelectedIndex = 0; status.Text = entries.Count == 0 ? "Esta carpeta está vacía." : entries.Count + (entries.Count == 200 ? " elementos · vista limitada" : " elementos") + " · Espacio para previsualizar"; Ready = true;
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { if (!closed && request == generation) { status.Text = ex.Message; Ready = true; } }
        }
        private void Preview() { var entry = Selected; if (entry != null && !entry.Folder) desktop.OpenPreview(entry.Path); }
        private void UpdatePin() { bool pinned = desktop.Preferences.Pins.Any(p => String.Equals(p.Target, FolderPath, StringComparison.OrdinalIgnoreCase)); pin.Content = pinned ? "Fijada en el Dock" : "Fijar carpeta"; pin.IsEnabled = !pinned && Directory.Exists(FolderPath); }
        private void OpenFile(string path) { try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true }); } catch (Exception ex) { MessageBox.Show(ex.Message, "MacFlow"); } }
    }
}

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace MacFlow
{
    internal sealed class PreviewData
    {
        internal string Name, Detail, Text;
        internal ImageSource Image;
    }
    internal static class FilePreview
    {
        internal const int TextLimit = 32768;
        private static readonly string[] TextExtensions = { ".txt", ".md", ".json", ".xml", ".csv", ".log", ".cs", ".xaml", ".ini", ".yaml", ".yml", ".html", ".css", ".js", ".py", ".ps1", ".hlsl" };
        private static readonly string[] Images = { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tif", ".tiff", ".ico" };
        internal static PreviewData Read(string path)
        {
            var info = new FileInfo(path); if (!info.Exists) throw new IOException("El archivo ya no está disponible.");
            var data = new PreviewData { Name = info.Name, Detail = FormatSize(info.Length) + " · " + info.LastWriteTime.ToString("dd MMM yyyy") };
            string extension = info.Extension.ToLowerInvariant();
            if (Images.Contains(extension))
            {
                if (info.Length > 32 * 1024 * 1024) { data.Text = "Esta imagen es demasiado grande para la vista rápida. Podés abrirla en su aplicación."; return data; }
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation | BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.None); var frame = decoder.Frames[0];
                    int width = frame.PixelWidth, height = frame.PixelHeight; if ((long)width * height > 64000000 || width < 1 || height < 1) throw new IOException("La imagen excede el tamaño de vista previa.");
                    double scale = Math.Min(1, Math.Min(1280.0 / width, 900.0 / height)); stream.Position = 0;
                    var image = new BitmapImage(); image.BeginInit(); image.CacheOption = BitmapCacheOption.OnLoad; image.CreateOptions = BitmapCreateOptions.IgnoreColorProfile; image.StreamSource = stream; image.DecodePixelWidth = Math.Max(1, (int)(width * scale)); image.DecodePixelHeight = Math.Max(1, (int)(height * scale)); image.EndInit(); image.Freeze(); data.Image = image;
                    data.Detail += " · " + width + " × " + height;
                }
            }
            else if (TextExtensions.Contains(extension))
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                using (var reader = new StreamReader(stream, Encoding.UTF8, true, 4096))
                { var chars = new char[TextLimit + 1]; int used = 0, count; while (used < chars.Length && (count = reader.Read(chars, used, chars.Length - used)) > 0) used += count; string text = new string(chars, 0, Math.Min(used, TextLimit)); data.Text = text.IndexOf('\0') >= 0 ? "Este archivo contiene datos binarios. Abrilo en su aplicación." : text; if (used > TextLimit) data.Detail += " · Vista parcial"; }
            }
            else data.Text = "Vista rápida de archivo\n\nAbrilo en su aplicación para ver su contenido.";
            return data;
        }
        internal static string FormatSize(long size) { return size >= 1048576 ? (size / 1048576.0).ToString("0.#") + " MB" : size >= 1024 ? (size / 1024.0).ToString("0.#") + " KB" : size + " bytes"; }
    }
    public sealed class FilePreviewWindow : FeaturePanel
    {
        private readonly string path;
        private readonly ContentControl view;
        private readonly TextBlock title, detail;
        private bool closed;
        internal bool Ready { get; private set; }
        public FilePreviewWindow(string file) : base("Vista rápida", 670, 525)
        {
            path = file; var head = new StackPanel { Margin = new Thickness(0, 0, 0, 15) }; title = Design.Text(Path.GetFileName(file), 20, true); title.TextTrimming = TextTrimming.CharacterEllipsis; detail = Caption("Preparando vista previa…"); detail.Margin = new Thickness(0, 6, 0, 0); head.Children.Add(title); head.Children.Add(detail); DockPanel.SetDock(head, System.Windows.Controls.Dock.Top); Body.Children.Add(head);
            var row = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 15, 0, 0) };
            row.Children.Add(Design.Button("Mostrar carpeta", delegate { Open(Path.GetDirectoryName(path)); }, false)); row.Children.Add(Design.Button("Abrir archivo", delegate { Open(path); }, true)); DockPanel.SetDock(row, System.Windows.Controls.Dock.Bottom); Body.Children.Add(row);
            view = new ContentControl { HorizontalContentAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Stretch }; Body.Children.Add(new GlassPiece(view, 16, new Thickness(16)));
            Closed += delegate { closed = true; view.Content = null; }; Loaded += async delegate
            {
                try { var result = await Task.Run(delegate { return FilePreview.Read(path); }); if (closed) return; title.Text = result.Name; detail.Text = result.Detail; view.Content = result.Image != null ? (object)new Image { Source = result.Image, Stretch = Stretch.Uniform } : new TextBox { Text = result.Text, IsReadOnly = true, TextWrapping = TextWrapping.Wrap, FontFamily = new FontFamily("Consolas"), FontSize = 12, Background = Brushes.Transparent, Foreground = Design.Ink, BorderThickness = new Thickness(0), VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled }; var textView = view.Content as TextBox; if (textView != null) textView.SetResourceReference(Control.ForegroundProperty, "GlassInk"); Ready = true; }
                catch (Exception ex) { if (!closed) { detail.Text = "No se pudo preparar la vista previa"; view.Content = Caption(ex.Message); Ready = true; } }
            };
        }
        private void Open(string target) { try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(target) { UseShellExecute = true }); } catch (Exception ex) { MessageBox.Show(ex.Message, "MacFlow"); } }
    }
}

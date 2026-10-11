using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Media.Imaging;
namespace MacFlow
{
    internal static class NewFeaturesRegression
    {
        private static void Check(bool value, string message, List<string> results) { if (!value) throw new Exception(message); results.Add("PASS 1.1.5: " + message); }
        internal static void Run(string directory, List<string> results)
        {
            Directory.CreateDirectory(directory); DateTime now = new DateTime(2026, 10, 11, 12, 0, 0, DateTimeKind.Utc);
            var session = new FocusSession(); session.Start(1500, "Escribir", now);
            Check(session.State == "Running" && session.Remaining(now.AddMinutes(10)) == 900 && Math.Abs(session.Progress(now.AddMinutes(10)) - .4) < .001, "Concentración calcula el tiempo por fecha límite, sin depender de cuadros ni ticks", results);
            session.Pause(now.AddMinutes(10)); session.Resume(now.AddHours(1));
            Check(session.Remaining(now.AddHours(1).AddMinutes(5)) == 600, "pausar conserva el tiempo y continuar crea una fecha límite nueva", results);
            Check(session.Advance(now.AddHours(2)) && !session.Advance(now.AddHours(2)) && session.State == "Complete", "reanudar después de suspensión termina una única vez", results);
            session.Reset(); session.Start(100000, new string('a', 120), now); Check(session.Duration == 7200 && session.Label.Length == 80, "duración y nombre tienen límites", results);
            string profile = Path.Combine(directory, "focus-profile"); using (var controller = new FocusController(profile)) { controller.Start(25, "Revisión"); controller.Toggle(); }
            var saved = FocusController.Load(Path.Combine(profile, "focus.json")); Check(saved.State == "Paused" && saved.Label == "Revisión" && saved.SavedSeconds <= 1500 && saved.SavedSeconds > 1490, "la sesión sobrevive al cierre y se guarda atómicamente", results);
            File.WriteAllText(Path.Combine(profile, "focus.json"), "{invalid"); Check(FocusController.Load(Path.Combine(profile, "focus.json")).State == "Idle", "un estado corrupto recupera Concentración sin afectar preferencias", results);
            string folder = Path.Combine(directory, "stack-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder); Directory.CreateDirectory(Path.Combine(folder, "Proyecto")); File.WriteAllText(Path.Combine(folder, "nota.txt"), "Ideas para mañana"); File.WriteAllText(Path.Combine(folder, "oculto.txt"), "oculto"); File.SetAttributes(Path.Combine(folder, "oculto.txt"), FileAttributes.Hidden);
            var entries = FolderSnapshot.Read(folder, CancellationToken.None); Check(entries.Count == 2 && entries[0].Folder && entries[1].Name == "nota.txt", "pilas agrupa carpetas primero y omite archivos ocultos", results);
            for (int i = 0; i < 230; i++) File.WriteAllText(Path.Combine(folder, "f-" + i + ".txt"), "x"); Check(FolderSnapshot.Read(folder, CancellationToken.None).Count == 200, "pilas limita la enumeración, sin recorrer subcarpetas", results);
            bool canceled = false; using (var cancel = new CancellationTokenSource()) { cancel.Cancel(); try { FolderSnapshot.Read(folder, cancel.Token); } catch (OperationCanceledException) { canceled = true; } } Check(canceled, "cambiar de carpeta cancela la lectura pendiente", results);
            var preview = FilePreview.Read(Path.Combine(folder, "nota.txt")); Check(preview.Text == "Ideas para mañana" && preview.Image == null, "vista rápida lee texto sin abrir ni ejecutar archivos", results);
            File.WriteAllText(Path.Combine(folder, "largo.txt"), new string('x', 40000)); preview = FilePreview.Read(Path.Combine(folder, "largo.txt")); Check(preview.Text.Length == FilePreview.TextLimit && preview.Detail.Contains("Vista parcial"), "la vista de texto está acotada y avisa cuando es parcial", results);
            File.WriteAllText(Path.Combine(folder, "binario.txt"), "a\0b"); Check(FilePreview.Read(Path.Combine(folder, "binario.txt")).Text.Contains("binarios"), "datos binarios no se presentan como texto", results);
            File.WriteAllBytes(Path.Combine(folder, "programa.exe"), new byte[] { 1, 2 }); Check(FilePreview.Read(Path.Combine(folder, "programa.exe")).Image == null, "otros formatos sólo muestran información; no cargan plugins ni ejecutables", results);
            string picture = Path.Combine(folder, "imagen.png"); using (var bitmap = new System.Drawing.Bitmap(2400, 1600)) bitmap.Save(picture, System.Drawing.Imaging.ImageFormat.Png);
            var image = FilePreview.Read(picture).Image as BitmapSource; Check(image != null && image.IsFrozen && image.PixelWidth <= 1280 && image.PixelHeight <= 900, "las imágenes se decodifican a resolución limitada y pueden cruzar de hilo", results);
            using (var stream = new FileStream(picture, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) Check(stream.Length > 0, "vista rápida libera el archivo al terminar de cargar", results);
            Check(Desktop.AutomaticDockPolicy(false, true, false, false, false) && Desktop.AutomaticDockPolicy(false, true, true, true, false) && !Desktop.AutomaticDockPolicy(false, false, false, true, false), "autoocultado en todas las apps no depende del nombre ni de que estén maximizadas", results);
            Check(!Desktop.ShouldAutoHide(true, true, false, false) && Desktop.ShouldAutoHide(false, true, false, false) && !Desktop.ShouldAutoHide(false, true, true, false), "el escritorio conserva el Dock y el borde lo recupera", results);
            var store = new SettingsStore(Path.Combine(directory, "migration")); Directory.CreateDirectory(store.DirectoryPath); File.WriteAllText(store.PathName, "{\"IconSize\":44,\"HideDockInCodex\":false}"); Check(store.Load().HideDockInApps && store.Load().IconSize == 44, "perfiles anteriores reciben el autoocultado general sin cambiar el tamaño del Dock", results);
            var preferences = store.Load(); preferences.HideDockInApps = false; store.Save(preferences); Check(!store.Load().HideDockInApps, "se respeta la elección posterior de desactivar el autoocultado general", results);
            if (Product.DefaultFeedUrl == "https://github.com/benjaaal/Flow/releases/latest/download/MacFlow-update.json")
            {
                preferences.UpdateFeedUrl = "https://github.com/colanabenjamin-spec/Flow/releases/latest/download/MacFlow-update.json"; preferences.UpdateRepository = "colanabenjamin-spec/Flow"; store.Save(preferences);
                var migrated = store.Load(); Check(migrated.UpdateFeedUrl == Product.DefaultFeedUrl && migrated.UpdateRepository == "benjaaal/Flow" && migrated.IconSize == preferences.IconSize, "la cuenta renombrada recibe el canal nuevo sin perder preferencias", results);
            }
        }
    }
}

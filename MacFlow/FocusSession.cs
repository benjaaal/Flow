using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Windows.Threading;

namespace MacFlow
{
    [DataContract]
    public sealed class FocusSession
    {
        [DataMember] public string State = "Idle";
        [DataMember] public string Label = "Un momento para vos";
        [DataMember] public int Duration = 1500;
        [DataMember] public double SavedSeconds = 1500;
        [DataMember] public DateTime DeadlineUtc;
        public double Remaining(DateTime utc) { return State == "Running" ? Math.Max(0, (DeadlineUtc - utc).TotalSeconds) : SavedSeconds; }
        public double Progress(DateTime utc) { return Math.Max(0, Math.Min(1, 1 - Remaining(utc) / Duration)); }
        public void Start(int seconds, string label, DateTime utc)
        {
            Duration = Math.Max(60, Math.Min(7200, seconds)); SavedSeconds = Duration;
            Label = String.IsNullOrWhiteSpace(label) ? "Un momento para vos" : label.Trim();
            if (Label.Length > 80) Label = Label.Substring(0, 80);
            DeadlineUtc = utc.AddSeconds(Duration); State = "Running";
        }
        public bool Advance(DateTime utc)
        {
            if (State != "Running" || Remaining(utc) > 0) return false;
            SavedSeconds = 0; State = "Complete"; return true;
        }
        public void Pause(DateTime utc) { if (State != "Running") return; if (Advance(utc)) return; SavedSeconds = Remaining(utc); State = "Paused"; }
        public void Resume(DateTime utc) { if (State != "Paused") return; DeadlineUtc = utc.AddSeconds(SavedSeconds); State = "Running"; }
        public void Reset() { State = "Idle"; SavedSeconds = Duration; }
        internal void Validate()
        {
            if (Duration < 60 || Duration > 7200 || Double.IsNaN(SavedSeconds) || Double.IsInfinity(SavedSeconds) || SavedSeconds < 0 || SavedSeconds > Duration || (State != "Idle" && State != "Paused" && State != "Running" && State != "Complete")) throw new IOException("Sesión de concentración inválida.");
            if (State == "Running" && DeadlineUtc.Kind != DateTimeKind.Utc) DeadlineUtc = DeadlineUtc.ToUniversalTime();
            if (String.IsNullOrWhiteSpace(Label)) Label = "Un momento para vos";
            if (Label.Length > 80) Label = Label.Substring(0, 80);
        }
    }
    public sealed class FocusController : IDisposable
    {
        public FocusSession Session { get; private set; }
        public event Action Changed;
        public event Action Completed;
        private readonly string file;
        private readonly DispatcherTimer timer;
        public FocusController(string directory)
        {
            file = Path.Combine(directory, "focus.json"); Session = Load(file);
            timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += delegate { bool ended = Session.Advance(DateTime.UtcNow); if (ended) { Save(); timer.Stop(); } Raise(); if (ended && Completed != null) Completed(); };
            if (Session.Advance(DateTime.UtcNow)) Save(); Sync();
        }
        internal static FocusSession Load(string path)
        {
            try { if (!File.Exists(path)) return new FocusSession(); if (new FileInfo(path).Length > 8192) throw new IOException("Sesión demasiado grande."); using (var stream = File.OpenRead(path)) { var value = (FocusSession)new DataContractJsonSerializer(typeof(FocusSession)).ReadObject(stream); value.Validate(); return value; } }
            catch (Exception ex) { Log.Write("Concentración: " + ex.Message); return new FocusSession(); }
        }
        private void Save()
        {
            try { Directory.CreateDirectory(Path.GetDirectoryName(file)); string temp = file + ".new"; using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None)) { new DataContractJsonSerializer(typeof(FocusSession)).WriteObject(stream, Session); stream.Flush(true); } if (File.Exists(file)) File.Replace(temp, file, null); else File.Move(temp, file); }
            catch (Exception ex) { Log.Write("No se pudo guardar Concentración: " + ex.Message); }
        }
        private void Raise() { if (Changed != null) Changed(); }
        private void Sync() { if (Session.State == "Running") timer.Start(); else timer.Stop(); }
        public void Start(int minutes, string label) { Session.Start(Math.Max(1, Math.Min(120, minutes)) * 60, label, DateTime.UtcNow); Save(); Sync(); Raise(); }
        public void Toggle() { if (Session.State == "Running") Session.Pause(DateTime.UtcNow); else Session.Resume(DateTime.UtcNow); Save(); Sync(); Raise(); }
        public void Reset() { Session.Reset(); Save(); Sync(); Raise(); }
        public void Dispose() { timer.Stop(); Changed = null; Completed = null; }
        public string TimeText { get { var seconds = (int)Math.Ceiling(Session.Remaining(DateTime.UtcNow)); return (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00"); } }
    }
}

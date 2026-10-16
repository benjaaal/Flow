using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Automation;
namespace MacFlow
{
    internal enum LocalIntent {Unknown,Open,Search,Gemini,Write,Minimize,Close,Read,Control,Settings,Tasks,VolumeUp,VolumeDown}
    internal sealed class LocalRequest {internal LocalIntent Intent;internal string Argument="";}
    internal static class LocalCommands
    {
        internal static string Normalize(string text){var b=new StringBuilder();foreach(char c in (text??"").Normalize(NormalizationForm.FormD))if(CharUnicodeInfo.GetUnicodeCategory(c)!=UnicodeCategory.NonSpacingMark)b.Append(Char.ToLowerInvariant(c));return b.ToString().Normalize(NormalizationForm.FormC).Trim();}
        internal static LocalRequest Parse(string text)
        {
            string raw=(text??"").Trim();string n=Normalize(raw);var r=new LocalRequest();
            if(n=="centro de control"){r.Intent=LocalIntent.Control;return r;}if(n=="configuracion"||n=="ajustes"){r.Intent=LocalIntent.Settings;return r;}if(n=="administrador de tareas"){r.Intent=LocalIntent.Tasks;return r;}
            if(n=="subi el volumen"||n=="sube el volumen"){r.Intent=LocalIntent.VolumeUp;return r;}if(n=="baja el volumen"||n=="baja volumen"){r.Intent=LocalIntent.VolumeDown;return r;}
            if(n=="lee"||n=="leeme"||n=="lee la pagina"||n=="lee gemini"||n=="leeme la respuesta"){r.Intent=LocalIntent.Read;return r;}
            var match=Regex.Match(n,@"^(abre|abri|abrir|abrime|inicia|ejecuta|busca|buscar|buscame|escribi|escribe|escribir|pon|pone|minimiza|minimizar|cerra|cierra|cerrar|gemini)\s+(.+)$");
            if(!match.Success){if(n=="gemini"){r.Intent=LocalIntent.Gemini;}return r;}
            string verb=match.Groups[1].Value;r.Argument=raw.Substring(match.Groups[1].Length).Trim();
            if(verb.StartsWith("abr")||verb=="inicia"||verb=="ejecuta")r.Intent=LocalIntent.Open;else if(verb.StartsWith("busc"))r.Intent=LocalIntent.Search;else if(verb.StartsWith("escrib")||verb=="pon"||verb=="pone")r.Intent=LocalIntent.Write;else if(verb.StartsWith("minim"))r.Intent=LocalIntent.Minimize;else if(verb=="gemini")r.Intent=LocalIntent.Gemini;else r.Intent=LocalIntent.Close;
            if(r.Intent==LocalIntent.Search)r.Argument=Regex.Replace(r.Argument,@"^(informaci[oó]n sobre|sobre)\s+","",RegexOptions.IgnoreCase);return r;
        }
        internal static Uri SearchUri(string term){return new Uri("https://www.google.com/search?q="+Uri.EscapeDataString(term??""));}
        internal static string ChromePath(System.Collections.Generic.IEnumerable<AppEntry> apps)
        {var app=apps.FirstOrDefault(a=>String.Equals(Path.GetFileName(a.Executable??a.Target),"chrome.exe",StringComparison.OrdinalIgnoreCase));if(app!=null&&File.Exists(app.Executable??app.Target))return app.Executable??app.Target;foreach(string folder in new[]{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}){var file=Path.Combine(folder,"Google","Chrome","Application","chrome.exe");if(File.Exists(file))return file;}return null;}
        internal static void OpenChrome(string path,Uri uri){if(path==null)throw new IOException("Chrome no está instalado o todavía no se detectó.");if(uri.Scheme!="https")throw new IOException("Sólo se abren búsquedas HTTPS.");Process.Start(new ProcessStartInfo(path,"--new-tab \""+uri.AbsoluteUri+"\""){UseShellExecute=true});}
    }
    internal static class LocalTextInput
    {
        [StructLayout(LayoutKind.Sequential)]private struct Key {public ushort Virtual,Scan;public uint Flags,Time;public UIntPtr Extra;}
        [StructLayout(LayoutKind.Explicit,Size=40)]private struct Input {[FieldOffset(0)]public uint Type;[FieldOffset(8)]public Key Keyboard;}
        [DllImport("user32.dll",SetLastError=true)]private static extern uint SendInput(uint count,Input[] inputs,int size);
        internal static async Task<bool> Write(WindowEntry target,string text)
        {if(String.IsNullOrEmpty(text)||text.Length>4000||!Native.Matches(target)||!Native.Activate(target))return false;await Task.Delay(180);if(Native.GetForegroundWindow()!=target.Handle||!Native.Matches(target))return false;var input=new Input[text.Length*2];for(int i=0;i<text.Length;i++){input[i*2]=new Input{Type=1,Keyboard=new Key{Scan=text[i],Flags=4}};input[i*2+1]=new Input{Type=1,Keyboard=new Key{Scan=text[i],Flags=6}};}return SendInput((uint)input.Length,input,Marshal.SizeOf(typeof(Input)))==input.Length;}
    }
    internal static class BrowserVisibleText
    {
        private static int reading;
        internal static string ReadingPart(string text)
        {
            foreach(string marker in new[]{"Visión general creada por IA","Vista creada con IA","AI Overview","Gemini ha dicho","Gemini said"}){int start=text.LastIndexOf(marker,StringComparison.OrdinalIgnoreCase);if(start<0)continue;string part=text.Substring(start+marker.Length).Trim();foreach(string end in new[]{"Otros usuarios también preguntan","People also ask","Más resultados"}){int cut=part.IndexOf(end,StringComparison.OrdinalIgnoreCase);if(cut>0)part=part.Substring(0,cut);}if(part.Length>0)return part.Substring(0,Math.Min(4000,part.Length));}return text;
        }
        internal static async Task<string> Read(WindowEntry target)
        {
            if(!Native.Matches(target)||!String.Equals(Path.GetFileName(target.Executable??""),"chrome.exe",StringComparison.OrdinalIgnoreCase))throw new IOException("Abrí la página en Chrome y volvé a abrir el asistente desde la barra.");
            if(Interlocked.CompareExchange(ref reading,1,0)!=0)throw new IOException("Todavía hay una lectura en curso.");
            var work=Task.Run(delegate{try{var root=AutomationElement.FromHandle(target.Handle);var docs=root.FindAll(TreeScope.Descendants,new PropertyCondition(AutomationElement.ControlTypeProperty,ControlType.Document));foreach(AutomationElement doc in docs){object pattern;if(!doc.Current.IsOffscreen&&doc.TryGetCurrentPattern(TextPattern.Pattern,out pattern)){var value=((TextPattern)pattern).DocumentRange.GetText(6000);if(!String.IsNullOrWhiteSpace(value))return value;}}return "";}finally{Interlocked.Exchange(ref reading,0);}});
            if(await Task.WhenAny(work,Task.Delay(3500))!=work)throw new IOException("Chrome no respondió a la lectura accesible. Podés copiar el texto y usar Leer texto.");var text=await work;if(String.IsNullOrWhiteSpace(text))throw new IOException("Chrome no ofrece texto accesible en esta página. No puedo leer una respuesta que no está disponible.");return ReadingPart(text);
        }
    }
}

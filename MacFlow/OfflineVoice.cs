using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Threading;
namespace MacFlow
{
    // One local model shared by wake-word and dictation. Pause releases the
    // recording device; switching modes does not reload the model.
    internal sealed class OfflineVoice : IDisposable
    {
        private readonly Func<ProcessStartInfo> startInfo;private readonly Dispatcher dispatcher;private readonly string profile;private Process worker;private bool ready,disposed;private string mode="pause";private Action<string> transcript,error;
        internal bool Available{get{return worker!=null;}}
        internal event Action Wake;internal event Action<bool,string> Status;
        internal OfflineVoice(Dispatcher ui,string directory,Func<ProcessStartInfo> fixture=null){dispatcher=ui;profile=directory;startInfo=fixture;}
        private void Report(bool listening,string text){var callback=Status;if(callback!=null)callback(listening,text);}
        internal void SetWake(bool enabled){SetMode(enabled?"wake":"pause");}
        internal void Listen(Action<string> result,Action<string> failed){transcript=result;error=failed;SetMode("dictate");}
        internal void CancelDictation(){transcript=error=null;SetMode("pause");}
        private void SetMode(string value)
        {if(disposed)return;if(mode==value&&worker!=null)return;mode=value;if(value=="pause"){transcript=error=null;Report(false,"Micrófono en pausa");}if(worker==null){if(value=="pause")return;Start();}else if(ready)Send(value);}
        private void Start()
        {
            if(startInfo==null&&!File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"MacFlow.Voice.exe"))){const string pending="Falta el componente de voz local. Reinstalá el paquete completo de MacFlow desde Configuración → Sistema → Actualizaciones.";Report(false,pending);var failed=error;transcript=error=null;if(failed!=null)failed(pending);return;}
            if(disposed||worker!=null)return;Report(false,"Preparando voz local en español…");
            try{var info=startInfo==null?new ProcessStartInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"MacFlow.Voice.exe"),"\""+profile+"\""){UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true}:startInfo();var child=new Process{StartInfo=info,EnableRaisingEvents=true};worker=child;
                child.OutputDataReceived+=delegate(object s,DataReceivedEventArgs e){if(e.Data==null||dispatcher.HasShutdownStarted)return;string line=e.Data;dispatcher.BeginInvoke(DispatcherPriority.Normal,new Action(delegate{if(disposed||worker!=child)return;Receive(line);}));};
                child.Exited+=delegate{if(dispatcher.HasShutdownStarted)return;dispatcher.BeginInvoke(new Action(delegate{if(disposed||worker!=child)return;worker=null;ready=false;child.Dispose();Report(false,"La voz local se detuvo. Reintentá desde el asistente.");var failed=error;transcript=error=null;if(failed!=null)failed("No se pudo mantener el micrófono activo. Revisá sus permisos y dispositivo predeterminado.");}));};child.Start();child.BeginOutputReadLine();child.BeginErrorReadLine();}
            catch(Exception ex){if(worker!=null)worker.Dispose();worker=null;ready=false;Report(false,"Voz local no disponible: "+ex.Message);var failed=error;transcript=error=null;if(failed!=null)failed(ex.Message);}
        }
        private void Receive(string line)
        {
            if(line=="READY"){ready=true;Send(mode);}else if(line=="WAKE_READY")Report(true,"Micrófono activo · Hey Siri · reconocimiento local en español");else if(line=="DICTATION_READY")Report(false,"Escuchando tu pedido en español…");
            else if(line=="WAKE"){SetMode("pause");var wake=Wake;if(wake!=null)wake();}
            else if(line.StartsWith("TEXT:")){var result=transcript;transcript=error=null;SetMode("pause");try{string text=Encoding.UTF8.GetString(Convert.FromBase64String(line.Substring(5)));if(result!=null)result(text);}catch(FormatException){Report(false,"El reconocimiento devolvió una respuesta inválida.");}}
            else if(line.StartsWith("ERROR:")){Report(false,line.Substring(6));var failed=error;transcript=error=null;mode="pause";if(failed!=null)failed(line.Substring(6));}
        }
        private void Send(string value){try{if(worker!=null&&!worker.HasExited)worker.StandardInput.WriteLine(value);}catch(Exception ex){Report(false,"No se pudo comunicar con la voz local: "+ex.Message);}}
        public void Dispose(){if(disposed)return;disposed=true;transcript=error=null;var child=worker;worker=null;if(child!=null){try{if(!child.HasExited){child.StandardInput.WriteLine("stop");if(!child.WaitForExit(800))child.Kill();}}catch(InvalidOperationException){}finally{child.Dispose();}}}
    }
}

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Windows.Automation.Text;
namespace MacFlow
{
    internal interface IGeminiBrowser
    {
        Task<string> Ask(string question,CancellationToken cancellation,Action<string> progress);
    }
    // Uses the user's visible Chrome session, never a key, private endpoint,
    // debugging port or injected script. Only the submitted question is sent.
    internal sealed class GeminiBrowser : IGeminiBrowser
    {
        private readonly Func<string> chromePath;private static readonly SemaphoreSlim gate=new SemaphoreSlim(1,1);
        internal GeminiBrowser(Func<string> path){chromePath=path;}
        internal static bool GeminiAddress(string value)
        {
            if(String.IsNullOrWhiteSpace(value))return false;value=value.Trim();if(value.StartsWith("gemini.google.com/",StringComparison.OrdinalIgnoreCase))value="https://"+value;
            Uri uri;return Uri.TryCreate(value,UriKind.Absolute,out uri)&&uri.Scheme=="https"&&String.Equals(uri.Host,"gemini.google.com",StringComparison.OrdinalIgnoreCase)&&String.IsNullOrEmpty(uri.UserInfo)&&uri.IsDefaultPort&&(uri.AbsolutePath=="/app"||uri.AbsolutePath.StartsWith("/app/",StringComparison.Ordinal));
        }
        private static string Value(AutomationElement element)
        {
            object pattern;if(element.TryGetCurrentPattern(ValuePattern.Pattern,out pattern))return ((ValuePattern)pattern).Current.Value??"";
            if(element.TryGetCurrentPattern(TextPattern.Pattern,out pattern)){var range=((TextPattern)pattern).DocumentRange.Clone();range.MoveEndpointByRange(TextPatternRangeEndpoint.Start,range,TextPatternRangeEndpoint.End);range.MoveEndpointByUnit(TextPatternRangeEndpoint.Start,TextUnit.Character,-90000);return range.GetText(90000)??"";}return "";
        }
        private static AutomationElement Root(WindowEntry entry)
        {
            if(!Native.Matches(entry)||!String.Equals(Path.GetFileName(entry.Executable),"chrome.exe",StringComparison.OrdinalIgnoreCase))throw new IOException("La ventana de Chrome ya no está disponible.");return AutomationElement.FromHandle(entry.Handle);
        }
        // Verify the browser's address control outside the webpage. Page titles
        // and content cannot establish the destination of simulated input.
        private static bool Verified(AutomationElement root)
        {
            var pending=new System.Collections.Generic.Queue<Tuple<AutomationElement,int>>();pending.Enqueue(Tuple.Create(root,0));int visited=0;
            while(pending.Count>0&&visited++<600)
            {
                var entry=pending.Dequeue();var node=entry.Item1;if(node.Current.ControlType==ControlType.Document)continue;
                if(node.Current.ControlType==ControlType.Edit&&!node.Current.IsOffscreen){string name=LocalCommands.Normalize(node.Current.Name);if((name.Contains("direccion")||name.Contains("address"))&&GeminiAddress(Value(node)))return true;}
                if(entry.Item2>=7)continue;for(var child=TreeWalker.ControlViewWalker.GetFirstChild(node);child!=null;child=TreeWalker.ControlViewWalker.GetNextSibling(child))pending.Enqueue(Tuple.Create(child,entry.Item2+1));
            }
            return false;
        }
        private static AutomationElement Document(AutomationElement root)
        {
            if(!Verified(root))throw new IOException("No pude verificar gemini.google.com en la barra de direcciones. Abrí Gemini en una ventana normal de Chrome.");
            foreach(AutomationElement doc in root.FindAll(TreeScope.Descendants,new PropertyCondition(AutomationElement.ControlTypeProperty,ControlType.Document)))if(!doc.Current.IsOffscreen)return doc;
            throw new IOException("Chrome no ofrece el contenido accesible de Gemini.");
        }
        private static AutomationElement Editor(AutomationElement doc)
        {
            foreach(AutomationElement edit in doc.FindAll(TreeScope.Descendants,new PropertyCondition(AutomationElement.ControlTypeProperty,ControlType.Edit)))
            {
                string name=LocalCommands.Normalize(edit.Current.Name);if(!edit.Current.IsOffscreen&&edit.Current.IsEnabled&&edit.Current.IsKeyboardFocusable&&!edit.Current.IsPassword&&(name.Contains("peticion")||name.Contains("prompt")||name.Contains("gemini")||name.Contains("pregunta")||name.Contains("consulta")||name.Contains("mensaje")||name.Contains("message")||name.Contains("escribe")||name.Contains("introduce")||name.Contains("ask")||name.Contains("habla")))return edit;
            }
            throw new IOException("Gemini no muestra el campo de consulta. Completá vos el inicio de sesión o los avisos que aparezcan en Chrome y volvé a intentar.");
        }
        private static AutomationElement Button(AutomationElement doc,bool stop)
        {
            foreach(AutomationElement item in doc.FindAll(TreeScope.Descendants,new PropertyCondition(AutomationElement.ControlTypeProperty,ControlType.Button)))
            {
                if(item.Current.IsOffscreen||!item.Current.IsEnabled)continue;string n=LocalCommands.Normalize(item.Current.Name);
                if(stop?(n=="detener"||n=="stop"||n.Contains("detener respuesta")||n.Contains("detener generacion")||n.Contains("stop response")||n.Contains("stop generating")):n=="enviar"||n=="send"||n.StartsWith("enviar mensaje")||n.StartsWith("send message")||n.StartsWith("enviar peticion")||n.StartsWith("enviar consulta")||n=="submit")return item;
            }
            return null;
        }
        internal static string AnswerPart(string page)
        {
            if(String.IsNullOrWhiteSpace(page))return "";int last=-1,length=0;
            foreach(string marker in new[]{"Gemini ha dicho","Gemini dijo","Gemini said","Respuesta de Gemini","Gemini's response"}){int at=page.LastIndexOf(marker,StringComparison.OrdinalIgnoreCase);if(at>last){last=at;length=marker.Length;}}
            if(last<0)return "";string text=page.Substring(last+length).Trim();
            foreach(string footer in new[]{"Mostrar borradores","Show drafts","Gemini puede cometer errores","Gemini can make mistakes"}){int at=text.IndexOf(footer,StringComparison.OrdinalIgnoreCase);if(at>=0)text=text.Substring(0,at).Trim();}
            return text.Length>6000?text.Substring(0,6000):text;
        }
        private static async Task<T> Bounded<T>(Func<T> action,CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();var task=Task.Run(action);if(await Task.WhenAny(task,Task.Delay(3500,cancellation))!=task){cancellation.ThrowIfCancellationRequested();throw new IOException("Chrome no respondió a la accesibilidad. La consulta no se reintentará automáticamente.");}cancellation.ThrowIfCancellationRequested();return await task;
        }
        public async Task<string> Ask(string question,CancellationToken cancellation,Action<string> progress)
        {
            if(String.IsNullOrWhiteSpace(question)||question.Length>30000)throw new IOException("La consulta supera el tamaño disponible. Resumí las pestañas por grupos.");
            if(!await gate.WaitAsync(0,cancellation))throw new IOException("Hay otra consulta de Gemini en curso.");
            try
            {
                progress("Abriendo Gemini en Chrome…");WindowEntry entry=null;
                foreach(var candidate in Native.Windows().Where(w=>String.Equals(Path.GetFileName(w.Executable),"chrome.exe",StringComparison.OrdinalIgnoreCase)&&w.Title.IndexOf("Gemini",StringComparison.OrdinalIgnoreCase)>=0))
                    if(await Bounded(()=>Verified(Root(candidate)),cancellation)){entry=candidate;break;}
                if(entry==null){LocalCommands.OpenChrome(chromePath(),new Uri("https://gemini.google.com/app"));for(int i=0;i<30&&entry==null;i++){await Task.Delay(400,cancellation);foreach(var candidate in Native.Windows().Where(w=>String.Equals(Path.GetFileName(w.Executable),"chrome.exe",StringComparison.OrdinalIgnoreCase)&&w.Title.IndexOf("Gemini",StringComparison.OrdinalIgnoreCase)>=0))if(await Bounded(()=>Verified(Root(candidate)),cancellation)){entry=candidate;break;}}}
                if(entry==null)throw new IOException("Abrí Gemini en Chrome y completá el inicio de sesión si lo pide. No se envió la pregunta.");
                if(!Native.Activate(entry))throw new IOException("Windows no permitió activar Chrome. No se envió la pregunta.");await Task.Delay(180,cancellation);
                string previous="";AutomationElement field=null;
                await Bounded(delegate{var doc=Document(Root(entry));previous=AnswerPart(Value(doc));field=Editor(doc);if(!String.IsNullOrWhiteSpace(Value(field)))throw new IOException("Hay un borrador en Gemini. Enviá o quitá ese texto antes de consultar desde MacFlow.");return true;},cancellation);
                // Mutating UIA calls are deliberately not abandoned on timeout:
                // a timed-out worker must never submit a query after cancellation.
                cancellation.ThrowIfCancellationRequested();progress("Escribiendo tu pregunta en Gemini…");
                await Task.Run(delegate{cancellation.ThrowIfCancellationRequested();Document(Root(entry));field.SetFocus();});cancellation.ThrowIfCancellationRequested();
                bool written=await LocalTextInput.WriteFocused(entry,question,cancellation,delegate{try{return Automation.Compare(AutomationElement.FocusedElement,field)&&Verified(Root(entry));}catch{return false;}});
                if(!written)throw new IOException("Cambió el foco de Chrome. Revisá el borrador en Gemini; no se envió automáticamente.");
                await Task.Run(delegate{cancellation.ThrowIfCancellationRequested();var doc=Document(Root(entry));if(LocalCommands.Normalize(Value(field))!=LocalCommands.Normalize(question))throw new IOException("Gemini no confirmó el texto de la consulta. Revisá el borrador en Chrome.");var send=Button(doc,false);object invoke;if(send==null||!send.TryGetCurrentPattern(InvokePattern.Pattern,out invoke))throw new IOException("No está disponible Enviar en Gemini. El texto queda como borrador.");cancellation.ThrowIfCancellationRequested();((InvokePattern)invoke).Invoke();});
                progress("Gemini está respondiendo…");string last="";int stable=0;bool generating=false;
                for(int i=0;i<90;i++)
                {
                    await Task.Delay(700,cancellation);var state=await Bounded(delegate{var doc=Document(Root(entry));return new Tuple<string,bool>(AnswerPart(Value(doc)),Button(doc,true)!=null);},cancellation);
                    generating|=state.Item2;string current=state.Item1;if(current.Length==0||current==previous&&!generating){stable=0;continue;}stable=current==last?stable+1:0;last=current;if(!state.Item2&&stable>=4)return current;
                }
                throw new IOException("La pregunta se envió, pero no pude confirmar una respuesta completa. Podés verla en Gemini; no voy a repetir el envío.");
            }
            finally{gate.Release();}
        }
    }
}

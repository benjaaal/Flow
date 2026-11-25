using System;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MacFlow
{
    internal static class OllamaAssistant
    {
        [DataContract]
        private sealed class Request
        {
            [DataMember] public string model,prompt,system;
            [DataMember] public bool stream;
            [DataMember] public int keep_alive;
        }

        [DataContract]
        private sealed class Reply
        {
            [DataMember] public string response;
            [DataMember] public bool done;
        }

        internal static bool ValidModel(string model)
        {
            return model!=null&&Regex.IsMatch(model,@"\A[a-zA-Z0-9][a-zA-Z0-9._:-]{0,99}\z")
                &&model.IndexOf("cloud",StringComparison.OrdinalIgnoreCase)<0;
        }

        internal static async Task<string> Ask(string model,string prompt,CancellationToken cancellation)
        {
            if(!ValidModel(model))throw new IOException("Elegí un modelo instalado de Ollama en Ajustes → Privacidad.");
            if(String.IsNullOrWhiteSpace(prompt)||prompt.Length>8000)throw new IOException("La consulta debe contener entre 1 y 8000 caracteres.");
            var request=(HttpWebRequest)WebRequest.Create("http://127.0.0.1:11434/api/generate");
            request.Proxy=null;request.AllowAutoRedirect=false;request.Method="POST";request.ContentType="application/json";
            var value=new Request{model=model,prompt=prompt,system="Respondé en español. MacFlow muestra tus respuestas como texto y ejecuta las órdenes locales por separado.",stream=false,keep_alive=0};
            using(var timeout=CancellationTokenSource.CreateLinkedTokenSource(cancellation))
            {
                timeout.CancelAfter(TimeSpan.FromMinutes(2));
                using(timeout.Token.Register(request.Abort))
                {
                    try
                    {
                        using(var payload=new MemoryStream())
                        {
                            new DataContractJsonSerializer(typeof(Request)).WriteObject(payload,value);
                            request.ContentLength=payload.Length;payload.Position=0;
                            using(var output=await request.GetRequestStreamAsync())await payload.CopyToAsync(output,8192,timeout.Token);
                        }
                        using(var reply=(HttpWebResponse)await request.GetResponseAsync())
                        using(var input=reply.GetResponseStream())
                        using(var data=new MemoryStream())
                        {
                            if(reply.StatusCode!=HttpStatusCode.OK)throw new IOException("Ollama devolvió una respuesta HTTP inesperada.");
                            var chunk=new byte[8192];int n;
                            while((n=await input.ReadAsync(chunk,0,chunk.Length,timeout.Token))>0)
                            {
                                if(data.Length+n>524288)throw new IOException("Ollama devolvió una respuesta demasiado extensa.");
                                data.Write(chunk,0,n);
                            }
                            data.Position=0;
                            var answer=(Reply)new DataContractJsonSerializer(typeof(Reply)).ReadObject(data);
                            if(answer==null||!answer.done||String.IsNullOrWhiteSpace(answer.response))throw new IOException("El modelo no devolvió una respuesta completa.");
                            return answer.response;
                        }
                    }
                    catch(WebException ex)
                    {
                        cancellation.ThrowIfCancellationRequested();
                        if(timeout.IsCancellationRequested)throw new IOException("El modelo no respondió en dos minutos.",ex);
                        using(var response=ex.Response as HttpWebResponse)
                        {
                            if(response!=null&&response.StatusCode==HttpStatusCode.NotFound)throw new IOException("El modelo no está instalado en Ollama.",ex);
                        }
                        throw new IOException("No se pudo consultar Ollama en 127.0.0.1:11434. Revisá que esté iniciado y que el modelo esté instalado.",ex);
                    }
                }
            }
        }
    }
}

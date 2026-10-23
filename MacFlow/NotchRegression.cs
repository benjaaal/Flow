using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
namespace MacFlow
{
    internal static class NotchRegression
    {
        internal static void Run(string directory,List<string> results)
        {
            Action<bool,string> check=delegate(bool value,string label){if(!value)throw new Exception(label);results.Add("PASS Notch: "+label);};
            check(Math.Abs(AstronomyWallpaper.Angle(new DateTime(2026,10,21,13,0,0))-AstronomyWallpaper.Angle(new DateTime(2026,10,21,12,0,0))-15)<.001,"Tierra avanza quince grados por hora local, sin depender de frames");
            check(AstronomyWallpaper.IsEarth("MacFlow-Tierra-1.png")&&!AstronomyWallpaper.IsEarth("foto.png"),"las selecciones antiguas de Tierra se identifican para retirarlas del escritorio");
            double intensity=Design.GlassOpacity;try{Design.GlassOpacity=.65;check(Design.MaterialTint(false,true).GradientStops.All(g=>g.Color.A<190)&&Design.MaterialTint(true,true).GradientStops.All(g=>g.Color.A<100),"subir intensidad no convierte cristal oscuro en placa opaca");}finally{Design.GlassOpacity=intensity;}
            check(AstronomyWallpaper.SunDirection(new DateTime(2026,10,22,12,0,0)).Z<-.99&&AstronomyWallpaper.SunDirection(new DateTime(2026,10,22,0,0,0)).Z>.99,"el sol local ilumina de frente al mediodía y desde atrás a medianoche");
            var resume=new ResumePresentationGate();check(!resume.Take(true,true),"iniciar sesión por sí solo no abre la pantalla de reanudación");resume.Suspend();resume.Resume();check(!resume.Take(false,true)&&resume.Take(true,true)&&!resume.Take(true,true),"reanudar espera desbloqueo y presenta una sola pantalla");resume.Suspend();resume.Resume();check(!resume.Take(true,false)&&!resume.Take(true,true),"desactivar la pantalla de suspensión elimina la presentación pendiente");
            string notices=Path.Combine(directory,"external-notices");var activity=new ActivityLog(notices);activity.Add("Local","Persistente");activity.SetExternal(new List<ActivityItem>{new ActivityItem{Title="App de prueba",Detail="contenido privado efímero",CreatedUtc=DateTime.UtcNow,External=true,ExternalKey="fixture"}});check(activity.Items.Count==2&&!File.ReadAllText(Path.Combine(notices,"activity.json")).Contains("contenido privado"),"avisos externos se combinan sólo en memoria y no se guardan en el historial");check(new ActivityLog(notices).Items.Count==1,"reiniciar no recupera contenido privado de otras apps");activity.Clear();check(activity.Items.Count==0,"limpiar el historial descarta la copia sin borrar avisos de Windows");
            var confidential=new ActivityItem{Title="App",Detail="Secreto de prueba",External=true};check(PrivacyScreen.NoticeDetail(confidential,false)=="Nueva notificación"&&PrivacyScreen.NoticeDetail(confidential,true)=="Secreto de prueba","las previsualizaciones de otras apps se ocultan por defecto y se muestran sólo por elección");
            var outline=NotchSurface.Outline(416,258);check(outline.IsFrozen&&outline.FillContains(new Point(208,1))&&!outline.FillContains(new Point(1,1)),"isla redondeada sin hombros, con geometría congelada y esquinas transparentes");
            check(outline.FillContains(new Point(208,257))&&!outline.FillContains(new Point(13,257)),"esquinas inferiores redondeadas y contenido recortado");
            var p=new Preferences{NotchScale=Double.NaN,ControlCenterLayout="iPad"};p.Validate();check(p.NotchScale==1&&p.ControlCenterLayout=="Mac","migra la distribución retirada sin recuperar el modo iPad");
            p.NotchScale=10;p.Validate();check(p.NotchScale==1.2&&NotchSurface.Width(p,300,true,true)<=284,"tamaño validado dentro de la pantalla");
            p.NotchShelf=false;p.NotchHover=false;p.NotchMedia=false;p.ShowNotifications=false;var copy=PreferencesIO.Clone(p);copy.IconSize=60;check(copy.NotchShelf==false&&copy.NotchHover==false&&!copy.NotchMedia&&!copy.ShowNotifications&&p.IconSize!=60,"clonar ajustes conserva opciones sin modificar el original");
            Directory.CreateDirectory(directory);string file=Path.Combine(directory,"preferences.json");PreferencesIO.Write(file,p);copy=PreferencesIO.Read(file);check(copy.NotchScale==1.2&&!copy.NotchShelf,"exportación e importación conservan las preferencias reales");
            File.WriteAllText(file,"{\"ControlCenterLayout\":\"iPad\"}");copy=PreferencesIO.Read(file);check(copy.ControlCenterLayout=="Mac"&&copy.NotchMedia&&copy.NotchTimers&&copy.NotchShelf&&copy.NotchHover,"un perfil antiguo recibe valores compatibles y no pierde actividades");
            File.WriteAllBytes(file,new byte[262145]);bool denied=false;try{PreferencesIO.Read(file);}catch(IOException){denied=true;}check(denied,"rechaza importaciones demasiado grandes");
            string prompt=AssistantResearch.Prompt("pregunta de prueba");check(prompt.Contains("tres fuentes")&&prompt.Contains("No inventes")&&prompt.Contains("no confiable"),"investigación pide comparación, citas y trata páginas como datos");
            string answer="https://learn.microsoft.com/a https://developer.apple.com/b https://example.org/c https://example.org/d";check(AssistantResearch.Domains(answer)==3&&AssistantResearch.Sources(answer).Count==4,"cuenta dominios distintos y conserva enlaces recibidos");
            check(AssistantResearch.Sources("https://127.0.0.1/a https://user:pass@example.org https://pc.local/a http://example.org").Count==0,"no ofrece enlaces locales, con credenciales o sin HTTPS");
            check(AssistantResearch.Result("No tengo fuentes.").Contains("no queda verificada"),"no afirma investigar varias páginas sin enlaces suficientes");
            check(typeof(Desktop).Assembly.GetType("MacFlow.TabletController")==null,"el modo iPad exclusivo no forma parte del ejecutable");var t=Design.Text("Mac"+"Flow · Configuración",13,false);check(t.Text=="MacOS · Configuración","marca visible nueva conserva identidad técnica para actualizaciones");
        }
    }
}

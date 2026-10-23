using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace MacFlow
{
    internal static class ModernizationRegression
    {
        private static void Check(bool ok,string text,List<string> results){if(!ok)throw new Exception(text);results.Add("PASS 26.2: "+text);}
        internal static void Run(string directory,List<string> results)
        {
            var store=new SettingsStore(directory);var preferences=new Preferences{Theme="Auto",GlassOpacity=Double.NaN};store.Save(preferences);var restored=store.Load();Check(restored.Theme=="Auto"&&restored.GlassOpacity==.09,"tema automático persiste y una intensidad NaN no llega al renderizador",results);
            preferences.GlassOpacity=Double.PositiveInfinity;preferences.Validate();Check(preferences.GlassOpacity==.09,"valida intensidad infinita",results);
            store.Save(preferences);File.WriteAllText(store.PathName,"{damaged");store.Save(store.Load());File.WriteAllText(store.PathName,"{damaged again");Check(store.Load().Theme=="Auto","reparar el archivo principal no sobrescribe una copia válida con JSON dañado",results);
            Check(AppearancePolicy.Resolve("Auto",true)&&!AppearancePolicy.Resolve("Auto",false)&&AppearancePolicy.Resolve("Dark",false)&&!AppearancePolicy.Resolve("Light",true),"tema automático sigue Windows sin alterar las elecciones manuales",results);
            var widgets=new WidgetState{Items=new List<WidgetSpec>{new WidgetSpec{Surface="Dark"},new WidgetSpec{Surface="obsolete"}}};widgets.Validate();ModuleStore.Write(Path.Combine(directory,"widget-materials.json"),widgets);var savedWidgets=ModuleStore.Read<WidgetState>(Path.Combine(directory,"widget-materials.json"));Check(savedWidgets.Items[0].Surface=="Dark"&&savedWidgets.Items[1].Surface=="Clear","material de cada widget persiste y migra valores antiguos",results);
            var first=Design.Brush("#F8FAFF");Check(Object.ReferenceEquals(first,Design.Brush("#f8faff"))&&first.IsFrozen,"recursos de color compartidos e inmutables",results);
            bool animations=Design.Animations;bool? prior=Motion.TestClientAreaAnimations;try{Design.Animations=true;Motion.TestClientAreaAnimations=false;var button=Design.Button("Prueba",null,false);button.ApplyTemplate();Design.Scale(button,.985);var face=(FrameworkElement)button.Template.FindName("AnimatedFace",button);var scale=(ScaleTransform)face.RenderTransform;Check(!scale.HasAnimatedProperties&&Math.Abs(scale.ScaleX-.985)<.001,"los botones respetan movimiento reducido sin crear relojes",results);}finally{Design.Animations=animations;Motion.TestClientAreaAnimations=prior;}
            LaunchDiagnostics.Begin(directory,true);LaunchDiagnostics.Mark("Dock visible");var record=LaunchDiagnostics.Read(directory);Check(record!=null&&record.Mode=="Windows"&&record.Phase=="Dock visible"&&record.FirstFrameMs>=0,"diagnóstico distingue arranque con Windows y primer cuadro",results);
            var activity=new ActivityLog(directory);activity.Add("Actualización","26.2.0 disponible");activity.Add("Actualización","26.2.0 disponible");Check(activity.Items.Count==1&&new ActivityLog(directory).Items[0].Detail=="26.2.0 disponible","actividad persiste y agrupa avisos duplicados",results);for(int i=0;i<65;i++)activity.Add("Aviso "+i,"Prueba");Check(activity.Items.Count==60&&activity.Items[0].Title=="Aviso 64","el historial tiene un límite de memoria y disco",results);activity.Clear();Check(new ActivityLog(directory).Items.Count==0,"borrar historial elimina los avisos guardados",results);
            int singles=0,doubles=0;using(var clicks=new AppleClickAction(delegate{singles++;},delegate{doubles++;},500)){clicks.Click();clicks.Click();clicks.Flush();Check(singles==0&&doubles==1,"doble clic en la manzana no dispara también el menú",results);clicks.Click();clicks.Flush();Check(singles==1&&doubles==1,"un clic en la manzana conserva su menú",results);clicks.Click();}Check(singles==1,"destruir la barra cancela un clic pendiente",results);
            Check(ControlPullGesture.Progress(-10,700)==0&&ControlPullGesture.Progress(6,700)==0&&ControlPullGesture.Progress(1000,700)==1&&ControlPullGesture.Progress(120,700)>.25,"gesto del centro de control tiene umbral y progreso acotado",results);
            Check(SettingsWindow.SearchMatches("Tema automático oscuro","automatico tema")&&!SettingsWindow.SearchMatches("Dock iconos","inicio"),"búsqueda de ajustes admite acentos y varias palabras",results);
            Check(CompositorGlass.BackdropCoverage(.09,true)<CompositorGlass.BackdropCoverage(.09)&&CompositorGlass.BackdropCoverage(.65,true)<=1,"material de navegación deja pasar más fondo sin perder composición",results);
        }
    }
}

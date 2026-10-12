using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
namespace MacFlow
{
    internal static class ModuleRegression
    {
        private static void Check(bool ok,string label,List<string> results){if(!ok)throw new Exception(label);results.Add("PASS "+label);}
        internal static void Run(string directory,List<string> results)
        {
            var state=new WidgetState();var first=new WidgetSpec{Kind="Calendar",Size="L"};state.Items.Add(first);var second=new WidgetSpec{Kind="Notes",Size="M",Text="Mi proyecto"};
            Check(WidgetCatalog.Place(second,state.Items,new Size(1280,800),0,0)&&!WidgetCatalog.Bounds(first).IntersectsWith(WidgetCatalog.Bounds(second)),"Widgets: cuadrícula evita superposición al añadir",results);state.Items.Add(second);
            Check(!WidgetCatalog.Place(new WidgetSpec{Size="L"},state.Items,new Size(300,250),0,0),"Widgets: rechaza tamaños que no caben en la pantalla",results);
            state.Items.Add(new WidgetSpec{Kind="Timer",TimerMinutes=2,Running=true,StartedUtc=DateTime.UtcNow.AddSeconds(-30)});string file=Path.Combine(directory,"widgets.json");ModuleStore.Write(file,state);var restored=ModuleStore.Read<WidgetState>(file);restored.Validate();Check(restored.Items.Count==3&&restored.Items[1].Text=="Mi proyecto"&&restored.Items[1].Column==second.Column,"Widgets: texto, posiciones y tipos sobreviven a reiniciar",results);
            var timer=restored.Items[2];Check(timer.Remaining(DateTime.UtcNow)>85&&timer.Remaining(DateTime.UtcNow)<91,"Widgets: temporizador recupera el tiempo transcurrido",results);timer.Toggle(DateTime.UtcNow);double paused=timer.Elapsed(DateTime.UtcNow);Check(!timer.Running&&Math.Abs(timer.Elapsed(DateTime.UtcNow.AddMinutes(2))-paused)<.01,"Widgets: pausa conserva tiempo sin avanzar",results);timer.Reset();Check(!timer.Running&&timer.Seconds==0&&!timer.Completed,"Widgets: reinicio restaura la duración",results);
            ModuleStore.Write(file,restored);Check(File.Exists(file+".bak")&&ModuleStore.Read<WidgetState>(file).Items.Count==3,"Widgets: reemplazo atómico conserva copia anterior",results);File.WriteAllText(file,"{broken");Check(ModuleStore.Read<WidgetState>(file).Items.Count==3,"Widgets: configuración dañada recupera la copia anterior",results);
            var bad=new WidgetState{Items=new List<WidgetSpec>{new WidgetSpec{Kind="Notes",Size="XL",Column=-9,Seconds=Double.NaN},new WidgetSpec{Kind="Invalid"}}};bad.Validate();Check(bad.Items.Count==1&&bad.Items[0].Size=="S"&&bad.Items[0].Column==0&&bad.Items[0].Seconds==0,"Widgets: valida tamaños, tipos, posición y tiempos",results);
            var group=new StageGroup{Name="Proyecto",Members=new List<StageMember>{new StageMember{Key="chrome",Title="Diseño"},new StageMember{Key="chrome",Title="Código"},new StageMember{Key="editor",Title="Notas"}}};var windows=new[]{new WindowEntry{Handle=new IntPtr(1),Executable="chrome",Title="Código"},new WindowEntry{Handle=new IntPtr(2),Executable="chrome",Title="Diseño"}};var matched=StageController.Match(group,windows);Check(matched.Count==3&&matched[0].Handle==new IntPtr(2)&&matched[1].Handle==new IntPtr(1)&&matched[2]==null,"Stage Manager: distingue varias ventanas y conserva las ausentes",results);
            var stage=new StageState{Side="Invalid",Size=400,Groups=new List<StageGroup>{new StageGroup{Members=new List<StageMember>{new StageMember{Key="app",X=Double.NaN,Y=99,Width=Double.PositiveInfinity,Height=-8}}}}};stage.Validate();Check(stage.Side=="Left"&&stage.Size==128&&stage.Groups[0].Members[0].Y<=.9&&!Double.IsNaN(stage.Groups[0].Members[0].X),"Stage Manager: valida configuración y geometría",results);string stageFile=Path.Combine(directory,"stage.json");ModuleStore.Write(stageFile,stage);Check(ModuleStore.Read<StageState>(stageFile).Groups.Count==1,"Stage Manager: grupos persisten separados de preferencias del Dock",results);
            using(var control=new ControlFixture()){control.Volume(72);control.Mute(true);control.SetBrightness(81).Wait();control.SwitchRadio(true,false,System.Threading.CancellationToken.None).Wait();Check(control.Sound().Volume==72&&control.Sound().Muted&&control.ReadBrightness().Result==81&&!control.Radio(true).Result.On,"Centro de control: contrato de controles reales con proveedor aislado",results);}
        }
    }
}

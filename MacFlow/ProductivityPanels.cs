using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace MacFlow
{
    internal sealed class CalculatorPanel : FeaturePanel
    {
        internal CalculatorPanel():base("Calculadora",385,420)
        {
            var input=ModuleUI.Input("",160);
            input.FontSize=23;
            System.Windows.Automation.AutomationProperties.SetName(input,"Expresión");
            var field=ModuleUI.Field(input);DockPanel.SetDock(field,Dock.Top);Body.Children.Add(field);
            var result=Caption("");result.FontSize=16;DockPanel.SetDock(result,Dock.Top);Body.Children.Add(result);
            Action calculate=delegate{
                string value;
                result.Text=Calculator.TryEvaluate(input.Text,out value)?value:"Expresión no válida";
                if(result.Text==value){ input.Text=value; input.CaretIndex=input.Text.Length; }
            };
            var keys=new System.Windows.Controls.Primitives.UniformGrid{Columns=4};
            foreach(string item in new[]{"7","8","9","÷","4","5","6","×","1","2","3","−","0",",","^","+","(",")","Borrar","="})
            {
                string key=item;
                keys.Children.Add(Design.Button(key,delegate{
                    if(key=="="){calculate();return;}
                    if(key=="Borrar"){input.Clear();result.Text="";input.Focus();return;}
                    if(input.Text.Length-input.SelectionLength>=input.MaxLength)return;
                    int position=input.SelectionStart;
                    input.SelectedText=key=="−"?"-":key;
                    input.CaretIndex=position+1;
                    input.Focus();
                },key=="="));
            }
            input.PreviewKeyDown+=delegate(object s,KeyEventArgs e){if(e.Key==Key.Enter){calculate();e.Handled=true;}};
            Body.Children.Add(keys);
            Loaded+=delegate{if(ShowActivated)input.Focus();};
        }
    }

    [DataContract]
    internal sealed class NoteDocument
    {
        [DataMember] public string Text="";
    }

    internal sealed class NotesPanel : FeaturePanel
    {
        private readonly TextBox editor;
        private readonly TextBlock status=ModuleUI.Caption("");
        private readonly string path;
        private string saved;
        internal NotesPanel(string directory):base("Notas",615,510)
        {
            path=Path.Combine(directory,"notes-app.json");
            saved=ModuleStore.Read<NoteDocument>(path).Text??"";
            editor=ModuleUI.Input(saved,28000,true);
            editor.VerticalScrollBarVisibility=ScrollBarVisibility.Auto;
            System.Windows.Automation.AutomationProperties.SetName(editor,"Nota del perfil");
            var actions=new DockPanel{Margin=new Thickness(0,12,0,0)};
            var save=Design.Button("Guardar",delegate{Save();},true);
            DockPanel.SetDock(save,Dock.Right);actions.Children.Add(save);actions.Children.Add(status);
            DockPanel.SetDock(actions,Dock.Bottom);Body.Children.Add(actions);Body.Children.Add(ModuleUI.Field(editor));
            editor.TextChanged+=delegate{status.Text=editor.Text==saved?"":"Cambios pendientes";};
            PreviewKeyDown+=delegate(object s,KeyEventArgs e){if(e.Key==Key.S&&(Keyboard.Modifiers&ModifierKeys.Control)!=0){Save();e.Handled=true;}};
            Closing+=delegate(object s,System.ComponentModel.CancelEventArgs e){if(editor.Text!=saved&&!Save())e.Cancel=true;};
        }
        private bool Save()
        {
            try{ModuleStore.Write(path,new NoteDocument{Text=editor.Text});saved=editor.Text;status.Text="Guardado";return true;}
            catch(Exception ex){status.Text="No se pudo guardar. "+ex.Message;return false;}
        }
    }

    [DataContract]
    internal sealed class CalendarEntries
    {
        [DataMember] public Dictionary<string,string> Days=new Dictionary<string,string>();
    }

    internal sealed class CalendarPanel : FeaturePanel
    {
        internal CalendarPanel(string directory):base("Calendario",655,430)
        {
            string path=Path.Combine(directory,"calendar-app.json");
            var agenda=ModuleStore.Read<CalendarEntries>(path);
            if(agenda.Days==null)agenda.Days=new Dictionary<string,string>();
            var calendar=new Calendar{SelectedDate=DateTime.Today,Margin=new Thickness(0,0,14,0),VerticalAlignment=VerticalAlignment.Top};
            DockPanel.SetDock(calendar,Dock.Left);Body.Children.Add(calendar);
            var heading=Design.Text("",18,true);
            var status=Caption("");
            var list=new TextBlock{TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,14,0,0)};
            list.SetResourceReference(TextBlock.ForegroundProperty,"GlassInk");
            var stack=new StackPanel();stack.Children.Add(heading);stack.Children.Add(status);
            Func<string> dateKey=()=> (calendar.SelectedDate??DateTime.Today).ToString("yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture);
            Action refresh=delegate{
                heading.Text=(calendar.SelectedDate??DateTime.Today).ToString("D");status.Text="";
                string value;list.Text=agenda.Days.TryGetValue(dateKey(),out value)?value:"Sin eventos";
            };
            calendar.SelectedDatesChanged+=delegate{refresh();};
            var edit=Design.Button("Editar agenda del día",delegate{
                string previous;
                string key=dateKey();
                agenda.Days.TryGetValue(key,out previous);
                string value=NamePrompt.Ask(this,"Agenda del día",previous??"");
                if(value==null)return;
                value=value.Trim();
                if(value.Length>240){status.Text="La agenda del día admite hasta 240 caracteres.";return;}
                if(previous==null&&agenda.Days.Count>=300){status.Text="El calendario admite hasta 300 días con eventos.";return;}
                if(value.Length==0)agenda.Days.Remove(key);else agenda.Days[key]=value;
                try{ModuleStore.Write(path,agenda);refresh();}
                catch(Exception ex){if(previous==null)agenda.Days.Remove(key);else agenda.Days[key]=previous;status.Text=ex.Message;}
            },true);
            stack.Children.Add(edit);
            stack.Children.Add(Design.Button("Hoy",delegate{calendar.DisplayDate=DateTime.Today;calendar.SelectedDate=DateTime.Today;},false));
            stack.Children.Add(list);Body.Children.Add(stack);refresh();
        }
    }

    internal sealed class ProcessPanel : FeaturePanel
    {
        private readonly ListBox rows=new ListBox{Background=System.Windows.Media.Brushes.Transparent,BorderThickness=new Thickness(0)};
        private readonly TextBlock status=ModuleUI.Caption("");
        private readonly DispatcherTimer timer=new DispatcherTimer(DispatcherPriority.Background){Interval=TimeSpan.FromSeconds(3)};
        private bool busy,closed;
        internal ProcessPanel(Desktop desktop):base("Monitor de actividad",615,505)
        {
            rows.SetResourceReference(Control.ForegroundProperty,"GlassInk");
            System.Windows.Automation.AutomationProperties.SetName(rows,"Procesos por memoria privada");
            var tools=new DockPanel();var manager=Design.Button("Administrador de tareas",desktop.OpenTaskManager,false);
            DockPanel.SetDock(manager,Dock.Right);tools.Children.Add(manager);tools.Children.Add(status);
            DockPanel.SetDock(tools,Dock.Top);Body.Children.Add(tools);Body.Children.Add(rows);
            timer.Tick+=delegate{Refresh();};
            IsVisibleChanged+=delegate{UpdateTimer();};StateChanged+=delegate{UpdateTimer();};
            Closed+=delegate{closed=true;timer.Stop();};
        }
        private void UpdateTimer()
        {
            if(closed||!IsVisible||WindowState==WindowState.Minimized){timer.Stop();return;}
            timer.Start();Refresh();
        }
        private async void Refresh()
        {
            if(busy)return;busy=true;
            try
            {
                var snapshot=await Task.Run(delegate{
                    var result=new List<Tuple<string,int,long>>();
                    foreach(var process in Process.GetProcesses())
                    {
                        using(process)
                        {
                            try{result.Add(Tuple.Create(process.ProcessName,process.Id,process.PrivateMemorySize64));}
                            catch(InvalidOperationException){}
                            catch(System.ComponentModel.Win32Exception){}
                        }
                    }
                    return result;
                });
                if(closed||!IsVisible||WindowState==WindowState.Minimized)return;
                rows.ItemsSource=snapshot.OrderByDescending(p=>p.Item3).Take(150).Select(p=>p.Item1+"  ·  PID "+p.Item2+"  ·  "+FilePreview.FormatSize(p.Item3)).ToArray();
                status.Text=snapshot.Count+" procesos · memoria privada"+(snapshot.Count>150?" · primeros 150":"");
            }
            catch(Exception ex){if(!closed)status.Text=ex.Message;}
            finally{busy=false;}
        }
    }
}

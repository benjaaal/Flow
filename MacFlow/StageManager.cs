using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
namespace MacFlow
{
    [DataContract]internal sealed class StageMember
    {[DataMember]public string Key="",Title="";[DataMember]public double X,Y,Width=.5,Height=.8;}
    [DataContract]internal sealed class StageGroup
    {[DataMember]public string Name="Grupo";[DataMember]public List<StageMember> Members=new List<StageMember>();public override string ToString(){return Name;}}
    [DataContract]internal sealed class StageState
    {
        [DataMember]public bool Enabled,Automatic=true,Labels=true,Live=true;
        [DataMember]public string Side="Left";
        [DataMember]public int Size=128;
        [DataMember]public List<StageGroup> Groups=new List<StageGroup>();
        internal void Validate()
        {
            if(Side!="Left"&&Side!="Right")Side="Left";if(Size!=96&&Size!=128&&Size!=160)Size=128;if(Groups==null)Groups=new List<StageGroup>();Groups=Groups.Where(g=>g!=null).Take(16).ToList();
            foreach(var g in Groups){g.Name=(g.Name??"Grupo").Substring(0,Math.Min(60,(g.Name??"Grupo").Length));if(g.Members==null)g.Members=new List<StageMember>();g.Members=g.Members.Where(m=>m!=null&&!String.IsNullOrWhiteSpace(m.Key)).Take(8).ToList();foreach(var m in g.Members){m.X=Clamp(m.X,0,.9);m.Y=Clamp(m.Y,0,.9);m.Width=Clamp(m.Width,.15,1);m.Height=Clamp(m.Height,.15,1);m.Width=Math.Min(m.Width,1-m.X);m.Height=Math.Min(m.Height,1-m.Y);}}
        }
        private static double Clamp(double value,double min,double max){return Double.IsNaN(value)||Double.IsInfinity(value)?min:Math.Max(min,Math.Min(max,value));}
    }
    internal sealed class StageController : IDisposable
    {
        internal readonly StageState State;internal readonly Desktop Desktop;private readonly string file;
        private StageSidebar sidebar;private StageSettings settings;private bool disposed,manual,blocked;private string signature="";
        internal StageSidebar Sidebar{get{return sidebar;}}
        internal StageController(Desktop desktop){Desktop=desktop;file=Path.Combine(desktop.Store.DirectoryPath,"stage-manager.json");State=ModuleStore.Read<StageState>(file);State.Validate();}
        internal void Save(){try{State.Validate();ModuleStore.Write(file,State);}catch(Exception ex){Log.Write("Stage Manager: "+ex.Message);}}
        internal void OpenSettings(){if(settings!=null){if(settings.WindowState==WindowState.Minimized)settings.WindowState=WindowState.Normal;if(!settings.IsVisible)settings.Show();if(!Desktop.IsTesting)settings.Activate();return;}settings=new StageSettings(this);settings.Closed+=delegate{settings=null;};Desktop.ShowModule(settings);}
        internal void ToggleManual(){manual=!manual;Update(Desktop.Windows,Native.GetForegroundWindow(),blocked);}
        internal void Reconfigure(){Update(Desktop.Windows,Native.GetForegroundWindow(),blocked);}
        internal void Update(IList<WindowEntry> entries,IntPtr foreground,bool hide)
        {
            if(disposed)return;blocked=hide;bool visible=State.Enabled&&!hide&&(State.Automatic||manual);
            if(!visible){if(sidebar!=null&&sidebar.IsVisible){sidebar.ReleasePreviews();sidebar.Hide();}return;}
            var actual=entries.Where(w=>w.ProcessId!=(uint)System.Diagnostics.Process.GetCurrentProcess().Id&&Native.Matches(w)).Take(64).ToList();
            if(actual.Count==0){if(sidebar!=null&&sidebar.IsVisible){sidebar.ReleasePreviews();sidebar.Hide();}return;}
            if(sidebar==null){sidebar=new StageSidebar(this);sidebar.Closed+=delegate{sidebar=null;signature="";};}
            sidebar.Position(actual.Count);string next=String.Join("|",actual.Select(w=>w.Handle+":"+w.Title+":"+Native.IsIconic(w.Handle)))+State.Size+State.Side+State.Labels+State.Live;
            if(!sidebar.IsVisible||next!=signature){signature=next;sidebar.Build(actual);if(!sidebar.IsVisible)sidebar.Show();}sidebar.Highlight(foreground);
        }
        internal StageGroup Capture(string name,IEnumerable<WindowEntry> selection)
        {
            var area=System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;var group=new StageGroup{Name=name};
            foreach(var w in selection.Take(8)){Native.RECT r;if(!Native.Matches(w)||!Native.GetWindowRect(w.Handle,out r)||Native.IsIconic(w.Handle))continue;group.Members.Add(new StageMember{Key=w.Key,Title=w.Title,X=(double)(r.Left-area.Left)/area.Width,Y=(double)(r.Top-area.Top)/area.Height,Width=(double)(r.Right-r.Left)/area.Width,Height=(double)(r.Bottom-r.Top)/area.Height});}
            if(group.Members.Count==0)return null;State.Groups.Add(group);Save();return group;
        }
        internal static List<WindowEntry> Match(StageGroup group,IEnumerable<WindowEntry> available)
        {
            var pool=available.ToList();var result=new List<WindowEntry>();foreach(var member in group.Members){var w=pool.FirstOrDefault(x=>String.Equals(x.Key,member.Key,StringComparison.OrdinalIgnoreCase)&&x.Title==member.Title)??pool.FirstOrDefault(x=>String.Equals(x.Key,member.Key,StringComparison.OrdinalIgnoreCase));result.Add(w);if(w!=null)pool.Remove(w);}return result;
        }
        internal async Task<string> Restore(StageGroup group,bool activate=true)
        {
            var matches=Match(group,Desktop.Windows);int missing=0,denied=0;var area=System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;
            foreach(var w in matches.Where(w=>w!=null)){if(!Native.Matches(w)){denied++;continue;}Native.ShowWindowAsync(w.Handle,9);}await Task.Delay(160);if(disposed)return "";
            for(int i=0;i<group.Members.Count;i++){var w=matches[i];if(w==null){missing++;continue;}if(!Native.Matches(w)){denied++;continue;}var m=group.Members[i];if(!Native.SetWindowPos(w.Handle,IntPtr.Zero,area.Left+(int)(area.Width*m.X),area.Top+(int)(area.Height*m.Y),Math.Max(240,(int)(area.Width*m.Width)),Math.Max(180,(int)(area.Height*m.Height)),0x14))denied++;}
            var target=matches.FirstOrDefault(w=>w!=null&&Native.Matches(w));if(activate&&target!=null&&!Native.Activate(target))denied++;return "Grupo recuperado"+(missing>0?" · "+missing+" ventanas no disponibles":"")+(denied>0?" · Windows no permitió mover algunas ventanas":"");
        }
        public void Dispose(){disposed=true;if(settings!=null)settings.Close();if(sidebar!=null)sidebar.Close();}
    }
    internal sealed class StageSidebar : Window
    {
        private readonly StageController owner;private readonly StackPanel rows=new StackPanel();private readonly Dictionary<IntPtr,Border> cards=new Dictionary<IntPtr,Border>();private readonly List<Thumbnail> previews=new List<Thumbnail>();
        internal StageSidebar(StageController owner)
        {
            this.owner=owner;Title="MacOS · Stage Manager";WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;ShowInTaskbar=false;ShowActivated=false;Topmost=true;Background=Brushes.Transparent;
            var root=new DockPanel();var header=new StackPanel{Orientation=Orientation.Horizontal};header.Children.Add(ModuleUI.Button("⋯",owner.OpenSettings,false));header.Children.Add(ModuleUI.Button("×",delegate{owner.State.Enabled=false;owner.Save();owner.Update(owner.Desktop.Windows,Native.GetForegroundWindow(),false);},false));DockPanel.SetDock(header,Dock.Top);root.Children.Add(header);root.Children.Add(new ScrollViewer{Content=rows,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled});Content=Design.Glass(root,22,false);
            SourceInitialized+=delegate{Native.Glass(this,Design.Dark);Native.SetWindowLong(new WindowInteropHelper(this).Handle,-20,Native.GetWindowLong(new WindowInteropHelper(this).Handle,-20)|0x80);};Closed+=delegate{ReleasePreviews();};
            if(owner.Desktop.IsTesting){IsHitTestVisible=false;SourceInitialized+=delegate{Native.NoActivate(this);};}
        }
        internal void Position(int count=-1){var s=SystemParameters.WorkArea;Width=owner.State.Size+48;double maximum=Math.Min(670,s.Height-100);Height=count<0?maximum:Math.Min(maximum,100+Math.Min(count,6)*(owner.State.Size*.56+48));Left=owner.State.Side=="Right"?s.Right-Width-8:s.Left+8;Top=s.Top+54;}
        internal void ReleasePreviews(){foreach(var p in previews)p.Dispose();previews.Clear();rows.Children.Clear();cards.Clear();}
        internal void Build(IList<WindowEntry> windows)
        {
            ReleasePreviews();if(windows.Count==0){rows.Children.Add(Design.Text("Sin ventanas",11,false));return;}
            int capacity=Math.Max(1,(int)((Height-115)/(owner.State.Size*.56+48)));
            foreach(var entry in windows.Take(capacity))
            {
                var w=entry;var content=new StackPanel();var app=new AppEntry{Name=w.Title,Target=w.Executable,Executable=w.Executable};
                var icon=Design.AppIcon(app,22);var label=new DockPanel();label.Children.Add(icon);if(owner.State.Labels){var text=Design.Text(w.Title,10,false);text.TextTrimming=TextTrimming.CharacterEllipsis;text.Margin=new Thickness(6,0,0,0);label.Children.Add(text);}content.Children.Add(label);
                if(owner.State.Live&&!Native.IsIconic(w.Handle)){var preview=new Thumbnail(w.Handle){Width=owner.State.Size,Height=owner.State.Size*.56,Margin=new Thickness(0,5,0,0)};previews.Add(preview);content.Children.Add(preview);}
                else if(Native.IsIconic(w.Handle))content.Children.Add(Design.Text("Minimizada",10,false));
                var button=Design.Button("",delegate{if(!Native.Activate(w))MessageBox.Show("Windows no permitió activar esa ventana.","MacOS");},false);button.Background=Brushes.Transparent;button.Content=new GlassPiece(content,13,new Thickness(6));button.Margin=new Thickness(0);button.Padding=new Thickness(0);var frame=new Border{Child=button,CornerRadius=new CornerRadius(14),BorderThickness=new Thickness(1),Margin=new Thickness(0,0,0,7)};cards[w.Handle]=frame;rows.Children.Add(frame);
            }
            if(windows.Count>capacity)rows.Children.Add(ModuleUI.Button("Más ventanas",owner.Desktop.OpenOverview,false));
        }
        internal void Highlight(IntPtr active){foreach(var pair in cards)pair.Value.BorderBrush=pair.Key==active?Design.Brush(Design.Accent):Brushes.Transparent;}
    }
    internal sealed class StageSettings : FeaturePanel
    {
        internal StageSettings(StageController owner):base("Stage Manager · Grupos",700,640)
        {
            var root=new StackPanel();root.Children.Add(Design.Text("Dale un lugar a cada proyecto.",23,true));root.Children.Add(Caption("Las miniaturas usan el compositor de Windows. Los grupos recuperan las ventanas disponibles; no cierran ni abren aplicaciones automáticamente."));
            Action change=delegate{owner.Save();owner.Reconfigure();};root.Children.Add(Design.Check("Activar Stage Manager",owner.State.Enabled,delegate(bool on){owner.State.Enabled=on;change();}));root.Children.Add(Design.Check("Mostrar automáticamente",owner.State.Automatic,delegate(bool on){owner.State.Automatic=on;change();}));root.Children.Add(ModuleUI.Button("Mostrar / ocultar en modo manual",owner.ToggleManual,false));
            var side=new ComboBox{ItemsSource=new[]{"Izquierda","Derecha"},SelectedIndex=owner.State.Side=="Right"?1:0};side.SelectionChanged+=delegate{owner.State.Side=side.SelectedIndex==1?"Right":"Left";change();};root.Children.Add(Caption("Ubicación de la barra"));root.Children.Add(side);var size=new ComboBox{ItemsSource=new[]{96,128,160},SelectedItem=owner.State.Size};size.SelectionChanged+=delegate{owner.State.Size=(int)size.SelectedItem;change();};root.Children.Add(Caption("Tamaño de miniaturas"));root.Children.Add(size);
            root.Children.Add(Design.Check("Mostrar etiquetas",owner.State.Labels,delegate(bool on){owner.State.Labels=on;change();}));root.Children.Add(Design.Check("Miniaturas en vivo · desactivar para ahorrar recursos",owner.State.Live,delegate(bool on){owner.State.Live=on;change();}));
            root.Children.Add(Design.Text("Nuevo grupo",17,true));var name=ModuleUI.Input("Mi proyecto",60);root.Children.Add(ModuleUI.Field(name));var choices=new List<Tuple<WindowEntry,CheckBox>>();
            foreach(var w in owner.Desktop.Windows.Where(w=>!Native.IsIconic(w.Handle)).Take(16)){var check=Design.Check(w.Title,false,delegate{});root.Children.Add(check);choices.Add(Tuple.Create(w,check));}
            var groups=new ComboBox{ItemsSource=owner.State.Groups.ToList()};var feedback=Caption("");root.Children.Add(ModuleUI.Button("Guardar ventanas seleccionadas",delegate{if(owner.State.Groups.Count>=16){feedback.Text="Límite de 16 grupos.";return;}var group=owner.Capture(String.IsNullOrWhiteSpace(name.Text)?"Grupo":name.Text.Trim(),choices.Where(x=>x.Item2.IsChecked==true).Select(x=>x.Item1));if(group==null){feedback.Text="Seleccioná al menos una ventana visible.";return;}groups.ItemsSource=owner.State.Groups.ToList();groups.SelectedItem=group;feedback.Text="Grupo guardado.";},false));root.Children.Add(groups);
            var restore=ModuleUI.Button("Recuperar grupo",null,false);restore.Click+=async delegate{var group=groups.SelectedItem as StageGroup;if(group==null)return;restore.IsEnabled=false;try{feedback.Text=await owner.Restore(group);}catch(Exception ex){feedback.Text="No se pudo recuperar el grupo.";Log.Write(ex.Message);}finally{restore.IsEnabled=true;}};root.Children.Add(restore);root.Children.Add(ModuleUI.Button("Eliminar grupo",delegate{var group=groups.SelectedItem as StageGroup;if(group==null)return;owner.State.Groups.Remove(group);owner.Save();groups.ItemsSource=owner.State.Groups.ToList();},false));root.Children.Add(feedback);Body.Children.Add(new ScrollViewer{Content=root,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});
        }
    }
}

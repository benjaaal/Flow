using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Windows;
using System.Windows.Controls;
namespace MacFlow
{
    [DataContract] internal sealed class ActivityItem
    {
        [DataMember] public string Title,Detail;
        [DataMember] public DateTime CreatedUtc;
    }
    internal sealed class ActivityLog
    {
        private readonly string path;private List<ActivityItem> items;
        internal event Action Changed;
        internal ActivityLog(string directory){path=Path.Combine(directory,"activity.json");}
        internal IList<ActivityItem> Items
        {
            get
            {
                if(items==null){try{using(var file=File.OpenRead(path))items=(List<ActivityItem>)new DataContractJsonSerializer(typeof(List<ActivityItem>)).ReadObject(file);}catch{items=new List<ActivityItem>();}items=(items??new List<ActivityItem>()).Where(i=>i!=null&&!String.IsNullOrWhiteSpace(i.Title)).Take(60).ToList();}
                return items.AsReadOnly();
            }
        }
        internal void Add(string title,string detail)
        {
            var ignored=Items;if(String.IsNullOrWhiteSpace(title))return;title=Trim(title,100);detail=Trim(detail??"",1000);
            if(items.Count>0&&items[0].Title==title&&items[0].Detail==detail&&DateTime.UtcNow-items[0].CreatedUtc<TimeSpan.FromMinutes(1))return;
            items.Insert(0,new ActivityItem{Title=title,Detail=detail,CreatedUtc=DateTime.UtcNow});if(items.Count>60)items.RemoveRange(60,items.Count-60);Save();if(Changed!=null)Changed();
        }
        private static string Trim(string value,int count){return value.Length>count?value.Substring(0,count):value;}
        internal void Clear(){var ignored=Items;items.Clear();Save();if(Changed!=null)Changed();}
        private void Save(){try{Directory.CreateDirectory(Path.GetDirectoryName(path));string temp=path+".tmp";using(var file=File.Create(temp))new DataContractJsonSerializer(typeof(List<ActivityItem>)).WriteObject(file,items);if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}catch(Exception ex){Log.Write("No se pudo guardar la actividad: "+ex.Message);}}
    }
    internal sealed class ActivityCenter : FeaturePanel
    {
        private readonly ActivityLog history;private readonly StackPanel list=new StackPanel();
        internal ActivityCenter(ActivityLog history):base("Actividad",390,510)
        {
            this.history=history;var footer=new DockPanel{Margin=new Thickness(0,12,0,0)};footer.Children.Add(Design.Button("Borrar historial",history.Clear,false));DockPanel.SetDock(footer,Dock.Bottom);Body.Children.Add(footer);
            var description=Caption("Actualizaciones, concentración y avisos de MacFlow. Se guardan sólo en esta computadora.");description.Margin=new Thickness(0,0,0,16);DockPanel.SetDock(description,Dock.Top);Body.Children.Add(description);Body.Children.Add(new ScrollViewer{Content=list,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled});
            history.Changed+=Refresh;Closed+=delegate{history.Changed-=Refresh;};Refresh();
        }
        private void Refresh()
        {
            list.Children.Clear();if(history.Items.Count==0){var empty=new StackPanel{Margin=new Thickness(0,45,0,0)};empty.Children.Add(Design.Symbol("bell",32,Design.Muted));var message=Design.Text("Todo al día",20,true);message.TextAlignment=TextAlignment.Center;message.Margin=new Thickness(0,15,0,8);empty.Children.Add(message);var detail=Caption("Los próximos avisos de MacFlow aparecerán acá.");detail.TextAlignment=TextAlignment.Center;empty.Children.Add(detail);list.Children.Add(empty);return;}
            foreach(var item in history.Items){var row=new StackPanel();var title=Design.Text(item.Title,14,true);title.TextWrapping=TextWrapping.Wrap;row.Children.Add(title);var body=Caption(item.Detail);body.Margin=new Thickness(0,6,0,8);row.Children.Add(body);row.Children.Add(Caption(item.CreatedUtc.ToLocalTime().ToString("ddd d MMM · HH:mm")));list.Children.Add(Design.Card(row));}
        }
    }
}

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Controls;
namespace MacFlow
{
    [DataContract] internal sealed class MacFlowProfile
    {
        [DataMember] public string Id="default",Name="Mi perfil",Avatar="●";
        public override string ToString(){return Avatar+" "+Name;}
    }
    [DataContract] internal sealed class ProfileCatalog
    {
        [DataMember] public string Active="default";
        [DataMember] public List<MacFlowProfile> Items=new List<MacFlowProfile>{new MacFlowProfile()};
    }
    internal static class ProfileStore
    {
        private static bool ValidId(string id){return id=="default"||id!=null&&System.Text.RegularExpressions.Regex.IsMatch(id,"^[a-f0-9]{32}$");}
        internal static ProfileCatalog Load(string root)
        {
            var value=ModuleStore.Read<ProfileCatalog>(Path.Combine(root,"profiles.json"));if(value.Items==null)value.Items=new List<MacFlowProfile>();
            value.Items.RemoveAll(p=>p==null||!ValidId(p.Id));if(!value.Items.Any(p=>p.Id=="default"))value.Items.Insert(0,new MacFlowProfile());
            if(!value.Items.Any(p=>p.Id==value.Active))throw new IOException("El perfil activo no existe. Elegí uno válido en profiles.json; los datos se conservaron.");return value;
        }
        internal static string ActiveDirectory(string root){string id=Load(root).Active;return id=="default"?root:Path.Combine(root,"Profiles",id);}
        internal static MacFlowProfile Create(string root,string name,string avatar="●"){name=(name??"").Trim();if(name.Length==0||name.Length>48||name.Any(Char.IsControl))throw new IOException("Usá un nombre de entre 1 y 48 caracteres.");var catalog=Load(root);if(catalog.Items.Count>=20)throw new IOException("Se admiten hasta veinte perfiles.");var profile=new MacFlowProfile{Id=Guid.NewGuid().ToString("N"),Name=name,Avatar=new[]{"●","◆","✦","☀"}.Contains(avatar)?avatar:"●"};Directory.CreateDirectory(Path.Combine(root,"Profiles",profile.Id));catalog.Items.Add(profile);ModuleStore.Write(Path.Combine(root,"profiles.json"),catalog);return profile;}
        internal static void Select(string root,string id){var catalog=Load(root);if(!catalog.Items.Any(p=>p.Id==id))throw new IOException("Perfil desconocido.");catalog.Active=id;ModuleStore.Write(Path.Combine(root,"profiles.json"),catalog);}
    }
    internal sealed class ProfileWindow : FeaturePanel
    {
        internal ProfileWindow():base("Perfiles de MacFlow",470,370)
        {
            var stack=new StackPanel();Body.Children.Add(stack);stack.Children.Add(Caption("Son perfiles de MacFlow dentro de tu sesión de Windows. No crean cuentas de Windows ni aíslan datos de otros usuarios de esta sesión."));
            var list=new ComboBox{ItemsSource=ProfileStore.Load(StoragePaths.DataDirectory).Items,Margin=new Thickness(0,14,0,14)};stack.Children.Add(list);var name=ModuleUI.Input("",48);name.ToolTip="Nombre de un perfil nuevo";stack.Children.Add(name);var avatar=new ComboBox{ItemsSource=new[]{"●","◆","✦","☀"},SelectedIndex=0,Margin=new Thickness(0,8,0,8)};stack.Children.Add(avatar);var status=Caption("");
            stack.Children.Add(Design.Button("Crear perfil",delegate{try{var profile=ProfileStore.Create(StoragePaths.DataDirectory,name.Text,(string)avatar.SelectedItem);list.ItemsSource=ProfileStore.Load(StoragePaths.DataDirectory).Items;list.SelectedItem=((System.Collections.Generic.IEnumerable<MacFlowProfile>)list.ItemsSource).First(p=>p.Id==profile.Id);status.Text="Creado con datos separados. Elegí Usar para seleccionarlo.";}catch(Exception ex){status.Text=ex.Message;}},false));
            stack.Children.Add(Design.Button("Usar al próximo inicio",delegate{try{var profile=list.SelectedItem as MacFlowProfile;if(profile==null)return;ProfileStore.Select(StoragePaths.DataDirectory,profile.Id);status.Text="Seleccionado. Salí de MacFlow y volvé a abrirlo para cambiar de perfil.";}catch(Exception ex){status.Text=ex.Message;}},true));stack.Children.Add(status);
        }
    }
    internal sealed class OnboardingWindow : FeaturePanel
    {
        internal OnboardingWindow(Desktop desktop):base("Bienvenido a MacFlow",490,480)
        {
            var stack=new StackPanel();Body.Children.Add(stack);stack.Children.Add(Caption("Windows sigue siendo tu sistema. Podés salir de MacFlow cuando quieras; su perfil no reemplaza la cuenta ni la pantalla segura de Windows."));
            stack.Children.Add(Design.Text("Español (Argentina)",15,true));var theme=new ComboBox{ItemsSource=new[]{"Light","Dark","Auto"},SelectedIndex=0,Margin=new Thickness(0,12,0,12)};stack.Children.Add(theme);
            bool reduced=false,solid=false,startup=false;stack.Children.Add(Design.Check("Reducir movimiento",false,v=>reduced=v));stack.Children.Add(Design.Check("Reducir transparencia",false,v=>solid=v));stack.Children.Add(Design.Check("Iniciar MacFlow con mi sesión de Windows",false,v=>startup=v));stack.Children.Add(Caption("Micrófono, activación por voz e IA externa están desactivados inicialmente. Se habilitan por separado en Privacidad."));var status=Caption("");
            stack.Children.Add(Design.Button("Guardar y empezar",delegate{try{var preferences=desktop.Preferences;preferences.Theme=(string)theme.SelectedItem;preferences.Animations=!reduced;preferences.LiveGlass=!solid;preferences.AutoHideTaskbar=false;preferences.VoiceWakeEnabled=false;preferences.AllowExternalAssistant=false;desktop.Save();desktop.ApplyPreferences(preferences,null);if(startup)Startup.Configure(true,Path.Combine(StoragePaths.InstallDirectory,"MacFlow.exe"));File.WriteAllText(Path.Combine(desktop.Store.DirectoryPath,"onboarding.complete"),DateTime.UtcNow.ToString("o"));Close();}catch(Exception ex){status.Text=ex.Message;}},true));stack.Children.Add(status);
        }
    }
}

using System;
using Microsoft.Win32;
namespace MacFlow
{
    internal static class AppearancePolicy
    {
        internal static bool Resolve(string preference,bool systemDark)
        {return preference=="Dark"||preference=="Auto"&&systemDark;}
        internal static bool SystemDark
        {
            get{try{using(var key=Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize")){return key!=null&&Convert.ToInt32(key.GetValue("AppsUseLightTheme",1))==0;}}catch{return false;}}
        }
    }
}

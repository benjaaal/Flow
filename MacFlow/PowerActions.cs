using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MacFlow
{
    public enum PowerAction {Shutdown,Restart,Sleep,SignOut,Lock}
    public static class PowerActions
    {
        [StructLayout(LayoutKind.Sequential)] private struct Luid {public uint low;public int high;}
        [StructLayout(LayoutKind.Sequential)] private struct Privilege {public uint count;public Luid id;public uint attributes;}
        [DllImport("kernel32.dll")] private static extern IntPtr GetCurrentProcess();
        [DllImport("kernel32.dll")] private static extern bool CloseHandle(IntPtr handle);
        [DllImport("advapi32.dll",SetLastError=true)] private static extern bool OpenProcessToken(IntPtr process,uint access,out IntPtr token);
        [DllImport("advapi32.dll",CharSet=CharSet.Unicode,SetLastError=true)] private static extern bool LookupPrivilegeValue(string system,string name,out Luid id);
        [DllImport("advapi32.dll",SetLastError=true)] private static extern bool AdjustTokenPrivileges(IntPtr token,bool disable,ref Privilege privilege,uint size,out Privilege previous,out uint needed);
        [DllImport("user32.dll",SetLastError=true)] private static extern bool ExitWindowsEx(uint flags,uint reason);
        [DllImport("user32.dll",SetLastError=true)] private static extern bool LockWorkStation();
        [DllImport("powrprof.dll",SetLastError=true)][return:MarshalAs(UnmanagedType.U1)] private static extern bool SetSuspendState([MarshalAs(UnmanagedType.U1)]bool hibernate,[MarshalAs(UnmanagedType.U1)]bool force,[MarshalAs(UnmanagedType.U1)]bool disableWake);
        public static string Label(PowerAction action)
        {
            switch(action){case PowerAction.Shutdown:return "Apagar";case PowerAction.Restart:return "Reiniciar";case PowerAction.Sleep:return "Suspender";case PowerAction.SignOut:return "Cerrar sesión";case PowerAction.Lock:return "Bloquear";default:throw new ArgumentOutOfRangeException("action");}
        }
        public static uint ExitFlags(PowerAction action)
        {
            switch(action){case PowerAction.Shutdown:return 8;case PowerAction.Restart:return 2;case PowerAction.SignOut:return 0;default:throw new ArgumentOutOfRangeException("action");}
        }
        public static void Execute(PowerAction action)
        {
            if(action==PowerAction.Lock){if(!LockWorkStation())throw new Win32Exception(Marshal.GetLastWin32Error());return;}
            if(action==PowerAction.SignOut){if(!ExitWindowsEx(ExitFlags(action),0x80000000))throw new Win32Exception(Marshal.GetLastWin32Error());return;}
            IntPtr token;if(!OpenProcessToken(GetCurrentProcess(),0x28,out token))throw new Win32Exception(Marshal.GetLastWin32Error());
            Privilege previous=new Privilege();bool enabled=false;
            try
            {
                Luid id;if(!LookupPrivilegeValue(null,"SeShutdownPrivilege",out id))throw new Win32Exception(Marshal.GetLastWin32Error());
                var requested=new Privilege{count=1,id=id,attributes=2};uint needed;
                if(!AdjustTokenPrivileges(token,false,ref requested,(uint)Marshal.SizeOf(typeof(Privilege)),out previous,out needed))throw new Win32Exception(Marshal.GetLastWin32Error());
                int error=Marshal.GetLastWin32Error();enabled=true;if(error!=0)throw new Win32Exception(error);
                bool ok=action==PowerAction.Sleep?SetSuspendState(false,false,false):ExitWindowsEx(ExitFlags(action),0x80000000);
                if(!ok)throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            finally{if(enabled&&previous.count>0){Privilege ignored;uint size;AdjustTokenPrivileges(token,false,ref previous,0,out ignored,out size);}CloseHandle(token);}
        }
    }
    public sealed class PowerPrompt : Window
    {
        public bool Confirmed {get;private set;}
        public PowerPrompt(PowerAction action,bool testing,Action confirmed)
        {
            string label=PowerActions.Label(action);Title="MacFlow · "+label;Width=460;Height=250;WindowStyle=WindowStyle.None;AllowsTransparency=false;ResizeMode=ResizeMode.NoResize;ShowInTaskbar=false;Topmost=true;Background=Brushes.Transparent;
            var content=new StackPanel();var heading=Design.Text(label+" la computadora?",24,true);if(action==PowerAction.SignOut)heading.Text="Cerrar tu sesión?";if(action==PowerAction.Lock)heading.Text="Bloquear la computadora?";heading.Margin=new Thickness(0,0,0,14);content.Children.Add(heading);
            var description=Design.Text(action==PowerAction.Sleep?"La sesión queda abierta y podés continuar al despertar.":action==PowerAction.Lock?"Tus aplicaciones siguen abiertas. Windows mostrará su pantalla de bloqueo.":"Guardá tu trabajo antes de continuar. Las aplicaciones pueden pedirte guardar cambios.",13,false);description.TextWrapping=TextWrapping.Wrap;description.Margin=new Thickness(0,0,0,22);content.Children.Add(description);
            var buttons=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};var cancel=Design.Button("Cancelar",delegate{Design.Dismiss(this);},false);cancel.IsCancel=true;buttons.Children.Add(cancel);var accept=Design.Button(label,delegate{Confirmed=true;Design.Dismiss(this,confirmed);},true);buttons.Children.Add(accept);content.Children.Add(buttons);
            var glass=Design.Glass(content,24,false);glass.Padding=new Thickness(24);Content=glass;SourceInitialized+=delegate{Native.Glass(this,Design.Dark);};KeyDown+=delegate(object sender,System.Windows.Input.KeyEventArgs e){if(e.Key==System.Windows.Input.Key.Escape)Close();};
            if(!testing)Loaded+=delegate{cancel.Focus();};
        }
    }
}

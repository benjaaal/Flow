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
        public static string Symbol(PowerAction action)
        {switch(action){case PowerAction.Restart:return "restart";case PowerAction.Sleep:return "moon";case PowerAction.SignOut:return "signout";case PowerAction.Lock:return "lock";default:return "power";}}
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
            string label=PowerActions.Label(action);Title="MacOS · "+label;Width=388;Height=304;WindowStyle=WindowStyle.None;AllowsTransparency=false;ResizeMode=ResizeMode.NoResize;ShowInTaskbar=false;Topmost=true;Background=Brushes.Transparent;
            var content=new DockPanel();var buttons=new Grid{Margin=new Thickness(0,20,0,0)};buttons.ColumnDefinitions.Add(new ColumnDefinition());buttons.ColumnDefinitions.Add(new ColumnDefinition());var cancel=Design.Button("Cancelar",delegate{Design.Dismiss(this);},false);cancel.IsCancel=true;cancel.Margin=new Thickness(0,0,5,0);cancel.Height=36;buttons.Children.Add(cancel);var accept=Design.Button(label,delegate{Confirmed=true;Design.Dismiss(this,confirmed);},true);accept.Margin=new Thickness(5,0,0,0);accept.Height=36;Grid.SetColumn(accept,1);buttons.Children.Add(accept);DockPanel.SetDock(buttons,Dock.Bottom);content.Children.Add(buttons);
            var body=new StackPanel();body.Children.Add(new Border{Width=58,Height=58,CornerRadius=new CornerRadius(19),Background=Design.Brush("#18FFFFFF"),Child=Design.Symbol(PowerActions.Symbol(action),28,Design.Ink),HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(0,0,0,16)});
            var heading=Design.Text(label+" la computadora?",22,true);if(action==PowerAction.SignOut)heading.Text="Cerrar tu sesión?";if(action==PowerAction.Lock)heading.Text="Bloquear la computadora?";heading.HorizontalAlignment=HorizontalAlignment.Center;heading.Margin=new Thickness(0,0,0,10);body.Children.Add(heading);
            var description=Design.Text(action==PowerAction.Sleep?"Tu trabajo queda abierto para cuando vuelvas.":action==PowerAction.Lock?"Windows protege tu sesión. Tus apps siguen abiertas.":"Guardá tu trabajo. Las aplicaciones pueden pedirte guardar cambios antes de continuar.",12,false);description.TextWrapping=TextWrapping.Wrap;description.TextAlignment=TextAlignment.Center;description.SetResourceReference(TextBlock.ForegroundProperty,"GlassMuted");body.Children.Add(description);content.Children.Add(body);
            var glass=Design.Glass(content,28,false);glass.Padding=new Thickness(28,24,28,24);Content=glass;SourceInitialized+=delegate{Native.Glass(this,Design.Dark);};KeyDown+=delegate(object sender,System.Windows.Input.KeyEventArgs e){if(e.Key==System.Windows.Input.Key.Escape){e.Handled=true;Design.Dismiss(this);}};
            if(!testing)Loaded+=delegate{cancel.Focus();};
        }
    }
}

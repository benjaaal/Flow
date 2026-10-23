using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Threading;

namespace MacFlow
{
    // Only transient chord state is retained. No text, key history or telemetry.
    public sealed class WindowsTapState
    {
        private int held;
        private bool chord;
        public bool IsHeld {get{return held!=0;}}
        public bool Process(int key, bool down, bool modifierHeld)
        {
            bool win=key==0x5B||key==0x5C;
            if(win&&down){if(held==0){held=key;chord=modifierHeld;}else if(held!=key)chord=true;return false;}
            if(win&&!down&&held==key){bool tap=!chord;held=0;chord=false;return tap;}
            if(held!=0)chord=true;
            return false;
        }
        public void CancelTap(){if(held!=0)chord=true;}
    }

    internal sealed class PrivacyChordState
    {
        private bool entered,recovered;
        internal int Process(int key,bool down,bool win,bool control,bool alt)
        {
            bool isWin=key==0x5B||key==0x5C,isControl=key==0x11||key==0xA2||key==0xA3,isAlt=key==0x12||key==0xA4||key==0xA5;
            if(isWin)win=down;if(isControl)control=down;if(isAlt)alt=down;
            if(!win||!control){entered=recovered=false;return 0;}if(!down)return 0;
            if(alt&&!recovered){entered=recovered=true;return 2;}if(!alt&&!entered){entered=true;return 1;}return 0;
        }
    }
    public sealed class WindowsMenuKey : IDisposable
    {
        private delegate IntPtr HookProc(int code,IntPtr w,IntPtr l);
        [StructLayout(LayoutKind.Sequential)] private struct KeyboardInput {public ushort key,scan;public uint flags,time;public UIntPtr extra;}
        [StructLayout(LayoutKind.Explicit,Size=40)] private struct Input {[FieldOffset(0)]public uint type;[FieldOffset(8)]public KeyboardInput keyboard;}
        [DllImport("user32.dll",SetLastError=true)] private static extern IntPtr SetWindowsHookEx(int type,HookProc callback,IntPtr module,uint thread);
        [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(IntPtr hook);
        [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hook,int code,IntPtr w,IntPtr l);
        [DllImport("user32.dll",SetLastError=true)] private static extern uint SendInput(uint count,Input[] input,int size);
        [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int key);
        [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] private static extern IntPtr GetModuleHandle(string name);
        [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
        [DllImport("user32.dll")] private static extern bool PostThreadMessage(uint thread,uint message,UIntPtr w,IntPtr l);
        private readonly WindowsTapState state=new WindowsTapState();
        private readonly Dispatcher dispatcher;
        private readonly Action open,privacy,recover;private readonly Func<bool> captureTap;private readonly PrivacyChordState privacyKeys=new PrivacyChordState();
        private readonly Thread thread;
        private readonly HookProc keyboardCallback,mouseCallback;
        private readonly Input[] left=Release(0x5B),right=Release(0x5C);
        private IntPtr keyboardHook,mouseHook,module;
        private uint threadId;
        private volatile bool stopped;
        public volatile bool Available;
        public WindowsMenuKey(Dispatcher ui,Action action,Action showPrivacy=null,Action recoverPrivacy=null,Func<bool> captureWindowsTap=null)
        {
            dispatcher=ui;open=action;privacy=showPrivacy;recover=recoverPrivacy;captureTap=captureWindowsTap;keyboardCallback=Keyboard;mouseCallback=Mouse;
            thread=new Thread(Run){IsBackground=true,Name="MacFlow Windows menu"};thread.SetApartmentState(ApartmentState.STA);thread.Start();
        }
        private static Input[] Release(ushort key)
        {
            // An unassigned menu-mask key cancels Start; release Win in the same
            // serial batch. Existing Win+letter chords are passed through untouched.
            return new[]{new Input{type=1,keyboard=new KeyboardInput{key=0xE8}},new Input{type=1,keyboard=new KeyboardInput{key=0xE8,flags=2}},new Input{type=1,keyboard=new KeyboardInput{key=key,flags=3}}};
        }
        private void Run()
        {
            try
            {
                threadId=GetCurrentThreadId();using(var context=new System.Windows.Forms.ApplicationContext())
                {
                    // Ensure the thread message queue exists before exposing hooks.
                    using(var queue=new System.Windows.Forms.Control()){var handle=queue.Handle;
                        if(stopped)return;module=GetModuleHandle(null);keyboardHook=SetWindowsHookEx(13,keyboardCallback,module,0);
                        Available=keyboardHook!=IntPtr.Zero;
                        if(!Available){Log.Write("Menú Windows: no disponible. Usá Ctrl+Alt+M o el menú superior.");return;}
                        Log.Write("Menú Windows: listener listo, sin registro de teclas.");
                        if(!stopped)System.Windows.Forms.Application.Run(context);
                    }
                }
            }
            catch(Exception ex){Log.Write("Menú Windows: "+ex.Message);}
            finally{Available=false;if(keyboardHook!=IntPtr.Zero)UnhookWindowsHookEx(keyboardHook);if(mouseHook!=IntPtr.Zero)UnhookWindowsHookEx(mouseHook);keyboardHook=mouseHook=IntPtr.Zero;}
        }
        private IntPtr Keyboard(int code,IntPtr w,IntPtr l)
        {
            if(code<0||stopped)return CallNextHookEx(IntPtr.Zero,code,w,l);
            int key=Marshal.ReadInt32(l),flags=Marshal.ReadInt32(l,8);if((flags&0x10)!=0){if(key!=0xE8&&key!=0x5B&&key!=0x5C)state.CancelTap();return CallNextHookEx(IntPtr.Zero,code,w,l);}
            int message=w.ToInt32();bool down=message==0x100||message==0x104;
            // Observe clicks only while Win is physically held; ordinary pointer
            // movement has no global mouse hook or UI-thread work from MacFlow.
            if(down&&(key==0x5B||key==0x5C)&&!state.IsHeld)mouseHook=SetWindowsHookEx(14,mouseCallback,module,0);
            bool modifiers=false;if(down&&(key==0x5B||key==0x5C))modifiers=(GetAsyncKeyState(0x10)&0x8000)!=0||(GetAsyncKeyState(0x11)&0x8000)!=0||(GetAsyncKeyState(0x12)&0x8000)!=0;
            bool winHeld=(GetAsyncKeyState(0x5B)&0x8000)!=0||(GetAsyncKeyState(0x5C)&0x8000)!=0;bool ctrlHeld=(GetAsyncKeyState(0x11)&0x8000)!=0;bool altHeld=(GetAsyncKeyState(0x12)&0x8000)!=0;
            int chord=privacyKeys.Process(key,down,winHeld,ctrlHeld,altHeld);if(chord!=0&&!dispatcher.HasShutdownStarted)dispatcher.BeginInvoke(new Action(delegate{if(stopped)return;if(chord==2){if(recover!=null)recover();}else if(privacy!=null)privacy();}));            bool tap=state.Process(key,down,modifiers||((key==0x5B||key==0x5C)&&down&&mouseHook==IntPtr.Zero));
            if(!state.IsHeld&&mouseHook!=IntPtr.Zero){UnhookWindowsHookEx(mouseHook);mouseHook=IntPtr.Zero;}
            if(tap&&(captureTap==null||captureTap())&&!dispatcher.HasShutdownStarted)
            {
                // If UIPI blocks the batch, keep the physical release and native
                // Start. Never leave the Windows key logically held down.
                if(SendInput(3,key==0x5B?left:right,40)==3)
                {
                    dispatcher.BeginInvoke(DispatcherPriority.Normal,new Action(delegate{if(!stopped)open();}));return new IntPtr(1);
                }
            }
            return CallNextHookEx(IntPtr.Zero,code,w,l);
        }
        private IntPtr Mouse(int code,IntPtr w,IntPtr l)
        {
            int message=w.ToInt32();if(code>=0&&(message==0x201||message==0x204||message==0x207||message==0x20B))state.CancelTap();
            return CallNextHookEx(IntPtr.Zero,code,w,l);
        }
        public void Dispose(){stopped=true;if(threadId!=0)PostThreadMessage(threadId,0x12,UIntPtr.Zero,IntPtr.Zero);}
    }
}

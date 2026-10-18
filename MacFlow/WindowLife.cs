using System;
using System.Runtime.CompilerServices;
using System.Windows;
namespace MacFlow
{
    // WPF windows are single-use objects. Keep their terminal state without
    // retaining the window in a static collection after its owner releases it.
    internal static class WindowLife
    {
        private sealed class State { internal bool Closed; }
        private static readonly ConditionalWeakTable<Window,State> states=new ConditionalWeakTable<Window,State>();
        internal static void Track(Window window)
        {
            if(window==null)return;
            states.GetValue(window,delegate(Window view){var state=new State();view.Closed+=delegate{state.Closed=true;};return state;});
        }
        internal static bool Alive(Window window)
        {if(window==null||window.Dispatcher.HasShutdownStarted)return false;Track(window);State state;return states.TryGetValue(window,out state)&&!state.Closed;}
        internal static void Close(Window window){if(Alive(window))window.Close();}
    }
}

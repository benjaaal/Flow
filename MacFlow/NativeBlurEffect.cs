using System;
using System.Runtime.InteropServices;
using Windows.Graphics.Effects;
namespace MacFlow
{
    // Direct2D's documented effect-description ABI. Only the background brush
    // passes through this graph; WPF text and icons are a separate foreground.
    [ComImport,Guid("2FC57384-A068-44D7-A331-30982FCF7177"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IEffectInterop
    {
        [PreserveSig]int GetEffectId(out Guid id);
        [PreserveSig]int GetNamedPropertyMapping([MarshalAs(UnmanagedType.LPWStr)]string name,out uint index,out uint mapping);
        [PreserveSig]int GetPropertyCount(out uint count);
        [PreserveSig]int GetProperty(uint index,out IntPtr value);
        [PreserveSig]int GetSource(uint index,out IntPtr source);
        [PreserveSig]int GetSourceCount(out uint count);
    }
    [ComVisible(true)]
    internal sealed class NativeBlurEffect : IGraphicsEffect,IGraphicsEffectSource,IEffectInterop
    {
        public string Name{get;set;}
        internal IGraphicsEffectSource Source;
        internal float Amount=10;
        public int GetEffectId(out Guid id){id=new Guid("1FEB6D69-2FE6-4AC9-8C58-1D7F93E7A6A5");return 0;}
        public int GetNamedPropertyMapping(string name,out uint index,out uint mapping){index=0;mapping=1;return name=="BlurAmount"?0:unchecked((int)0x80070057);}
        public int GetPropertyCount(out uint count){count=3;return 0;}
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]private delegate int FloatValue(IntPtr self,float value,out IntPtr result);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]private delegate int UIntValue(IntPtr self,uint value,out IntPtr result);
        public int GetProperty(uint index,out IntPtr value)
        {
            value=IntPtr.Zero;if(index>2)return unchecked((int)0x80070057);IntPtr unknown=IntPtr.Zero,statics=IntPtr.Zero;
            try
            {
                // The .NET projection unboxes PropertyValue to a managed float.
                // Ask the documented native factory for its actual WinRT box.
                var factory=System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.GetActivationFactory(typeof(Windows.Foundation.PropertyValue));unknown=Marshal.GetIUnknownForObject(factory);var iid=new Guid("629BDBC8-D932-4FF4-96B9-8D96C5C1E858");Marshal.ThrowExceptionForHR(Marshal.QueryInterface(unknown,ref iid,out statics));var table=Marshal.ReadIntPtr(statics);
                if(index==0)return ((FloatValue)Marshal.GetDelegateForFunctionPointer(Marshal.ReadIntPtr(table,14*IntPtr.Size),typeof(FloatValue)))(statics,Amount,out value);
                return ((UIntValue)Marshal.GetDelegateForFunctionPointer(Marshal.ReadIntPtr(table,11*IntPtr.Size),typeof(UIntValue)))(statics,1,out value);
            }
            catch(Exception ex){Log.Write("Propiedad de desenfoque: "+ex.Message);return Marshal.GetHRForException(ex);}
            finally{if(statics!=IntPtr.Zero)Marshal.Release(statics);if(unknown!=IntPtr.Zero)Marshal.Release(unknown);}
        }
        public int GetSource(uint index,out IntPtr source){source=IntPtr.Zero;if(index!=0||Source==null)return unchecked((int)0x80070057);source=Marshal.GetComInterfaceForObject(Source,typeof(IGraphicsEffectSource));return 0;}
        public int GetSourceCount(out uint count){count=1;return 0;}
    }
}

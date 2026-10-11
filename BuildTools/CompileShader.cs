using System;
using System.IO;
using System.Runtime.InteropServices;
internal static class CompileShader
{
    [ComImport,Guid("8BA5FB08-5195-40E2-AC58-0D989C3A0102"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] private interface Blob
    { [PreserveSig] IntPtr GetBufferPointer(); [PreserveSig] UIntPtr GetBufferSize(); }
    [DllImport("d3dcompiler_47.dll")] private static extern int D3DCompile(byte[] source,UIntPtr length,[MarshalAs(UnmanagedType.LPStr)]string name,IntPtr macros,IntPtr include,[MarshalAs(UnmanagedType.LPStr)]string entry,[MarshalAs(UnmanagedType.LPStr)]string target,uint flags,uint flags2,out Blob code,out Blob errors);
    public static int Main(string[] args)
    {
        Blob code=null,errors=null;
        try {byte[] source=File.ReadAllBytes(args[0]);int hr=D3DCompile(source,(UIntPtr)source.Length,args[0],IntPtr.Zero,IntPtr.Zero,"main","ps_3_0",32768,0,out code,out errors);if(hr<0){if(errors!=null)Console.Error.WriteLine(Marshal.PtrToStringAnsi(errors.GetBufferPointer()));Marshal.ThrowExceptionForHR(hr);}var output=new byte[(int)code.GetBufferSize().ToUInt64()];Marshal.Copy(code.GetBufferPointer(),output,0,output.Length);File.WriteAllBytes(args[1],output);return 0;}
        catch(Exception ex){Console.Error.WriteLine(ex.Message);return 1;}
        finally {if(code!=null)Marshal.ReleaseComObject(code);if(errors!=null)Marshal.ReleaseComObject(errors);}
    }
}

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Security.Credentials.UI;
namespace MacFlow
{
    internal static class WindowsHello
    {
        [ComImport,Guid("39E050C3-4E74-441A-8DC0-B81104DF949C"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IVerifierInterop
        {
            void RequestVerificationForWindowAsync(IntPtr window,[MarshalAs(UnmanagedType.HString)]string message,ref Guid iid,[MarshalAs(UnmanagedType.IInspectable)]out object operation);
        }
        private static async Task<T> Complete<T>(IAsyncOperation<T> operation)
        {
            try{while(operation.Status==AsyncStatus.Started)await Task.Delay(60);return operation.GetResults();}
            finally{operation.Close();}
        }
        internal static Task<UserConsentVerifierAvailability> Availability()
        {return Complete(UserConsentVerifier.CheckAvailabilityAsync());}
        internal static Task<UserConsentVerificationResult> Verify(IntPtr owner)
        {
            if(owner==IntPtr.Zero)throw new InvalidOperationException("Falta la ventana de MacFlow.");
            var factory=(IVerifierInterop)WindowsRuntimeMarshal.GetActivationFactory(typeof(UserConsentVerifier));
            var iid=typeof(IAsyncOperation<UserConsentVerificationResult>).GUID;object operation;
            factory.RequestVerificationForWindowAsync(owner,"Volver al escritorio de MacFlow",ref iid,out operation);
            return Complete((IAsyncOperation<UserConsentVerificationResult>)operation);
        }
    }
}

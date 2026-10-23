using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
namespace MacFlow
{
    internal static class LidRegression
    {
        internal static void Run(List<string> results)
        {
            Check(LidTransition.AngleProgress(110)==0&&LidTransition.AngleProgress(40)>.4&&LidTransition.AngleProgress(0)==.95&&LidTransition.AngleProgress(Double.NaN)==0,"Tapa: ángulo normal no despliega overlay; pliegue acotado y entradas inválidas rechazadas",results);
            IntPtr data=Marshal.AllocHGlobal(24);
            try
            {
                Marshal.StructureToPtr(new Guid("BA3E0F4D-B817-4094-A2D1-D56379E6A0F3"),data,false);Marshal.WriteInt32(data,16,4);Marshal.WriteInt32(data,20,0);
                Check(LidTransition.ParseSetting(data)==false,"Tapa: interpreta cierre documentado de Windows",results);Marshal.WriteInt32(data,20,1);Check(LidTransition.ParseSetting(data)==true,"Tapa: interpreta apertura documentada de Windows",results);
                Marshal.WriteInt32(data,20,99);Check(!LidTransition.ParseSetting(data).HasValue,"Tapa: ignora estado desconocido",results);Marshal.WriteInt32(data,16,2);Check(!LidTransition.ParseSetting(data).HasValue&&!LidTransition.ParseSetting(IntPtr.Zero).HasValue,"Tapa: rechaza notificaciones incompletas",results);
                Marshal.StructureToPtr(Guid.Empty,data,false);Marshal.WriteInt32(data,16,4);Check(!LidTransition.ParseSetting(data).HasValue,"Tapa: ignora otros avisos de energía",results);
            }
            finally{Marshal.FreeHGlobal(data);}
        }
        private static void Check(bool ok,string label,List<string> results){if(!ok)throw new Exception(label);results.Add("PASS "+label);}
    }
}

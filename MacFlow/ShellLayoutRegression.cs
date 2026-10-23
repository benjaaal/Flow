using System;
using System.Collections.Generic;
namespace MacFlow
{
    internal static class ShellLayoutRegression
    {
        private static void Check(bool value,string label,List<string> results){if(!value)throw new Exception(label);results.Add("PASS Shell: "+label);}
        internal static void Run(List<string> results)
        {
            var queue=new Queue<Action>();var reservation=new AppbarReservation();int layouts=0,commits=0,queries=0;ShellLayout coordinator=null;var rect=new Native.RECT{Left=-1920,Top=-200,Right=0,Bottom=-164};
            coordinator=new ShellLayout(a=>queue.Enqueue(a),()=>layouts++,delegate{queries++;reservation.Update(rect,delegate{commits++;coordinator.Request(false);});});
            for(int i=0;i<100;i++)coordinator.Request(false);
            Check(queue.Count==1,"cien avisos de posición se agrupan en una sola tarea",results);
            int drained=0;while(queue.Count>0&&drained++<10)queue.Dequeue()();
            Check(queue.Count==0&&commits==1&&queries==2&&layouts==0,"una reserva que genera su propio aviso se estabiliza sin recolocar el Dock ni volver a reservar",results);
            coordinator.Request(false);coordinator.Request(true);coordinator.Request(false);queue.Dequeue()();Check(layouts==1&&queue.Count==0,"un cambio real de monitor prevalece sobre avisos repetidos",results);
            rect.Bottom++;Check(reservation.Update(rect,()=>commits++)&&commits==2,"una reserva realmente distinta se aplica incluso con coordenadas negativas",results);
            bool failed=false;rect.Top++;try{reservation.Update(rect,delegate{throw new InvalidOperationException("fixture");});}catch(InvalidOperationException){failed=true;}Check(failed&&reservation.Update(rect,()=>commits++)&&commits==3,"una reserva fallida puede reintentarse y no queda marcada como aplicada",results);
            reservation.Invalidate();Check(reservation.Update(rect,()=>commits++)&&commits==4,"reiniciar Explorer permite volver a registrar la misma reserva",results);
            coordinator.Request(false);coordinator.Dispose();queue.Dequeue()();coordinator.Request(true);Check(queries==2&&layouts==1&&queue.Count==0,"cerrar cancela las tareas pendientes sin tocar ventanas cerradas",results);
        }
    }
}

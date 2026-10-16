using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
namespace MacFlow
{
    // Original short chimes synthesized in memory; no Apple sound recordings.
    internal static class SoundCue
    {
        internal static bool Enabled;private static readonly Dictionary<string,byte[]> cache=new Dictionary<string,byte[]>();private static SoundPlayer player;private static MemoryStream stream;private static DateTime last;
        internal static byte[] Wave(string kind)
        {
            byte[] data;if(cache.TryGetValue(kind,out data))return data;const int rate=22050;double seconds=kind=="timer"?.65:kind=="open"?.14:.24;int count=(int)(rate*seconds);using(var output=new MemoryStream())using(var writer=new BinaryWriter(output)){
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+count*2);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(count*2);
                double frequency=kind=="open"?660:kind=="timer"?523.25:783.99;for(int i=0;i<count;i++){double t=(double)i/rate,attack=Math.Min(1,t/.012),release=Math.Pow(1-t/seconds,2.5);double sample=Math.Sin(2*Math.PI*frequency*t)+.35*Math.Sin(2*Math.PI*frequency*1.5*t);writer.Write((short)(sample*attack*release*2800));}writer.Flush();data=output.ToArray();}cache[kind]=data;return data;
        }
        internal static void Play(string kind)
        {if(!Enabled||DateTime.UtcNow-last<TimeSpan.FromMilliseconds(280))return;last=DateTime.UtcNow;try{if(player!=null){player.Stop();player.Dispose();stream.Dispose();}stream=new MemoryStream(Wave(kind),false);player=new SoundPlayer(stream);player.Play();}catch(Exception ex){Log.Write("Sonido: "+ex.Message);}}
        internal static void Stop(){if(player!=null){player.Stop();player.Dispose();player=null;}if(stream!=null){stream.Dispose();stream=null;}}
    }
}

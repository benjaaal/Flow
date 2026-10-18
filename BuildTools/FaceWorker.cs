using System;
using System.IO;using System.Runtime.InteropServices;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using OpenCvSharp;
using OpenCvSharp.Dnn;
internal static class FaceWorker
{
    private static readonly Point2f[] target={new Point2f(38.2946f,51.6963f),new Point2f(73.5318f,51.5014f),new Point2f(56.0252f,71.7366f),new Point2f(41.5493f,92.3655f),new Point2f(70.7299f,92.2041f)};
    private static float Read(Mat value,int col){var cell=new float[1];Marshal.Copy(IntPtr.Add(value.Data,col*4),cell,0,1);return cell[0];}
    private static float[] Feature(Mat frame,Mat faces,Net net)
    {
        var points=new Point2f[5];for(int i=0;i<5;i++)points[i]=new Point2f(Read(faces,4+i*2),Read(faces,5+i*2));
        using(var from=InputArray.Create(points))using(var to=InputArray.Create(target))using(var transform=Cv2.EstimateAffinePartial2D(from,to))using(var aligned=new Mat())
        {Cv2.WarpAffine(frame,aligned,transform,new Size(112,112));using(var blob=CvDnn.BlobFromImage(aligned,1,new Size(112,112),Scalar.All(0),true,false)){net.SetInput(blob);using(var output=net.Forward()){var feature=new float[(int)output.Total()];Marshal.Copy(output.Data,feature,0,feature.Length);if(feature.Length!=128)throw new InvalidDataException("Vector facial inválido");double norm=Math.Sqrt(feature.Sum(x=>(double)x*x));if(norm<.0001)throw new InvalidDataException("Rostro sin información");for(int i=0;i<feature.Length;i++)feature[i]/=(float)norm;return feature;}}}
    }
    private static double Live(Mat frame,Mat faces,Net model)
    {
        double x=Read(faces,0),y=Read(faces,1),w=Read(faces,2),h=Read(faces,3),scale=Math.Min(2.7,Math.Min((frame.Width-1)/w,(frame.Height-1)/h));int width=(int)(w*scale),height=(int)(h*scale);int left=(int)Math.Max(0,Math.Min(frame.Width-width,x+w/2-width/2.0)),top=(int)Math.Max(0,Math.Min(frame.Height-height,y+h/2-height/2.0));
        using(var crop=new Mat(frame,new Rect(left,top,width,height)))using(var blob=CvDnn.BlobFromImage(crop,1,new Size(80,80),Scalar.All(0),false,false))
        {model.SetInput(blob);using(var output=model.Forward()){if(output.Total()!=3)throw new InvalidDataException("Salida de presencia inválida");var logits=new float[3];Marshal.Copy(output.Data,logits,0,3);double max=logits.Max(),sum=logits.Sum(z=>Math.Exp(z-max));return Math.Exp(logits[1]-max)/sum;}}
    }
    internal static double Similarity(float[] a,float[] b){if(a.Length!=128||b.Length!=128)return -1;double sum=0;for(int i=0;i<128;i++)sum+=(double)a[i]*b[i];return sum;}
    private static string Pack(IEnumerable<float[]> values){var all=values.SelectMany(x=>x).ToArray();var bytes=new byte[all.Length*4];Buffer.BlockCopy(all,0,bytes,0,bytes.Length);return Convert.ToBase64String(bytes);}
    private static float[][] Unpack(string value){var bytes=Convert.FromBase64String(value);if(bytes.Length!=1536)throw new InvalidDataException();var all=new float[384];Buffer.BlockCopy(bytes,0,all,0,bytes.Length);return new[]{all.Take(128).ToArray(),all.Skip(128).Take(128).ToArray(),all.Skip(256).Take(128).ToArray()};}
    private static void Say(string value){Console.WriteLine(value);Console.Out.Flush();}
    private static int Main(string[] args)
    {
        try
        {
            Cv2.SetNumThreads(2);string folder=AppDomain.CurrentDomain.BaseDirectory;
            using(var detector=FaceDetectorYN.Create(Path.Combine(folder,"yunet.onnx"),"",new Size(640,480),.9f,.3f,5000))
            using(var net=CvDnn.ReadNetFromOnnx(Path.Combine(folder,"sface.onnx")))
            using(var spoof=CvDnn.ReadNetFromOnnx(Path.Combine(folder,"antispoof.onnx")))
            {
                net.SetPreferableBackend(Backend.OPENCV);net.SetPreferableTarget(Target.CPU);
                if(args.Length==2&&args[0]=="--self-test")
                {using(var test=Cv2.ImRead(args[1]))using(var faces=new Mat()){Cv2.Resize(test,test,new Size(640,480));detector.Detect(test,faces);if(faces.Rows!=1)throw new Exception("No se detectó el rostro de prueba");var a=Feature(test,faces,net);using(var mirror=new Mat()){Cv2.Flip(test,mirror,FlipMode.Y);detector.Detect(mirror,faces);var b=Feature(mirror,faces,net);double score=Similarity(a,b);if(score<.5||Similarity(a,new float[128])>=.5)throw new Exception("Comparación incorrecta");Say("PASS detección, alineación, 128 valores y comparación con variante; score="+score.ToString("F3"));}return 0;}}
                if(args.Length==2&&args[0]=="--spoof-test"){using(var test=Cv2.ImRead(args[1]))using(var faces=new Mat()){Cv2.Resize(test,test,new Size(640,480));detector.Detect(test,faces);if(faces.Rows!=1)throw new Exception("No se detectó el rostro de prueba");double live=Live(test,faces,spoof);Say("LIVE="+live.ToString("F5",System.Globalization.CultureInfo.InvariantCulture));return 0;}}
                bool enroll=args.Length==1&&args[0]=="--enroll";float[][] reference=enroll?null:Unpack(Console.ReadLine());var samples=new List<float[]>();int matched=0,liveFrames=0;double? startNose=null;bool moved=false;double turn=new Random().Next(2)==0?-1:1;
                using(var camera=new VideoCapture(0,VideoCaptureAPIs.DSHOW))using(var frame=new Mat())using(var faces=new Mat())
                {
                    if(!camera.IsOpened())throw new IOException("No se pudo abrir la cámara. Revisá sus permisos en Windows.");camera.Set(VideoCaptureProperties.FrameWidth,640);camera.Set(VideoCaptureProperties.FrameHeight,480);
                    var clock=Stopwatch.StartNew();int count=0;
                    while(clock.Elapsed.TotalSeconds<30)
                    {
                        if(!camera.Read(frame)||frame.Empty()){Thread.Sleep(100);continue;}Cv2.Resize(frame,frame,new Size(640,480));detector.Detect(frame,faces);
                        if(++count%2==0)Say("FRAME:"+Convert.ToBase64String(frame.ToBytes(".jpg",new[]{(int)ImwriteFlags.JpegQuality,65})));
                        if(faces.Rows!=1||Read(faces,2)<100){Say("STATUS:Ubicá un solo rostro dentro del círculo, con buena luz.");matched=liveFrames=0;moved=false;startNose=null;Thread.Sleep(160);continue;}
                        double liveness=Live(frame,faces,spoof);if(liveness<.95){liveFrames=matched=0;moved=false;startNose=null;Say("STATUS:No se verificó presencia real. Evitá fotos, pantallas y reflejos; podés usar tu PIN.");Thread.Sleep(160);continue;}if(++liveFrames<3)continue;
                        float[] feature=Feature(frame,faces,net);double nose=(Read(faces,8)-(Read(faces,4)+Read(faces,6))/2)/Math.Max(1,Math.Abs(Read(faces,4)-Read(faces,6)));
                        if(!startNose.HasValue)startNose=nose;if((nose-startNose.Value)*turn>.12)moved=true;
                        if(enroll){if(samples.Count==0||moved){if(samples.Count>0&&Similarity(samples[0],feature)<.5){Say("STATUS:El rostro cambió. Volvé a empezar.");samples.Clear();moved=false;startNose=nose;continue;}samples.Add(feature);moved=false;startNose=nose;turn=-turn;Say("STATUS:"+samples.Count+" / 3 · Girá suavemente la cabeza "+(turn<0?"hacia tu derecha":"hacia tu izquierda")+".");if(samples.Count==3){Say("ENROLLED:"+Pack(samples));return 0;}}else Say("STATUS:Mové suavemente la cabeza para completar el registro.");}
                        else{double score=reference.Max(x=>Similarity(x,feature));matched=score>=.5?matched+1:0;if(matched==0){moved=false;startNose=null;}Say("STATUS:"+(score>=.5?"Rostro reconocido · girá suavemente "+(turn<0?"hacia tu derecha":"hacia tu izquierda"):"No te reconozco. Podés usar tu PIN."));if(matched>=3&&moved){Say("VERIFIED");return 0;}}
                        Thread.Sleep(160);
                    }
                    Say("STATUS:Terminó el intento. Usá tu PIN o intentá de nuevo.");return 2;
                }
            }
        }
        catch(Exception ex){Say("ERROR:"+ex.Message);return 1;}
    }
}

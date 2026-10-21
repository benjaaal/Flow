using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
namespace MacFlow
{
    // Only integration fixtures use this provider; production uses WindowsMediaSource.
    internal sealed class MediaFixture : IMediaSource,ISeekMediaSource
    {
        public event Action Changed;
        internal MediaState State=new MediaState {Title="Canción de prueba",Artist="Artista de prueba",CanToggle=true,CanNext=true,CanPrevious=true};
        internal int Calls;internal bool Disposed;internal bool FailNextRead;
        internal TaskCompletionSource<MediaState> Delayed;
        public Task<MediaState> Read(){if(FailNextRead){FailNextRead=false;throw new Exception("Fallo multimedia transitorio de prueba");}if(Delayed!=null){var task=Delayed.Task;Delayed=null;return task;}return Task.FromResult(State);}
        public Task<bool> Command(MediaCommand command){Calls++;if(command==MediaCommand.Toggle)State.Playing=!State.Playing;return Task.FromResult(true);}
        public Task<bool> Seek(TimeSpan position){State.Position=position;return Task.FromResult(true);}
        internal void Publish(){var callback=Changed;if(callback!=null)callback();}
        public void Dispose(){Disposed=true;Changed=null;}
    }
    internal static class MediaRegression
    {
        internal static int Probe(string directory)
        {
            System.IO.Directory.CreateDirectory(directory);
            using(var source=new WindowsMediaSource())
            {
                try{var read=source.Read();Pump(()=>read.IsCompleted);var state=read.GetAwaiter().GetResult();System.IO.File.WriteAllText(System.IO.Path.Combine(directory,"media-test.txt"),"PASS API multimedia real consultada sin cambiar reproducción. Sesión con controles: "+(state.CanToggle||state.CanNext||state.CanPrevious)+". Los nombres de canciones no se guardan en este informe.");return 0;}
                catch(Exception ex){System.IO.File.WriteAllText(System.IO.Path.Combine(directory,"media-test.txt"),"UNAVAILABLE "+ex.GetType().Name+": "+ex.Message+". El panel muestra información no disponible y controles desactivados.");return 1;}
            }
        }
        private static void Pump(Func<bool> ready)
        {
            var until=DateTime.UtcNow.AddSeconds(3);
            while(!ready()&&DateTime.UtcNow<until){var frame=new DispatcherFrame();var timer=new DispatcherTimer {Interval=TimeSpan.FromMilliseconds(15)};timer.Tick+=delegate{timer.Stop();frame.Continue=false;};timer.Start();Dispatcher.PushFrame(frame);}
            if(!ready())throw new Exception("Multimedia: respuesta pendiente");
        }
        internal static void Run(List<string> results)
        {
            var provider=new MediaFixture();var card=new MediaCard(provider);card.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));Pump(()=>card.TrackTitle==provider.State.Title);
            if(!card.Toggle.IsEnabled||!card.Next.IsEnabled)throw new Exception("Multimedia: controles disponibles");card.Toggle.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Pump(()=>provider.Calls==1&&card.Toggle.IsEnabled);
            if(!provider.State.Playing||!Equals(card.Toggle.Content,"Ⅱ"))throw new Exception("Multimedia: estado de reproducción");results.Add("PASS Multimedia: título/artista consultados y reproducir/pausar confirmado por proveedor, sin teclas simuladas.");
            provider.State=new MediaState();provider.Publish();Pump(()=>card.TrackTitle=="Sin reproducción");if(card.Toggle.IsEnabled||card.Next.IsEnabled||card.Previous.IsEnabled)throw new Exception("Multimedia: controles sin sesión");results.Add("PASS Multimedia: al desaparecer el reproductor se vacían los datos y se desactivan sus controles.");
            var delayed=new TaskCompletionSource<MediaState>();provider.Delayed=delayed;provider.Publish();Pump(()=>provider.Delayed==null);provider.State=new MediaState {Title="Nueva sesión"};provider.Publish();delayed.SetResult(new MediaState {Title="Sesión anterior"});Pump(()=>card.TrackTitle=="Nueva sesión");results.Add("PASS Multimedia: un cambio de sesión durante una consulta pendiente termina con los datos actuales.");
            card.Dispose();provider.State=new MediaState {Title="Después de cerrar"};provider.Publish();if(!provider.Disposed||card.TrackTitle!="Nueva sesión")throw new Exception("Multimedia: cierre");results.Add("PASS Multimedia: cerrar el panel libera suscripciones y no deja un sondeo activo.");
        }
    }
}

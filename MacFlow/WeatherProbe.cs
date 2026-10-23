using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
namespace MacFlow
{
    internal static class WeatherProbe
    {
        internal static int Run(string directory)
        {
            Directory.CreateDirectory(directory);var results=new List<string>();try
            {
                Task.Run(async delegate
                {
                    var cities=await WidgetWeather.Search("Buenos Aires",CancellationToken.None);if(cities.Length==0)throw new Exception("Geocodificación sin resultados");var city=cities[0];results.Add("PASS búsqueda HTTPS de ciudad: "+city);
                    var weather=await WidgetWeather.Forecast(city.latitude,city.longitude,CancellationToken.None);if(weather.daily.time==null||weather.daily.time.Length!=3||weather.daily.temperature_2m_max.Length!=3)throw new Exception("Pronóstico incompleto");results.Add("PASS temperatura actual y pronóstico real de tres días: "+weather.current.temperature_2m+" °C");
                    using(var cancelled=new CancellationTokenSource()){cancelled.Cancel();bool handled=false;try{await WidgetWeather.Forecast(city.latitude,city.longitude,cancelled.Token);}catch(Exception){handled=true;}if(!handled)throw new Exception("No cancela");results.Add("PASS consulta cancelada termina sin escribir preferencias");}
                }).GetAwaiter().GetResult();File.WriteAllLines(Path.Combine(directory,"weather-probe.txt"),results);return 0;
            }
            catch(Exception ex){results.Add("FAIL "+ex);File.WriteAllLines(Path.Combine(directory,"weather-probe.txt"),results);return 1;}
        }
    }
}

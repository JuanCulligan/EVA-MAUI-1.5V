using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Logic.Services;
using Logic.Processors;

namespace Logic.Providers
{
    public class WeatherProvider
    {
        private readonly OpenWeatherService _service = new OpenWeatherService();
        private readonly OpenWeatherProcessor _processor = new OpenWeatherProcessor();


        public async Task<double> GetTemperature(double startLat, double startLon)
        {
            // consigue la respuesta raw del Json bbasada en la ubbicacion del usuario
            string json = await _service.GetWeatherJson(startLat, startLon);

            return _processor.ParseTemperature(json);// Pasa el json a un double 
        }
    }
}

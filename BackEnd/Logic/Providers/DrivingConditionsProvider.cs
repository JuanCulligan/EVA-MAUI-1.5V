using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Entities.Models;

namespace Logic.Providers
{
    public class DrivingConditionsProvider
    {
        private readonly WeatherProvider _weather = new WeatherProvider();

        public async Task<DrivingConditions> BuildConditions(double lat, double lon, double speedKmh)
        {
            double temp = await _weather.GetTemperature(lat, lon);

            return new DrivingConditions
            {
                SpeedKmh = speedKmh,
                TemperatureC = temp
            };
        }
    }
}

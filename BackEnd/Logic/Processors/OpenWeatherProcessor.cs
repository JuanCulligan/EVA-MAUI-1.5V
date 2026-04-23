using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Logic.Processors
{
    public class OpenWeatherProcessor
    {
        public double ParseTemperature(string jsonResponse)
        {
            var json = JObject.Parse(jsonResponse);

            return json["main"]?["temp"]?.Value<double>() ?? 0;
        }
    }
}

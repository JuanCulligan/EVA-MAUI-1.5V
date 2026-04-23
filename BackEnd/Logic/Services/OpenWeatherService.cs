using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;

namespace Logic.Services
{
    public class OpenWeatherService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private readonly string _apiKey;

        public OpenWeatherService()
        {
            _apiKey = Environment.GetEnvironmentVariable("OPENWEATHER_API_KEY");
        }

        // manda request y devulve json raw
        public async Task<string> GetWeatherJson(double startLat, double startLon)
        {
            string url = $"https://api.openweathermap.org/data/2.5/weather?lat={startLat}&lon={startLon}&appid={_apiKey}&units=metric";

            return await _httpClient.GetStringAsync(url);
        }
    }
}

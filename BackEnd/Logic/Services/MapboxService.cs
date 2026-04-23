using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace Logic.Services
{
    public class MapboxService
    {
        // crea obbjeto para manadte request HTTP a el API
        private static readonly HttpClient _httpClient = new HttpClient();

        // Crea variable para guardar api key, que solo se lee una vez
        private readonly string _token;

        public MapboxService()
        {
            // se le da valor al token de mis variables de entorno 
            _token = Environment.GetEnvironmentVariable("MapBox_API_Key");
        }

        // le pasamos el punto de inico y el final mas el token y nos devulve un JSON
        public async Task<string> GetRouteJson(double startLon, double startLat, double endLon, double endLat)
        {
            //Crea la URL con los valores dados
            string url = $"https://api.mapbox.com/directions/v5/mapbox/driving/{startLon},{startLat};{endLon},{endLat}?geometries=geojson&overview=full&access_token={_token}";

            // Envia el Request y espera por la respuesta (JSON)
            return await _httpClient.GetStringAsync(url);
        }

        public async Task<string> GetElevationJson(double lon, double lat)
        {
            string url =$"https://api.mapbox.com/v4/mapbox.mapbox-terrain-v2/tilequery/{lon},{lat}.json?layers=contour&access_token={_token}";

            return await _httpClient.GetStringAsync(url);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Services
{
    public class OpenChargeService
    {

        private static readonly HttpClient _httpClient = new HttpClient();
        string _apikey = Environment.GetEnvironmentVariable("opencharge_api_key");
        public async Task<string> GetChargersJson(double lat, double lon, int radiusKm = 10, int maxResults = 30)
        {
            string url = string.Format(System.Globalization.CultureInfo.InvariantCulture,"https://api.openchargemap.io/v3/poi/?latitude={0}&longitude={1}&distance={2}&distanceunit=km&maxresults={3}&key={4}",lat, lon, radiusKm, maxResults, _apikey);


            var request = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"OpenCharge API error: {response.StatusCode}");
            }

            string result = await response.Content.ReadAsStringAsync();

            return result;
        }
    }
}

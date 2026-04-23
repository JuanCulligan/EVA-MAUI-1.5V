using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Logic.Services
{
    public class AresepService
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        public async Task<string> GetTarifasJson()
        {
            string url = "https://datos.aresep.go.cr/ws.datosabiertos/Services/IE/TarifasElectricidad.svc/ObtenerTarifasPreciosMedios/0";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"ARESEP API error: {response.StatusCode}");
            }

            return await response.Content.ReadAsStringAsync();
        }
    }
}

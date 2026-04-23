using Core.Entities.Models;
using Logic.Processors;
using Logic.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Logic.Providers
{
    public class ChargerProvider
    {
        private readonly OpenChargeService _service = new OpenChargeService();
        private readonly ChargerProcessor _processor = new ChargerProcessor();

        public async Task<List<ChargerStation>> GetChargers(double lat, double lon, int radiusKm = 10, int maxResults = 30)
        {
            var json = await _service.GetChargersJson(lat, lon, radiusKm, maxResults);

            if (string.IsNullOrWhiteSpace(json))
                return new List<ChargerStation>();

            return _processor.ParseChargers(json);
        }
    }
}
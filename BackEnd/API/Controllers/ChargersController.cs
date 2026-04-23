using Core.Entities.Models;
using Core.Entities.Request;
using Core.Entities.Response;
using Logic.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace API.Controllers
{
    public class ChargerController : ApiController
    {
        [HttpPost]
        [Route("api/chargers/nearby")]
        public async Task<List<ChargerStation>> Nearby(ReqNearbyChargers req)
        {
            var provider = new ChargerProvider();

            int radiusKm = req.RadiusKm > 0 ? req.RadiusKm : 8;

            return await provider.GetChargers(req.Lat, req.Lon, radiusKm, 50);
        }
    }
}
using Core.Entities.Models;
using Core.Entities.Request;
using Core.Entities.Response;
using Logic.Providers;
using Logic.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace API.Controllers
{
    public class BatteryEstimationController : ApiController
    {
        [HttpPost]
        [Route("api/battery/estimate")]
        public async Task<IHttpActionResult> Estimate(BatteryEstimationRequest request)
        {
            if (request == null)
            {
                return BadRequest("Request body is required.");
            }

            var service = new BatteryEstimationService();
            BatteryEstimationResponse result = await service.EstimateBattery(request);

            if (result == null)
            {
                return InternalServerError();
            }
            return Ok(result);
        }
    }
}
using Core.Entities.Request;
using Core.Entities.Response;
using Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;

namespace API.Controllers
{
    public class VehicleController : ApiController
    {
        [HttpGet]
        [Route("api/vehicle/{guid}")]
        public ResGetVehicle Get(Guid guid)
        {
            return new VehicleLogic().GetVehicle(new ReqGetVehicle { guid = guid });
        }

        [HttpPost]
        [Route("api/vehicle/save")]
        public ResSaveVehicle Save(ReqSaveVehicle req)
        {
            return new VehicleLogic().SaveVehicle(req);
        }

        [HttpPut]
        [Route("api/vehicle/update")]
        public ResUpdateVehicle Update(ReqUpdateVehicle req)
        {
            return new VehicleLogic().UpdateVehicle(req);
        }

        [HttpDelete]
        [Route("api/vehicle/{guid}")]
        public ResDeleteVehicle Delete(Guid guid)
        {
            return new VehicleLogic().DeleteVehicle(guid);
        }
    }
}
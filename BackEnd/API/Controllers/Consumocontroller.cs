using Core.Entities.Response;
using DTO.Consumo;
using Logic;
using System;
using System.Threading.Tasks;
using System.Web.Http;

namespace API.Controllers
{
    public class ConsumoController : ApiController
    {
        [HttpPost]
        [Route("api/consumo/guardar")]
        public async Task<ResBase> Guardar(DTOConsumo dto)
        {
            BatteryEstimationResponse estimacion = new BatteryEstimationResponse();
            estimacion.TotalEnergyWh = dto.TotalEnergyWh;
            estimacion.PercentUsed = dto.PercentUsed;

            return await new ConsumoLogic().GuardarConsumo(dto.guid, dto.nombreReferencia, estimacion);
        }

        [HttpGet]
        [Route("api/consumo/historial/{guid}")]
        public ResHistorialConsumo Historial(Guid guid)
        {
            return new ConsumoLogic().GetHistorial(guid);
        }
    }
}
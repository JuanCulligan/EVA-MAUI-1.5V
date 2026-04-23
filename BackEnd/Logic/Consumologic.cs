using Core.Entities.Models;
using Core.Entities.Response;
using Core.Enums;
using DataAccess;
using Helpers;
using Logic.Processors;
using Logic.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Logic
{
    public class ConsumoLogic
    {
        private readonly AresepService _service = new AresepService();
        private readonly AresepProcessor _processor = new AresepProcessor();



        public async Task<ResBase> GuardarConsumo(Guid guid, string nombreReferencia, BatteryEstimationResponse estimacion)
        {
            ResBase res = new ResBase();
            res.Result = false;
            res.errors = new List<Error>();

            try
            {
                if (guid == Guid.Empty)
                {
                    res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.GuidMissing));
                }

                if (estimacion == null)
                {
                    res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.DataBaseError));
                }

                if (res.errors.Count > 0)
                {
                    return res;
                }

                double consumoKwh = estimacion.TotalEnergyWh / 1000.0;
                double costoColones = 0;

                try
                {
                    string json = await _service.GetTarifasJson();
                    costoColones = _processor.GetCostoColones(json, estimacion.TotalEnergyWh);
                }
                catch (Exception)
                {
                    // ARESEP no disponible, usar ultima tarifa ICE Residencial conocida
                    const double tarifaFallback = 75.62;
                    costoColones = Math.Round(consumoKwh * tarifaFallback, 2);
                }

                if (costoColones == 0)
                {
                    const double tarifaFallback = 75.62;
                    costoColones = Math.Round(consumoKwh * tarifaFallback, 2);
                }

                using (ConexionDataContext conexion = new ConexionDataContext())
                {
                    conexion.SP_InsertarConsumoEnergia(guid, nombreReferencia, (decimal)consumoKwh);
                    conexion.SP_InsertarConsumoDinero(guid, nombreReferencia, (decimal)costoColones);
                }

                res.Result = true;
                res.errors = null;
            }
            catch (Exception ex)
            {
                res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.ExceptionThrown));
            }

            return res;
        }

        public ResHistorialConsumo GetHistorial(Guid guid)
        {
            ResHistorialConsumo res = new ResHistorialConsumo();
            res.Result = false;
            res.errors = new List<Error>();
            res.historial = new List<ConsumoItem>();

            try
            {
                if (guid == Guid.Empty)
                {
                    res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.GuidMissing));
                    return res;
                }

                using (ConexionDataContext conexion = new ConexionDataContext())
                {
                    var rows = conexion.SP_ObtenerHistorialConsumo(guid).ToList();

                    foreach (var r in rows)
                    {
                        ConsumoItem item = new ConsumoItem();
                        item.nombreReferencia = r.NombreReferencia;
                        item.consumoKwh = (double)r.ConsumoKwh;
                        item.monto = (double)r.Monto;
                        item.fechaConsumo = r.FechaConsumo;
                        res.historial.Add(item);
                    }
                }

                res.Result = true;
                res.errors = null;
            }
            catch (Exception ex)
            {
                res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.ExceptionThrown));
            }

            return res;
        }
    }
}
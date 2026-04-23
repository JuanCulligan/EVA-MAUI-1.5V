using Core.Entities.Models;
using Core.Entities.Request;
using Core.Entities.Response;
using Core.Enums;
using DataAccess;
using Logic.Algorithm;
using Logic.Processors;
using Logic.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Logic.Services
{
    public class BatteryEstimationService
    {
        private readonly VehicleSpecProvider _vehicleSpecProvider = new VehicleSpecProvider();
        private readonly DrivingConditionsProvider _drivingConditionsProvider = new DrivingConditionsProvider();
        private readonly MapboxService _mapboxService = new MapboxService();
        private readonly MapDataProcessor _mapDataProcessor = new MapDataProcessor();
        private readonly RouteSegmentBuilder _segmentBuilder = new RouteSegmentBuilder();
        private readonly SegmentElevationEnricher _elevationEnricher = new SegmentElevationEnricher();
        private readonly AlgBatteryEstimation _algorithm = new AlgBatteryEstimation();
        private readonly ConsumoLogic _consumoLogic = new ConsumoLogic();

        public async Task<BatteryEstimationResponse> EstimateBattery(BatteryEstimationRequest req)
        {
            BatteryEstimationResponse res = new BatteryEstimationResponse();
            res.errors = new List<Error>();
            res.Result = false;

            try
            {
                string brand;
                string model;
                int year;
                using (ConexionDataContext conexion = new ConexionDataContext())
                {
                    var vehicle = conexion.SP_ObtenerVehiculo(req.guid).FirstOrDefault();

                    if (vehicle == null)
                    {
                        res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.DataBaseError));
                        return res;
                    }

                    brand = vehicle.MARCA;
                    model = vehicle.MODELO;
                    year = vehicle.ANO;
                }

                VehicleSpecs specs = await _vehicleSpecProvider.GetVehicleSpecs(brand, model, year);
                if (specs == null)
                {
                    res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.EmptyResponse));
                    return res;
                }

                DrivingConditions conditions = await _drivingConditionsProvider.BuildConditions(req.StartLat, req.StartLon, req.SpeedKmh);
                string routeJson = await _mapboxService.GetRouteJson(req.StartLon, req.StartLat, req.EndLon, req.EndLat);
                List<RoutePoint> points = _mapDataProcessor.ParseRoutePoints(routeJson);
                if (points == null)
                {
                    res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.InvalidRoute));
                    return res;
                }

                List<RouteSegment> segments = _segmentBuilder.BuildSegments(points);
                await _elevationEnricher.AddElevationToSegments(segments);

                BatteryEstimationResponse result = _algorithm.BateryEstimation(segments, specs, conditions);
                result.Result = true;

                string nombreReferencia = string.Format(System.Globalization.CultureInfo.InvariantCulture,"{0},{1} - {2},{3}",req.StartLat, req.StartLon, req.EndLat, req.EndLon);

                await _consumoLogic.GuardarConsumo(req.guid, nombreReferencia, result);

                return result;
            }
            catch (Exception)
            {
                res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.ExceptionThrown));
                return res;
            }
        }
    }
}
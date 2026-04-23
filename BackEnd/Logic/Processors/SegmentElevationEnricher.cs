using Logic.Services;
using Logic.Processors;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Entities.Models;

namespace Logic.Processors
{
    public class SegmentElevationEnricher
    {
        private readonly MapboxService _mapbox = new MapboxService();
        private readonly MapDataProcessor _processor = new MapDataProcessor();

        // Creamos un diccionario para guardar las cordenadas con su elevvacion.
        private Dictionary<string, double> _elevationCache = new Dictionary<string, double>();

        public async Task AddElevationToSegments(List<RouteSegment> segments)
        {
            foreach (var segment in segments)
            {
                if (segment.Points == null || !segment.Points.Any())
                {
                    continue;
                }


                var startPoint = segment.Points.First();
                var endPoint = segment.Points.Last();

                // consigue la elevacion al punto incial
                double startEle = await GetElevationCached(startPoint);

                // consigue la elevacion al punto final
                double endEle = await GetElevationCached(endPoint);

                // asigna los valores
                segment.StartElevation = startEle;
                segment.EndElevation = endEle;

                // Consigue el cambbio de elevacion
                segment.ElevationChange = endEle - startEle;
            }
        }

        // Solo se llama al inicio y si el segundo punto no tiene una elevacion asignada
        private async Task<double> GetElevationCached(RoutePoint point)
        {
            // convierte una cordenada en string. Este actua como un ID
            string key = $"{point.Latitude},{point.Longitude}";

            // Revisa el diccionario y si esa cordenada/ID tiene una elevacion la devuelve
            if (_elevationCache.ContainsKey(key))
                return _elevationCache[key];

            // si no tiene una elevacion se la pide a mapbox
            string json = await _mapbox.GetElevationJson(
                point.Longitude,
                point.Latitude
            );

            double elevation = _processor.ParseElevation(json);

            // Guarda la elevacion para futuros segmentos
            _elevationCache[key] = elevation;

            return elevation;
        }
    }
}
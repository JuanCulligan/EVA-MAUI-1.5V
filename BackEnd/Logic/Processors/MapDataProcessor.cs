using Core.Entities.Models;
using Core.Enums;
using Helpers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Processors
{
    public class MapDataProcessor
    {
        public List<RoutePoint> ParseRoutePoints(string jsonResponse)
        {
            // Crea lista de todas las cordenadas entre los puntos
            var points = new List<RoutePoint>();

            // toma el jsonResponse y lo hace un objeto
            var json = JObject.Parse(jsonResponse);

            //dentro del json response buscamos los datos necesarios 
            var coordinates = json["routes"]?[0]?["geometry"]?["coordinates"];

            if (coordinates != null)
            {
                foreach (var coord in coordinates)
                {
                    //Agrega todas las cordenadas a la lista 
                    points.Add(new RoutePoint{Longitude = (double)coord[0],Latitude = (double)coord[1]});
                }
            }

            return points;
        }

        public double ParseElevation(string jsonResponse)
        {
            var json = JObject.Parse(jsonResponse);

            var elevations = new List<double>();

            foreach (var f in json["features"] ?? new JArray())
            {
                double ele = f["properties"]?["ele"]?.Value<double>() ?? 0;
                elevations.Add(ele);
            }

            if (elevations != null && elevations.Any())
            {
                return elevations.FirstOrDefault();
            }

            return 0;
        }


    }
}

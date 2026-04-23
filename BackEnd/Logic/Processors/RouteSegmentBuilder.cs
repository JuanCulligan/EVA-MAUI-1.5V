using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Helpers;
using Core.Entities.Models;

namespace Logic.Processors
{
    public class RouteSegmentBuilder
    {
        //recibe la lista de cordenadas y toma el tamano de cada segmento
        public List<RouteSegment> BuildSegments(List<RoutePoint> points, double segmentSize = 500)
        {
            //lista que va a contener todos los segmentos
            var segments = new List<RouteSegment>();

            //
            var currentSegment = new RouteSegment();
            double currentDistance = 0; // lleva la cuenta de cuantos metros hay en cada cordenada

            for (int i = 0; i < points.Count - 1; i++)
            {
                // toma un punto y el que sigue
                var p1 = points[i];
                var p2 = points[i + 1];

                // calcula distancia entre ambas cordenadas
                double dist = Helpers.Helpers.GetDistanceMeters(p1, p2);

                currentSegment.Points.Add(p1); // anade ese segmento a nuestra lista

                //En la ultima iteracion anade el ultimo punto a la lista tambien
                if (i == points.Count - 2) { 
                currentSegment.Points.Add(p2);
                }
                currentDistance += dist; // suma los metros a nuesta cuenta total

                // revisa si la distancia actual es igual o mayor a 500 metros
                if (currentDistance >= segmentSize)
                {
                    currentSegment.DistanceMeters = currentDistance;// Guarda la ultima ditsnacia de ese chunk
                    segments.Add(currentSegment);// guarda el chunk final a la lista final

                    // reinica los valores para el proximo segmento 
                    currentSegment = new RouteSegment();
                    currentDistance = 0;
                }
            }
            // si la ruta es muy corta y el segemnto no llega a los 500m se manda el sgemnto actual.
            if (currentSegment.DistanceMeters > 0) {
                currentSegment.DistanceMeters = currentDistance;
                segments.Add(currentSegment);
            }

            return segments;
        }
    }
}

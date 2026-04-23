using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities.Models
{
    public class RouteSegment
    {
        public List<RoutePoint> Points { get; set; } = new List<RoutePoint>();

        public double DistanceMeters { get; set; }

        public double StartElevation { get; set; }
        public double EndElevation { get; set; }
        public double ElevationChange { get; set; }
    }
}

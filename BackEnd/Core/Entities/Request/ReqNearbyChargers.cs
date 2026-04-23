using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities.Request
{
    public class ReqNearbyChargers
    {
        public double Lat { get; set; }
        public double Lon { get; set; }

        /// <summary>Radio en km. Si es 0 o negativo, el API usa 8 km.</summary>
        public int RadiusKm { get; set; }
    }
}

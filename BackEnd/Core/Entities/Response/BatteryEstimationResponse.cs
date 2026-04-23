using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities.Response
{
    public class BatteryEstimationResponse: ResBase
    {
        public double TotalEnergyWh { get; set; }
        public double PercentUsed { get; set; }
    }
}

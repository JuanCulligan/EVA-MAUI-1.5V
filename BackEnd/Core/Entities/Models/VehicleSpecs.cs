using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities.Models
{
    public class VehicleSpecs
    {
        public string Brand { get; set; }
        public string Model { get; set; }
        public int Year { get; set; }

        public double BatteryCapacityKWh { get; set; }  

        public double VehicleMassKg { get; set; }        

        public double EfficiencyWhPerKm { get; set; }    

        public double RegenEfficiencyPercent { get; set; } 
    }
}

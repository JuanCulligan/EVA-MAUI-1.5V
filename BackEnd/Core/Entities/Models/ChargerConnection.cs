using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities.Models
{
    public class ChargerConnection
    {
        public string ConnectionType { get; set; } // CCS, Type2, CHAdeMO
        public double PowerKW { get; set; }        // 50kW, 22kW, etc
    }
}

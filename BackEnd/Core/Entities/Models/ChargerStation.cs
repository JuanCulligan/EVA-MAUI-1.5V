using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities.Models
{
    public class ChargerStation
    {
        public string Id { get; set; }
        public string Name { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public string Address { get; set; }
        public string OperationalStatus { get; set; }
        public string UsageType { get; set; }
        public List<ChargerConnection> Connections { get; set; }= new List<ChargerConnection>();

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities.Models
{
    public class Location
    {
        public long Id { get; set; } 
        public string locationName { get; set; }
        public double lat { get; set; }
        public double lon { get; set; }
    }
}

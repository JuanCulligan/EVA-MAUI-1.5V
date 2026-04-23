using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities.Request
{
    public class ReqSaveRecentLocation
    {
        public Guid guid { get; set; }
        public string locationName { get; set; }
        public double lat { get; set; }
        public double lon { get; set; }
    }
}

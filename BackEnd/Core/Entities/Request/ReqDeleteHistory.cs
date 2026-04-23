using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities.Request
{
    public class ReqDeleteHistory
    {
        public Guid guid {  get; set; }
        public long idHistorial { get; set; }
    }
}

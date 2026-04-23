using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities.Response
{
    public class ResHistorialConsumo : ResBase
    {
        public List<ConsumoItem> historial { get; set; }
    }
}

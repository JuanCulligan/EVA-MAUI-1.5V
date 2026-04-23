using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities.Response
{
    public class ConsumoItem
    {
        public string nombreReferencia { get; set; }
        public double consumoKwh { get; set; }
        public double monto { get; set; }
        public DateTime fechaConsumo { get; set; }
    }
}

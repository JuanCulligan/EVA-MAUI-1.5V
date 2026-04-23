using Core.Entities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities.Response
{
    public class ResGetFavorites: ResBase
    {
        public List<Location> favorites { get; set; }
    }
}

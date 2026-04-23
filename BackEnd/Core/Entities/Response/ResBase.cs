using Core.Entities.Models;
using Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities.Response
{
    public class ResBase
    {
        public bool Result { get; set; }
        public List<Error> errors { get; set; }
    }
}

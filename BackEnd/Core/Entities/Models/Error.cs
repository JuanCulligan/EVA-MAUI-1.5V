using Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities.Models
{
    public class Error
    {
        public EnumErrors code { get; set; }
        public string message { get; set; }
    }
}

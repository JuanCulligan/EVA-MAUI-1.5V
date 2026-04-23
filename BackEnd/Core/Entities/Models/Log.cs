using Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities.Models
{
    public class Log
    {
        public string Class { get; set; }
        public string Method { get; set; }
        public EnumLog type { get; set; }
        public int ErrorID { get; set; }
        public string Description { get; set; }
        public string Request { get; set; }
        public string Response { get; set; }
    }
}

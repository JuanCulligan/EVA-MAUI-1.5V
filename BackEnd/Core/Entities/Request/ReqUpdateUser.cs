using Core.Entities.Models;
using Core.Entities.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities.Request
{
    public class ReqUpdateUser
    {
        public Guid guid { get; set; }
        public User user { get; set; }
    }
}

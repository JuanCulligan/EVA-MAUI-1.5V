using Core.Entities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities.Response
{
    public class ResLogin : ResBase
    {
        public User user {  get; set; }
    }
}

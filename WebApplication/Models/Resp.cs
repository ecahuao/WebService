using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication.Models
{
    public class Resp
    {
        public string message{ get; set; }
        public ObjectResult resp { get; set; }
    }
}

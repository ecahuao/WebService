using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication.Models
{
    public class Dats
    {
        public string operation { get; set; }
        public string path { get; set; }
        //public List<string> target { get; set; }
        public List<Contents> content { get; set; }
        public int quantity { get; set; }
    }
    public class Contents
    {
        public string key { get; set; }
        public string target { get; set; }
        public List<Values> value { get; set; }
    }
    public class Values
    {
        public Values()
        {
            this.valueType = "";
            this.value = "";
            this.row = "";
        }
        public string valueType { get; set; }
        public string value { get; set; }
        public string row { get; set; }
    }
}

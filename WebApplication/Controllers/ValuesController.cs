using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using WebApplication.Data;
using WebApplication.Models;

namespace WebApplication.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        private readonly dataRepository _dataRep;
        public ValuesController(dataRepository dataRep)
        {
            _dataRep = dataRep;
        }
        // GET api/values
        [HttpGet]
        public ActionResult<IEnumerable<string>> Get()
        {
            return new string[] { "value1", "value2" };
            //return  Ok(_dataRep.InsertData);
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public ActionResult<string> Get(int id)
        {
            return "value";
        }

        // POST api/values
        [HttpPost]
        public async Task Post([FromBody] Dats datos)
        {
            //_connectionString = configuration.GetValue<string>("Context");
            
            await _dataRep.handleData(datos,"0");
            //return Ok(new string[] { "value1", "value2" });
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public async Task Put(string id, [FromBody] Dats datos)
        {
            await _dataRep.handleData(datos,id);
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}

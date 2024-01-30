using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TavisAPI.Models;

namespace TavisAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StoreController : Controller
    {
        private readonly IConfiguration _configuration;

        public StoreController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet()]
        public JArray Get(string uid)
        {
            Database db = new(_configuration);
            return db.FetchStores(uid);
        }
  
        [HttpPost()]
        public void Post([FromBody] Store store)
        {
            Database db = new(_configuration);
            db.InsertUserPreference(store);
        }

        [HttpDelete()]
        public void Delete([FromBody] Store store)
        {
            Database db = new(_configuration);
            db.DeleteUserPreference(store);
        }    
    }
}
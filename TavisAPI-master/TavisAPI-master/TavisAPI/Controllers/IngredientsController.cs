using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TavisAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IngredientsController : Controller
    {
        private readonly IConfiguration _configuration;

        public IngredientsController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet()]
        public JArray Get()
        {
            Database db = new(_configuration);
            return db.FetchIngredients();
        }
    }
}

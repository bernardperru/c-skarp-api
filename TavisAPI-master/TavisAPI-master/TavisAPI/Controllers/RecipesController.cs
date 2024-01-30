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
    public class RecipesController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public RecipesController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet()]
        [Route("/api/Recipes")]
        public JArray Get(string search = "", string type = "", string ingredients = "", string uid = "", [FromQuery] string[] time = null)
        {
            Database db = new (_configuration);
            return db.FetchRecipes(search, time, ingredients, type, uid);
        }

        [HttpGet()]
        [Route("/api/Recipe")]
        public JArray Get(int id, string uid)
        {
            Database db = new(_configuration);
            return db.FetchRecipe(id, uid);
        }
    }
}

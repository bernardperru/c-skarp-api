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
    public class RefigeratorController : Controller
    {
        private readonly IConfiguration _configuration;

        public RefigeratorController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet()]
        public JArray Get(string uid)
        {
            Database db = new(_configuration);
            return db.FetchRefrigerator(uid);
        }

        [HttpPost()]
        public void Post([FromBody] Ingredient ingredient)
        {
            Database db = new(_configuration);
            db.InsertIngredient(ingredient.Title, ingredient.Unit, ingredient.Amount, ingredient.UserId);
        }

        [HttpDelete()]
        public void Delete([FromBody] Ingredient ingredient)
        {
            Database db = new(_configuration);
            db.DeleteIngredient(ingredient.Id);
        }
    }
}
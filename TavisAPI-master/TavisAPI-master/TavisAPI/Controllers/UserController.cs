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
    public class UserController
    {
        private readonly IConfiguration _configuration;

        public UserController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost()]
        public void Post([FromBody] User user)
        {
            Database db = new(_configuration);
            db.InsertUser(user.Email, user.UID);
        }

        [HttpPut()]
        public void Put([FromBody] User user)
        {
            Database db = new(_configuration);
            db.UpdateUser(user.Email, user.UID);
        }

        [HttpDelete()]
        public void Delete([FromBody] User user)
        {
            Database db = new(_configuration);
            db.DeleteUser(user);
        }
    }
}

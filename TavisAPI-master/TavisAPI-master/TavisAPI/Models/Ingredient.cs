using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TavisAPI.Models
{
    public class Ingredient
    {
        public int Id { get; set; }

        public string UserId { get; set; }

        public string Title { get; set; }

        public float Amount { get; set; }

        public string Unit { get; set; }
    }
}

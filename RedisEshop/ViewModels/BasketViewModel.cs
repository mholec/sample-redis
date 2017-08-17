using System.Collections.Generic;
using System.Linq;
using RedisEshop.Entities;

namespace RedisEshop.ViewModels
{
    public class BasketViewModel
    {
		public List<Product> Products { get; set; }
	    public decimal TotalPrice => Products?.Sum(x => x.Price) ?? 0;
	    public decimal TotalItems => Products?.Count ?? 0;
    }
}

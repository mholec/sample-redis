using System;
using System.Collections.Generic;

namespace RedisEshop.ViewModels
{
    public class ProductViewModel
    {
	    public int ProductId { get; set; }
	    public string Identifier { get; set; }
	    public string Title { get; set; }
	    public string Description { get; set; }
	    public decimal Price { get; set; }
		public int PurchasesCount { get; set; }
		public int Views { get; set; }
		public DateTime Added { get; set; }

		public List<string> Tags { get; set; }
    }
}

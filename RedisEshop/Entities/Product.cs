using System;
using System.Collections.Generic;

namespace RedisEshop.Entities
{
    public class Product
    {
		public int ProductId { get; set; }
	    public string Identifier { get; set; }
	    public string Title { get; set; }
	    public string Description { get; set; }
		public decimal Price { get; set; }
		public DateTime Added { get; set; }
		public int Likes { get; set; }
		public int Views { get; set; }

		public ICollection<ProductTag> ProductTags { get; set; }
		public ICollection<OrderItem> OrderedItems { get; set; }
    }
}
using System;

namespace RedisEshop.Entities
{
	public class OrderItem
	{
		public Guid OrderItemId { get; set; }
		public Guid OrderId {get; set; }
		public int? ProductId {get; set; }
		public string Name {get; set; }
		public decimal Price { get; set; }
		public decimal Count { get; set; }
		public decimal TotalPrice { get; set; }

		public Order Order { get; set; }
		public Product Product { get; set; }
	}
}
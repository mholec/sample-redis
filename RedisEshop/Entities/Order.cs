using System;
using System.Collections.Generic;

namespace RedisEshop.Entities
{
    public class Order
    {
		public Guid OrderId { get; set; }
		public DateTime Created { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string PostalCode { get; set; }

		public virtual ICollection<OrderItem> OrderItems { get; set; }
    }
}

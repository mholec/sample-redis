using System.Collections.Generic;

namespace RedisEshop.ViewModels
{
	public class OrderViewModel
	{
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public Dictionary<string, int> Items { get; set; }
		public List<ProductViewModel> Products { get; set; }
	}
}
using System;
using System.Collections.Generic;

namespace RedisEshop.ViewModels
{
    public class ShoppingCartViewModel
    {
	    public ShoppingCartViewModel()
	    {
		    Items = new List<ShoppingCartItemViewModel>();
	    }

		public Guid ShoppingCartId { get; set; }
		public ICollection<ShoppingCartItemViewModel> Items { get; set; }
    }
}

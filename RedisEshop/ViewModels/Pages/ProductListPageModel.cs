using System.Collections.Generic;

namespace RedisEshop.ViewModels.Pages
{
	public class ProductListPageModel
	{
		public ProductListPageModel()
		{
			SelectedTags = new int[0];
		}

		public List<ProductViewModel> Products { get; set; }
		public int[] SelectedTags { get; set; }
	}
}
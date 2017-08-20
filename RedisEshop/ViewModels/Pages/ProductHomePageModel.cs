using System.Collections.Generic;

namespace RedisEshop.ViewModels.Pages
{
    public class ProductHomePageModel
    {
		public List<ProductViewModel> LatestProducts { get; set; }
		public List<ProductViewModel> Bestsellers { get; set; }
		public List<ProductViewModel> MostViewed { get; set; }
    }
}
using System.Collections.Generic;

namespace RedisEshop.ViewModels.Pages
{
    public class ProductHomePageModel
    {
		public List<ProductViewModel> LatestProducts { get; set; }
		public List<ProductViewModel> TopRatedProducts { get; set; }
		public List<ProductViewModel> MostViewedProducts { get; set; }
    }
}
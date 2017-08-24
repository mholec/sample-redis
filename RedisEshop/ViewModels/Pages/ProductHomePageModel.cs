using System.Collections.Generic;

namespace RedisEshop.ViewModels.Pages
{
    public class ProductHomePageModel
    {
	    public List<ProductBaseViewModel> Bestsellers { get; set; }
	    public List<ProductViewModel> LatestProducts { get; set; }
	    public List<ProductViewModel> MostViewed { get; set; }
    }
}
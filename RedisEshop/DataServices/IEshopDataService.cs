using System.Collections.Generic;
using RedisEshop.ViewModels;

namespace RedisEshop.DataServices
{
    public interface IEshopDataService
    {
	    List<ProductViewModel> GetLatestProducts(int count);
	    List<ProductViewModel> GetProductsByTags(int[] tagIds);
	    List<ProductViewModel> GetRandomProducts(int count);
	    List<ProductViewModel> GetTopRatedProducts(int count);
	    List<ProductViewModel> GetMostViewedProducts(int count);
	    int AddAndGetProductVisits(int productId);

    }
}
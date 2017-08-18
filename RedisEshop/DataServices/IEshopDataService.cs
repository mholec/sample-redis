using System.Collections.Generic;
using RedisEshop.ViewModels;

namespace RedisEshop.DataServices
{
    public interface IEshopDataService
    {
	    List<ProductViewModel> GetLatestProducts(int count);
	    List<ProductViewModel> GetProductsByTags(int[] tagIds);
	    List<ProductViewModel> GetRandomProducts(int count);
	    List<ProductViewModel> Bestsellers(int count);
	    int AddAndGetProductVisits(int productId);

    }
}
using System.Collections.Generic;
using RedisEshop.Entities;
using RedisEshop.ViewModels;

namespace RedisEshop.DataServices
{
	public interface IEshopDataService
	{
		List<ProductViewModel> GetLatestProducts(int count);
		List<ProductViewModel> GetProductsByTags(int[] tagIds);
		List<ProductViewModel> GetRandomProducts(int count);
		List<ProductViewModel> Bestsellers(int count);
		List<ProductViewModel> GetMostViewedProducts(int count);
		int AddAndGetProductVisits(int productId);
		ProductViewModel GetProduct(string identifier);
		(string, string) NewsletterSubscribe(string email);
		IEnumerable<string> SendNewsletters();
		List<PostalCode> GetMunicipalities(string postalCode);
		ShoppingCartViewModel GetShoppingCart();
		void AddToShoppingCart(string identifier);
		void RemoveFromShoppingCart(string identifier);
		OrderViewModel CreateOrderFromShoppingCart();
	}
}
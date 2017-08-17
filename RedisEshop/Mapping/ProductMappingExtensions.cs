using System.Collections.Generic;
using System.Linq;
using RedisEshop.Entities;
using RedisEshop.ViewModels;

namespace RedisEshop.Mapping
{
	public static class ProductMappingExtensions
	{
		public static List<ProductViewModel> ToViewModel(this IQueryable<Product> products)
		{
			return products.Select(x => new ProductViewModel
			{
				Price = x.Price,
				Description = x.Description,
				Identifier = x.Identifier,
				ProductId = x.ProductId,
				Title = x.Title,
				Likes = x.Likes,
				Views = x.Views,
				Added = x.Added,
				Tags = x.ProductTags != null ? x.ProductTags.Select(pt => pt.Tag.Title).ToList() : new List<string>()
			}).ToList();
		}
	}
}

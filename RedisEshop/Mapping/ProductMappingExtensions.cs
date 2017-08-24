using System.Collections.Generic;
using System.Linq;
using RedisEshop.Dto;
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
				Views = x.Views,
				Added = x.Added,
				Tags = x.ProductTags != null ? x.ProductTags.Where(t => t.Tag != null).Select(pt => pt.Tag.Title).ToList() : new List<string>()
			}).ToList();
		}

		public static List<ProductBaseViewModel> ToViewModel(this IQueryable<ProductBase> products)
		{
			return products.Select(x => new ProductBaseViewModel
			{
				Identifier = x.Identifier,
				ProductId = x.ProductId,
				Title = x.Title,
			}).ToList();
		}
	}
}

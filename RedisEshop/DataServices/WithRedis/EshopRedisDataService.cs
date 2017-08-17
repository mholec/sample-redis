using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using RedisEshop.Entities;
using RedisEshop.Mapping;
using RedisEshop.ViewModels;

namespace RedisEshop.DataServices.WithRedis
{
	/// <summary>
	/// Služba napojená na SQL databázi s využitím Redis
	/// </summary>
	public class EshopRedisDataService : IEshopDataService
	{
		private readonly AppDbContext _db;
		private readonly RedisService _redisService;

		public EshopRedisDataService(AppDbContext db, RedisService redisService)
		{
			_db = db;
			_redisService = redisService;
		}

		public List<ProductViewModel> GetLatestProducts(int count)
		{
			int[] latest = _redisService.LatestProducts(count);

			IQueryable<Product> dataQuery = _db.Products
				.Include(x => x.ProductTags).ThenInclude(x => x.Tag)
				.Where(x => latest.Contains(x.ProductId));

			return dataQuery.ToViewModel()
				.OrderByDescending(x => x.Added).ToList();
		}

		public List<ProductViewModel> GetProductsByTags(int[] tagIds)
		{
			IQueryable<Product> dataQuery = _db.Products
				.Include(x => x.ProductTags).ThenInclude(x => x.Tag);

			if (tagIds.Any())
			{
				int[] filtered = _redisService.ProductsByTags(tagIds);
				dataQuery = dataQuery.Where(x => filtered.Contains(x.ProductId));
			}

			return dataQuery.ToViewModel();
		}

		public List<ProductViewModel> GetRandomProducts(int count)
		{
			int[] random = _redisService.RandomProducts(count);

			IQueryable<Product> dataQuery = _db.Products
				.Include(x => x.ProductTags).ThenInclude(x => x.Tag)
				.Where(x => random.Contains(x.ProductId));

			return dataQuery.ToViewModel();
		}

		public List<ProductViewModel> GetTopRatedProducts(int count)
		{
			Dictionary<int, double> topRated = _redisService.TopRatedProducts(count);

			IQueryable<Product> dataQuery = _db.Products
				.Include(x => x.ProductTags).ThenInclude(x => x.Tag)
				.Where(x => topRated.Select(t => t.Key).Contains(x.ProductId));

			List<ProductViewModel> data = dataQuery.ToViewModel();
			data.ForEach(x => x.Likes = (int)topRated[x.ProductId]);

			return data;
		}

		public List<ProductViewModel> GetMostViewedProducts(int count)
		{
			int[] mostViewed = _redisService.RandomProducts(count);

			IQueryable<Product> dataQuery = _db.Products
				.Include(x => x.ProductTags).ThenInclude(x => x.Tag)
				.Where(x => mostViewed.Contains(x.ProductId));

			return dataQuery.ToViewModel();
		}

		public int AddAndGetProductVisits(int productId)
		{
			return _redisService.AddProductVisit(productId);
		}
	}
}
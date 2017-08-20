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
			List<Product> latest = _redisService.LatestProducts(count);

			return latest.AsQueryable().ToViewModel().OrderByDescending(x => x.Added).ToList();
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

		public List<ProductViewModel> Bestsellers(int count)
		{
			Dictionary<Product, double> topRated = _redisService.Bestsellers(count);
			Dictionary<int, double> scoresheet = topRated.ToDictionary(x => x.Key.ProductId, x => x.Value);

			List<ProductViewModel> data = topRated.Select(x => x.Key).AsQueryable().ToViewModel();
			data.ForEach(x => x.PurchasesCount = (int)scoresheet[x.ProductId]);

			return data;
		}

		public int AddAndGetProductVisits(int productId)
		{
			return _redisService.AddProductVisit(productId);
		}

		public ProductViewModel GetProduct(string identifier)
		{
			int? id = _redisService.GetProductIdByIdenfitier(identifier);

			var products = id != null
				? _db.Products.Where(x => x.ProductId == id)			// just example, should be Find()
				: _db.Products.Where(x => x.Identifier == identifier);	// just example, should be FirstOrDefault()

			return products.ToViewModel().FirstOrDefault();
		}

		public (string, string) NewsletterSubscribe(string email)
		{
			var added = _redisService.TryAddNewsletterSubscriber(email);

			if (!added)
			{
				return ("warning", "Email byl již v minulosti přihlášen");
			}

			EmailMessageViewModel model = new EmailMessageViewModel
			{
				To = email,
				Subject = "Úspěšná registrace k odběru novinek",
				Message = "Děkujeme za přihlášení k odběru novinek na naší webové stránce ...."
			};

			_redisService.QueueNewsletterWelcomeMail(model);

			return ("success", "Email byl uložen k odběru novinek");
		}

		public List<ProductViewModel> GetMostViewedProducts(int count)
		{
			Dictionary<int, double> mostViewed = _redisService.MostViewedProducts(count);

			List<ProductViewModel> result = _db.Products.Include(x => x.ProductTags).ThenInclude(x => x.Tag)
				.Where(x => mostViewed.Select(y => y.Key).Contains(x.ProductId)).ToViewModel();

			result.ForEach(x => x.Views = (int) mostViewed[x.ProductId]);

			return result.OrderByDescending(x => x.Views).ToList();
		}

		public IEnumerable<string> SendNewsletters()
		{
			EmailMessageViewModel mail;
			while ((mail = _redisService.DequeueNewsletterWelcomeMail()) != null)
			{
				// emailService.Send(email);

				yield return mail.To;
			}
		}
	}
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using RedisEshop.Entities;
using RedisEshop.Mapping;
using RedisEshop.ViewModels;
using RedLock;

namespace RedisEshop.DataServices.WithRedis
{
	/// <summary>
	/// Služba napojená na SQL databázi s využitím Redis
	/// </summary>
	public class EshopRedisDataService : IEshopDataService
	{
		private readonly AppDbContext _db;
		private readonly RedisService _redisService;
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly RedisLockFactory _redisLockFactory;

		public EshopRedisDataService(AppDbContext db, RedisService redisService, IHttpContextAccessor httpContextAccessor, RedisLockFactory redisLockFactory)
		{
			_db = db;
			_redisService = redisService;
			_httpContextAccessor = httpContextAccessor;
			_redisLockFactory = redisLockFactory;
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
				? _db.Products.Where(x => x.ProductId == id)            // just example, should be Find()
				: _db.Products.Where(x => x.Identifier == identifier);  // just example, should be FirstOrDefault()

			var product = products.ToViewModel().FirstOrDefault();

			if (product != null)
			{
				product.InBasket = _redisService.CountShoppingCartItem(ResolveShoppingCartId(), identifier);
			}

			return product;
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

			result.ForEach(x => x.Views = (int)mostViewed[x.ProductId]);

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

		public List<PostalCode> GetMunicipalities(string postalCode)
		{
			if (string.IsNullOrEmpty(postalCode) || postalCode.Length != 5)
			{
				return new List<PostalCode>();
			}

			return _redisService.GetPostalCodes(int.Parse(postalCode));
		}

		public ShoppingCartViewModel GetShoppingCart()
		{
			Guid cartId = ResolveShoppingCartId();

			List<ShoppingCartItemViewModel> items = _redisService.GetShoppingCartItems(cartId);

			return new ShoppingCartViewModel
			{
				ShoppingCartId = cartId,
				Items = items
			};
		}

		public void AddToShoppingCart(string identifier)
		{
			Guid cartId = ResolveShoppingCartId();

			_redisService.AddShoppingCartItem(cartId, identifier, amount: 1);
		}

		public void RemoveFromShoppingCart(string identifier)
		{
			Guid cartId = ResolveShoppingCartId();

			_redisService.RemoveShoppingCartItem(cartId, identifier);
		}

		public OrderViewModel CreateOrderFromShoppingCart()
		{
			Guid cartId = ResolveShoppingCartId();

			List<ShoppingCartItemViewModel> items = _redisService.GetShoppingCartItems(cartId);

			return new OrderViewModel()
			{
				Items = items.ToDictionary(x => x.Name, x => x.Items),
				Products = _db.Products.Where(x => items.Select(i => i.Name).Contains(x.Identifier)).ToViewModel()
			};
		}

		public void ProcessOrder()
		{
			Guid cartId = ResolveShoppingCartId();

			Dictionary<string, int> shoppingCartItems = _redisService.GetShoppingCartItems(cartId)
				.ToDictionary(x => x.Name, x => x.Items);

			List<Product> products = _db.Products.Where(x => shoppingCartItems.Select(i => i.Key).Contains(x.Identifier)).ToList();

			List<OrderItem> orderItems = new List<OrderItem>();
			foreach (var shoppingCartItem in shoppingCartItems)
			{
				var product = products.FirstOrDefault(x => x.Identifier == shoppingCartItem.Key);
				orderItems.Add(new OrderItem
				{
					Count = shoppingCartItem.Value,
					ProductId = product.ProductId,
					Price = product.Price,
					Name = product.Title,
					TotalPrice = product.Price * shoppingCartItem.Value,
				});
			}

			var order = new Order
			{
				Created = DateTime.Now,
				OrderItems = orderItems
			};

			_db.Orders.Add(order);
			_db.SaveChanges();

			_redisService.RemoveShoppingCart(cartId);

			// update redis (bestsellers)
			// bud background service provede prepocet všeho - blbé řešení, pomalé
			// ideálně by se měl aktualizovat jen redis sorted set (problém unikátnosti klíčů)
			// předělat to jen na IDčka?
		}

		private Guid ResolveShoppingCartId()
		{
			string cartId = _httpContextAccessor.HttpContext.Request.Cookies["CartId"];

			if (cartId != null)
			{
				return Guid.Parse(cartId);
			}
			else
			{
				Guid newCartId = Guid.NewGuid();
				_httpContextAccessor.HttpContext.Response.Cookies.Append("CartId", newCartId.ToString());

				return newCartId;
			}
		}

		public void AddPostalCodeWithSimpleLock(int code, string name)
		{
			_db.PostalCodes.Add(new PostalCode
			{
				Code = code,
				Name = name
			});

			if (_redisService.GetLock(code + name))
			{
				// long running operation
				Thread.Sleep(30000);

				_db.SaveChanges();
				_redisService.ReleaseLock(code + name);
			}
			else
			{
				// when locked
			}
		}

		public void AddPostalCodeWithRedisLock(int code, string name)
		{
			_db.PostalCodes.Add(new PostalCode
			{
				Code = code,
				Name = name
			});

			using (var redisLock = _redisLockFactory.Create(code + name, TimeSpan.FromSeconds(60)))
			{
				if (redisLock.IsAcquired)
				{
					// long running operation
					Thread.Sleep(30000);

					_db.SaveChanges();
				}
				else
				{
					// when locked
				}
			}
		}
	}
}
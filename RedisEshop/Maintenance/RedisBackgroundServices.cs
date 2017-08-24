using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using RedisEshop.DataServices;
using RedisEshop.Dto;
using RedisEshop.Entities;
using StackExchange.Redis;

namespace RedisEshop.Maintenance
{
	public class RedisBackgroundServices
	{
		private readonly ConnectionMultiplexer _redis;
		private readonly CommonListService _commonListService;
		private readonly AppDbContext _db;

		public RedisBackgroundServices(ConnectionMultiplexer redis, CommonListService commonListService, AppDbContext db)
		{
			_redis = redis;
			_commonListService = commonListService;
			_db = db;
		}

		/// <summary>
		/// Inicializace Redis databáze do výchozího stavu
		/// </summary>
		public void RestoreRedis(bool flushDb = true)
		{
			// odstranění celé databáze
			if (flushDb)
			{
				_redis.GetServer("127.0.0.1:6379").FlushAllDatabases();
			}

			// úvodní nastavení štítků a přiřazených produktů
			InitTagFamilies();

			// úvodní nastavení posledních produktů
			_redis.GetDatabase().KeyDelete("products:latest");
			_redis.GetDatabase().KeyDelete("products:latest-ids");
			var products = _db.Products.OrderByDescending(x => x.ProductId).Take(20).OrderBy(x => x.ProductId).ToList();
			products.ForEach(AddNewProduct);

			// úvodní nastavení počtů objednávek
			_redis.GetDatabase().KeyDelete("products:bestsellers");
			InitBestsellers();

			// úvodní nastavení mapování mezi productId a identifier
			InitIdentifierMaps(_db.Products.ToList());

			// import PSČ
			InitPostalCodes();
		}

		public void AddNewProduct(Product product)
		{
			// serialize
			var json = JsonConvert.SerializeObject(product, new JsonSerializerSettings()
			{
				ReferenceLoopHandling = ReferenceLoopHandling.Ignore
			});

			// přidá 1 nový prvek a ořízne pole na 15 prvků
			_redis.GetDatabase().ListLeftPush("products:latest", json, When.Always, CommandFlags.FireAndForget);
			_redis.GetDatabase().ListTrim("products:latest", 0, 15, CommandFlags.FireAndForget);

			_redis.GetDatabase().ListLeftPush("products:latest-ids", product.ProductId, When.Always, CommandFlags.FireAndForget);
			_redis.GetDatabase().ListTrim("products:latest-ids", 0, 15, CommandFlags.FireAndForget);
		}

		private void InitTagFamilies()
		{
			// tagFamilies
			int[] tagFamilies = _commonListService.TagTypes().Select(x => (int)x.Key).ToArray();
			_redis.GetDatabase().KeyDelete("tagFamilies");
			_redis.GetDatabase().SetAdd("tagFamilies", tagFamilies.Select(x => (RedisValue)x).ToArray());

			// tagFamilies:1
			Dictionary<int, int> tags = _db.Tags.Select(x => new { x.TagId, x.Type }).ToDictionary(x => x.TagId, x => (int)x.Type);
			Dictionary<int, List<int>> categories = new Dictionary<int, List<int>>();
			foreach (var tagId in tags.Select(x => x.Key))
			{
				if (tags.ContainsKey(tagId))
				{
					if (!categories.ContainsKey(tags[tagId]))
					{
						categories.Add(tags[tagId], new List<int>() { tagId });
					}
					else
					{
						categories[tags[tagId]].Add(tagId);
					}
				}
			}

			foreach (var category in categories)
			{
				_redis.GetDatabase().KeyDelete("tagFamilies:" + category.Key);
				_redis.GetDatabase().SetAdd("tagFamilies:" + category.Key, category.Value.Select(x => (RedisValue)x).ToArray());
			}

			var productTags = _db.ProductTags.ToList();

			// tag:1:products
			foreach (int tag in tags.Select(x => x.Key))
			{
				var productIds = productTags.Where(x => x.TagId == tag).Select(x => x.ProductId).ToArray();

				_redis.GetDatabase().KeyDelete("tag:" + tag + ":products");

				if (productIds.Any())
				{
					_redis.GetDatabase().SetAdd("tag:" + tag + ":products", productIds.Select(x => (RedisValue)x).ToArray());
				}
			}
		}

		private void InitBestsellers()
		{
			SortedSetEntry[] data = _db.Products.Include(x => x.OrderedItems)
				// select from database
				.Select(x => new { Product = x, Orders = x.OrderedItems.Sum(y => y.Count) }).ToList()

				// project to sortedset
				.Select(x => new SortedSetEntry(JsonConvert.SerializeObject(new ProductBase
				{
					ProductId = x.Product.ProductId,
					Identifier = x.Product.Identifier,
					Title = x.Product.Title
				}, new JsonSerializerSettings()
				{
					ReferenceLoopHandling = ReferenceLoopHandling.Ignore
				}), (double)x.Orders)).ToArray();

			_redis.GetDatabase().SortedSetAdd("products:bestsellers", data);
		}

		private void InitIdentifierMaps(List<Product> products)
		{
			var batch = _redis.GetDatabase().CreateBatch();

			foreach (var product in products)
			{
				batch.StringSetAsync("mapping:product:identifier-to-id:" + product.Identifier, product.ProductId);
				batch.StringSetAsync("mapping:product:id-to-identifier:" + product.ProductId, product.Identifier);
			}

			batch.Execute();
		}

		private void InitPostalCodes()
		{
			SortedSetEntry[] data = _db.PostalCodes.ToList().Select(x => new SortedSetEntry((RedisValue)x.Name, Convert.ToDouble(x.Code))).ToArray();

			_redis.GetDatabase().SortedSetAdd("postalcodes", data);
		}
	}
}

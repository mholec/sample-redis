using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using RedisEshop.Entities;
using RedisEshop.ViewModels;
using StackExchange.Redis;
using Order = StackExchange.Redis.Order;

namespace RedisEshop.DataServices.WithRedis
{
	/// <summary>
	/// Služba pro komunikaci s Redis serverem
	/// </summary>
	public class RedisService
	{
		private readonly ConnectionMultiplexer _redis;

		public RedisService(ConnectionMultiplexer redis)
		{
			this._redis = redis;
		}

		public List<Product> LatestProducts(int count)
		{
			string keyName = "products:latest";

			string[] data = _redis.GetDatabase().ListRange(keyName, 0, 10).Select(x => (string)x).ToArray();

			return data.Select(JsonConvert.DeserializeObject<Product>).ToList();
		}

		public int[] LatestProductIds(int count)
		{
			string keyName = "products:latest-ids";

			RedisValue[] data = _redis
				.GetDatabase()
				.ListRange(keyName, 0, 10);

			return data.Select(x => (int)x).ToArray();
		}

		public int[] RandomProducts(int count)
		{
			string keyName = "products:all";

			RedisValue[] data = _redis
				.GetDatabase()
				.SetRandomMembers(keyName, count);

			return data.Select(x => (int)x).ToArray();
		}

		public int[] ProductsByTags(params int[] tags)
		{
			// seznam všech typů tagů (families)
			int[] families = _redis.GetDatabase().SetMembers("tagFamilies").Select(x => (int)x).ToArray();

			// náhodný dočasný klíč a uložení do redis
			string allTagsTempKey = Guid.NewGuid().ToString();
			_redis.GetDatabase().SetAdd(allTagsTempKey, tags.Select(x => (RedisValue)x).ToArray());

			List<string> familyKeys = new List<string>();
			foreach (int family in families)
			{
				string[] familyTagKeys = _redis.GetDatabase()
					.SetCombine(SetOperation.Intersect, "tagFamilies:" + family, allTagsTempKey) // najdu průnik tagů v dané family
					.Select(x => "tag:" + x + ":products").ToArray(); // z těchto tagů si vytvořím klíče pro další práci

				// dočasný klíč pro konkrétní family (pod klíčem bude seznam článků)
				string randomKeyFamily = allTagsTempKey + ":" + family;

				// uložím do redis UNION ProductId pro všechny tagy z dané family
				if (familyTagKeys.Any())
				{
					familyKeys.Add(randomKeyFamily);
					_redis.GetDatabase().SetCombineAndStore(SetOperation.Union, randomKeyFamily, familyTagKeys.Select(x => (RedisKey)x).ToArray());
				}
			}

			// průnik mezi families
			int[] articleIds = _redis.GetDatabase()
				.SetCombine(SetOperation.Intersect, familyKeys.Select(x => (RedisKey)x).ToArray())
				.Select(x => (int)x).ToArray();

			// úklid v redis
			_redis.GetDatabase().KeyDelete(allTagsTempKey);
			_redis.GetDatabase().KeyDelete(familyKeys.Select(x => (RedisKey)x).ToArray());

			return articleIds;
		}

		public Dictionary<Product, double> Bestsellers(int count)
		{
			string keyName = "products:bestsellers";

			SortedSetEntry[] data = _redis.GetDatabase().SortedSetRangeByRankWithScores(keyName, 0, count - 1, Order.Descending);

			return data.ToDictionary(x => JsonConvert.DeserializeObject<Product>(x.Element), x => x.Score);
		}
		public Dictionary<int, double> MostViewedProducts(int count)
		{
			string keyName = "products:visits";

			var data = _redis
				.GetDatabase()
				.SortedSetRangeByRankWithScores(keyName, 0, count - 1, Order.Descending);

			return data.ToDictionary(x => (int)x.Element, x => x.Score);
		}

		public int AddProductVisit(int productId)
		{
			string keyName = "products:" + productId + ":visits";

			var scoreFromString = _redis.GetDatabase().StringIncrement(keyName, 1);
			_redis.GetDatabase().SortedSetIncrement("products:visits", productId, 1);

			return (int)scoreFromString;
		}

		public void CacheSetProduct(Product product)
		{
			string keyName = $"product:{product.ProductId}:object";

			var serialized = JsonConvert.SerializeObject(product);

			_redis
				.GetDatabase()
				.StringSet(keyName, serialized);
		}

		public Product CacheGetProduct(int productId)
		{
			string keyName = $"product:{productId}:object";

			string serialized = _redis
				.GetDatabase()
				.StringGet(keyName);

			Product product = JsonConvert.DeserializeObject<Product>(serialized);

			return product;
		}

		public int? GetProductIdByIdenfitier(string identifier)
		{
			string keyName = "mapping:product:identifier-to-id:" + identifier;

			RedisValue result = _redis.GetDatabase().StringGet(keyName);

			return (result.IsInteger) ? (int)result : default(int?);
		}

		public bool TryAddNewsletterSubscriber(string email)
		{
			string keyName = "newsletter:subscribers";

			bool result = _redis.GetDatabase().SetAdd(keyName, email);

			return result;
		}

		public void QueueNewsletterWelcomeMail(EmailMessageViewModel email)
		{
			string keyName = "emails:newsletter-welcome";

			string value = JsonConvert.SerializeObject(email);

			_redis.GetDatabase().ListLeftPush(keyName, value);
		}

		public EmailMessageViewModel DequeueNewsletterWelcomeMail()
		{
			string keyName = "emails:newsletter-welcome";

			var value = _redis.GetDatabase().ListRightPop(keyName);

			if (value.IsNullOrEmpty)
			{
				return null;
			}

			EmailMessageViewModel email = JsonConvert.DeserializeObject<EmailMessageViewModel>(value);

			return email;
		}

		public List<PostalCode> GetPostalCodes(int code)
		{
			var data = _redis.GetDatabase()
				.SortedSetRangeByScoreWithScores("postalcodes", code, code, Exclude.None);

			return data.Select(x => new PostalCode()
			{
				Code = (int) x.Score,
				Name = x.Element
			}).ToList();
		}

		public List<ShoppingCartItemViewModel> GetShoppingCartItems(Guid id)
		{
			HashEntry[] items = _redis.GetDatabase().HashGetAll("shoppingCart:" + id);

			return items.Select(x => new ShoppingCartItemViewModel()
			{
				Name = x.Name,
				Items = int.Parse(x.Value)
			}).ToList();
		}

		public void AddShoppingCartItem(Guid id, string identifier, int items)
		{
			_redis.GetDatabase().HashIncrement("shoppingCart:" + id, identifier, items, CommandFlags.FireAndForget);
		}

		public bool HasShoppingCartItem(Guid id, string identifier)
		{
			return _redis.GetDatabase().HashExists("shoppingCart:" + id, identifier);
		}

		public int CountShoppingCartItem(Guid id, string identifier)
		{
			return (int)_redis.GetDatabase().HashGet("shoppingCart:" + id, identifier);
		}

		public void RemoveShoppingCartItem(Guid id, string identifier)
		{
			_redis.GetDatabase().HashDelete("shoppingCart:" + id, identifier, CommandFlags.FireAndForget);
		}
	}
}
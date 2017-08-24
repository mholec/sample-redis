using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RedisEshop.Dto;
using RedisEshop.Entities;
using RedisEshop.ViewModels;
using StackExchange.Redis;
using Order = StackExchange.Redis.Order;

namespace RedisEshop.DataServices
{
	/// <summary>
	/// Služba pro komunikaci s Redis serverem
	/// </summary>
	public class RedisService
	{
		private readonly ConnectionMultiplexer _redis;

		public RedisService(ConnectionMultiplexer redis)
		{
			_redis = redis;
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

		public Dictionary<ProductBase, double> Bestsellers(int count)
		{
			string keyName = "products:bestsellers";

			SortedSetEntry[] data = _redis.GetDatabase().SortedSetRangeByRankWithScores(keyName, 0, count - 1, Order.Descending);

			return data.ToDictionary(x => JsonConvert.DeserializeObject<ProductBase>(x.Element), x => x.Score);
		}

		public void UpdateBestsellers(ProductBase product, int count)
		{
			string keyName = "products:bestsellers";

			_redis.GetDatabase().SortedSetIncrement(keyName, JsonConvert.SerializeObject(product), count);
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

		// todo: možná to má rovnou vracet dictionary
		public List<ShoppingCartItemViewModel> GetShoppingCartItems(Guid id)
		{
			HashEntry[] items = _redis.GetDatabase().HashGetAll("shoppingCart:" + id);

			return items.Select(x => new ShoppingCartItemViewModel()
			{
				Name = x.Name,
				Items = int.Parse(x.Value)
			}).ToList();
		}

		public void AddShoppingCartItem(Guid cartId, string identifier, int amount)
		{
			_redis.GetDatabase().HashIncrement("shoppingCart:" + cartId, identifier, amount, CommandFlags.FireAndForget);
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

		public void RemoveShoppingCart(Guid id)
		{
			_redis.GetDatabase().KeyDelete("shoppingCart:" + id, CommandFlags.FireAndForget);
		}

		public bool GetLock(string name)
		{
			return _redis.GetDatabase().StringSet("redis-locks:" + name, 1, TimeSpan.FromSeconds(60), When.NotExists);
		}

		public bool ReleaseLock(string name)
		{
			return _redis.GetDatabase().KeyDelete("redis-locks:" + name);
		}

		public void Extras()
		{
			// PIPELINING
			Task<bool> a = _redis.GetDatabase().StringSetAsync("pipeline:test:A", DateTime.Now.ToString(CultureInfo.InvariantCulture));
			Task<bool> b = _redis.GetDatabase().StringSetAsync("pipeline:test:B", DateTime.Now.ToString(CultureInfo.InvariantCulture));
			_redis.GetDatabase().Wait(a);
			_redis.GetDatabase().Wait(b);
			_redis.GetDatabase().StringSetAsync("pipeline:test:C", DateTime.Now.ToString(CultureInfo.InvariantCulture));
			_redis.GetDatabase().StringSetAsync("pipeline:test:D", DateTime.Now.ToString(CultureInfo.InvariantCulture));

			// TRANSACTIONS
			// - balík požadavků, který se pošle na server najednou
			// - balík požadavků, který se na serveru zpracuje najednou (sekvenčně)
			ITransaction transaction = _redis.GetDatabase().CreateTransaction();
			transaction.StringSetAsync("transaction:test:A", DateTime.Now.ToString(CultureInfo.InvariantCulture));
			transaction.StringSetAsync("transaction:test:B", DateTime.Now.ToString(CultureInfo.InvariantCulture));
			transaction.StringSetAsync("transaction:test:C", DateTime.Now.ToString(CultureInfo.InvariantCulture));
			transaction.StringSetAsync("transaction:test:D", DateTime.Now.ToString(CultureInfo.InvariantCulture));
			transaction.Execute();

			// BATCH
			// - balík požadavků, který se pošle na server najednou (vše)
			// - nejedná se o transakci, která by zaručila vykonání na straně serveru sekvenčně
			// - jedná se ale o dávku z pohledu multiplexeru (dávka nebude narušena ničím jiným odeslaným z tohoto projektu)
			IBatch batch = _redis.GetDatabase().CreateBatch("ss");
			batch.StringSetAsync("batch:test:A", DateTime.Now.ToString(CultureInfo.InvariantCulture));
			batch.StringSetAsync("batch:test:B", DateTime.Now.ToString(CultureInfo.InvariantCulture));
			batch.StringSetAsync("batch:test:C", DateTime.Now.ToString(CultureInfo.InvariantCulture));
			batch.StringSetAsync("batch:test:D", DateTime.Now.ToString(CultureInfo.InvariantCulture));
			batch.Execute();
		}

		public void SubscribeToPageViews()
		{
			string notificationChannel = "__keyspace@" + _redis.GetDatabase().Database + "__:*";

			var subscriber = _redis.GetSubscriber();
			subscriber.Subscribe(notificationChannel, (channel, notificationType) =>
			{
				string key = GetKeyForKeyspaceNotification(channel);
				if (notificationType == "incrby")
				{
					int visits = (int)_redis.GetDatabase().StringGet(key);
				}
			});
		}

		private static string GetKeyForKeyspaceNotification(string channel)
        {
            var index = channel.IndexOf(':');
            if (index >= 0 && index < channel.Length - 1)
                return channel.Substring(index + 1);

            //we didn't find the delimeter, so just return the whole thing
            return channel;
        }
	}
}
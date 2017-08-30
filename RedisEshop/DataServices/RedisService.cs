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
			string[] data = _redis.GetDatabase().ListRange("products:latest", 0, count).Select(x => (string)x).ToArray();

			return data.Select(JsonConvert.DeserializeObject<Product>).ToList();
		}

		public int[] LatestProductIds(int count)
		{
			RedisValue[] data = _redis.GetDatabase().ListRange("products:latest-ids", 0, 10);

			return data.Select(x => (int)x).ToArray();
		}

		public int[] RandomProducts(int count)
		{
			RedisValue[] data = _redis.GetDatabase().SetRandomMembers("products:all", count);

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
			SortedSetEntry[] data = _redis.GetDatabase().SortedSetRangeByRankWithScores("products:bestsellers", 0, count - 1, Order.Descending);

			return data.ToDictionary(x => JsonConvert.DeserializeObject<ProductBase>(x.Element), x => x.Score);
		}

		public void UpdateBestsellers(ProductBase product, int count)
		{
			_redis.GetDatabase().SortedSetIncrement("products:bestsellers", JsonConvert.SerializeObject(product), count);
		}

		public Dictionary<int, double> MostViewedProducts(int count)
		{
			var data = _redis
				.GetDatabase()
				.SortedSetRangeByRankWithScores("products:visits", 0, count - 1, Order.Descending);

			return data.ToDictionary(x => (int)x.Element, x => x.Score);
		}

		public int AddProductVisit(int productId)
		{
			//long scoreFromString = _redis.GetDatabase().StringIncrement($"products:{productId}:visits", 1);
			double scoreFromString =_redis.GetDatabase().SortedSetIncrement("products:visits", productId, 1);

			return (int)scoreFromString;
		}

		public void CacheSetProduct(Product product)
		{
			string serialized = JsonConvert.SerializeObject(product);

			_redis.GetDatabase().StringSet($"product:{product.ProductId}:object", serialized);
		}

		public Product CacheGetProduct(int productId)
		{
			string serialized = _redis.GetDatabase().StringGet($"product:{productId}:object");

			Product product = JsonConvert.DeserializeObject<Product>(serialized);

			return product;
		}

		public int? GetProductIdByIdenfitier(string identifier)
		{
			RedisValue result = _redis.GetDatabase().StringGet($"mapping:product:identifier-to-id:{identifier}");

			return (int)result;
		}

		public bool TryAddNewsletterSubscriber(string email)
		{
			bool result = _redis.GetDatabase().SetAdd("newsletter:subscribers", email);

			return result;
		}

		public void QueueNewsletterWelcomeMail(EmailMessageViewModel email)
		{
			string value = JsonConvert.SerializeObject(email);

			_redis.GetDatabase().ListLeftPush("emails:newsletter-welcome", value);
		}

		public EmailMessageViewModel DequeueNewsletterWelcomeMail()
		{
			RedisValue value = _redis.GetDatabase().ListRightPop("emails:newsletter-welcome");

			if (value.IsNullOrEmpty)
			{
				return null;
			}

			EmailMessageViewModel email = JsonConvert.DeserializeObject<EmailMessageViewModel>(value);

			return email;
		}

		public List<ShoppingCartItemViewModel> GetShoppingCartItems(Guid id)
		{
			HashEntry[] items = _redis.GetDatabase().HashGetAll($"shoppingCart:{id}");

			return items.Select(x => new ShoppingCartItemViewModel()
			{
				Name = x.Name,
				Items = int.Parse(x.Value)
			}).ToList();
		}

		public void AddShoppingCartItem(Guid cartId, string identifier, int amount)
		{
			_redis.GetDatabase().HashIncrement($"shoppingCart:{cartId}", identifier, amount, CommandFlags.FireAndForget);
		}

		public bool HasShoppingCartItem(Guid cartId, string identifier)
		{
			return _redis.GetDatabase().HashExists($"shoppingCart:{cartId}", identifier);
		}

		public int CountShoppingCartItem(Guid cartId, string identifier)
		{
			return (int)_redis.GetDatabase().HashGet($"shoppingCart:{cartId}", identifier);
		}

		public void RemoveShoppingCartItem(Guid cartId, string identifier)
		{
			_redis.GetDatabase().HashDelete($"shoppingCart:{cartId}", identifier, CommandFlags.FireAndForget);
		}

		public void RemoveShoppingCart(Guid cartId)
		{
			_redis.GetDatabase().KeyDelete($"shoppingCart:{cartId}", CommandFlags.FireAndForget);
		}

		public bool GetLock(string name)
		{
			return _redis.GetDatabase().StringSet($"redis-locks:{name}", 1, TimeSpan.FromSeconds(60), When.NotExists);
		}

		public bool ReleaseLock(string name)
		{
			return _redis.GetDatabase().KeyDelete($"redis-locks:{name}");
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

            return channel;
        }
	}
}
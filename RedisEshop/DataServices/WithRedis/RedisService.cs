using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using RedisEshop.Entities;
using StackExchange.Redis;

namespace RedisEshop.DataServices.WithRedis
{
	/// <summary>
	/// Služba pro komunikaci s Redis serverem
	/// </summary>
	public class RedisService
	{
		private readonly ConnectionMultiplexer _connectionMultiplexer;

		public RedisService(ConnectionMultiplexer connectionMultiplexer)
		{
			this._connectionMultiplexer = connectionMultiplexer;
		}

		public int[] LatestProducts(int count)
		{
			string keyName = "products:dates";

			RedisValue[] data = _connectionMultiplexer
				.GetDatabase()
				.SortedSetRangeByRank(keyName, 0, count, Order.Descending);

			return data.Select(x=> (int)x).ToArray();
		}

		public int[] RandomProducts(int count)
		{
			string keyName = "products:all";

			RedisValue[] data = _connectionMultiplexer
				.GetDatabase()
				.SetRandomMembers(keyName, count);

			return data.Select(x=> (int)x).ToArray();
		}

		public int[] ProductsByTags(params int[] tags)
		{
			// seznam všech typů tagů (families)
			int[] families = _connectionMultiplexer.GetDatabase().SetMembers("tagFamilies").Select(x => (int)x).ToArray();

			// náhodný dočasný klíč a uložení do redis
			string allTagsTempKey = Guid.NewGuid().ToString();
			_connectionMultiplexer.GetDatabase().SetAdd(allTagsTempKey, tags.Select(x => (RedisValue)x).ToArray());

			List<string> familyKeys = new List<string>();
			foreach (int family in families)
			{
				string[] familyTagKeys = _connectionMultiplexer.GetDatabase()
					.SetCombine(SetOperation.Intersect, "tagFamilies:" + family, allTagsTempKey) // najdu průnik tagů v dané family
					.Select(x => "tag:" + x  + ":products").ToArray(); // z těchto tagů si vytvořím klíče pro další práci

				// dočasný klíč pro konkrétní family (pod klíčem bude seznam článků)
				string randomKeyFamily = allTagsTempKey + ":" + family;

				// uložím do redis UNION ProductId pro všechny tagy z dané family
				if (familyTagKeys.Any())
				{
					familyKeys.Add(randomKeyFamily);
					_connectionMultiplexer.GetDatabase().SetCombineAndStore(SetOperation.Union, randomKeyFamily, familyTagKeys.Select(x => (RedisKey) x).ToArray());
				}
			}

			// průnik mezi families
			int[] articleIds = _connectionMultiplexer.GetDatabase()
				.SetCombine(SetOperation.Intersect, familyKeys.Select(x => (RedisKey)x).ToArray())
				.Select(x => (int) x).ToArray();

			// úklid v redis
			_connectionMultiplexer.GetDatabase().KeyDelete(allTagsTempKey);
			_connectionMultiplexer.GetDatabase().KeyDelete(familyKeys.Select(x => (RedisKey) x).ToArray());

			return articleIds;
		}

		public Dictionary<int, double> TopRatedProducts(int count)
		{
			string keyName = "products:likes";

			SortedSetEntry[] data = _connectionMultiplexer
				.GetDatabase()
				.SortedSetRangeByRankWithScores(keyName, 0, count, Order.Descending);

			return data.ToDictionary(x => (int) x.Element, x => x.Score);
		}
		public int[] MostViewedProducts(int count)
		{
			string keyName = "products:visits";

			RedisValue[] data = _connectionMultiplexer
				.GetDatabase()
				.SortedSetRangeByRank(keyName, 0, count, Order.Descending);

			return data.Select(x=> (int)x).ToArray();
		}

		public int AddProductVisit(int productId)
		{
			//string keyName = "products:" + productId + ":visits";

			//var score = _connectionMultiplexer
			//	.GetDatabase()
			//	.StringIncrement(keyName, 1);

			//return (int)score;

			string keyName = "products:visits";

			var score = _connectionMultiplexer
				.GetDatabase()
				.SortedSetIncrement(keyName, productId, 1);

			return (int)score;
		}

		public void CacheSetProduct(Product product)
		{
			string keyName = $"product:{product.ProductId}:object";

			var serialized = JsonConvert.SerializeObject(product);

			_connectionMultiplexer
				.GetDatabase()
				.StringSet(keyName, serialized);
		}

		public Product CacheGetProduct(int productId)
		{
			string keyName = $"product:{productId}:object";

			string serialized = _connectionMultiplexer
				.GetDatabase()
				.StringGet(keyName);

			Product product = JsonConvert.DeserializeObject<Product>(serialized);

			return product;
		}
	}
}
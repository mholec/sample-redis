using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RedisEshop.DataServices;
using RedisEshop.Entities;
using StackExchange.Redis;

namespace RedisEshop.Maintenance
{
	public class RedisCleanup
	{
		private readonly ConnectionMultiplexer _multiplexer;
		private readonly CommonListService _commonListService;
		private readonly AppDbContext _db;

		public RedisCleanup(ConnectionMultiplexer multiplexer, CommonListService commonListService, AppDbContext db)
		{
			_multiplexer = multiplexer;
			_commonListService = commonListService;
			_db = db;
		}

		public void PrepareData()
		{
			// tagFamilies
			int[] tagFamilies = _commonListService.TagTypes().Select(x => (int)x.Key).ToArray();
			_multiplexer.GetDatabase().KeyDelete("tagFamilies");
			_multiplexer.GetDatabase().SetAdd("tagFamilies", tagFamilies.Select(x => (RedisValue)x).ToArray());

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
				_multiplexer.GetDatabase().KeyDelete("tagFamilies:" + category.Key);
				_multiplexer.GetDatabase().SetAdd("tagFamilies:" + category.Key, category.Value.Select(x => (RedisValue)x).ToArray());
			}

			var productTags = _db.ProductTags.ToList();

			// tag:1:products
			foreach (int tag in tags.Select(x=> x.Key))
			{
				var productIds = productTags.Where(x => x.TagId == tag).Select(x=> x.ProductId).ToArray();

				_multiplexer.GetDatabase().KeyDelete("tag:" + tag + ":products");

				if (productIds.Any())
				{
					_multiplexer.GetDatabase().SetAdd("tag:" + tag + ":products", productIds.Select(x => (RedisValue)x).ToArray());
				}
			}
		}
	}
}

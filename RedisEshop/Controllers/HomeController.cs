using System;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using RedisEshop.Serialization;
using RedisEshop.ViewModels;

namespace RedisEshop.Controllers
{
	public class HomeController : Controller
	{
		private readonly IDistributedCache _distributedCache;
		private readonly IDistributedCacheSerializer<StatusViewModel> _serializer;

		public HomeController(IDistributedCache distributedCache)
		{
			this._distributedCache = distributedCache;
			this._serializer = new ProtobufDistributedCacheSerializer<StatusViewModel>();
		}

		[Route("")]
		[Route("home/quickstart")]
		public IActionResult QuickStart()
		{
			return View();
		}

		[Route("home/distributed-cache")]
		public IActionResult DistributedCache()
		{
			// from cache
			byte[] item = _distributedCache.Get("status");
			if (item != null)
			{
				return View(_serializer.Deserialize(item));
			}

			// to cache
			StatusViewModel status = new StatusViewModel()
			{
				Name = "Jack Daniels",
				Created = DateTime.Now
			};

			byte[] bytes = _serializer.Serialize(status);
			_distributedCache.Set("status", bytes, new DistributedCacheEntryOptions
			{
				AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(15)
			});

			return View(status);
		}

	}
}

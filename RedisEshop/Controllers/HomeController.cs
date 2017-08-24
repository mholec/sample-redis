using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using RedisEshop.DataServices;
using RedisEshop.Serialization;
using RedisEshop.ViewModels;

namespace RedisEshop.Controllers
{
	public class HomeController : Controller
	{
		private readonly IDistributedCache _distributedCache;
		private readonly IDistributedCacheSerializer<StatusViewModel> _serializer;
		private readonly RedisService _redisService;

		public HomeController(IDistributedCache distributedCache, IDistributedCacheSerializer<StatusViewModel> serializer, RedisService redisService)
		{
			_distributedCache = distributedCache;
			_serializer = serializer;
			_redisService = redisService;
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

		[Route("home/pipelining")]
		public IActionResult Pipelining()
		{
			_redisService.Extras();

			return RedirectToAction("QuickStart");
		}
	}
}

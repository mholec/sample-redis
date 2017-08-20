using System;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using RedisEshop.ViewModels;

namespace RedisEshop.Controllers
{
	public class HomeController : Controller
	{
		private readonly IDistributedCache _distributedCache;

		public HomeController(IDistributedCache distributedCache)
		{
			this._distributedCache = distributedCache;
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
			var item = _distributedCache.Get("status");
			if (item != null)
			{
				return View(JsonConvert.DeserializeObject<StatusViewModel>(Encoding.UTF8.GetString(item)));
			}

			// to cache
			StatusViewModel status = new StatusViewModel()
			{
				Name = "Jack Daniels",
				Created = DateTime.Now
			};

			byte[] bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(status));
			_distributedCache.Set("status", bytes, new DistributedCacheEntryOptions
			{
				AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(15)
			});

			return View(status);
		}

	}
}

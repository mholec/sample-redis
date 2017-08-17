using Microsoft.AspNetCore.Mvc;
using RedisEshop.Maintenance;

namespace RedisEshop.Controllers
{
	public class SystemController : Controller
	{
		private readonly RedisCleanup _redisCleanup;

		public SystemController(RedisCleanup redisCleanup)
		{
			_redisCleanup = redisCleanup;
		}

		[Route("system/redis-cleanup")]
		public IActionResult Cleanup()
		{
			_redisCleanup.PrepareData();

			return Content("OK");
		}
	}
}
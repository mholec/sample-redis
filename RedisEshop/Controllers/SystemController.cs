using Microsoft.AspNetCore.Mvc;
using RedisEshop.Maintenance;

namespace RedisEshop.Controllers
{
	public class SystemController : Controller
	{
		private readonly RedisBackgroundServices _redisCleanup;

		public SystemController(RedisBackgroundServices redisCleanup)
		{
			_redisCleanup = redisCleanup;
		}

		[Route("system/redis-cleanup")]
		public IActionResult Cleanup()
		{
			_redisCleanup.RestoreRedis();

			return Content("OK");
		}
	}
}
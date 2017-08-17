using Microsoft.AspNetCore.Mvc;

namespace RedisEshop.Controllers
{
	public class OutputCacheController : Controller
	{
		public OutputCacheController()
		{
		}

		[Route("")]
		[ResponseCache(Duration = 20)]
		public IActionResult Index()
		{
			return View();
		}

	}
}

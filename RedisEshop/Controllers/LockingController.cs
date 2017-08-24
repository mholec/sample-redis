using Microsoft.AspNetCore.Mvc;
using RedisEshop.DataServices;

namespace RedisEshop.Controllers
{
	public class LockingController : Controller
	{
		private readonly IEshopDataService _eshopDataService;

		public LockingController(IEshopDataService eshopDataService)
		{
			_eshopDataService = eshopDataService;
		}

		[Route("locking/index")]
		[HttpGet]
		public IActionResult Index()
		{
			return View();
		}

		[Route("locking/simple-lock")]
		[HttpGet]
		public IActionResult SimpleLock(int code, string name)
		{
			_eshopDataService.AddPostalCodeWithSimpleLock(code, name);

			return RedirectToAction("Index");
		}

		[Route("locking/redlock")]
		[HttpGet]
		public IActionResult RedLock(int code, string name)
		{
			_eshopDataService.AddPostalCodeWithRedisLock(code, name);

			return RedirectToAction("Index");
		}
	}
}

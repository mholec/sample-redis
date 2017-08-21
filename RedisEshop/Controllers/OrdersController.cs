using Microsoft.AspNetCore.Mvc;
using RedisEshop.DataServices;
using RedisEshop.ViewModels;

namespace RedisEshop.Controllers
{
	public class OrdersController : Controller
	{
		private readonly IEshopDataService _eshopDataService;

		public OrdersController(IEshopDataService eshopDataService)
		{
			_eshopDataService = eshopDataService;
		}

		[Route("orders/process")]
		[HttpGet]
		public IActionResult Process()
		{
			var order = _eshopDataService.CreateOrderFromShoppingCart();

			return View(order);
		}

		[Route("orders/process")]
		[HttpPost]
		public IActionResult Process(OrderViewModel inputModel)
		{
			return View();
		}

		[Route("orders/municipalities/{postalCode}")]
		public IActionResult GetMunicipalities(string postalCode)
		{
			var result = _eshopDataService.GetMunicipalities(postalCode);

			return Ok(result);
		}
	}
}
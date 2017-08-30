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
			_eshopDataService.ProcessOrder(inputModel);

			return RedirectToAction("Home", "Products");
		}
	}
}
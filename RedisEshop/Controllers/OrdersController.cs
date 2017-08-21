using Microsoft.AspNetCore.Mvc;
using RedisEshop.DataServices;

namespace RedisEshop.Controllers
{
	public class OrdersController : Controller
	{
		private readonly IEshopDataService _eshopDataService;

		public OrdersController(IEshopDataService eshopDataService)
		{
			_eshopDataService = eshopDataService;
		}

		[Route("orders/municipalities/{postalCode}")]
		public IActionResult GetMunicipalities(string postalCode)
		{
			var result = _eshopDataService.GetMunicipalities(postalCode);

			return Ok(result);
		}

		public IActionResult AddToShoppingCart(string identifier)
		{
			_eshopDataService.AddToShoppingCart(identifier);

			return RedirectToAction("Detail", "Products", new { identifier });
		}
	}
}
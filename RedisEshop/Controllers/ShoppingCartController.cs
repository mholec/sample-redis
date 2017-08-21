using Microsoft.AspNetCore.Mvc;
using RedisEshop.DataServices;

namespace RedisEshop.Controllers
{
	public class ShoppingCartController : Controller
	{
		private readonly IEshopDataService _eshopDataService;

		public ShoppingCartController(IEshopDataService eshopDataService)
		{
			this._eshopDataService = eshopDataService;
		}

		[Route("shoppingcart/add/{identifier}")]
		public IActionResult AddToShoppingCart(string identifier)
		{
			_eshopDataService.AddToShoppingCart(identifier);

			return RedirectToAction("Detail", "Products", new { identifier });
		}

		[Route("shoppingcart/remove/{identifier}")]
		public IActionResult RemoveFromShoppingCart(string identifier)
		{
			_eshopDataService.RemoveFromShoppingCart(identifier);

			return RedirectToAction("Detail", "Products", new { identifier });
		}
	}
}
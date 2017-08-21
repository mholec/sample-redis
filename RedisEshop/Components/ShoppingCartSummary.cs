using Microsoft.AspNetCore.Mvc;
using RedisEshop.DataServices;

namespace RedisEshop.Components
{
    public class ShoppingCartSummary : ViewComponent
    {
	    private readonly IEshopDataService _eshopDataService;

	    public ShoppingCartSummary(IEshopDataService eshopDataService)
	    {
		    _eshopDataService = eshopDataService;
	    }

	    public IViewComponentResult Invoke()
	    {
		    var data = _eshopDataService.GetShoppingCart();

		    return View(data);
	    }
    }
}

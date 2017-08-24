using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using RedisEshop.DataServices;
using RedisEshop.ViewModels;
using RedisEshop.ViewModels.Pages;

namespace RedisEshop.Controllers
{
	public class ProductsController : Controller
	{
		private readonly IEshopDataService _eshopDataService;

		public ProductsController(IEshopDataService eshopDataService)
		{
			_eshopDataService = eshopDataService;
		}

		[Route("products/home")]
		public IActionResult Home()
		{
			var pageModel = new ProductHomePageModel
			{
				LatestProducts = _eshopDataService.GetLatestProducts(10),
				Bestsellers = _eshopDataService.Bestsellers(5),
				MostViewed = _eshopDataService.GetMostViewedProducts(5)
			};

			return View(pageModel);
		}

		[Route("products/index")]
		public IActionResult Index(List<Item> tag = null)
		{
			int[] items = tag?.Where(x => x.Selected != null).Select(x => x.Id).ToArray() ?? new int[0];

			var pageModel = new ProductListPageModel
			{
				Products = _eshopDataService.GetProductsByTags(items),
				SelectedTags = items
			};

			return View(pageModel);
		}

		[Route("products/detail/{identifier}")]
		public IActionResult Detail(string identifier)
		{
			var result = _eshopDataService.GetProduct(identifier);

			return View(result);
		}

		[Route("products/visit/{productId}")]
		public int Visit(int productId)
		{
			return _eshopDataService.AddAndGetProductVisits(productId);
		}
	}
}
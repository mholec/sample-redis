using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using RedisEshop.DataServices;

namespace RedisEshop.Controllers
{
	public class NewsletterController : Controller
	{
		private readonly IEshopDataService _eshopDataService;

		public NewsletterController(IEshopDataService eshopDataService)
		{
			_eshopDataService = eshopDataService;
		}

		[HttpPost]
		[Route("newsletter/subscribe")]
		public IActionResult Subscribe(string email)
		{
			(string alertType, string message) result = _eshopDataService.NewsletterSubscribe(email);

			ViewBag.Result = result.message;
			ViewBag.AlertType = result.alertType;

			return View("Status");
		}

		[Route("newsletter/send")]
		public IActionResult SendNewsletters()
		{
			IEnumerable<string> emails = _eshopDataService.SendNewsletters();

			return Content(string.Join(", ", emails));
		}
	}
}
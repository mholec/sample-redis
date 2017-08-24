using Microsoft.AspNetCore.Mvc;
using RedisEshop.DataServices;

namespace RedisEshop.Controllers
{
    public class SubscriberController : Controller
    {
	    private readonly RedisService _redisService;

	    public SubscriberController(RedisService redisService)
	    {
		    _redisService = redisService;
	    }

		[Route("subscriber/subscribe")]
	    public IActionResult Subscribe()
	    {
		    _redisService.SubscribeToPageViews();

		    return Ok();
	    }
    }
}

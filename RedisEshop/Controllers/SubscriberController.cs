using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using RedisEshop.DataServices.WithRedis;

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

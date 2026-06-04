using Microsoft.AspNetCore.Mvc;
using Trading.Api.Services;

namespace Trading.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PricesController : ControllerBase
{
    private readonly IPriceCache _cache;

    public PricesController(IPriceCache cache) => _cache = cache;

    [HttpGet]
    public IActionResult Get() => Ok(_cache.GetAll());
}

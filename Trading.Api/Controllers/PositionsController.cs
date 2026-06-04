using Microsoft.AspNetCore.Mvc;
using Trading.Api.Services;

namespace Trading.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PositionsController : ControllerBase
{
    private readonly ITradeService _trades;
    private readonly IPriceCache _cache;

    public PositionsController(ITradeService trades, IPriceCache cache)
    {
        _trades = trades;
        _cache = cache;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var tradeList = await _trades.GetTradesAsync(500, ct);
        return Ok(_trades.ComputePositions(tradeList, _cache.GetAll()));
    }
}

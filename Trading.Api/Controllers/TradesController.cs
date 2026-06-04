using Microsoft.AspNetCore.Mvc;
using Trading.Api.Services;

namespace Trading.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TradesController : ControllerBase
{
    private readonly ITradeService _trades;

    public TradesController(ITradeService trades) => _trades = trades;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int limit = 100, CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 500);
        return Ok(await _trades.GetTradesAsync(limit, ct));
    }
}

using Microsoft.AspNetCore.Mvc;
using Trading.Api.Models;
using Trading.Api.Services;

namespace Trading.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ITradeService _trades;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(ITradeService trades, ILogger<OrdersController> logger)
    {
        _trades = trades;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<PlaceOrderResponse>> Post([FromBody] PlaceOrderRequest request, CancellationToken ct)
    {
        var result = await _trades.PlaceOrderAsync(request, ct);
        if (!result.Success)
        {
            _logger.LogWarning("Order rejected: {Message}", result.Message);
            return BadRequest(result);
        }
        return Created(string.Empty, result);
    }
}

namespace Trading.Api.Models;

public class TradeDto
{
    public string TradeId { get; set; } = "";
    public string Symbol { get; set; } = "";
    public string Side { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public string Timestamp { get; set; } = "";
    public string Status { get; set; } = "";
}

public class PlaceOrderRequest
{
    public string Symbol { get; set; } = "";
    public string Side { get; set; } = "";
    public decimal Quantity { get; set; }
}

public class PlaceOrderResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public TradeDto? Trade { get; set; }
}

public class PositionDto
{
    public string Symbol { get; set; } = "";
    public decimal NetQuantity { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal MarketPrice { get; set; }
    public decimal UnrealizedPnL { get; set; }
}

public class HealthResponse
{
    public string Status { get; set; } = "ok";
    public string Timestamp { get; set; } = "";
    public AuthStatusDto Auth { get; set; } = new();
    public WebSocketStatusDto Websocket { get; set; } = new();
}

public class AuthStatusDto
{
    public bool Ok { get; set; }
    public string Message { get; set; } = "";
    public DateTime? FetchedAt { get; set; }
}

public class WebSocketStatusDto
{
    public string State { get; set; } = "Disconnected";
    public string? LastError { get; set; }
    public long MessageCount { get; set; }
}

public class FeedStatusPayload
{
    public string State { get; set; } = "";
    public string? LastError { get; set; }
    public IReadOnlyList<PriceQuoteDto> Prices { get; set; } = Array.Empty<PriceQuoteDto>();
}

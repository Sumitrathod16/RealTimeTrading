namespace Trading.Api.Data.Entities;

public class TradeEntity
{
    public string TradeId { get; set; } = "";
    public string Symbol { get; set; } = "";
    public string Side { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public string Timestamp { get; set; } = "";
    public string Status { get; set; } = "";
}

namespace Trading.Api.Models;

public record PriceQuoteDto(
    string Symbol,
    decimal Price,
    decimal Bid,
    decimal Ask,
    string Timestamp,
    string Direction = "flat");

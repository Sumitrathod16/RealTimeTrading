using System.Collections.Concurrent;
using Trading.Api.Models;

namespace Trading.Api.Services;

public sealed class PriceCache : IPriceCache
{
    private readonly ConcurrentDictionary<string, PriceQuoteDto> _prices = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<PriceQuoteDto> GetAll() =>
        _prices.Values.OrderBy(p => p.Symbol, StringComparer.OrdinalIgnoreCase).ToList();

    public PriceQuoteDto? Get(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol)) return null;
        _prices.TryGetValue(symbol.Trim().ToUpperInvariant(), out var quote);
        return quote;
    }

    public void Update(IEnumerable<PriceQuoteDto> updates)
    {
        foreach (var u in updates)
        {
            var key = u.Symbol.ToUpperInvariant();
            _prices.AddOrUpdate(
                key,
                u,
                (_, prev) =>
                {
                    var direction = u.Price > prev.Price ? "up" : u.Price < prev.Price ? "down" : "flat";
                    return u with { Direction = direction };
                });
        }
    }
}

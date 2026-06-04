using System.Text.Json;
using Trading.Api.Models;

namespace Trading.Api.Services;

public static class PriceMessageParser
{
    public static IReadOnlyList<PriceQuoteDto> Parse(string raw)
    {
        var results = new List<PriceQuoteDto>();
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            var items = root.ValueKind == JsonValueKind.Array
                ? root.EnumerateArray()
                : EnumerateObjects(root);

            foreach (var item in items)
            {
                var quote = ParseItem(item);
                if (quote != null) results.Add(quote);
            }
        }
        catch (JsonException)
        {
            // malformed — skip
        }
        return results;
    }

    private static IEnumerable<JsonElement> EnumerateObjects(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (var key in new[] { "prices", "data", "ticks" })
            {
                if (root.TryGetProperty(key, out var arr) && arr.ValueKind == JsonValueKind.Array)
                    return arr.EnumerateArray().ToArray();
            }
            return new[] { root };
        }
        return Array.Empty<JsonElement>();
    }

    private static PriceQuoteDto? ParseItem(JsonElement item)
    {
        var symbol = GetString(item, "symbol", "Symbol", "instrument", "Instrument", "pair");
        var bid = GetDecimal(item, "bid", "Bid", "b");
        var ask = GetDecimal(item, "ask", "Ask", "a");
        var mid = GetDecimal(item, "price", "Price", "last", "Last", "mid", "Mid");
        var price = mid ?? (bid.HasValue && ask.HasValue ? (bid.Value + ask.Value) / 2 : bid ?? ask);
        if (string.IsNullOrEmpty(symbol) || !price.HasValue) return null;

        return new PriceQuoteDto(
            symbol.ToUpperInvariant(),
            price.Value,
            bid ?? price.Value,
            ask ?? price.Value,
            DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
            "flat");
    }

    private static string? GetString(JsonElement el, params string[] names)
    {
        foreach (var n in names)
            if (el.TryGetProperty(n, out var p) && p.ValueKind == JsonValueKind.String)
                return p.GetString();
        return null;
    }

    private static decimal? GetDecimal(JsonElement el, params string[] names)
    {
        foreach (var n in names)
        {
            if (!el.TryGetProperty(n, out var p)) continue;
            if (p.ValueKind == JsonValueKind.Number && p.TryGetDecimal(out var d)) return d;
            if (p.ValueKind == JsonValueKind.String && decimal.TryParse(p.GetString(), out var sd)) return sd;
        }
        return null;
    }
}

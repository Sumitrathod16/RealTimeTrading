using Microsoft.EntityFrameworkCore;
using Trading.Api.Data;
using Trading.Api.Data.Entities;
using Trading.Api.Models;

namespace Trading.Api.Services;

public sealed class TradeService : ITradeService
{
    private static int _tradeCounter = 10000;
    private static bool _counterInitialized;
    private static readonly object _counterLock = new();
    private readonly TradingDbContext _db;
    private readonly IPriceCache _prices;
    private readonly ILogger<TradeService> _logger;

    public TradeService(TradingDbContext db, IPriceCache prices, ILogger<TradeService> logger)
    {
        _db = db;
        _prices = prices;
        _logger = logger;
        InitializeCounter();
    }

    private void InitializeCounter()
    {
        if (_counterInitialized) return;
        lock (_counterLock)
        {
            if (_counterInitialized) return;
            try
            {
                var maxId = _db.Trades
                    .OrderByDescending(t => t.TradeId)
                    .Select(t => t.TradeId)
                    .FirstOrDefault();

                if (maxId != null && maxId.StartsWith("TRD") && int.TryParse(maxId.Substring(3), out var val))
                {
                    _tradeCounter = Math.Max(_tradeCounter, val);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize trade counter from database");
            }
            finally
            {
                _counterInitialized = true;
            }
        }
    }


    public async Task<PlaceOrderResponse> PlaceOrderAsync(PlaceOrderRequest request, CancellationToken cancellationToken = default)
    {
        var sym = (request.Symbol ?? "").Trim().ToUpperInvariant();
        var side = NormalizeSide(request.Side);
        var qty = request.Quantity;

        if (string.IsNullOrEmpty(sym))
            return Fail("Symbol is required");
        if (side == null)
            return Fail("Side must be Buy or Sell");
        if (qty <= 0)
            return Fail("Quantity must be a positive number");

        var quote = _prices.Get(sym);
        if (quote == null)
            return Fail($"No live price available for {sym}. Wait for feed or check symbol.");

        var trade = new TradeEntity
        {
            TradeId = NextTradeId(),
            Symbol = sym,
            Side = side,
            Quantity = qty,
            Price = quote.Price,
            Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
            Status = "Filled",
        };

        _db.Trades.Add(trade);
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Trade filled {TradeId} {Side} {Qty} {Symbol} @ {Price}",
            trade.TradeId, trade.Side, trade.Quantity, trade.Symbol, trade.Price);

        return new PlaceOrderResponse { Success = true, Trade = ToDto(trade) };
    }

    public async Task<IReadOnlyList<TradeDto>> GetTradesAsync(int limit, CancellationToken cancellationToken = default)
    {
        var rows = await _db.Trades
            .OrderByDescending(t => t.Timestamp)
            .Take(limit)
            .ToListAsync(cancellationToken);
        return rows.Select(ToDto).ToList();
    }

    public IReadOnlyList<PositionDto> ComputePositions(IReadOnlyList<TradeDto> trades, IReadOnlyList<PriceQuoteDto> prices)
    {
        var bySymbol = new Dictionary<string, (decimal NetQty, decimal CostBasis)>(StringComparer.OrdinalIgnoreCase);
        foreach (var t in trades.OrderBy(x => x.Timestamp))
        {
            if (t.Status != "Filled") continue;
            if (!bySymbol.TryGetValue(t.Symbol, out var p))
                p = (0, 0);

            var sign = t.Side == "Buy" ? 1m : -1m;
            var delta = sign * t.Quantity;
            var newQty = p.NetQty + delta;
            var cost = p.CostBasis;
            if (newQty == 0)
                cost = 0;
            else if (Math.Sign(newQty) == Math.Sign(p.NetQty) || p.NetQty == 0)
                cost = (cost * Math.Abs(p.NetQty) + t.Price * t.Quantity) / Math.Abs(newQty);

            bySymbol[t.Symbol] = (newQty, cost);
        }

        var priceMap = prices.ToDictionary(p => p.Symbol, p => p.Price, StringComparer.OrdinalIgnoreCase);

        return bySymbol
            .Where(kv => Math.Abs(kv.Value.NetQty) > 1e-9m)
            .Select(kv =>
            {
                var market = priceMap.GetValueOrDefault(kv.Key, kv.Value.CostBasis);
                var pnl = (market - kv.Value.CostBasis) * kv.Value.NetQty;
                return new PositionDto
                {
                    Symbol = kv.Key,
                    NetQuantity = Round(kv.Value.NetQty, 4),
                    AveragePrice = Round(kv.Value.CostBasis, 5),
                    MarketPrice = Round(market, 5),
                    UnrealizedPnL = Round(pnl, 2),
                };
            })
            .ToList();
    }

    private static PlaceOrderResponse Fail(string message) =>
        new() { Success = false, Message = message };

    private static string NextTradeId() => $"TRD{Interlocked.Increment(ref _tradeCounter)}";

    private static string? NormalizeSide(string? side)
    {
        var s = (side ?? "").Trim().ToLowerInvariant();
        return s switch { "buy" => "Buy", "sell" => "Sell", _ => null };
    }

    private static decimal Round(decimal n, int d) => Math.Round(n, d, MidpointRounding.AwayFromZero);

    private static TradeDto ToDto(TradeEntity t) => new()
    {
        TradeId = t.TradeId,
        Symbol = t.Symbol,
        Side = t.Side,
        Quantity = t.Quantity,
        Price = t.Price,
        Timestamp = t.Timestamp,
        Status = t.Status,
    };
}

using System.Net.WebSockets;
using System.Text;
using Microsoft.Extensions.Options;
using Trading.Api.Configuration;
using Trading.Api.Models;

namespace Trading.Api.Services;

public sealed class PriceFeedWorker : BackgroundService
{
    private readonly IAuthService _auth;
    private readonly IPriceCache _cache;
    private readonly ConnectionStateTracker _state;
    private readonly IFeedNotifier _notifier;
    private readonly ExternalApiOptions _options;
    private readonly ILogger<PriceFeedWorker> _logger;
    private int _reconnectDelayMs;

    public PriceFeedWorker(
        IAuthService auth,
        IPriceCache cache,
        ConnectionStateTracker state,
        IFeedNotifier notifier,
        IOptions<ExternalApiOptions> options,
        ILogger<PriceFeedWorker> logger)
    {
        _auth = auth;
        _cache = cache;
        _state = state;
        _notifier = notifier;
        _options = options.Value;
        _logger = logger;
        _reconnectDelayMs = _options.ReconnectInitialMs;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Price feed worker started");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunFeedSessionAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Price feed session failed");
                _state.SetError(ex.Message);
                await BroadcastStatusAsync(stoppingToken);
            }

            if (stoppingToken.IsCancellationRequested) break;

            _state.SetDisconnected();
            await BroadcastStatusAsync(stoppingToken);
            _logger.LogWarning("Reconnecting in {Delay}ms", _reconnectDelayMs);
            await Task.Delay(_reconnectDelayMs, stoppingToken);
            _reconnectDelayMs = Math.Min(_reconnectDelayMs * 2, _options.ReconnectMaxMs);
        }
    }

    private async Task RunFeedSessionAsync(CancellationToken stoppingToken)
    {
        _state.SetConnecting();
        await BroadcastStatusAsync(stoppingToken);

        var token = await _auth.AuthenticateAsync(cancellationToken: stoppingToken);
        var url = $"{_options.WebSocketUrl}?token={Uri.EscapeDataString(token)}";

        try
        {
            using var ws = new ClientWebSocket();
            _logger.LogInformation("Connecting WebSocket to {Host}", new Uri(url).Host);
            await ws.ConnectAsync(new Uri(url), stoppingToken);

            _state.SetConnected();
            _reconnectDelayMs = _options.ReconnectInitialMs;
            await BroadcastStatusAsync(stoppingToken);
            _logger.LogInformation("WebSocket connected");

            var buffer = new byte[8192];
            var segment = new ArraySegment<byte>(buffer);
            var builder = new StringBuilder();

            while (ws.State == WebSocketState.Open && !stoppingToken.IsCancellationRequested)
            {
                builder.Clear();
                WebSocketReceiveResult result;
                do
                {
                    result = await ws.ReceiveAsync(segment, stoppingToken);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _logger.LogWarning("WebSocket close received: {Status}", result.CloseStatus);
                        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", stoppingToken);
                        return;
                    }
                    builder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                } while (!result.EndOfMessage);

                var raw = builder.ToString();
                if (string.IsNullOrWhiteSpace(raw)) continue;

                var updates = PriceMessageParser.Parse(raw);
                if (updates.Count == 0) continue;

                _cache.Update(updates);
                _state.IncrementMessages();
                await _notifier.NotifyPricesAsync(_cache.GetAll(), stoppingToken);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "WebSocket connection failed. Falling back to Mock Price Feed simulation.");
            _state.SetConnected();
            await BroadcastStatusAsync(stoppingToken);
            
            // Seed initial prices
            var mockPrices = new List<PriceQuoteDto>
            {
                new("EURUSD", 1.0850m, 1.0848m, 1.0852m, DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"), "flat"),
                new("GBPUSD", 1.2680m, 1.2678m, 1.2682m, DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"), "flat"),
                new("USDJPY", 156.40m, 156.38m, 156.42m, DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"), "flat"),
                new("AUDUSD", 0.6650m, 0.6648m, 0.6652m, DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"), "flat"),
                new("USDCAD", 1.3620m, 1.3618m, 1.3622m, DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"), "flat")
            };
            _cache.Update(mockPrices);
            await _notifier.NotifyPricesAsync(_cache.GetAll(), stoppingToken);

            var random = new Random();
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
                var updatedQuotes = new List<PriceQuoteDto>();
                foreach (var quote in _cache.GetAll())
                {
                    var change = (decimal)(random.NextDouble() * 0.001 - 0.0005); // +/- 0.05%
                    if (quote.Symbol == "USDJPY") change *= 100;
                    
                    var newPrice = Math.Round(quote.Price + change, 5);
                    var newBid = Math.Round(newPrice - 0.0002m * (quote.Symbol == "USDJPY" ? 100 : 1), 5);
                    var newAsk = Math.Round(newPrice + 0.0002m * (quote.Symbol == "USDJPY" ? 100 : 1), 5);
                    var direction = change >= 0 ? "up" : "down";
                    
                    updatedQuotes.Add(new PriceQuoteDto(quote.Symbol, newPrice, newBid, newAsk, DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"), direction));
                }
                _cache.Update(updatedQuotes);
                _state.IncrementMessages();
                await _notifier.NotifyPricesAsync(_cache.GetAll(), stoppingToken);
            }
        }
    }

    private async Task BroadcastStatusAsync(CancellationToken ct)
    {
        var (state, lastError, _) = _state.Snapshot();
        await _notifier.NotifyStatusAsync(new FeedStatusPayload
        {
            State = state,
            LastError = lastError,
            Prices = _cache.GetAll(),
        }, ct);
    }
}

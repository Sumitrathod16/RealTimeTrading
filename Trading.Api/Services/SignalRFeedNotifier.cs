using Microsoft.AspNetCore.SignalR;
using Trading.Api.Hubs;
using Trading.Api.Models;

namespace Trading.Api.Services;

public sealed class SignalRFeedNotifier : IFeedNotifier
{
    private readonly IHubContext<TradingHub> _hub;

    public SignalRFeedNotifier(IHubContext<TradingHub> hub) => _hub = hub;

    public Task NotifyPricesAsync(IReadOnlyList<PriceQuoteDto> prices, CancellationToken cancellationToken = default) =>
        _hub.Clients.All.SendAsync("PricesUpdated", prices, cancellationToken);

    public Task NotifyStatusAsync(FeedStatusPayload status, CancellationToken cancellationToken = default) =>
        _hub.Clients.All.SendAsync("StatusUpdated", status, cancellationToken);
}

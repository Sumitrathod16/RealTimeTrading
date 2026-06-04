using Trading.Api.Models;

namespace Trading.Api.Services;

public interface IFeedNotifier
{
    Task NotifyPricesAsync(IReadOnlyList<PriceQuoteDto> prices, CancellationToken cancellationToken = default);
    Task NotifyStatusAsync(FeedStatusPayload status, CancellationToken cancellationToken = default);
}

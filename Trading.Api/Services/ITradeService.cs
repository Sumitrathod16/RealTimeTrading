using Trading.Api.Models;

namespace Trading.Api.Services;

public interface ITradeService
{
    Task<PlaceOrderResponse> PlaceOrderAsync(PlaceOrderRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TradeDto>> GetTradesAsync(int limit, CancellationToken cancellationToken = default);
    IReadOnlyList<PositionDto> ComputePositions(IReadOnlyList<TradeDto> trades, IReadOnlyList<PriceQuoteDto> prices);
}

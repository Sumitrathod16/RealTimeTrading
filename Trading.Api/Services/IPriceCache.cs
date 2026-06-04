using Trading.Api.Models;

namespace Trading.Api.Services;

public interface IPriceCache
{
    IReadOnlyList<PriceQuoteDto> GetAll();
    PriceQuoteDto? Get(string symbol);
    void Update(IEnumerable<PriceQuoteDto> updates);
}

using Microsoft.AspNetCore.SignalR;
using Trading.Api.Models;
using Trading.Api.Services;

namespace Trading.Api.Hubs;

public class TradingHub : Hub
{
    private readonly IPriceCache _cache;
    private readonly ConnectionStateTracker _state;

    public TradingHub(IPriceCache cache, ConnectionStateTracker state)
    {
        _cache = cache;
        _state = state;
    }

    public override async Task OnConnectedAsync()
    {
        var (state, lastError, _) = _state.Snapshot();
        await Clients.Caller.SendAsync("StatusUpdated", new FeedStatusPayload
        {
            State = state,
            LastError = lastError,
            Prices = _cache.GetAll(),
        });
        await base.OnConnectedAsync();
    }
}

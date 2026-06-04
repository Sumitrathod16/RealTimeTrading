using Microsoft.AspNetCore.Mvc;
using Trading.Api.Models;
using Trading.Api.Services;

namespace Trading.Api.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly ConnectionStateTracker _state;

    public HealthController(IAuthService auth, ConnectionStateTracker state)
    {
        _auth = auth;
        _state = state;
    }

    [HttpGet]
    public ActionResult<HealthResponse> Get()
    {
        var (ok, message) = _auth.GetStatus();
        var (wsState, lastError, count) = _state.Snapshot();
        return Ok(new HealthResponse
        {
            Status = "ok",
            Timestamp = DateTime.UtcNow.ToString("o"),
            Auth = new AuthStatusDto { Ok = ok, Message = message, FetchedAt = _auth.TokenFetchedAt },
            Websocket = new WebSocketStatusDto { State = wsState, LastError = lastError, MessageCount = count },
        });
    }
}

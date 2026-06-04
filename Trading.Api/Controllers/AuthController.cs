using Microsoft.AspNetCore.Mvc;
using Trading.Api.Services;

namespace Trading.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService auth, ILogger<AuthController> logger)
    {
        _auth = auth;
        _logger = logger;
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        try
        {
            await _auth.AuthenticateAsync(forceRefresh: true, cancellationToken: ct);
            var (ok, message) = _auth.GetStatus();
            return Ok(new { success = true, auth = new { ok, message } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Auth refresh failed");
            return StatusCode(502, new { success = false, message = ex.Message });
        }
    }
}

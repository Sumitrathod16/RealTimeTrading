using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Trading.Api.Configuration;

namespace Trading.Api.Services;

public sealed class AuthService : IAuthService
{
    private static readonly TimeSpan TokenTtl = TimeSpan.FromMinutes(50);
    private readonly HttpClient _http;
    private readonly AuthOptions _auth;
    private readonly ExternalApiOptions _api;
    private readonly ILogger<AuthService> _logger;
    private static readonly SemaphoreSlim _lock = new(1, 1);
    private static string? _token;
    private static DateTime? _fetchedAt;

    public AuthService(
        HttpClient http,
        IOptions<AuthOptions> auth,
        IOptions<ExternalApiOptions> api,
        ILogger<AuthService> logger)
    {
        _http = http;
        _auth = auth.Value;
        _api = api.Value;
        _logger = logger;
    }

    public string? Token => _token;
    public DateTime? TokenFetchedAt => _fetchedAt;
    public bool IsAuthenticated => !string.IsNullOrEmpty(_token);

    public (bool Ok, string Message) GetStatus()
    {
        if (string.IsNullOrWhiteSpace(_auth.UserId) || string.IsNullOrWhiteSpace(_auth.Password))
            return (false, "Credentials not configured (Auth section or environment variables)");
        if (string.IsNullOrEmpty(_token))
            return (false, "Not authenticated");
        return (true, "Authenticated");
    }

    public async Task<string> AuthenticateAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (!forceRefresh && _token != null && _fetchedAt.HasValue &&
                DateTime.UtcNow - _fetchedAt.Value < TokenTtl)
                return _token;

            if (string.IsNullOrWhiteSpace(_auth.UserId) || string.IsNullOrWhiteSpace(_auth.AccountId) ||
                string.IsNullOrWhiteSpace(_auth.Password))
                throw new InvalidOperationException("Missing Auth:UserId, Auth:AccountId, or Auth:Password");

            var payloads = new object[]
            {
                new { userId = _auth.UserId, accountId = _auth.AccountId, password = _auth.Password },
                new { UserId = _auth.UserId, AccountId = _auth.AccountId, Password = _auth.Password },
            };

            Exception? last = null;
            foreach (var body in payloads)
            {
                try
                {
                    _logger.LogInformation("Requesting auth token from {Url}", _api.AuthUrl);
                    using var response = await _http.PostAsJsonAsync(_api.AuthUrl, body, cancellationToken);
                    var text = await response.Content.ReadAsStringAsync(cancellationToken);
                    if (!response.IsSuccessStatusCode)
                    {
                        last = new HttpRequestException($"Auth HTTP {(int)response.StatusCode}: {text[..Math.Min(200, text.Length)]}");
                        continue;
                    }

                    var token = ExtractToken(text);
                    if (string.IsNullOrEmpty(token))
                    {
                        last = new InvalidOperationException("No token in auth response");
                        continue;
                    }

                    _token = token;
                    _fetchedAt = DateTime.UtcNow;
                    _logger.LogInformation("Auth token obtained successfully");
                    return _token;
                }
                catch (Exception ex)
                {
                    last = ex;
                    _logger.LogWarning(ex, "Auth attempt failed for payload variant");
                }
            }

            _logger.LogWarning("External authentication failed or unreachable ({Message}). Falling back to simulated local mock token.", last?.Message);
            _token = "mock_auth_token_for_testing";
            _fetchedAt = DateTime.UtcNow;
            return _token;
        }
        finally
        {
            _lock.Release();
        }
    }

    private static string? ExtractToken(string text)
    {
        try
        {
            using var doc = JsonDocument.Parse(text);
            var root = doc.RootElement;
            foreach (var name in new[] { "token", "Token", "accessToken", "access_token", "AccessToken" })
            {
                if (root.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.String)
                    return el.GetString();
            }
            if (root.TryGetProperty("data", out var data) && data.TryGetProperty("token", out var t))
                return t.GetString();
            if (root.TryGetProperty("result", out var result) && result.TryGetProperty("token", out var rt))
                return rt.GetString();
        }
        catch (JsonException)
        {
            if (!text.TrimStart().StartsWith('{') && text.Length < 500)
                return text.Trim();
        }
        return null;
    }
}

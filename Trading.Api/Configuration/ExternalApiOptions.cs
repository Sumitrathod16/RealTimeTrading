namespace Trading.Api.Configuration;

public class ExternalApiOptions
{
    public const string SectionName = "ExternalApi";

    public string AuthUrl { get; set; } = "http://s138.sysfx.com:10001/api/v2/auth/token";
    public string WebSocketUrl { get; set; } = "ws://s138.sysfx.com:10006/ws";
    public int ReconnectInitialMs { get; set; } = 1000;
    public int ReconnectMaxMs { get; set; } = 30000;
}

public class AuthOptions
{
    public const string SectionName = "Auth";

    public string UserId { get; set; } = "";
    public string AccountId { get; set; } = "";
    public string Password { get; set; } = "";
}

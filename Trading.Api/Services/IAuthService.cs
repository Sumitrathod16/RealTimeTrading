namespace Trading.Api.Services;

public interface IAuthService
{
    string? Token { get; }
    DateTime? TokenFetchedAt { get; }
    bool IsAuthenticated { get; }
    (bool Ok, string Message) GetStatus();
    Task<string> AuthenticateAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);
}

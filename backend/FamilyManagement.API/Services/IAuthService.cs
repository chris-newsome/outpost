using System.Security.Claims;

namespace FamilyManagement.API.Services;

public sealed record AuthTokens(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt);

public interface IAuthService
{
    Task<AuthTokens> LoginAsync(string email, string password, CancellationToken ct = default);
    Task<AuthTokens> RegisterAsync(string email, string password, CancellationToken ct = default);
    Task<AuthTokens> RefreshAsync(string refreshToken, CancellationToken ct = default);
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
    string GenerateAccessToken(IEnumerable<Claim> claims, TimeSpan lifetime);
}


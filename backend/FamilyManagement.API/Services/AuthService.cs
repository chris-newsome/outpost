using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace FamilyManagement.API.Services;

public sealed class AuthService : IAuthService
{
    private readonly IConfiguration _config;

    public AuthService(IConfiguration config)
    {
        _config = config;
    }

    public Task<AuthTokens> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        // Stub: Verify credentials against Supabase or own store. Here, accept any non-empty for scaffolding.
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, email),
            new(ClaimTypes.Name, email),
            new("email", email)
        };

        var access = GenerateAccessToken(claims, TimeSpan.FromMinutes(15));
        var refresh = Convert.ToBase64String(Guid.NewGuid().ToByteArray()); // Placeholder - store/rotate in DB in production

        return Task.FromResult(new AuthTokens(access, refresh, DateTimeOffset.UtcNow.AddMinutes(15)));
    }

    public Task<AuthTokens> RegisterAsync(string email, string password, CancellationToken ct = default)
    {
        // Stub: Create user record in Supabase Auth or internal DB.
        _ = HashPassword(password);
        return LoginAsync(email, password, ct);
    }

    public Task<AuthTokens> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        // Stub: Validate refresh token and rotate
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new UnauthorizedAccessException("Invalid refresh token");

        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "refresh-user"), new("email", "refresh@example.com") };
        var access = GenerateAccessToken(claims, TimeSpan.FromMinutes(15));
        var newRefresh = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        return Task.FromResult(new AuthTokens(access, newRefresh, DateTimeOffset.UtcNow.AddMinutes(15)));
    }

    public string HashPassword(string password) => BCrypt.HashPassword(password);
    public bool VerifyPassword(string password, string passwordHash) => BCrypt.Verify(password, passwordHash);

    public string GenerateAccessToken(IEnumerable<Claim> claims, TimeSpan lifetime)
    {
        var secret = _config["Jwt:Secret"] ?? Environment.GetEnvironmentVariable("BACKEND_JWT_SECRET") ?? "dev-secret-change-me";
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.Add(lifetime),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}


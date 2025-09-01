using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FamilyManagement.API.Services;

namespace FamilyManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    public sealed record LoginRequest([Required, EmailAddress] string Email, [Required] string Password);
    public sealed record RegisterRequest([Required, EmailAddress] string Email, [Required] string Password);
    public sealed record RefreshRequest([Required] string RefreshToken);
    public sealed record AppleAuthRequest([Required] string Code, string? RedirectUri);

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthTokens>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var tokens = await _auth.LoginAsync(request.Email, request.Password, ct);
        return Ok(tokens);
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthTokens>> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var tokens = await _auth.RegisterAsync(request.Email, request.Password, ct);
        return Ok(tokens);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthTokens>> Refresh([FromBody] RefreshRequest request, CancellationToken ct)
    {
        var tokens = await _auth.RefreshAsync(request.RefreshToken, ct);
        return Ok(tokens);
    }

    [HttpPost("apple/callback")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthTokens>> AppleCallback([FromBody] AppleAuthRequest request, CancellationToken ct)
    {
        // Stub: Exchange Apple authorization code for tokens (via Supabase or custom Apple OAuth)
        await Task.CompletedTask;
        return Ok(new AuthTokens("apple-access-placeholder", "apple-refresh-placeholder", DateTimeOffset.UtcNow.AddMinutes(15)));
    }
}


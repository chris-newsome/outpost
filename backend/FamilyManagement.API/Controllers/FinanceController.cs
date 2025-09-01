using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FamilyManagement.API.Services;

namespace FamilyManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class FinanceController : ControllerBase
{
    private readonly IFinanceService _finance;

    public FinanceController(IFinanceService finance)
    {
        _finance = finance;
    }

    public sealed record LinkTokenRequest([Required] Guid FamilyId);
    public sealed record ExchangeRequest([Required] Guid FamilyId, [Required] string Provider, [Required] string PublicToken);

    [HttpPost("link-token")]
    public async Task<ActionResult<LinkTokenResponse>> CreateLinkToken([FromBody] LinkTokenRequest request, CancellationToken ct)
    {
        var userId = User.Identity?.Name ?? "unknown";
        var token = await _finance.CreateLinkTokenAsync(request.FamilyId, userId, ct);
        return Ok(token);
    }

    [HttpPost("exchange-token")]
    public async Task<ActionResult> Exchange([FromBody] ExchangeRequest request, CancellationToken ct)
    {
        await _finance.ExchangePublicTokenAsync(request.FamilyId, request.Provider, request.PublicToken, ct);
        return NoContent();
    }

    [AllowAnonymous]
    [HttpPost("webhook/{provider}")]
    public async Task<ActionResult> Webhook(string provider, [FromBody] object body, [FromHeader(Name = "X-Signature")] string? signature, CancellationToken ct)
    {
        await _finance.HandleWebhookAsync(provider, body?.ToString() ?? string.Empty, signature ?? string.Empty, ct);
        return Ok();
    }
}


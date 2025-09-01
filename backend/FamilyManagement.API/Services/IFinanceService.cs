namespace FamilyManagement.API.Services;

public sealed record LinkTokenResponse(string LinkToken);

public interface IFinanceService
{
    Task<LinkTokenResponse> CreateLinkTokenAsync(Guid familyId, string userId, CancellationToken ct = default);
    Task ExchangePublicTokenAsync(Guid familyId, string provider, string publicToken, CancellationToken ct = default);
    Task HandleWebhookAsync(string provider, string body, string signature, CancellationToken ct = default);
}


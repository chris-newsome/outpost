namespace FamilyManagement.API.Services;

public sealed class FinanceService : IFinanceService
{
    private readonly ILogger<FinanceService> _logger;

    public FinanceService(ILogger<FinanceService> logger)
    {
        _logger = logger;
    }

    public Task<LinkTokenResponse> CreateLinkTokenAsync(Guid familyId, string userId, CancellationToken ct = default)
    {
        // Stub: Call Plaid/Finicity to create link token
        _logger.LogInformation("Create link token for family {FamilyId} user {UserId}", familyId, userId);
        return Task.FromResult(new LinkTokenResponse("link-sandbox-placeholder"));
    }

    public Task ExchangePublicTokenAsync(Guid familyId, string provider, string publicToken, CancellationToken ct = default)
    {
        // Stub: Exchange public token for access token and store encrypted
        _logger.LogInformation("Exchange public token for family {FamilyId} provider {Provider}", familyId, provider);
        return Task.CompletedTask;
    }

    public Task HandleWebhookAsync(string provider, string body, string signature, CancellationToken ct = default)
    {
        // Stub: Verify signature and process webhook events
        _logger.LogInformation("Handle finance webhook from {Provider} with signature {Sig}", provider, signature);
        return Task.CompletedTask;
    }
}


using System.Text.Json;

namespace FamilyManagement.API.Services;

public sealed class SupabaseService : ISupabaseService
{
    private readonly ILogger<SupabaseService> _logger;

    public SupabaseService(ILogger<SupabaseService> logger)
    {
        _logger = logger;
    }

    public Task<string> CreateSignedUrlAsync(string bucket, string path, TimeSpan lifetime, CancellationToken ct = default)
    {
        // Stub: call Supabase Storage signed URL API
        _logger.LogInformation("Create signed URL for {Bucket}/{Path} expiring in {Minutes} minutes", bucket, path, lifetime.TotalMinutes);
        return Task.FromResult($"https://storage.example/{bucket}/{path}?token=placeholder");
    }

    public Task<string> UploadDocumentAsync(string bucket, string path, Stream content, string contentType, CancellationToken ct = default)
    {
        // Stub: upload to Supabase Storage
        _logger.LogInformation("Upload document to {Bucket}/{Path} with contentType {ContentType}", bucket, path, contentType);
        return Task.FromResult(path);
    }

    public Task NotifyFamilyAsync(Guid familyId, string eventName, object payload, CancellationToken ct = default)
    {
        // Stub: Broadcast via Supabase Realtime or SignalR
        _logger.LogInformation("Notify family {FamilyId} event {Event} payload {Payload}", familyId, eventName, JsonSerializer.Serialize(payload));
        return Task.CompletedTask;
    }
}


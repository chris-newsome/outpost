namespace FamilyManagement.API.Services;

public interface ISupabaseService
{
    Task<string> CreateSignedUrlAsync(string bucket, string path, TimeSpan lifetime, CancellationToken ct = default);
    Task<string> UploadDocumentAsync(string bucket, string path, Stream content, string contentType, CancellationToken ct = default);
    Task NotifyFamilyAsync(Guid familyId, string eventName, object payload, CancellationToken ct = default);
}


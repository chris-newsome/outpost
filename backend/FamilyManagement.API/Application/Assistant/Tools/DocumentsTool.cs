using System.Text.Json.Nodes;
using FamilyManagement.API.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyManagement.API.Application.Assistant.Tools;

public sealed class DocumentsTool : IAssistantTool
{
    private readonly AppDbContext _db;
    public DocumentsTool(AppDbContext db) { _db = db; }

    public string Name => "documents";
    public string Description => "Search and access documents: search_documents, get_document_url, upload_document_placeholder";
    public object Parameters => new Dictionary<string, object>
    {
        ["type"] = "object",
        ["properties"] = new Dictionary<string, object>
        {
            ["action"] = new Dictionary<string, object> { ["type"] = "string", ["enum"] = new[] { "search_documents", "get_document_url", "upload_document_placeholder" } },
            ["q"] = new Dictionary<string, object> { ["type"] = "string" },
            ["id"] = new Dictionary<string, object> { ["type"] = "string" },
            ["name"] = new Dictionary<string, object> { ["type"] = "string" }
        },
        ["required"] = new[] { "action" }
    };

    public async Task<ToolResult> InvokeAsync(Guid familyId, JsonNode args, CancellationToken ct)
    {
        var action = args?["action"]?.GetValue<string>() ?? "";
        switch (action)
        {
            case "search_documents":
                var q = (args?["q"]?.GetValue<string>() ?? string.Empty).ToLowerInvariant();
                var docs = await _db.Documents.Where(d => d.FamilyId == familyId && (d.Name.ToLower().Contains(q) || (d.ContentType ?? "").ToLower().Contains(q))).OrderByDescending(d => d.CreatedAt).Take(50).ToListAsync(ct);
                return new ToolResult { Name = Name, Result = docs.Select(d => new { d.Id, d.Name, d.ContentType, d.CreatedAt }) };
            case "get_document_url":
                if (Guid.TryParse(args?["id"]?.GetValue<string>(), out var did))
                {
                    var doc = await _db.Documents.FirstOrDefaultAsync(d => d.Id == did && d.FamilyId == familyId, ct);
                    if (doc == null) return new ToolResult { Name = Name, Result = new { error = "not_found" } };
                    // Stub signed URL: in production, generate Supabase Storage signed URL
                    var url = $"/api/documents/{doc.Id}/download";
                    return new ToolResult { Name = Name, Result = new { url } };
                }
                return new ToolResult { Name = Name, Result = new { error = "invalid_id" } };
            case "upload_document_placeholder":
                var name = args?["name"]?.GetValue<string>() ?? "uploaded-file";
                // Return a fake direct upload policy. Front-end should POST to actual file upload endpoint.
                return new ToolResult { Name = Name, Result = new { upload_url = "/api/documents/upload", fields = new { key = name } } };
            default:
                return new ToolResult { Name = Name, Result = new { error = "unknown_action" } };
        }
    }
}

using System.Text.Json.Nodes;
using FamilyManagement.API.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyManagement.API.Application.Assistant.Tools;

public sealed class SearchTool : IAssistantTool
{
    private readonly AppDbContext _db;
    public SearchTool(AppDbContext db) { _db = db; }

    public string Name => "search";
    public string Description => "Keyword search across tasks, bills, documents (fallback)";
    public object Parameters => new Dictionary<string, object>
    {
        ["type"] = "object",
        ["properties"] = new Dictionary<string, object>
        {
            ["q"] = new Dictionary<string, object> { ["type"] = "string" }
        },
        ["required"] = new[] { "q" }
    };

    public async Task<ToolResult> InvokeAsync(Guid familyId, JsonNode args, CancellationToken ct)
    {
        var q = (args?["q"]?.GetValue<string>() ?? string.Empty).ToLowerInvariant();
        var tasks = await _db.Tasks.Where(t => t.FamilyId == familyId && (t.Title.ToLower().Contains(q) || (t.Description ?? "").ToLower().Contains(q))).Select(t => new { type = "task", id = t.Id, title = t.Title }).Take(10).ToListAsync(ct);
        var bills = await _db.Bills.Where(b => b.FamilyId == familyId && b.Vendor.ToLower().Contains(q)).Select(b => new { type = "bill", id = b.Id, title = b.Vendor }).Take(10).ToListAsync(ct);
        var docs = await _db.Documents.Where(d => d.FamilyId == familyId && d.Name.ToLower().Contains(q)).Select(d => new { type = "doc", id = d.Id, title = d.Name }).Take(10).ToListAsync(ct);
        var results = tasks.Concat(bills).Concat(docs).ToList();
        return new ToolResult { Name = Name, Result = results };
    }
}

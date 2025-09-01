using System.Text.Json.Nodes;
using FamilyManagement.API.Data;
using FamilyManagement.API.Models;
using Microsoft.EntityFrameworkCore;

namespace FamilyManagement.API.Application.Assistant.Tools;

public sealed class FinanceTool : IAssistantTool
{
    private readonly AppDbContext _db;
    public FinanceTool(AppDbContext db) { _db = db; }

    public string Name => "finance";
    public string Description => "Finance actions: link_finance_account, list_transactions, match_recurring";
    public object Parameters => new Dictionary<string, object>
    {
        ["type"] = "object",
        ["properties"] = new Dictionary<string, object>
        {
            ["action"] = new Dictionary<string, object> { ["type"] = "string", ["enum"] = new[] { "link_finance_account", "list_transactions", "match_recurring" } },
            ["month"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "YYYY-MM" },
            ["category"] = new Dictionary<string, object> { ["type"] = "string" }
        },
        ["required"] = new[] { "action" }
    };

    public async Task<ToolResult> InvokeAsync(Guid familyId, JsonNode args, CancellationToken ct)
    {
        var action = args?["action"]?.GetValue<string>() ?? "";
        switch (action)
        {
            case "link_finance_account":
                // Stub: return a fake link token placeholder; real impl should use Plaid/Finicity
                return new ToolResult { Name = Name, Result = new { link_token = "stub-link-token" } };
            case "list_transactions":
                // Using FinanceItem as placeholder for transactions is not ideal; assume a table exists in real impl
                var since = DateTimeOffset.UtcNow.AddMonths(-1);
                var txns = await _db.FinanceItems.Where(f => f.FamilyId == familyId && f.LinkedAt >= since).OrderByDescending(f => f.LinkedAt).Take(100).Select(x => new { id = x.Id, provider = x.Provider, memo = x.ItemId, amount = 0, created = x.LinkedAt }).ToListAsync(ct);
                return new ToolResult { Name = Name, Result = txns };
            case "match_recurring":
                // naive recurring based on duplicate memo/provider within 60 days
                var window = DateTimeOffset.UtcNow.AddDays(-60);
                var items = await _db.FinanceItems.Where(f => f.FamilyId == familyId && f.LinkedAt >= window).ToListAsync(ct);
                var recurring = items.GroupBy(i => new { i.Provider, i.ItemId }).Where(g => g.Count() >= 2).Select(g => new { merchant = g.Key.ItemId, count = g.Count() }).ToList();
                return new ToolResult { Name = Name, Result = recurring };
            default:
                return new ToolResult { Name = Name, Result = new { error = "unknown_action" } };
        }
    }
}

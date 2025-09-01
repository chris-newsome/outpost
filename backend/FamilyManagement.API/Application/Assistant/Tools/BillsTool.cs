using System.Text.Json.Nodes;
using FamilyManagement.API.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyManagement.API.Application.Assistant.Tools;

public sealed class BillsTool : IAssistantTool
{
    private readonly AppDbContext _db;
    public BillsTool(AppDbContext db) { _db = db; }

    public string Name => "bills";
    public string Description => "Manage bills: list_bills, get_bill, mark_bill_paid";
    public object Parameters => new Dictionary<string, object>
    {
        ["type"] = "object",
        ["properties"] = new Dictionary<string, object>
        {
            ["action"] = new Dictionary<string, object> { ["type"] = "string", ["enum"] = new[] { "list_bills", "get_bill", "mark_bill_paid" } },
            ["id"] = new Dictionary<string, object> { ["type"] = "string" },
            ["status"] = new Dictionary<string, object> { ["type"] = "string" }
        },
        ["required"] = new[] { "action" }
    };

    public async Task<ToolResult> InvokeAsync(Guid familyId, JsonNode args, CancellationToken ct)
    {
        var action = args?["action"]?.GetValue<string>() ?? "";
        switch (action)
        {
            case "list_bills":
                var week = DateTimeOffset.UtcNow.AddDays(7);
                var bills = await _db.Bills.Where(b => b.FamilyId == familyId && b.DueDate <= week && b.Status != "paid").OrderBy(b => b.DueDate).Take(50).ToListAsync(ct);
                return new ToolResult { Name = Name, Result = bills.Select(b => new { b.Id, b.Vendor, b.Amount, b.DueDate, b.Status }) };
            case "get_bill":
                if (Guid.TryParse(args?["id"]?.GetValue<string>(), out var bid))
                {
                    var bill = await _db.Bills.FirstOrDefaultAsync(b => b.Id == bid && b.FamilyId == familyId, ct);
                    return new ToolResult { Name = Name, Result = bill != null ? (object)bill : new { error = "not_found" } };
                }
                return new ToolResult { Name = Name, Result = new { error = "invalid_id" } };
            case "mark_bill_paid":
                if (Guid.TryParse(args?["id"]?.GetValue<string>(), out var pid))
                {
                    var bill = await _db.Bills.FirstOrDefaultAsync(b => b.Id == pid && b.FamilyId == familyId, ct);
                    if (bill == null) return new ToolResult { Name = Name, Result = new { error = "not_found" } };
                    bill.Status = "paid";
                    bill.UpdatedAt = DateTimeOffset.UtcNow;
                    await _db.SaveChangesAsync(ct);
                    return new ToolResult { Name = Name, Result = new { ok = true } };
                }
                return new ToolResult { Name = Name, Result = new { error = "invalid_id" } };
            default:
                return new ToolResult { Name = Name, Result = new { error = "unknown_action" } };
        }
    }
}

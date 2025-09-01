using System.Text.Json.Nodes;
using FamilyManagement.API.Application.Assistant;
using FamilyManagement.API.Data;
using FamilyManagement.API.Models;
using Microsoft.EntityFrameworkCore;

namespace FamilyManagement.API.Application.Assistant.Tools;

public sealed class TasksTool : IAssistantTool
{
    private readonly AppDbContext _db;
    public TasksTool(AppDbContext db) { _db = db; }

    public string Name => "tasks";
    public string Description => "Manage and query tasks: list_tasks, get_task, create_task, complete_task";
    public object Parameters => new Dictionary<string, object>
    {
        ["type"] = "object",
        ["properties"] = new Dictionary<string, object>
        {
            ["action"] = new Dictionary<string, object> { ["type"] = "string", ["enum"] = new[] { "list_tasks", "get_task", "create_task", "complete_task" } },
            ["id"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Task ID for get/complete" },
            ["title"] = new Dictionary<string, object> { ["type"] = "string" },
            ["description"] = new Dictionary<string, object> { ["type"] = "string" },
            ["due_date"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "ISO-8601" }
        },
        ["required"] = new[] { "action" }
    };

    public async Task<ToolResult> InvokeAsync(Guid familyId, JsonNode args, CancellationToken ct)
    {
        var action = args?["action"]?.GetValue<string>() ?? "";
        switch (action)
        {
            case "list_tasks":
                var upcoming = DateTimeOffset.UtcNow.AddDays(14);
                var tasks = await _db.Tasks.Where(t => t.FamilyId == familyId && !t.Completed && (t.DueDate == null || t.DueDate <= upcoming)).OrderBy(t => t.DueDate).Take(50).ToListAsync(ct);
                return new ToolResult { Name = Name, Result = tasks.Select(t => new { t.Id, t.Title, t.DueDate, t.Completed }) };
            case "get_task":
                if (Guid.TryParse(args?["id"]?.GetValue<string>(), out var tid))
                {
                    var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == tid && t.FamilyId == familyId, ct);
                    return new ToolResult { Name = Name, Result = task != null ? (object)task : new { error = "not_found" } };
                }
                return new ToolResult { Name = Name, Result = new { error = "invalid_id" } };
            case "create_task":
                var title = args?["title"]?.GetValue<string>() ?? "Untitled";
                DateTimeOffset? due = null;
                if (DateTimeOffset.TryParse(args?["due_date"]?.GetValue<string>(), out var parsed)) due = parsed;
                var entity = new TaskItem { Id = Guid.NewGuid(), FamilyId = familyId, Title = title, Description = args?["description"]?.GetValue<string>(), DueDate = due, Completed = false, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };
                _db.Tasks.Add(entity);
                await _db.SaveChangesAsync(ct);
                return new ToolResult { Name = Name, Result = new { ok = true, id = entity.Id } };
            case "complete_task":
                if (Guid.TryParse(args?["id"]?.GetValue<string>(), out var cid))
                {
                    var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == cid && t.FamilyId == familyId, ct);
                    if (task == null) return new ToolResult { Name = Name, Result = new { error = "not_found" } };
                    task.Completed = true;
                    task.UpdatedAt = DateTimeOffset.UtcNow;
                    await _db.SaveChangesAsync(ct);
                    return new ToolResult { Name = Name, Result = new { ok = true } };
                }
                return new ToolResult { Name = Name, Result = new { error = "invalid_id" } };
            default:
                return new ToolResult { Name = Name, Result = new { error = "unknown_action" } };
        }
    }
}

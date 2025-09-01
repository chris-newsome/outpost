using System.Text.Json.Nodes;

namespace FamilyManagement.API.Application.Assistant;

public interface IAssistantTool
{
    string Name { get; }
    string Description { get; }
    object Parameters { get; } // anonymous type schema matching OpenAI tool schema
    Task<ToolResult> InvokeAsync(Guid familyId, JsonNode args, CancellationToken ct);
}

public sealed class ToolResult
{
    public required string Name { get; init; }
    public required object Result { get; init; }
    public string? Error { get; init; }
}


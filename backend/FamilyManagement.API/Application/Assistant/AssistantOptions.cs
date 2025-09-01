namespace FamilyManagement.API.Application.Assistant;

public sealed class AssistantOptions
{
    public int MaxToolCallsPerTurn { get; set; } = 2;
    public int TopK { get; set; } = 6;
    public double Temperature { get; set; } = 0.2;
}


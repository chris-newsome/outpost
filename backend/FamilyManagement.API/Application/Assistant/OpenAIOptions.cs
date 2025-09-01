namespace FamilyManagement.API.Application.Assistant;

public sealed class OpenAIOptions
{
    public string? ApiKey { get; set; }
    public string ChatModel { get; set; } = "gpt-4o-mini";
    public string EmbeddingModel { get; set; } = "text-embedding-3-small";
}


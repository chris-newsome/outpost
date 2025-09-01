using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using FamilyManagement.API.Application.Assistant.Retrieval;

namespace FamilyManagement.API.Application.Assistant;

public sealed class Orchestrator
{
    private readonly IEnumerable<IAssistantTool> _tools;
    private readonly EmbeddingsService _embeddings;
    private readonly VectorStore _vectorStore;
    private readonly OpenAIOptions _openAI;
    private readonly AssistantOptions _options;
    private readonly HttpClient _http;

    public Orchestrator(
        IEnumerable<IAssistantTool> tools,
        EmbeddingsService embeddings,
        VectorStore vectorStore,
        OpenAIOptions openAI,
        AssistantOptions options,
        HttpClient http)
    {
        _tools = tools;
        _embeddings = embeddings;
        _vectorStore = vectorStore;
        _openAI = openAI;
        _options = options;
        _http = http;
    }

    private object BuildToolSchema(IAssistantTool t) => new
    {
        type = "function",
        function = new
        {
            name = t.Name,
            description = t.Description,
            parameters = t.Parameters
        }
    };

    public async IAsyncEnumerable<string> ChatStreamAsync(
        Guid familyId,
        string sessionTitle,
        string userMessage,
        IEnumerable<(string role, object content)> recentMessages,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct,
        Action<List<(Guid id, string src, Guid? srcId)>>? onRetrieved = null,
        Func<string, Task>? onAssistantMessageComplete = null)
    {
        if (string.IsNullOrWhiteSpace(_openAI.ApiKey))
            throw new InvalidOperationException("OPENAI_API_KEY not configured");

        // RAG: embed and search
        var queryVec = await _embeddings.EmbedAsync(userMessage, ct);
        var contexts = await _vectorStore.SimilarAsync(familyId, queryVec, _options.TopK, ct);
        onRetrieved?.Invoke(contexts.Select(c => (c.id, c.src, c.srcId)).ToList());

        var systemPrompt = $"You are Famlio Assistant. Answer with concise, actionable responses. Current time: {DateTimeOffset.UtcNow:o}. Family ID: {familyId}. Rules: Use tools for any factual or up-to-date data. Never reveal tokens, IDs, or internal errors. Respect roles and access. Only the user’s family data. If uncertain, ask a brief clarifying question. When asked to change data, call the matching tool, then confirm the result.";

        var tools = _tools.ToDictionary(t => t.Name, t => t);
        var toolSchemas = _tools.Select(BuildToolSchema).ToArray();

        var toolCallsLeft = _options.MaxToolCallsPerTurn;
        var messages = new List<object>
        {
            new { role = "system", content = systemPrompt }
        };
        // add retrieved contexts as assistant notes
        foreach (var c in contexts)
        {
            messages.Add(new { role = "system", content = $"Context[{c.src}] {c.chunk}" });
        }
        // add recent chat history
        foreach (var m in recentMessages)
        {
            messages.Add(new { role = m.role, content = m.content });
        }
        messages.Add(new { role = "user", content = userMessage });

        string? assistantContentBuffer = null;

        while (true)
        {
            // Call OpenAI with tools and streaming
            using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _openAI.ApiKey);
            var body = new
            {
                model = _openAI.ChatModel,
                temperature = _options.Temperature,
                tools = toolSchemas,
                tool_choice = "auto",
                stream = true,
                messages
            };
            req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            using var res = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            res.EnsureSuccessStatusCode();
            using var stream = await res.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(stream);
            string? line;
            string? toolName = null;
            JsonNode? toolArgs = null;
            var textBuffer = new StringBuilder();

            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (ct.IsCancellationRequested) yield break;
                if (!line.StartsWith("data:")) continue;
                var payload = line[5..].Trim();
                if (payload == "[DONE]") break;
                string? pieceToYield = null;
                using var doc = JsonDocument.Parse(payload);
                var root = doc.RootElement;
                var delta = root.GetProperty("choices")[0].GetProperty("delta");
                if (delta.TryGetProperty("tool_calls", out var toolCalls) && toolCalls.ValueKind == JsonValueKind.Array)
                {
                    var first = toolCalls[0];
                    toolName = first.GetProperty("function").GetProperty("name").GetString();
                    var argsStr = first.GetProperty("function").GetProperty("arguments").GetString() ?? "{}";
                    toolArgs = JsonNode.Parse(argsStr);
                }
                if (delta.TryGetProperty("content", out var contentElem))
                {
                    pieceToYield = contentElem.GetString();
                }
                if (!string.IsNullOrEmpty(pieceToYield))
                {
                    textBuffer.Append(pieceToYield);
                    yield return pieceToYield;
                }
            }

            assistantContentBuffer = textBuffer.ToString();

            if (toolName != null && toolCallsLeft > 0 && tools.TryGetValue(toolName, out var tool))
            {
                // append assistant tool call message
                messages.Add(new
                {
                    role = "assistant",
                    content = (string?)null,
                    tool_calls = new object[]
                    {
                        new { id = Guid.NewGuid().ToString("N"), type = "function", function = new { name = toolName, arguments = toolArgs?.ToJsonString() ?? "{}" } }
                    }
                });

                var result = await tool.InvokeAsync(familyId, toolArgs ?? new JsonObject(), ct);
                // provide tool result
                messages.Add(new { role = "tool", name = toolName, content = JsonSerializer.Serialize(result.Result) });
                toolCallsLeft--;
                // loop for another model turn to use tool result
                continue;
            }

            // no tool call: finalize
            if (onAssistantMessageComplete != null && assistantContentBuffer is not null)
                await onAssistantMessageComplete(assistantContentBuffer);
            yield break;
        }
    }
}

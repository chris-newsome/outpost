using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace FamilyManagement.API.Application.Assistant.Retrieval;

public sealed class EmbeddingsService
{
    private readonly HttpClient _http;
    private readonly OpenAIOptions _options;

    public EmbeddingsService(HttpClient http, OpenAIOptions options)
    {
        _http = http;
        _options = options;
    }

    public async Task<float[]> EmbedAsync(string input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("OPENAI_API_KEY not configured");

        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/embeddings");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        var body = new
        {
            model = _options.EmbeddingModel,
            input
        };
        req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        using var res = await _http.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();
        using var stream = await res.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        var vec = doc.RootElement
            .GetProperty("data")[0]
            .GetProperty("embedding")
            .EnumerateArray()
            .Select(e => (float)e.GetDouble())
            .ToArray();
        return vec;
    }
}


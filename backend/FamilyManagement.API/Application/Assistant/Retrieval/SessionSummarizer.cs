using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace FamilyManagement.API.Application.Assistant.Retrieval;

public sealed class SessionSummarizer
{
    private readonly HttpClient _http;
    private readonly OpenAIOptions _options;

    public SessionSummarizer(HttpClient http, OpenAIOptions options)
    {
        _http = http;
        _options = options;
    }

    public async Task<string> SummarizeAsync(string transcript, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("OPENAI_API_KEY not configured");

        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        var system = "Summarize the session into 3-5 crisp bullets capturing decisions, preferences, and follow-ups. Keep under 80 words.";
        var body = new
        {
            model = _options.ChatModel,
            temperature = 0,
            messages = new object[]
            {
                new { role = "system", content = system },
                new { role = "user", content = transcript }
            }
        };
        req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        using var res = await _http.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();
        using var stream = await res.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
    }
}


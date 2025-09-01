using System;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Threading;
using System.Text.Json;
using FamilyManagement.API.Application.Assistant;
using FamilyManagement.API.Application.Assistant.Retrieval;
using Xunit;

public class OrchestratorTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly string _sse;
        public StubHandler(string sse) { _sse = sse; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var res = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(_sse, Encoding.UTF8, "text/event-stream")
            };
            return Task.FromResult(res);
        }
    }

    [Fact]
    public async Task StreamsAssistantText()
    {
        // SSE chunk for assistant content only
        var sse = "data: {\"choices\":[{\"delta\":{\"content\":\"Hello\"}}]}\n\n" +
                  "data: {\"choices\":[{\"delta\":{\"content\":\" world\"}}]}\n\n" +
                  "data: [DONE]\n\n";
        var http = new HttpClient(new StubHandler(sse));
        var embeddings = new EmbeddingsService(http, new OpenAIOptions { ApiKey = "test", EmbeddingModel = "text-embedding-3-small", ChatModel = "gpt-4o-mini" });
        var vector = new VectorStore("Host=localhost;Database=test;Username=postgres;Password=postgres");
        var orch = new Orchestrator(Array.Empty<IAssistantTool>(), embeddings, vector, new OpenAIOptions { ApiKey = "test" }, new AssistantOptions(), http);
        var output = new StringBuilder();
        await foreach (var token in orch.ChatStreamAsync(Guid.NewGuid(), "Session", "Hi", Array.Empty<(string, object)>(), CancellationToken.None))
        {
            output.Append(token);
        }
        Assert.Contains("Hello", output.ToString());
    }
}

using System.Text;
using System.Text.Json;
using FamilyManagement.API.Application.Assistant;
using FamilyManagement.API.Application.Assistant.Retrieval;
using FamilyManagement.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FamilyManagement.API.Controllers;

[ApiController]
[Route("api/assistant")] 
public sealed class AssistantController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly Orchestrator _orchestrator;
    private readonly EmbeddingsService _embeddings;
    private readonly VectorStore _vectorStore;

    public AssistantController(AppDbContext db, Orchestrator orchestrator, EmbeddingsService embeddings, VectorStore vectorStore)
    {
        _db = db;
        _orchestrator = orchestrator;
        _embeddings = embeddings;
        _vectorStore = vectorStore;
    }

    [AllowAnonymous]
    [HttpPost("webhook/plaid")]
    public IActionResult PlaidWebhook()
    {
        // Stub: accept webhook and return 200; real impl should verify, update finance domain and embeddings
        return Ok(new { ok = true });
    }

    [AllowAnonymous]
    [HttpPost("webhook/finicity")]
    public IActionResult FinicityWebhook()
    {
        // Stub: accept webhook and return 200
        return Ok(new { ok = true });
    }

    public sealed record ChatRequest(Guid? SessionId, string Message);

    [Authorize]
    [HttpPost("chat")]
    public async Task Chat([FromBody] ChatRequest req, CancellationToken ct)
    {
        // Identify family from membership
        var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst("user_id")?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim)) { Response.StatusCode = 401; return; }
        // For demo, pick first family of user
        var membership = await _db.Memberships.FirstOrDefaultAsync(m => m.UserId.ToString() == userIdClaim, ct);
        if (membership == null) { Response.StatusCode = 403; return; }
        var familyId = membership.FamilyId;

        // Load session messages (recent)
        var recent = new List<(string role, object content)>();
        // Optional: persist in ai_chat_* tables; for now use in-memory recent pass-through

        Response.StatusCode = 200;
        Response.ContentType = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["Connection"] = "keep-alive";

        var retrieved = new List<(Guid id, string src, Guid? srcId)>();
        var assistantText = new StringBuilder();
        // Optionally ensure a session exists
        Guid sessionId = req.SessionId ?? Guid.NewGuid();
        // Persist session + user message (best-effort)
        await PersistChatMessageAsync(sessionId, familyId, "user", new { content = req.Message }, ct);
        await foreach (var token in _orchestrator.ChatStreamAsync(
            familyId,
            sessionTitle: "Session",
            userMessage: req.Message,
            recentMessages: recent,
            ct: ct,
            onRetrieved: list => retrieved = list,
            onAssistantMessageComplete: msg => { assistantText.Append(msg); return Task.CompletedTask; }))
        {
            await Response.WriteAsync($"data: {JsonSerializer.Serialize(new { content = token })}\n\n");
            await Response.Body.FlushAsync(ct);
        }

        // Send citations if any
        if (retrieved.Count > 0)
        {
            await Response.WriteAsync($"data: {JsonSerializer.Serialize(new { sources = retrieved.Select(r => new { id = r.id, src = r.src }) })}\n\n");
        }

        // Persist assistant reply
        if (assistantText.Length > 0)
        {
            await PersistChatMessageAsync(sessionId, familyId, "assistant", new { content = assistantText.ToString() }, ct);
        }
        // Final done event
        await Response.WriteAsync("data: [DONE]\n\n");
    }

    private async Task PersistChatMessageAsync(Guid sessionId, Guid familyId, string role, object content, CancellationToken ct)
    {
        try
        {
            await using var conn = new Npgsql.NpgsqlConnection(_db.Database.GetConnectionString());
            await conn.OpenAsync(ct);
            // Ensure session exists
            await using (var cmd = new Npgsql.NpgsqlCommand("insert into ai_chat_sessions(id, family_id) values (@id,@fid) on conflict (id) do nothing", conn))
            {
                cmd.Parameters.AddWithValue("id", sessionId);
                cmd.Parameters.AddWithValue("fid", familyId);
                await cmd.ExecuteNonQueryAsync(ct);
            }
            // Insert message
            await using (var cmd2 = new Npgsql.NpgsqlCommand("insert into ai_chat_messages(session_id, family_id, role, content) values (@sid,@fid,@role,@content)", conn))
            {
                cmd2.Parameters.AddWithValue("sid", sessionId);
                cmd2.Parameters.AddWithValue("fid", familyId);
                cmd2.Parameters.AddWithValue("role", role);
                cmd2.Parameters.AddWithValue("content", JsonSerializer.SerializeToElement(content));
                await cmd2.ExecuteNonQueryAsync(ct);
            }
        }
        catch
        {
            // swallow persistence errors in streaming path
        }
    }

    public sealed record IndexRequest(bool Rebuild = false);

    [Authorize(Roles = "admin")]
    [HttpPost("index")]
    public async Task<IActionResult> Index([FromBody] IndexRequest req, CancellationToken ct)
    {
        // Simple indexer for tasks/bills/docs; add others similarly
        var families = await _db.Families.ToListAsync(ct);
        foreach (var f in families)
        {
            var tasks = await _db.Tasks.Where(t => t.FamilyId == f.Id).ToListAsync(ct);
            foreach (var t in tasks)
            {
                var chunk = $"Task: {t.Title}. {(t.Description ?? string.Empty)} Due: {t.DueDate?.ToString("yyyy-MM-dd")}";
                var vec = await _embeddings.EmbedAsync(chunk, ct);
                await _vectorStore.UpsertEmbeddingAsync(f.Id, "task", t.Id, chunk, vec, ct);
            }
            var bills = await _db.Bills.Where(b => b.FamilyId == f.Id).ToListAsync(ct);
            foreach (var b in bills)
            {
                var chunk = $"Bill: {b.Vendor}. Amount: {b.Amount}. Due: {b.DueDate:yyyy-MM-dd}. Status: {b.Status}";
                var vec = await _embeddings.EmbedAsync(chunk, ct);
                await _vectorStore.UpsertEmbeddingAsync(f.Id, "bill", b.Id, chunk, vec, ct);
            }
            var docs = await _db.Documents.Where(d => d.FamilyId == f.Id).ToListAsync(ct);
            foreach (var d in docs)
            {
                var chunk = $"Document: {d.Name}. Type: {d.ContentType}";
                var vec = await _embeddings.EmbedAsync(chunk, ct);
                await _vectorStore.UpsertEmbeddingAsync(f.Id, "doc", d.Id, chunk, vec, ct);
            }
        }
        return Ok(new { ok = true });
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FamilyManagement.API.Data;
using FamilyManagement.API.Models;
using FamilyManagement.API.Services;

namespace FamilyManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class DocumentsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ISupabaseService _supabase;
    private const string Bucket = "documents";

    public DocumentsController(AppDbContext db, ISupabaseService supabase)
    {
        _db = db;
        _supabase = supabase;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Document>>> Get([FromQuery] Guid familyId, CancellationToken ct)
    {
        var items = await _db.Documents.Where(d => d.FamilyId == familyId).OrderByDescending(d => d.CreatedAt).ToListAsync(ct);
        return Ok(items);
    }

    [HttpPost("upload")]
    [RequestSizeLimit(20_000_000)]
    public async Task<ActionResult<Document>> Upload([FromForm] Guid familyId, [FromForm] IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return BadRequest("No file");
        var path = $"{familyId}/{Guid.NewGuid()}-{file.FileName}";
        await using var stream = file.OpenReadStream();
        var storedPath = await _supabase.UploadDocumentAsync(Bucket, path, stream, file.ContentType, ct);
        var doc = new Document
        {
            Id = Guid.NewGuid(),
            FamilyId = familyId,
            Name = file.FileName,
            ContentType = file.ContentType,
            StoragePath = storedPath,
            UploadedByUserId = User.Identity?.Name
        };
        _db.Documents.Add(doc);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = doc.Id }, doc);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Document>> GetById(Guid id, CancellationToken ct)
    {
        var doc = await _db.Documents.FindAsync(new object[] { id }, ct);
        return doc is null ? NotFound() : Ok(doc);
    }

    [HttpGet("{id:guid}/signed-url")]
    public async Task<ActionResult<object>> GetSignedUrl(Guid id, CancellationToken ct)
    {
        var doc = await _db.Documents.FindAsync(new object[] { id }, ct);
        if (doc is null) return NotFound();
        var url = await _supabase.CreateSignedUrlAsync(Bucket, doc.StoragePath, TimeSpan.FromMinutes(10), ct);
        return Ok(new { url });
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
    {
        var doc = await _db.Documents.FindAsync(new object[] { id }, ct);
        if (doc is null) return NotFound();
        _db.Documents.Remove(doc);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}


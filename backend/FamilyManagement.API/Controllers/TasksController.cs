using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FamilyManagement.API.Data;
using FamilyManagement.API.Models;

namespace FamilyManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class TasksController : ControllerBase
{
    private readonly AppDbContext _db;

    public TasksController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TaskItem>>> Get([FromQuery] Guid familyId, CancellationToken ct)
    {
        var items = await _db.Tasks.Where(t => t.FamilyId == familyId).OrderBy(t => t.DueDate).ToListAsync(ct);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TaskItem>> GetById(Guid id, CancellationToken ct)
    {
        var item = await _db.Tasks.FindAsync(new object[] { id }, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<TaskItem>> Create([FromBody] TaskItem item, CancellationToken ct)
    {
        item.Id = Guid.NewGuid();
        item.CreatedAt = DateTimeOffset.UtcNow;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        _db.Tasks.Add(item);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update(Guid id, [FromBody] TaskItem update, CancellationToken ct)
    {
        var existing = await _db.Tasks.FindAsync(new object[] { id }, ct);
        if (existing is null) return NotFound();
        existing.Title = update.Title;
        existing.Description = update.Description;
        existing.DueDate = update.DueDate;
        existing.Completed = update.Completed;
        existing.AssignedToUserId = update.AssignedToUserId;
        existing.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
    {
        var existing = await _db.Tasks.FindAsync(new object[] { id }, ct);
        if (existing is null) return NotFound();
        _db.Tasks.Remove(existing);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}


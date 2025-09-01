using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FamilyManagement.API.Data;
using FamilyManagement.API.Models;

namespace FamilyManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class BillsController : ControllerBase
{
    private readonly AppDbContext _db;

    public BillsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Bill>>> Get([FromQuery] Guid familyId, CancellationToken ct)
    {
        var items = await _db.Bills.Where(b => b.FamilyId == familyId).OrderBy(b => b.DueDate).ToListAsync(ct);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Bill>> GetById(Guid id, CancellationToken ct)
    {
        var item = await _db.Bills.FindAsync(new object[] { id }, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<Bill>> Create([FromBody] Bill item, CancellationToken ct)
    {
        item.Id = Guid.NewGuid();
        item.CreatedAt = DateTimeOffset.UtcNow;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        _db.Bills.Add(item);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update(Guid id, [FromBody] Bill update, CancellationToken ct)
    {
        var existing = await _db.Bills.FindAsync(new object[] { id }, ct);
        if (existing is null) return NotFound();
        existing.Vendor = update.Vendor;
        existing.Amount = update.Amount;
        existing.DueDate = update.DueDate;
        existing.Status = update.Status;
        existing.Category = update.Category;
        existing.Recurring = update.Recurring;
        existing.SubscriptionId = update.SubscriptionId;
        existing.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
    {
        var existing = await _db.Bills.FindAsync(new object[] { id }, ct);
        if (existing is null) return NotFound();
        _db.Bills.Remove(existing);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}


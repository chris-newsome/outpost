using FamilyManagement.API.Models;
using Microsoft.EntityFrameworkCore;

namespace FamilyManagement.API.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<Bill> Bills => Set<Bill>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Family> Families => Set<Family>();
    public DbSet<Membership> Memberships => Set<Membership>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<FinanceItem> FinanceItems => Set<FinanceItem>();
    public DbSet<FinanceAccount> FinanceAccounts => Set<FinanceAccount>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Membership>().HasKey(m => new { m.FamilyId, m.UserId });

        modelBuilder.Entity<TaskItem>()
            .HasIndex(t => new { t.FamilyId, t.DueDate });

        modelBuilder.Entity<Bill>()
            .HasIndex(b => new { b.FamilyId, b.DueDate, b.Status });

        modelBuilder.Entity<Document>()
            .HasIndex(d => new { d.FamilyId, d.CreatedAt });

        modelBuilder.Entity<Subscription>()
            .HasIndex(s => new { s.FamilyId, s.NextDueDate });

        modelBuilder.Entity<FinanceItem>()
            .HasIndex(i => new { i.FamilyId, i.Provider, i.ItemId });

        modelBuilder.Entity<FinanceAccount>()
            .HasIndex(a => new { a.FamilyId, a.AccountId });
    }
}


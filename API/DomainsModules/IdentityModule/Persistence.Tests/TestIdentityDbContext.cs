using IdentityModule.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace IdentityModule.Persistence.Tests;

internal sealed class TestIdentityDbContext(DbContextOptions<TestIdentityDbContext> options)
    : DbContext(options), IIdentityModuleDbContext
{
    public DbSet<AccountListEntry> AccountListEntries => Set<AccountListEntry>();

    public static TestIdentityDbContext Create()
    {
        var context = new TestIdentityDbContext(new DbContextOptionsBuilder<TestIdentityDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options);
        context.Database.OpenConnection();
        context.Database.EnsureCreated();
        return context;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<AccountListEntry>(account =>
        {
            account.ToTable("AccountList");
            account.HasKey(entry => entry.AccountId);
        });
}

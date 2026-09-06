using EventSourcing.Persistence;
using EventSourcing.Persistence.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace OutboxProcessingModule.Tests;

/// <summary>
/// Sqlite has no store-generated row version, so this context stamps one on
/// every save. That reproduces the concurrency behaviour the PostgreSQL mapping
/// gets from its shadow xmin property.
/// </summary>
public sealed class OutboxTestDbContext(DbContextOptions<EventSourcingDbContext> options)
    : EventSourcingDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder
            .Entity<UniqueEventConstraint>()
            .HasKey(constraint => constraint.ConstraintHash)
            .Metadata.RemoveAnnotation("SqlServer:Clustered");

        builder
            .Entity<SerializedPayloadMessage>()
            .Property(message => message.Version)
            .IsConcurrencyToken()
            .ValueGeneratedNever();
    }

    public override Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<SerializedPayloadMessage>())
            if (entry.State is EntityState.Added or EntityState.Modified)
                entry.Entity.Version = Guid.NewGuid().ToByteArray();

        return base.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// One in-memory Sqlite database shared by every context a test opens. Each
/// context gets its own connection through the shared cache, so two of them can
/// race for the same row the way two publishers would.
/// </summary>
public sealed class OutboxDatabase : IDisposable
{
    private readonly string _connectionString =
        $"DataSource=file:{Guid.NewGuid():N}?mode=memory&cache=shared";

    private readonly SqliteConnection _keepAlive;

    public OutboxDatabase()
    {
        // The database lives only while at least one connection to it is open.
        _keepAlive = new SqliteConnection(_connectionString);
        _keepAlive.Open();

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public OutboxTestDbContext CreateContext() =>
        new(
            new DbContextOptionsBuilder<EventSourcingDbContext>()
                .UseSqlite(_connectionString)
                .Options
        );

    public void Dispose() => _keepAlive.Dispose();
}

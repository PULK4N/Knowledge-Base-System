using EventSourcing.Persistence;
using EventSourcing.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace PostgreSqlModule;

internal sealed class PostgreSqlEventSourcingDbContext(
    DbContextOptions<EventSourcingDbContext> options
) : EventSourcingDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var payloadMessage = modelBuilder.Entity<SerializedPayloadMessage>();

        payloadMessage.Ignore(message => message.Version);
        payloadMessage
            .Property<uint>("xmin")
            .HasColumnName("xmin")
            .IsRowVersion();

        modelBuilder
            .Entity<UniqueEventConstraint>()
            .HasKey(constraint => constraint.ConstraintHash)
            .Metadata.RemoveAnnotation("SqlServer:Clustered");
    }
}

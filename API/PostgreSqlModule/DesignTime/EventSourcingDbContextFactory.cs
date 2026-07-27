using EventSourcing.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PostgreSqlModule.DesignTime;

public sealed class EventSourcingDbContextFactory
    : IDesignTimeDbContextFactory<EventSourcingDbContext>
{
    public EventSourcingDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<EventSourcingDbContext>();

        PostgreSqlDbContextOptions.Configure(
            options,
            DesignTimeConnectionString.Get(),
            PostgreSqlModuleDefaults.EventSourcingMigrationsHistoryTable
        );

        return new PostgreSqlEventSourcingDbContext(options.Options);
    }
}

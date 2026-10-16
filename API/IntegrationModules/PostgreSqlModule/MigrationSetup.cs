using AttachmentsModule.Persistence;
using EventSourcing.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace PostgreSqlModule;

public static class MigrationSetup
{
    public static async Task ApplyPostgreSqlMigrations(
        this IServiceProvider services
    )
    {
        using var scope = services.CreateScope();

        await scope
            .ServiceProvider
            .GetRequiredService<EventSourcingDbContext>()
            .Database
            .MigrateAsync();
        await scope
            .ServiceProvider
            .GetRequiredService<AttachmentsModuleDbContext>()
            .Database
            .MigrateAsync();
    }
}

using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;

namespace PostgreSqlModule;

internal static class PostgreSqlDbContextOptions
{
    public static void Configure(
        DbContextOptionsBuilder options,
        string connectionString,
        string migrationsHistoryTable
    )
    {
        options.UseNpgsql(
            connectionString,
            npgsql =>
            {
                npgsql.MigrationsAssembly(
                    typeof(PostgreSqlDbContextOptions)
                        .Assembly
                        .GetName()
                        .Name
                );
                npgsql.MigrationsHistoryTable(migrationsHistoryTable);
                npgsql.UseVector();
            }
        );
    }
}

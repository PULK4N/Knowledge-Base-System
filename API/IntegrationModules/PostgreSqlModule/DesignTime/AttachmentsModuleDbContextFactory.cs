using AttachmentsModule.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PostgreSqlModule.DesignTime;

public sealed class AttachmentsModuleDbContextFactory
    : IDesignTimeDbContextFactory<AttachmentsModuleDbContext>
{
    public AttachmentsModuleDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AttachmentsModuleDbContext>();

        PostgreSqlDbContextOptions.Configure(
            options,
            DesignTimeConnectionString.Get(),
            PostgreSqlModuleDefaults.AttachmentsMigrationsHistoryTable
        );

        return new AttachmentsModuleDbContext(options.Options);
    }
}

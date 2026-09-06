using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SkillsModule.Persistence;

namespace PostgreSqlModule.DesignTime;

public sealed class SkillsModuleDbContextFactory
    : IDesignTimeDbContextFactory<SkillsModuleDbContext>
{
    public SkillsModuleDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SkillsModuleDbContext>();

        PostgreSqlDbContextOptions.Configure(
            options,
            DesignTimeConnectionString.Get(),
            PostgreSqlModuleDefaults.SkillsMigrationsHistoryTable
        );

        return new SkillsModuleDbContext(options.Options);
    }
}

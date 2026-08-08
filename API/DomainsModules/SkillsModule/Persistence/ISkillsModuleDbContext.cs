using Microsoft.EntityFrameworkCore;
using SkillsModule.Persistence.Models;

namespace SkillsModule.Persistence;

public interface ISkillsModuleDbContext
{
    DbSet<SkillSummaryEntry> SkillSummaries { get; }

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default
    );
}

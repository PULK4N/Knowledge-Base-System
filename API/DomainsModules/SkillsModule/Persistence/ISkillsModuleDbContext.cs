using Microsoft.EntityFrameworkCore;
using SharedModule.Persistence;
using SkillsModule.Persistence.Models;

namespace SkillsModule.Persistence;

public interface ISkillsModuleDbContext : IEntityRelationDbContext
{
    DbSet<SkillSummaryEntry> SkillSummaries { get; }
    DbSet<SkillListEntry> SkillListEntries { get; }
    DbSet<SkillListTagEntry> SkillListTags { get; }
}

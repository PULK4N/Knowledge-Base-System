using Microsoft.EntityFrameworkCore;
using SkillsModule.Domain;
using SkillsModule.Persistence.Interfaces;
using SkillsModule.Persistence.Models;

namespace SkillsModule.Persistence;

public sealed class SkillSummaryRepository(
    ISkillsModuleDbContext dbContext
) : ISkillSummaryRepository
{
    public Task<List<SkillsModule.Persistence.Interfaces.SkillSummary>> List() =>
        dbContext.SkillSummaries
            .AsNoTracking()
            .OrderBy(summary => summary.Name)
            .ThenBy(summary => summary.SkillAggregateId)
            .Select(
                summary => new SkillsModule.Persistence.Interfaces.SkillSummary(
                    summary.SkillAggregateId,
                    summary.Name
                )
            )
            .ToListAsync();

    public async Task Replace(List<SkillStateData> skills)
    {
        var skillIds = skills
            .Select(skill => skill.Id.Value)
            .ToList();

        await dbContext.SkillSummaries
            .Where(
                summary => skillIds.Contains(
                    summary.SkillAggregateId
                )
            )
            .ExecuteDeleteAsync();

        await dbContext.SkillSummaries.AddRangeAsync(
            skills
                .Where(skill => !skill.IsDeleted)
                .Select(
                    skill => new SkillSummaryEntry
                    {
                        SkillAggregateId = skill.Id.Value,
                        Name = skill.Name
                    }
                )
        );
        await dbContext.SaveChangesAsync();
    }
}

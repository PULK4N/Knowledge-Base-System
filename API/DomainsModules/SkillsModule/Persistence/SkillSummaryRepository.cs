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

    public Task<SkillSummary?> GetByName(
        string name,
        CancellationToken cancellationToken = default
    )
    {
        var normalizedName = name.Trim().ToUpperInvariant();

        return dbContext.SkillSummaries
            .AsNoTracking()
            .Where(summary => summary.Name.ToUpper() == normalizedName)
            .Select(
                summary => new SkillSummary(
                    summary.SkillAggregateId,
                    summary.Name
                )
            )
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<SkillSummarySearchResult> Search(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default
    )
    {
        var query = dbContext.SkillSummaries.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLowerInvariant();
            query = query.Where(
                summary => summary.Name.ToLower().Contains(normalizedSearch)
            );
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(summary => summary.Name)
            .ThenBy(summary => summary.SkillAggregateId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(
                summary => new SkillsModule.Persistence.Interfaces.SkillSummary(
                    summary.SkillAggregateId,
                    summary.Name
                )
            )
            .ToListAsync(cancellationToken);

        return new SkillSummarySearchResult(items, totalCount);
    }

    public async Task Write(List<SkillStateData> skills)
    {
        var context = dbContext as DbContext
            ?? throw new InvalidOperationException(
                $"{nameof(ISkillsModuleDbContext)} must be implemented by a {nameof(DbContext)}."
            );
        await using var transaction = context.Database.CurrentTransaction is null
            ? await context.Database.BeginTransactionAsync()
            : null;
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

        if (transaction is not null)
            await transaction.CommitAsync();
    }
}

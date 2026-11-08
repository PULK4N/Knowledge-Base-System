using Microsoft.EntityFrameworkCore;
using SettingsModule.Application.Models;
using SettingsModule.Application.Persistence;
using SettingsModule.Domain;
using SettingsModule.Persistence.Models;
using SharedModule.Constants;

namespace SettingsModule.Persistence;

public sealed class CodeReviewSettingsRepository(ISettingsModuleDbContext dbContext)
    : ICodeReviewSettingsReader
{
    public Task<CodeReviewSettingsSnapshot?> Get(CancellationToken cancellationToken = default) =>
        dbContext.CodeReviewSettings.AsNoTracking()
            .Where(value => value.AggregateId == StateDataAggregateIds.Settings)
            .Select(value => new CodeReviewSettingsSnapshot(value.BatchSize, value.TaskMessage))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task Write(List<SettingsStateData> states)
    {
        foreach (var state in states.Where(value => value.IsInitialized)
            .GroupBy(value => value.Id)
            .Select(group => group.OrderByDescending(value => value.OrderNumber).First()))
        {
            var existing = await dbContext.CodeReviewSettings
                .SingleOrDefaultAsync(value => value.AggregateId == state.Id.Value);
            if (existing is not null && existing.OrderNumber >= state.OrderNumber)
                continue;
            if (existing is null)
            {
                existing = new CodeReviewSettingsEntry { AggregateId = state.Id.Value };
                dbContext.CodeReviewSettings.Add(existing);
            }
            existing.BatchSize = state.CodeReviewBatchSize;
            existing.TaskMessage = state.CodeReviewTaskMessage;
            existing.OrderNumber = state.OrderNumber;
        }
        await dbContext.SaveChangesAsync();
    }
}

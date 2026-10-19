using ActionModule.Shared;
using ActionModule.Shared.Models;
using MemoryModule.Persistence.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace MemoryModule.Application.Queries;

/// <summary>
/// Returns every model recorded on a memory prompt. The list changes rarely
/// and grouping all sessions costs a table scan, so it is cached for four
/// hours; a model recorded for the first time appears after the cache expires.
/// </summary>
public sealed class GetMemoryModelsQuery(
    IMemorySummaryRepository repository,
    IMemoryCache cache
) : Query<List<string>>
{
    public static readonly TimeSpan CacheDuration = TimeSpan.FromHours(4);

    private const string CacheKey = "memory-models";

    protected override async Task<List<string>> ExecuteInternal(
        Executor executor
    )
    {
        var models = await cache.GetOrCreateAsync(
            CacheKey,
            entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                return repository.GetModels();
            }
        );

        return models!.ToList();
    }
}

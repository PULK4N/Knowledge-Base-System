using FeatureModule.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace FeatureModule.Persistence;

public interface IFeatureModuleDbContext
{
    DbSet<FeatureSummaryEntry> FeatureSummaries { get; }

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default
    );
}

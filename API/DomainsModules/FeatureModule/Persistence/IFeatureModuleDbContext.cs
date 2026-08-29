using FeatureModule.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using SharedModule.Persistence;

namespace FeatureModule.Persistence;

public interface IFeatureModuleDbContext : IEntityRelationDbContext
{
    DbSet<FeatureSummaryEntry> FeatureSummaries { get; }
    DbSet<FeatureSearchEntry> FeatureSearchEntries { get; }
}

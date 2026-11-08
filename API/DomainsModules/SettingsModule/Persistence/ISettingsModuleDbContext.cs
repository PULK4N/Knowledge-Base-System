using Microsoft.EntityFrameworkCore;
using SettingsModule.Persistence.Models;

namespace SettingsModule.Persistence;

public interface ISettingsModuleDbContext
{
    DbSet<CodeReviewSettingsEntry> CodeReviewSettings { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

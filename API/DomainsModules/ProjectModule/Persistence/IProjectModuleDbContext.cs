using Microsoft.EntityFrameworkCore;
using ProjectModule.Persistence.Models;

namespace ProjectModule.Persistence;

public interface IProjectModuleDbContext
{
    DbSet<ProjectPolicyText> ProjectPolicyTexts { get; }
    DbSet<PolicyProjectSummaryEntry> PolicyProjectSummaries { get; }
    DbSet<ProjectPolicyTopic> ProjectPolicyTopics { get; }

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default
    );
}

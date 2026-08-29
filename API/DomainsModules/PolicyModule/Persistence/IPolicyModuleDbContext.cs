using Microsoft.EntityFrameworkCore;
using PolicyModule.Persistence.Models;

namespace PolicyModule.Persistence;

public interface IPolicyModuleDbContext
{
    DbSet<GeneralPolicyText> GeneralPolicyTexts { get; }
    DbSet<ProjectPolicyText> ProjectPolicyTexts { get; }
    DbSet<PolicyProjectSummaryEntry> PolicyProjectSummaries { get; }
    DbSet<TopicPolicyText> TopicPolicyTexts { get; }
    DbSet<ProjectPolicyTopic> ProjectPolicyTopics { get; }
    DbSet<AgentFamilyPolicyText> AgentFamilyPolicyTexts { get; }

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default
    );
}

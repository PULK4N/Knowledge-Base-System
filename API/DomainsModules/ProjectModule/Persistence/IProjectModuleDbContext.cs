using Microsoft.EntityFrameworkCore;
using ProjectModule.Persistence.Models;
using SharedModule.Persistence;

namespace ProjectModule.Persistence;

public interface IProjectModuleDbContext : IEntityRelationDbContext
{
    DbSet<ProjectPolicyText> ProjectPolicyTexts { get; }
    DbSet<PolicyProjectSummaryEntry> PolicyProjectSummaries { get; }
    DbSet<ProjectPolicyTopic> ProjectPolicyTopics { get; }
}

using Microsoft.EntityFrameworkCore;
using PolicyModule.Persistence.Models;

namespace PolicyModule.Persistence.Tests;

internal sealed class TestPolicyDbContext(
    DbContextOptions<TestPolicyDbContext> options
) : DbContext(options), IPolicyModuleDbContext
{
    public DbSet<GeneralPolicyText> GeneralPolicyTexts =>
        Set<GeneralPolicyText>();
    public DbSet<ProjectPolicyText> ProjectPolicyTexts =>
        Set<ProjectPolicyText>();
    public DbSet<PolicyProjectSummaryEntry> PolicyProjectSummaries =>
        Set<PolicyProjectSummaryEntry>();
    public DbSet<TopicPolicyText> TopicPolicyTexts =>
        Set<TopicPolicyText>();
    public DbSet<ProjectPolicyTopic> ProjectPolicyTopics =>
        Set<ProjectPolicyTopic>();
    public DbSet<AgentFamilyPolicyText> AgentFamilyPolicyTexts =>
        Set<AgentFamilyPolicyText>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .Entity<GeneralPolicyText>()
            .HasKey(text => text.Id);
        modelBuilder
            .Entity<ProjectPolicyText>()
            .HasKey(text => text.Id);
        modelBuilder
            .Entity<PolicyProjectSummaryEntry>()
            .HasKey(summary => summary.Id);
        modelBuilder
            .Entity<TopicPolicyText>()
            .HasKey(text => text.Id);
        modelBuilder
            .Entity<ProjectPolicyTopic>()
            .HasKey(relation => relation.Id);
        modelBuilder
            .Entity<AgentFamilyPolicyText>()
            .HasKey(text => text.Id);
        modelBuilder
            .Entity<ProjectPolicyTopic>()
            .HasIndex(
                relation =>
                    new
                    {
                        relation.ProjectAggregateId,
                        relation.TopicName
                    }
            )
            .IsUnique();
    }
}

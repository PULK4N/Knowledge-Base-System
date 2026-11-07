using Microsoft.EntityFrameworkCore;
using PolicyModule.Persistence;
using PolicyModule.Persistence.Models;
using ProjectModule.Persistence.Models;

namespace ProjectModule.Persistence.Tests;

/// <summary>
/// One database for both modules, as in production, so the Agents.MD text can
/// be composed from the policy and project projections together.
/// </summary>
internal sealed class TestKnowledgeBaseDbContext(
    DbContextOptions<TestKnowledgeBaseDbContext> options
) : DbContext(options), IPolicyModuleDbContext, IProjectModuleDbContext
{
    public DbSet<GeneralPolicyText> GeneralPolicyTexts =>
        Set<GeneralPolicyText>();
    public DbSet<TopicPolicyText> TopicPolicyTexts =>
        Set<TopicPolicyText>();
    public DbSet<AgentFamilyPolicyText> AgentFamilyPolicyTexts =>
        Set<AgentFamilyPolicyText>();
    public DbSet<ProjectPolicyText> ProjectPolicyTexts =>
        Set<ProjectPolicyText>();
    public DbSet<PolicyProjectSummaryEntry> PolicyProjectSummaries =>
        Set<PolicyProjectSummaryEntry>();
    public DbSet<ProjectPolicyTopic> ProjectPolicyTopics =>
        Set<ProjectPolicyTopic>();

    public static TestKnowledgeBaseDbContext Create()
    {
        var context = new TestKnowledgeBaseDbContext(
            new DbContextOptionsBuilder<TestKnowledgeBaseDbContext>()
                .UseSqlite("Data Source=:memory:")
                .Options
        );
        context.Database.OpenConnection();
        context.Database.EnsureCreated();

        return context;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GeneralPolicyText>().HasKey(text => text.Id);
        modelBuilder.Entity<TopicPolicyText>().HasKey(text => text.Id);
        modelBuilder.Entity<AgentFamilyPolicyText>().HasKey(text => text.Id);
        modelBuilder.Entity<ProjectPolicyText>().HasKey(text => text.Id);
        modelBuilder.Entity<PolicyProjectSummaryEntry>().HasKey(summary => summary.Id);
        modelBuilder.Entity<ProjectPolicyTopic>().HasKey(relation => relation.Id);
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

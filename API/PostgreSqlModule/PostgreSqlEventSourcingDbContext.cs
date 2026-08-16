using EventSourcing.Persistence;
using EventSourcing.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using PolicyModule.Persistence;
using PolicyModule.Persistence.Models;
using SkillsModule.Persistence;
using SkillsModule.Persistence.Models;

namespace PostgreSqlModule;

internal sealed class PostgreSqlEventSourcingDbContext(
    DbContextOptions<EventSourcingDbContext> options
) : EventSourcingDbContext(options),
    IPolicyModuleDbContext,
    ISkillsModuleDbContext
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
    public DbSet<SkillSummaryEntry> SkillSummaries =>
        Set<SkillSummaryEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("vector");

        var payloadMessage = modelBuilder.Entity<SerializedPayloadMessage>();

        payloadMessage.Ignore(message => message.Version);
        payloadMessage
            .Property<uint>("xmin")
            .HasColumnName("xmin")
            .IsRowVersion();

        modelBuilder
            .Entity<UniqueEventConstraint>()
            .HasKey(constraint => constraint.ConstraintHash)
            .Metadata.RemoveAnnotation("SqlServer:Clustered");

        modelBuilder.Entity<ProjectPolicyText>(
            policyText =>
            {
                policyText.HasKey(text => text.Id);
                policyText
                    .HasIndex(text => text.ProjectAggregateId)
                    .IsUnique();
                policyText.Property(text => text.Text).IsRequired();
            }
        );

        modelBuilder.Entity<PolicyProjectSummaryEntry>(
            summary =>
            {
                summary.HasKey(project => project.Id);
                summary
                    .HasIndex(project => project.ProjectAggregateId)
                    .IsUnique();
                summary
                    .HasIndex(project => project.ProjectName)
                    .IsUnique();
                summary.Property(project => project.ProjectName).IsRequired();
                summary
                    .Property(project => project.RepositoryPathsJson)
                    .IsRequired();
            }
        );

        modelBuilder.Entity<GeneralPolicyText>(
            policyText =>
            {
                policyText.HasKey(text => text.Id);
                policyText
                    .HasIndex(text => text.AggregateId)
                    .IsUnique();
                policyText.Property(text => text.Text).IsRequired();
            }
        );

        modelBuilder.Entity<TopicPolicyText>(
            policyText =>
            {
                policyText.HasKey(text => text.Id);
                policyText
                    .HasIndex(text => text.TopicName)
                    .IsUnique();
                policyText.Property(text => text.Text).IsRequired();
            }
        );

        modelBuilder.Entity<ProjectPolicyTopic>(
            relation =>
            {
                relation.HasKey(topic => topic.Id);
                relation.HasIndex(
                    topic =>
                        new
                        {
                            topic.ProjectAggregateId,
                            topic.TopicName
                        }
                ).IsUnique();
            }
        );

        modelBuilder.Entity<SkillSummaryEntry>(
            skill =>
            {
                skill.HasKey(summary => summary.Id);
                skill
                    .HasIndex(summary => summary.SkillAggregateId)
                    .IsUnique();
                skill.HasIndex(summary => summary.Name).IsUnique();
                skill.Property(summary => summary.Name).IsRequired();
            }
        );

        modelBuilder.Entity<MemorySearchEntry>(
            memory =>
            {
                memory.ToTable("MemorySearchEntries");
                memory.HasKey(
                    entry =>
                        new
                        {
                            entry.MemoryAggregateId,
                            entry.PromptId,
                            entry.HookIndex,
                            entry.ChunkIndex
                        }
                );
                memory.Property(entry => entry.HookEventName).IsRequired();
                memory.Property(entry => entry.Text).IsRequired();
                memory
                    .Property(entry => entry.Embedding)
                    .HasColumnType("vector(1024)")
                    .IsRequired();
                memory
                    .HasGeneratedTsVectorColumn(
                        entry => entry.SearchVector,
                        "simple",
                        entry => new
                        {
                            entry.HookEventName,
                            entry.Text
                        }
                    )
                    .HasIndex(entry => entry.SearchVector)
                    .HasMethod("GIN");
                memory
                    .HasIndex(entry => entry.Embedding)
                    .HasMethod("hnsw")
                    .HasOperators("vector_cosine_ops");
                memory.HasIndex(entry => entry.ThreadId);
                memory.HasIndex(entry => entry.PromptStartTimestamp);
            }
        );

        modelBuilder.Entity<MemorySummaryEntry>(
            memory =>
            {
                memory.ToTable("MemorySummaryEntries");
                memory.HasKey(summary => summary.MemoryAggregateId);
                memory
                    .Property(summary => summary.MemoryAggregateId)
                    .ValueGeneratedNever();
                memory.Property(summary => summary.Summary).IsRequired();
                memory.HasIndex(summary => summary.ThreadId);
                memory.HasIndex(summary => summary.LastActivityTimestamp);
            }
        );

        modelBuilder.Entity<SkillSearchEntry>(
            skill =>
            {
                skill.ToTable("SkillSearchEntries");
                skill.HasKey(
                    entry =>
                        new
                        {
                            entry.SkillAggregateId,
                            entry.SourcePath,
                            entry.ChunkIndex
                        }
                );
                skill.Property(entry => entry.SkillName).IsRequired();
                skill.Property(entry => entry.SourcePath).IsRequired();
                skill.Property(entry => entry.Text).IsRequired();
                skill
                    .Property(entry => entry.Embedding)
                    .HasColumnType("vector(1024)")
                    .IsRequired();
                skill
                    .HasGeneratedTsVectorColumn(
                        entry => entry.SearchVector,
                        "simple",
                        entry => new
                        {
                            entry.SkillName,
                            entry.SourcePath,
                            entry.Text
                        }
                    )
                    .HasIndex(entry => entry.SearchVector)
                    .HasMethod("GIN");
                skill
                    .HasIndex(entry => entry.Embedding)
                    .HasMethod("hnsw")
                    .HasOperators("vector_cosine_ops");
                skill.HasIndex(entry => entry.SkillName);
            }
        );
    }
}

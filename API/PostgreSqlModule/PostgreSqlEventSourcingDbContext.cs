using EventSourcing.Persistence;
using EventSourcing.Persistence.Models;
using FeatureModule.Persistence;
using FeatureModule.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using PolicyModule.Persistence;
using PolicyModule.Persistence.Models;
using SkillsModule.Persistence;
using SkillsModule.Persistence.Models;

namespace PostgreSqlModule;

internal sealed class PostgreSqlEventSourcingDbContext(
    DbContextOptions<EventSourcingDbContext> options
) : EventSourcingDbContext(options),
    IFeatureModuleDbContext,
    IPolicyModuleDbContext,
    ISkillsModuleDbContext
{
    public DbSet<FeatureSummaryEntry> FeatureSummaries =>
        Set<FeatureSummaryEntry>();
    public DbSet<FeatureSearchEntry> FeatureSearchEntries =>
        Set<FeatureSearchEntry>();
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
    public DbSet<SkillListEntry> SkillListEntries =>
        Set<SkillListEntry>();
    public DbSet<SkillListTagEntry> SkillListTags =>
        Set<SkillListTagEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("vector");
        modelBuilder.HasPostgresExtension("pg_trgm");

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

        modelBuilder.Entity<SkillListEntry>(
            skill =>
            {
                skill.ToTable("SkillListEntries");
                skill.HasKey(entry => entry.Id);
                skill
                    .HasIndex(entry => entry.SkillAggregateId)
                    .IsUnique();
                skill.HasIndex(
                    entry => new
                    {
                        entry.NormalizedName,
                        entry.Name,
                        entry.SkillAggregateId
                    }
                ).HasFilter("\"IsDeleted\" = FALSE");
                skill.HasIndex(
                    entry => new
                    {
                        entry.ReferenceCount,
                        entry.SkillAggregateId
                    }
                ).HasFilter("\"IsDeleted\" = FALSE");
                skill.HasIndex(
                    entry => new
                    {
                        entry.AttachmentCount,
                        entry.SkillAggregateId
                    }
                ).HasFilter("\"IsDeleted\" = FALSE");
                skill
                    .HasIndex(entry => entry.SearchText)
                    .HasMethod("GIN")
                    .HasOperators("gin_trgm_ops")
                    .HasFilter("\"IsDeleted\" = FALSE");
                skill.Property(entry => entry.Name).IsRequired();
                skill.Property(entry => entry.NormalizedName).IsRequired();
                skill.Property(entry => entry.Description).IsRequired();
                skill.Property(entry => entry.SearchText).IsRequired();
                skill
                    .HasMany(entry => entry.Tags)
                    .WithOne()
                    .HasForeignKey(tag => tag.SkillListEntryId)
                    .OnDelete(DeleteBehavior.Cascade);
            }
        );

        modelBuilder.Entity<SkillListTagEntry>(
            tag =>
            {
                tag.ToTable("SkillListTags");
                tag.HasKey(entry => entry.Id);
                tag.HasIndex(
                    entry => new
                    {
                        entry.SkillListEntryId,
                        entry.NormalizedTag
                    }
                ).IsUnique();
                tag.HasIndex(
                    entry => new
                    {
                        entry.NormalizedTag,
                        entry.SkillListEntryId
                    }
                );
                tag.Property(entry => entry.Tag).IsRequired();
                tag.Property(entry => entry.NormalizedTag).IsRequired();
            }
        );

        modelBuilder.Entity<FeatureSummaryEntry>(
            feature =>
            {
                feature.HasKey(summary => summary.Id);
                feature
                    .HasIndex(summary => summary.FeatureAggregateId)
                    .IsUnique();
                feature.HasIndex(summary => summary.ProjectId);
                feature.HasIndex(summary => summary.Name).IsUnique();
                feature.Property(summary => summary.Name).IsRequired();
                feature.Property(summary => summary.Summary).IsRequired();
                feature.Property(summary => summary.Status).IsRequired();
            }
        );

        modelBuilder.Entity<FeatureSearchEntry>(
            feature =>
            {
                feature.ToTable("FeatureSearchEntries");
                feature.HasKey(entry => entry.Id);
                feature
                    .HasIndex(entry => entry.FeatureAggregateId)
                    .IsUnique();
                feature.HasIndex(
                    entry => new
                    {
                        entry.NormalizedName,
                        entry.Name,
                        entry.FeatureAggregateId
                    }
                ).HasFilter("\"IsDeleted\" = FALSE");
                feature.HasIndex(
                    entry => new
                    {
                        entry.ProjectId,
                        entry.NormalizedName,
                        entry.Name,
                        entry.FeatureAggregateId
                    }
                ).HasFilter("\"IsDeleted\" = FALSE");
                feature.HasIndex(
                    entry => new
                    {
                        entry.ProjectId,
                        entry.PlanCount,
                        entry.FeatureAggregateId
                    }
                ).HasFilter("\"IsDeleted\" = FALSE");
                feature.HasIndex(
                    entry => new
                    {
                        entry.ProjectId,
                        entry.RecordCount,
                        entry.FeatureAggregateId
                    }
                ).HasFilter("\"IsDeleted\" = FALSE");
                feature.HasIndex(
                    entry => new
                    {
                        entry.PlanCount,
                        entry.FeatureAggregateId
                    }
                ).HasFilter("\"IsDeleted\" = FALSE");
                feature.HasIndex(
                    entry => new
                    {
                        entry.RecordCount,
                        entry.FeatureAggregateId
                    }
                ).HasFilter("\"IsDeleted\" = FALSE");
                feature
                    .HasIndex(entry => entry.SearchText)
                    .HasMethod("GIN")
                    .HasOperators("gin_trgm_ops")
                    .HasFilter("\"IsDeleted\" = FALSE");
                feature.Property(entry => entry.Name).IsRequired();
                feature.Property(entry => entry.NormalizedName).IsRequired();
                feature.Property(entry => entry.Summary).IsRequired();
                feature.Property(entry => entry.SearchText).IsRequired();
                feature.Property(entry => entry.Status).IsRequired();
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

        modelBuilder.Entity<FeatureResearchSearchEntry>(
            research =>
            {
                research.ToTable("FeatureResearchSearchEntries");
                research.HasKey(
                    entry =>
                        new
                        {
                            entry.FeatureAggregateId,
                            entry.ResearchDiscoveryId,
                            entry.ChunkIndex
                        }
                );
                research.Property(entry => entry.FeatureName).IsRequired();
                research.Property(entry => entry.Title).IsRequired();
                research.Property(entry => entry.SourceType).IsRequired();
                research.Property(entry => entry.SourceReference).IsRequired();
                research.Property(entry => entry.Text).IsRequired();
                research
                    .Property(entry => entry.Embedding)
                    .HasColumnType("vector(1024)")
                    .IsRequired();
                research
                    .HasGeneratedTsVectorColumn(
                        entry => entry.SearchVector,
                        "simple",
                        entry => new
                        {
                            entry.FeatureName,
                            entry.Title,
                            entry.SourceType,
                            entry.SourceReference,
                            entry.Text
                        }
                    )
                    .HasIndex(entry => entry.SearchVector)
                    .HasMethod("GIN");
                research
                    .HasIndex(entry => entry.Embedding)
                    .HasMethod("hnsw")
                    .HasOperators("vector_cosine_ops");
            }
        );

        modelBuilder.Entity<KnowledgeSearchEntry>(
            knowledge =>
            {
                knowledge.ToTable("KnowledgeSearchEntries");
                knowledge.HasKey(entry => entry.Id);
                knowledge.Property(entry => entry.OwnerType).IsRequired();
                knowledge.Property(entry => entry.SourceType).IsRequired();
                knowledge.Property(entry => entry.SourceKey).IsRequired();
                knowledge
                    .Property(entry => entry.MetadataJson)
                    .HasColumnName("Metadata")
                    .HasColumnType("jsonb")
                    .IsRequired();
                knowledge.Property(entry => entry.SearchableMetadata).IsRequired();
                knowledge.Property(entry => entry.Text).IsRequired();
                knowledge
                    .Property(entry => entry.Embedding)
                    .HasColumnType("vector(1024)")
                    .IsRequired();
                knowledge
                    .HasIndex(
                        entry => new
                        {
                            entry.OwnerType,
                            entry.OwnerAggregateId,
                            entry.SourceType,
                            entry.SourceKey,
                            entry.ChunkIndex
                        }
                    )
                    .IsUnique();
                knowledge
                    .HasGeneratedTsVectorColumn(
                        entry => entry.SearchVector,
                        "simple",
                        entry => new
                        {
                            entry.SourceType,
                            entry.SourceKey,
                            entry.SearchableMetadata,
                            entry.Text
                        }
                    )
                    .HasIndex(entry => entry.SearchVector)
                    .HasMethod("GIN");
                knowledge
                    .HasIndex(entry => entry.Embedding)
                    .HasMethod("hnsw")
                    .HasOperators("vector_cosine_ops");
            }
        );
    }
}

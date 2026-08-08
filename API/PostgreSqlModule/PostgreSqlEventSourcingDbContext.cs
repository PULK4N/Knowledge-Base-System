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
    public DbSet<TopicPolicyText> TopicPolicyTexts =>
        Set<TopicPolicyText>();
    public DbSet<ProjectPolicyTopic> ProjectPolicyTopics =>
        Set<ProjectPolicyTopic>();
    public DbSet<SkillSummaryEntry> SkillSummaries =>
        Set<SkillSummaryEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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
    }
}

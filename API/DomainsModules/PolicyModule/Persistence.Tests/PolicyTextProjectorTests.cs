using EventSourcing.Shared.Models;
using Microsoft.EntityFrameworkCore;
using PolicyModule.Domain;
using PolicyModule.Domain.Models;
using PolicyModule.Persistence.Models;

namespace PolicyModule.Persistence.Tests;

public sealed class PolicyTextProjectorTests
{
    private static readonly AggregateId ProjectId =
        AggregateId.FromDatabaseGuid(
            Guid.Parse("11111111-1111-1111-1111-111111111111")
        );

    [Fact]
    public async Task Projections_AreJoinedInProjectTopicGeneralOrder()
    {
        await using var context = CreateContext();
        var repository = new PolicyTextRepository(context);
        var general = CreateGeneralPolicies("General policy");
        var project = CreateProject();
        var generalStateInfo = CreateStateInfo(general);
        var projectStateInfo = CreateStateInfo(project);

        await new GeneralPolicyTextProjector(repository).Update(
            [generalStateInfo]
        );
        await new TopicPolicyTextProjector(repository).Update(
            [generalStateInfo]
        );
        await new ProjectPolicyTextProjector(repository).Update(
            [projectStateInfo]
        );
        await new ProjectTopicProjector(repository).Update(
            [projectStateInfo]
        );
        await new GeneralPolicyTextProjector(repository).Update(
            [generalStateInfo]
        );
        await new TopicPolicyTextProjector(repository).Update(
            [generalStateInfo]
        );
        await new ProjectPolicyTextProjector(repository).Update(
            [projectStateInfo]
        );
        await new ProjectTopicProjector(repository).Update(
            [projectStateInfo]
        );

        Assert.Equal(
            "# Project \"Policy project\" policies\n\n"
                + "## Project policy\nProject text.\n\n"
                + "# Topic \"cloud\" policies\n\n"
                + "## Topic policy\nTopic text.\n\n"
                + "# General policies\n\n"
                + "## General policy\nGeneral text.",
            await repository.Get(ProjectId)
        );
        Assert.Single(context.GeneralPolicyTexts);
        Assert.Single(context.ProjectPolicyTexts);
        Assert.Single(context.TopicPolicyTexts);
        Assert.Single(context.ProjectPolicyTopics);
    }

    [Fact]
    public async Task GeneralProjection_UpdatesOnlyGeneralText()
    {
        await using var context = CreateContext();
        var repository = new PolicyTextRepository(context);
        var general = CreateGeneralPolicies("General policy");
        var project = CreateProject();
        var generalStateInfo = CreateStateInfo(general);
        var projectStateInfo = CreateStateInfo(project);
        await new GeneralPolicyTextProjector(repository).Update(
            [generalStateInfo]
        );
        await new TopicPolicyTextProjector(repository).Update(
            [generalStateInfo]
        );
        await new ProjectPolicyTextProjector(repository).Update(
            [projectStateInfo]
        );
        await new ProjectTopicProjector(repository).Update(
            [projectStateInfo]
        );
        var projectText = context.ProjectPolicyTexts.Single().Text;
        var topicText = context.TopicPolicyTexts.Single().Text;

        general.Policies.Clear();
        AddPolicy(
            general.Policies,
            "aaaaaaaa-4444-4444-4444-444444444444",
            "Updated general policy",
            "Updated general text."
        );
        await new GeneralPolicyTextProjector(repository).Update(
            [generalStateInfo]
        );

        Assert.Equal(
            "# General policies\n\n"
                + "## Updated general policy\nUpdated general text.",
            context.GeneralPolicyTexts.Single().Text
        );
        Assert.Equal(projectText, context.ProjectPolicyTexts.Single().Text);
        Assert.Equal(topicText, context.TopicPolicyTexts.Single().Text);
        Assert.Single(context.ProjectPolicyTopics);
    }

    [Fact]
    public async Task DeletedProject_RemovesItsTextAndTopicRelations()
    {
        await using var context = CreateContext();
        var repository = new PolicyTextRepository(context);
        var project = CreateProject();
        var stateInfo = CreateStateInfo(project);
        await new ProjectPolicyTextProjector(repository).Update([stateInfo]);
        await new ProjectTopicProjector(repository).Update([stateInfo]);

        project.IsDeleted = true;
        await new ProjectPolicyTextProjector(repository).Update([stateInfo]);
        await new ProjectTopicProjector(repository).Update([stateInfo]);

        Assert.Empty(context.ProjectPolicyTexts);
        Assert.Empty(context.ProjectPolicyTopics);
        Assert.Null(await repository.Get(ProjectId));
    }

    [Fact]
    public async Task ProjectSummaryProjection_ListsNamesAndRepositoryPaths()
    {
        await using var context = CreateContext();
        var repository = new PolicyProjectSummaryRepository(context);
        var project = CreateProject();
        var stateInfo = CreateStateInfo(project);
        var projector = new PolicyProjectSummaryProjector(repository);

        await projector.Update([stateInfo]);

        var summary = Assert.Single(await repository.List());
        Assert.Equal(ProjectId.Value, summary.ProjectId);
        Assert.Equal("Policy project", summary.ProjectName);
        Assert.Equal(
            ["/workspace/policy-project"],
            summary.RepositoryPaths
        );
        Assert.Equal(
            ProjectId.Value,
            (await repository.GetByName("  POLICY PROJECT "))?.ProjectId
        );
        Assert.Null(await repository.GetByName("missing"));

        project.ProjectName = "Renamed policy project";
        project.RepositoryPaths.Add("/workspace/secondary");
        await projector.Update([stateInfo]);

        summary = Assert.Single(await repository.List());
        Assert.Equal("Renamed policy project", summary.ProjectName);
        Assert.Equal(
            [
                "/workspace/policy-project",
                "/workspace/secondary"
            ],
            summary.RepositoryPaths
        );

        project.IsDeleted = true;
        await projector.Update([stateInfo]);

        Assert.Empty(await repository.List());
    }

    private static TestPolicyDbContext CreateContext()
    {
        var context = new TestPolicyDbContext(
            new DbContextOptionsBuilder<TestPolicyDbContext>()
                .UseSqlite("Data Source=:memory:")
                .Options
        );
        context.Database.OpenConnection();
        context.Database.EnsureCreated();

        return context;
    }

    private static GeneralPoliciesStateData CreateGeneralPolicies(
        string generalPolicyTitle
    )
    {
        var state = new GeneralPoliciesStateData(
            AggregateId.FromDatabaseGuid(
                Guid.Parse("99999999-9999-9999-9999-999999999999")
            )
        );
        AddPolicy(
            state.Policies,
            "aaaaaaaa-1111-1111-1111-111111111111",
            generalPolicyTitle,
            "General text."
        );
        var topicName = new TopicName("cloud");
        var topic = new Topic
        {
            TopicName = topicName,
            Description = "Cloud policies."
        };
        AddPolicy(
            topic.Policies,
            "aaaaaaaa-3333-3333-3333-333333333333",
            "Topic policy",
            "Topic text."
        );
        state.Topics.Add(topicName, topic);

        return state;
    }

    private static ProjectPoliciesStateData CreateProject()
    {
        var state = new ProjectPoliciesStateData(ProjectId)
        {
            ProjectName = "Policy project",
            ProjectDescription = "Projection test project.",
            RepositoryPaths = ["/workspace/policy-project"]
        };
        AddPolicy(
            state.Policies,
            "aaaaaaaa-2222-2222-2222-222222222222",
            "Project policy",
            "Project text."
        );
        state.RelatedTopics.Add(new TopicName("cloud"));

        return state;
    }

    private static void AddPolicy(
        Dictionary<PolicyId, Policy> policies,
        string id,
        string title,
        string description
    )
    {
        var policy = new Policy
        {
            PolicyId = PolicyId.FromDatabaseGuid(Guid.Parse(id)),
            Title = title,
            Description = description
        };
        policies.Add(policy.PolicyId, policy);
    }

    private static StateInfo CreateStateInfo(object stateData) =>
        StateInfo.Create(
            stateData,
            "test-state-machine",
            stateData switch
            {
                GeneralPoliciesStateData general => general.Id,
                ProjectPoliciesStateData project => project.Id,
                _ => throw new InvalidOperationException()
            }
        );

    private sealed class TestPolicyDbContext(
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
}

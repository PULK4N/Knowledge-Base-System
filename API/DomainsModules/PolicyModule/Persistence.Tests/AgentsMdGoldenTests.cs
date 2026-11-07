using EventSourcing.Shared.Models;
using Microsoft.EntityFrameworkCore;
using PolicyModule.Domain;
using PolicyModule.Domain.Models;

namespace PolicyModule.Persistence.Tests;

/// <summary>
/// Pins the exact Agents.MD policy text that agent hooks receive, so moving
/// the composition between modules cannot change it.
/// </summary>
public sealed class AgentsMdGoldenTests
{
    private static readonly AggregateId ProjectId =
        AggregateId.FromDatabaseGuid(
            Guid.Parse("12121212-1212-1212-1212-121212121212")
        );

    private const string GeneralText =
        "# General policies\n\n"
        + "## General one\nGeneral one text.\n\n"
        + "## General two\nGeneral two text.";

    private const string ProjectText =
        "# Project \"Golden project\" policies\n\n"
        + "## Project one\nProject one text.\n\n"
        + "## Project two\nProject two text.";

    private const string TopicsText =
        "# Topic \"dotnet\" policies\n\n"
        + "## Dotnet one\nDotnet one text.\n\n"
        + "## Dotnet two\nDotnet two text.\n\n"
        + "# Topic \"cloud\" policies\n\n"
        + "## Cloud one\nCloud one text.";

    public static TheoryData<string?, string> FoundCases =>
        new()
        {
            {
                "claude",
                GeneralText + "\n\n" + ProjectText + "\n\n"
                    + "# Agent family \"claude\" policies\n\n"
                    + "## Claude one\nClaude one text.\n\n"
                    + TopicsText
            },
            {
                "codex",
                GeneralText + "\n\n" + ProjectText + "\n\n"
                    + "# Agent family \"codex\" policies\n\n"
                    + "## Codex one\nCodex one text.\n\n"
                    + "## Codex two\nCodex two text.\n\n"
                    + TopicsText
            },
            {
                "empty-family",
                GeneralText + "\n\n" + ProjectText + "\n\n" + TopicsText
            },
            {
                null,
                GeneralText + "\n\n" + ProjectText + "\n\n" + TopicsText
            }
        };

    [Theory]
    [MemberData(nameof(FoundCases))]
    public async Task ProjectedPolicies_ComposeTheExactAgentsMdText(
        string? agentFamily,
        string expected
    )
    {
        await using var context = await CreateProjectedContext(
            CreateProject(withPolicies: true)
        );

        Assert.Equal(
            expected,
            await new PolicyTextRepository(context).Get(ProjectId, agentFamily)
        );
    }

    [Fact]
    public async Task ProjectWithoutPolicies_StillReturnsTheOtherSections()
    {
        await using var context = await CreateProjectedContext(
            CreateProject(withPolicies: false)
        );

        Assert.Equal(
            GeneralText + "\n\n"
                + "# Agent family \"claude\" policies\n\n"
                + "## Claude one\nClaude one text.\n\n"
                + TopicsText,
            await new PolicyTextRepository(context).Get(ProjectId, "claude")
        );
    }

    [Fact]
    public async Task ProjectWithoutProjection_ReturnsNull()
    {
        await using var context = await CreateProjectedContext(null);

        Assert.Null(
            await new PolicyTextRepository(context).Get(ProjectId, "claude")
        );
    }

    private static async Task<TestPolicyDbContext> CreateProjectedContext(
        ProjectPoliciesStateData? project
    )
    {
        var context = new TestPolicyDbContext(
            new DbContextOptionsBuilder<TestPolicyDbContext>()
                .UseSqlite("Data Source=:memory:")
                .Options
        );
        context.Database.OpenConnection();
        context.Database.EnsureCreated();
        var repository = new PolicyTextRepository(context);
        var general = CreateStateInfo(CreateGeneralPolicies());

        await new GeneralPolicyTextProjector(repository).Update([general]);
        await new TopicPolicyTextProjector(repository).Update([general]);
        await new AgentFamilyPolicyTextProjector(repository).Update([general]);

        if (project is not null)
        {
            var projectStateInfo = CreateStateInfo(project);
            await new ProjectPolicyTextProjector(repository).Update([projectStateInfo]);
            await new ProjectTopicProjector(repository).Update([projectStateInfo]);
        }

        return context;
    }

    private static GeneralPoliciesStateData CreateGeneralPolicies()
    {
        var state = new GeneralPoliciesStateData(ProjectId);
        AddPolicy(state.Policies, 1, "General one");
        AddPolicy(state.Policies, 2, "General two");
        AddTopic(state, "cloud", ("Cloud one", 3));
        AddTopic(state, "dotnet", ("Dotnet one", 4), ("Dotnet two", 5));
        AddTopic(state, "empty-topic");
        AddAgentFamily(state, "claude", ("Claude one", 6));
        AddAgentFamily(state, "codex", ("Codex one", 7), ("Codex two", 8));
        AddAgentFamily(state, "empty-family");

        return state;
    }

    private static ProjectPoliciesStateData CreateProject(bool withPolicies)
    {
        var state = new ProjectPoliciesStateData(ProjectId)
        {
            ProjectName = "Golden project",
            ProjectDescription = "Golden test project.",
            RepositoryPaths = ["/workspace/golden"]
        };

        if (withPolicies)
        {
            AddPolicy(state.Policies, 9, "Project one");
            AddPolicy(state.Policies, 10, "Project two");
        }

        // Order is the project's link order, not alphabetical; the missing
        // and empty topics must not leave gaps in the text.
        state.RelatedTopics.Add(new TopicName("dotnet"));
        state.RelatedTopics.Add(new TopicName("empty-topic"));
        state.RelatedTopics.Add(new TopicName("missing-topic"));
        state.RelatedTopics.Add(new TopicName("cloud"));

        return state;
    }

    private static void AddTopic(
        GeneralPoliciesStateData state,
        string name,
        params (string Title, int Id)[] policies
    )
    {
        var topic = new Topic
        {
            TopicName = new TopicName(name),
            Description = $"{name} policies."
        };

        foreach (var (title, id) in policies)
            AddPolicy(topic.Policies, id, title);

        state.Topics.Add(topic.TopicName, topic);
    }

    private static void AddAgentFamily(
        GeneralPoliciesStateData state,
        string name,
        params (string Title, int Id)[] policies
    )
    {
        var agentFamily = new AgentFamily
        {
            AgentFamilyName = new AgentFamilyName(name),
            Description = $"{name} policies."
        };

        foreach (var (title, id) in policies)
            AddPolicy(agentFamily.Policies, id, title);

        state.AgentFamilies.Add(agentFamily.AgentFamilyName, agentFamily);
    }

    private static void AddPolicy(
        Dictionary<PolicyId, Policy> policies,
        int id,
        string title
    )
    {
        var policy = new Policy
        {
            PolicyId = PolicyId.FromDatabaseGuid(
                Guid.Parse($"aaaaaaaa-0000-0000-0000-{id:D12}")
            ),
            Title = title,
            Description = $"{title} text."
        };
        policies.Add(policy.PolicyId, policy);
    }

    private static StateInfo CreateStateInfo(ISharedStateData stateData) =>
        StateInfo.Create(stateData, "test-state-machine", stateData.Id);
}

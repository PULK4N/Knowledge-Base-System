using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Core.Providers;
using EventSourcing.Persistence.Interfaces;
using EventSourcing.Shared.Models;
using Microsoft.Extensions.DependencyInjection;
using PolicyModule.Domain;
using PolicyModule.Domain.Models;
using PolicyModule.Persistence;
using ProjectModule.Application.Models;
using ProjectModule.Application.Queries;
using ProjectModule.Domain;
using ProjectModule.Domain.Events;
using SharedModule.Constants;
using SharedModule.Exceptions;
using SkillsModule.Persistence.Interfaces;

namespace ProjectModule.Persistence.Tests;

/// <summary>
/// Pins the exact Agents.MD policy text that agent hooks receive, so moving
/// the composition between modules cannot change it.
/// </summary>
public sealed class AgentsMdGoldenTests
{
    private const string RepositoryPath = "/workspace/golden";
    private static readonly object RegistrationLock = new();
    private static bool _typesRegistered;

    private static readonly Executor Executor =
        new()
        {
            Id = EventExecutor.FromDatabaseGuid(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
            )
        };

    public AgentsMdGoldenTests()
    {
        lock (RegistrationLock)
        {
            if (_typesRegistered)
                return;

            new ServiceCollection().RegisterEventSourcingCore(
                typeof(ProjectStateData).Assembly
            );
            _typesRegistered = true;
        }
    }
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

    private static readonly SkillUseScenarios AngularSkill = new(
        Guid.Parse("21212121-0000-0000-0000-000000000001"),
        "angular-code-writter",
        ["Writing or reviewing Angular code"]
    );
    private static readonly SkillUseScenarios PlanSkill = new(
        Guid.Parse("21212121-0000-0000-0000-000000000002"),
        "how-to-grill-user-to-write-a-better-plan",
        ["Writing a plan", "Reviewing or stress-testing a plan"]
    );
    private static readonly SkillUseScenarios PlainSkill = new(
        Guid.Parse("21212121-0000-0000-0000-000000000003"),
        "some-skill-without-scenarios",
        []
    );
    private static readonly Guid DeletedSkillId =
        Guid.Parse("21212121-0000-0000-0000-000000000004");

    private const string ProjectSkillsText =
        "# Project skills\n"
        + "- angular-code-writter — Writing or reviewing Angular code\n"
        + "- how-to-grill-user-to-write-a-better-plan — Writing a plan; Reviewing or stress-testing a plan\n"
        + "- some-skill-without-scenarios";

    private const string TopicsText =
        "# Topic \"dotnet\" policies\n\n"
        + "## Dotnet one\nDotnet one text.\n\n"
        + "## Dotnet two\nDotnet two text.\n\n"
        + "# Topic \"cloud\" policies\n\n"
        + "## Cloud one\nCloud one text.";

    public static TheoryData<string, string> FoundCases =>
        new()
        {
            {
                "claude",
                GeneralText + "\n\n" + ProjectText + "\n\n"
                    + "# Agent family \"claude\" policies\n\n"
                    + "## Claude one\nClaude one text.\n\n"
                    + TopicsText + "\n\n" + ProjectSkillsText
            },
            {
                "codex",
                GeneralText + "\n\n" + ProjectText + "\n\n"
                    + "# Agent family \"codex\" policies\n\n"
                    + "## Codex one\nCodex one text.\n\n"
                    + "## Codex two\nCodex two text.\n\n"
                    + TopicsText + "\n\n" + ProjectSkillsText
            },
            {
                "empty-family",
                GeneralText + "\n\n" + ProjectText + "\n\n" + TopicsText
                    + "\n\n" + ProjectSkillsText
            }
        };

    [Theory]
    [MemberData(nameof(FoundCases))]
    public async Task ProjectedPolicies_ComposeTheExactAgentsMdText(
        string agentFamily,
        string expected
    )
    {
        await using var context = await CreateProjectedContext(
            CreateProject(withPolicies: true)
        );

        var result = await CreateQuery(context, agentFamily).Execute(Executor);

        Assert.Equal(GetPoliciesByRepositoryResult.OkStatus, result.Status);
        Assert.Equal(expected, result.Policies);
    }

    [Fact]
    public async Task ProjectWithoutPoliciesOrSkills_StillReturnsTheOtherSections()
    {
        await using var context = await CreateProjectedContext(
            CreateProject(withPolicies: false, withSkills: false)
        );

        Assert.Equal(
            GeneralText + "\n\n"
                + "# Agent family \"claude\" policies\n\n"
                + "## Claude one\nClaude one text.\n\n"
                + TopicsText,
            (await CreateQuery(context, "claude").Execute(Executor)).Policies
        );
    }

    [Fact]
    public async Task ProjectWithoutProjection_IsNotFound()
    {
        await using var context = await CreateProjectedContext(null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => CreateQuery(context, "claude").Execute(Executor)
        );
    }

    [Fact]
    public async Task UnknownAgentFamily_IsReportedWithoutText()
    {
        await using var context = await CreateProjectedContext(
            CreateProject(withPolicies: true)
        );

        var result = await CreateQuery(context, "gemini").Execute(Executor);

        Assert.Equal(
            GetPoliciesByRepositoryResult.AgentFamilyNotFoundStatus,
            result.Status
        );
        Assert.Null(result.Policies);
    }

    [Fact]
    public async Task UnmappedRepository_AsksForAProject()
    {
        await using var context = await CreateProjectedContext(
            CreateProject(withPolicies: true)
        );

        var result = await CreateQuery(
            context,
            "claude",
            "/workspace/unmapped"
        ).Execute(Executor);

        Assert.Equal(
            GetPoliciesByRepositoryResult.RepositoryMappingRequiredStatus,
            result.Status
        );
        Assert.Equal(
            "Golden project",
            Assert.Single(result.Projects).ProjectName
        );
    }

    private static GetPoliciesByRepositoryQuery CreateQuery(
        TestKnowledgeBaseDbContext context,
        string agentFamily,
        string repositoryPath = RepositoryPath
    )
    {
        var definitions = new YamlStateMachineDefinitionProvider(
            Path.Combine(AppContext.BaseDirectory, "StateMachines")
        );

        return new GetPoliciesByRepositoryQuery(
            new StateCalculator(
                new OrderNumberHelper(),
                new StateMachineStateDataProvider(definitions),
                new EventValidatorProvider(definitions),
                definitions
            ),
            new RepositoryMapEventStore(),
            new ProjectPolicyTextRepository(context),
            new PolicyTextRepository(context),
            new PolicyProjectSummaryRepository(context),
            new StubSkillSummaryRepository([AngularSkill, PlanSkill, PlainSkill])
        )
        {
            RepositoryPath = repositoryPath,
            AgentFamily = agentFamily
        };
    }

    /// <summary>Holds only the mapping of the golden repository to the project.</summary>
    private sealed class RepositoryMapEventStore : IEventStore
    {
        public Task<Dictionary<AggregateId, List<EventPayload>>> GetEvents(
            List<AggregateId> aggregateIds
        )
        {
            var mapId = AggregateId.FromDatabaseGuid(
                StateDataAggregateIds.RepositoryToProjectMap
            );
            var payload = EventPayload.Create(
                Executor.Id,
                mapId,
                ProjectModule.Application.Constants.StateMachineIds.RepositoryToProjectMap,
                new RepositoryToProjectMapAddedV1(RepositoryPath, ProjectId)
            );
            payload.EventExecutionInfo.OrderNumber = 1;

            return Task.FromResult(
                aggregateIds.ToDictionary(
                    aggregateId => aggregateId,
                    aggregateId => aggregateId == mapId
                        ? new List<EventPayload> { payload }
                        : []
                )
            );
        }

        public Task Write(List<StateInfo> stateInfos) =>
            throw new NotSupportedException();
    }

    private static async Task<TestKnowledgeBaseDbContext> CreateProjectedContext(
        ProjectStateData? project
    )
    {
        var context = TestKnowledgeBaseDbContext.Create();
        var repository = new PolicyTextRepository(context);
        var general = CreateStateInfo(CreateGeneralPolicies());

        await new GeneralPolicyTextProjector(repository).Update([general]);
        await new TopicPolicyTextProjector(repository).Update([general]);
        await new AgentFamilyPolicyTextProjector(repository).Update([general]);

        if (project is not null)
        {
            var projectRepository = new ProjectPolicyTextRepository(context);
            var projectStateInfo = CreateStateInfo(project);
            await new ProjectPolicyTextProjector(projectRepository).Update([projectStateInfo]);
            await new ProjectTopicProjector(projectRepository).Update([projectStateInfo]);
            await new PolicyProjectSummaryProjector(
                new PolicyProjectSummaryRepository(context)
            ).Update([projectStateInfo]);
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

    private static ProjectStateData CreateProject(
        bool withPolicies,
        bool withSkills = true
    )
    {
        var state = new ProjectStateData(ProjectId)
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

        // Listed by skill name, not link order; a deleted skill has no summary
        // and must not leave a line.
        if (withSkills)
        {
            state.RelatedSkillIds.Add(AggregateId.FromDatabaseGuid(PlainSkill.SkillId));
            state.RelatedSkillIds.Add(AggregateId.FromDatabaseGuid(DeletedSkillId));
            state.RelatedSkillIds.Add(AggregateId.FromDatabaseGuid(AngularSkill.SkillId));
            state.RelatedSkillIds.Add(AggregateId.FromDatabaseGuid(PlanSkill.SkillId));
        }

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

    /// <summary>Returns the active skills among the requested IDs, ordered by name like the read model.</summary>
    private sealed class StubSkillSummaryRepository(
        List<SkillUseScenarios> skills
    ) : ISkillSummaryRepository
    {
        public Task<List<SkillSummary>> List() =>
            throw new NotSupportedException();

        public Task<SkillSummary?> GetByName(
            string name,
            CancellationToken cancellationToken = default
        ) => throw new NotSupportedException();

        public Task<SkillSummarySearchResult> Search(
            int page,
            int pageSize,
            string? search,
            CancellationToken cancellationToken = default
        ) => throw new NotSupportedException();

        public Task<List<SkillUseScenarios>> GetUseScenarios(
            List<Guid> skillIds,
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult(
                skills
                    .Where(skill => skillIds.Contains(skill.SkillId))
                    .OrderBy(skill => skill.Name, StringComparer.Ordinal)
                    .ToList()
            );
    }
}

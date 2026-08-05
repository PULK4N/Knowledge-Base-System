using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Core.Interfaces;
using EventSourcing.Core.Providers;
using EventSourcing.Persistence.Interfaces;
using EventSourcing.Shared.Exceptions;
using EventSourcing.Shared.Models;
using Microsoft.Extensions.DependencyInjection;
using PolicyModule.Application.Commands;
using PolicyModule.Application.Models;
using PolicyModule.Application.Queries;
using PolicyModule.Domain;
using PolicyModule.Domain.Events;
using PolicyModule.Domain.Models;
using PolicyModule.Persistence.Interfaces;
using Shared.Interfaces;
using SharedModule.Constants;
using SharedModule.Exceptions;
using UUIDNext;

namespace PolicyModule.Application.Tests;

public sealed class PolicyCommandTests
{
    private static readonly object RegistrationLock = new();
    private static bool _typesRegistered;

    private static readonly Executor Executor =
        new()
        {
            Id = EventExecutor.FromDatabaseGuid(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
            )
        };

    public PolicyCommandTests()
    {
        DatabaseFriendlyGuidGenerator
            .SetDefaultGuidGenerationDatabase(Database.SqlServer);
        RegisterPolicyTypesOnce();
    }

    [Fact]
    public async Task GeneralPolicyCommands_UseFixedGlobalStream()
    {
        var eventStore = new CapturingEventStoreWithOutbox();
        var handler = CreateHandler(eventStore);
        var addResult = Assert.IsType<PolicyAddedCommandResult>(
            await new AddGeneralPolicyCommand(handler)
            {
                Title = "Always run focused tests",
                Description = "Verify only the affected behavior."
            }.Execute(Executor)
        );

        await new UpdateGeneralPolicyCommand(handler)
        {
            PolicyId = addResult.PolicyId,
            Title = "Always run the smallest focused tests",
            Description = "Verify the affected behavior first."
        }.Execute(Executor);
        await new RemoveGeneralPolicyCommand(handler)
        {
            PolicyId = addResult.PolicyId
        }.Execute(Executor);

        var globalAggregateId = AggregateId.FromDatabaseGuid(
            StateDataAggregateIds.GeneralPolicies
        );
        var events = eventStore.GetStoredEvents(globalAggregateId);
        Assert.Collection(
            events,
            payload =>
            {
                var added = Assert.IsType<GeneralPolicyAddedV1>(
                    payload.EventData
                );
                Assert.Equal(
                    addResult.PolicyId,
                    added.Policy.PolicyId.Value
                );
                Assert.Equal(
                    PolicyModule.Application.Constants.StateMachineIds.GeneralPolicies,
                    payload.EventExecutionInfo.StateMachineId
                );
            },
            payload =>
            {
                var updated = Assert.IsType<GeneralPolicyUpdatedV1>(
                    payload.EventData
                );
                Assert.Equal(
                    "Always run the smallest focused tests",
                    updated.Policy.Title
                );
            },
            payload =>
                Assert.IsType<GeneralPolicyRemovedV1>(
                    payload.EventData
                )
        );
        Assert.Empty(
            Assert.IsType<GeneralPoliciesStateData>(
                eventStore.LastWritten[globalAggregateId].StateData
            ).Policies
        );
    }

    [Fact]
    public async Task TopicCommands_CreateAddAndRemovePolicyOnGlobalStream()
    {
        var eventStore = new CapturingEventStoreWithOutbox();
        var handler = CreateHandler(eventStore);
        const string topicName = "cloud";

        await new CreateTopicCommand(handler)
        {
            TopicName = topicName,
            Description = "Policies for cloud usage."
        }.Execute(Executor);
        await new UpdateTopicCommand(handler)
        {
            TopicName = topicName,
            Description = "Updated cloud policies."
        }.Execute(Executor);
        var addedPolicy = Assert.IsType<PolicyAddedCommandResult>(
            await new AddTopicPolicyCommand(handler)
            {
                TopicName = topicName,
                Title = "Use managed identities",
                Description = "Do not store service credentials."
            }.Execute(Executor)
        );
        await new UpdateTopicPolicyCommand(handler)
        {
            TopicName = topicName,
            PolicyId = addedPolicy.PolicyId,
            Title = "Use workload identities",
            Description = "Do not store service credentials."
        }.Execute(Executor);
        await new RemoveTopicPolicyCommand(handler)
        {
            TopicName = topicName,
            PolicyId = addedPolicy.PolicyId
        }.Execute(Executor);
        await new RemoveTopicCommand(handler)
        {
            TopicName = topicName
        }.Execute(Executor);

        var globalAggregateId = AggregateId.FromDatabaseGuid(
            StateDataAggregateIds.GeneralPolicies
        );
        var events = eventStore.GetStoredEvents(globalAggregateId);
        Assert.Collection(
            events,
            payload => Assert.IsType<TopicCreatedV1>(payload.EventData),
            payload => Assert.IsType<TopicUpdatedV1>(payload.EventData),
            payload => Assert.IsType<TopicPolicyAddedV1>(payload.EventData),
            payload =>
                Assert.IsType<TopicPolicyUpdatedV1>(
                    payload.EventData
                ),
            payload =>
            {
                var removed = Assert.IsType<TopicPolicyRemovedV1>(
                    payload.EventData
                );
                Assert.Equal(
                    addedPolicy.PolicyId,
                    removed.PolicyId.Value
                );
            },
            payload => Assert.IsType<TopicRemovedV1>(payload.EventData)
        );
        var state = Assert.IsType<GeneralPoliciesStateData>(
            eventStore.LastWritten[globalAggregateId].StateData
        );
        Assert.Empty(state.Topics);
    }

    [Fact]
    public async Task ProjectCommands_CreateMappingsAndReuseGeneratedProjectStream()
    {
        var eventStore = new CapturingEventStoreWithOutbox();
        var handler = CreateHandler(eventStore);
        var repositoryPaths = new List<string>
        {
            "/workspace/main",
            "/workspace/secondary-checkout"
        };
        var created = Assert.IsType<ProjectCreatedCommandResult>(
            await new CreateProjectCommand(handler)
            {
                ProjectName = "MCP Skill System",
                ProjectDescription = "Event-sourced Codex tooling.",
                RepositoryPaths = repositoryPaths
            }.Execute(Executor)
        );
        var projectId = AggregateId.FromDatabaseGuid(created.ProjectId);

        await new UpdateProjectCommand(handler)
        {
            ProjectId = created.ProjectId,
            ProjectName = "MCP Skill and Policy System",
            ProjectDescription = "Updated event-sourced tooling."
        }.Execute(Executor);

        var addedPolicy = Assert.IsType<PolicyAddedCommandResult>(
            await new AddProjectPolicyCommand(handler)
            {
                ProjectId = created.ProjectId,
                Title = "Keep policies separate from skills",
                Description = "Policies are injected into every chat."
            }.Execute(Executor)
        );
        await new UpdateProjectPolicyCommand(handler)
        {
            ProjectId = created.ProjectId,
            PolicyId = addedPolicy.PolicyId,
            Title = "Keep policies distinct from skills",
            Description = "Policies are injected into every chat."
        }.Execute(Executor);
        await new RemoveProjectPolicyCommand(handler)
        {
            ProjectId = created.ProjectId,
            PolicyId = addedPolicy.PolicyId
        }.Execute(Executor);
        await new AddTopicRelationToProjectCommand(handler)
        {
            ProjectId = created.ProjectId,
            TopicName = "cloud"
        }.Execute(Executor);
        await new RemoveTopicRelationFromProjectCommand(handler)
        {
            ProjectId = created.ProjectId,
            TopicName = "cloud"
        }.Execute(Executor);
        await new DeleteProjectCommand(handler)
        {
            ProjectId = created.ProjectId
        }.Execute(Executor);

        Assert.NotEqual(Guid.Empty, created.ProjectId);
        var projectEvents = eventStore.GetStoredEvents(projectId);
        Assert.Collection(
            projectEvents,
            payload => Assert.IsType<ProjectCreatedV1>(payload.EventData),
            payload => Assert.IsType<ProjectUpdatedV1>(payload.EventData),
            payload => Assert.IsType<ProjectPolicyAddedV1>(payload.EventData),
            payload => Assert.IsType<ProjectPolicyUpdatedV1>(payload.EventData),
            payload => Assert.IsType<ProjectPolicyRemovedV1>(payload.EventData),
            payload => Assert.IsType<TopicRelationAddedToProjectV1>(payload.EventData),
            payload => Assert.IsType<TopicRelationRemovedFromProjectV1>(payload.EventData),
            payload => Assert.IsType<ProjectDeletedV1>(payload.EventData)
        );
        var mapAggregateId = AggregateId.FromDatabaseGuid(
            StateDataAggregateIds.RepositoryToProjectMap
        );
        var mapEvents = eventStore.GetStoredEvents(mapAggregateId);
        Assert.Collection(
            mapEvents,
            payload => AssertMappingAdded(payload, projectId),
            payload => AssertMappingAdded(payload, projectId),
            payload => AssertMappingRemoved(payload, projectId),
            payload => AssertMappingRemoved(payload, projectId)
        );
        var projectState = Assert.IsType<ProjectPoliciesStateData>(
            eventStore.LastWritten[projectId].StateData
        );
        Assert.Equal(
            "MCP Skill and Policy System",
            projectState.ProjectName
        );
        Assert.Equal(repositoryPaths, projectState.RepositoryPaths);
        Assert.Empty(projectState.Policies);
        Assert.Empty(projectState.RelatedTopics);
        Assert.True(projectState.IsDeleted);
        Assert.Empty(
            Assert.IsType<RepositoryToProjectMapStateData>(
                eventStore.LastWritten[mapAggregateId].StateData
            ).RepositoryToProjectMap
        );
    }

    [Fact]
    public async Task ProjectUpdate_AfterDeletion_IsRejectedWithoutWrite()
    {
        var eventStore = new CapturingEventStoreWithOutbox();
        var handler = CreateHandler(eventStore);
        var project = Assert.IsType<ProjectCreatedCommandResult>(
            await new CreateProjectCommand(handler)
            {
                ProjectName = "Deleted project",
                ProjectDescription = "Validation test.",
                RepositoryPaths = []
            }.Execute(Executor)
        );
        var projectId = AggregateId.FromDatabaseGuid(
            project.ProjectId
        );

        await new DeleteProjectCommand(handler)
        {
            ProjectId = project.ProjectId
        }.Execute(Executor);
        var eventCount = eventStore.GetStoredEvents(projectId).Count;

        var exception = await Assert.ThrowsAsync<EventValidationException>(
            () =>
                new UpdateProjectCommand(handler)
                {
                    ProjectId = project.ProjectId,
                    ProjectName = "Invalid update",
                    ProjectDescription = "Must not be persisted."
                }.Execute(Executor)
        );

        Assert.Contains("project is deleted", exception.Message);
        Assert.Equal(
            eventCount,
            eventStore.GetStoredEvents(projectId).Count
        );
    }

    [Fact]
    public async Task RepositoryPath_CanBeReusedAfterProjectDeletion()
    {
        var eventStore = new CapturingEventStoreWithOutbox();
        var handler = CreateHandler(eventStore);
        const string repositoryPath = "/workspace/reusable";
        var firstProject = Assert.IsType<ProjectCreatedCommandResult>(
            await new CreateProjectCommand(handler)
            {
                ProjectName = "First project",
                ProjectDescription = "Will be deleted.",
                RepositoryPaths = [repositoryPath]
            }.Execute(Executor)
        );

        await new DeleteProjectCommand(handler)
        {
            ProjectId = firstProject.ProjectId
        }.Execute(Executor);

        var secondProject = Assert.IsType<ProjectCreatedCommandResult>(
            await new CreateProjectCommand(handler)
            {
                ProjectName = "Second project",
                ProjectDescription = "Reuses the released path.",
                RepositoryPaths = [repositoryPath]
            }.Execute(Executor)
        );
        var mapAggregateId = AggregateId.FromDatabaseGuid(
            StateDataAggregateIds.RepositoryToProjectMap
        );
        var mapState = Assert.IsType<RepositoryToProjectMapStateData>(
            eventStore.LastWritten[mapAggregateId].StateData
        );

        Assert.Equal(
            AggregateId.FromDatabaseGuid(secondProject.ProjectId),
            mapState.RepositoryToProjectMap[repositoryPath]
        );
        Assert.Collection(
            eventStore.GetStoredEvents(mapAggregateId),
            payload => Assert.IsType<RepositoryToProjectMapAddedV1>(
                payload.EventData
            ),
            payload => Assert.IsType<RepositoryToProjectMapRemovedV1>(
                payload.EventData
            ),
            payload => Assert.IsType<RepositoryToProjectMapAddedV1>(
                payload.EventData
            )
        );
    }

    [Fact]
    public async Task GetPoliciesByRepository_MergesGeneralProjectAndTopicPolicies()
    {
        var eventStore = new CapturingEventStoreWithOutbox();
        var handler = CreateHandler(eventStore);
        const string repositoryPath = "/workspace/policy-project";

        await new AddGeneralPolicyCommand(handler)
        {
            Title = "General policy",
            Description = "Applies to every project."
        }.Execute(Executor);
        await new CreateTopicCommand(handler)
        {
            TopicName = "cloud",
            Description = "Cloud policies."
        }.Execute(Executor);
        await new AddTopicPolicyCommand(handler)
        {
            TopicName = "cloud",
            Title = "Cloud policy",
            Description = "Applies to cloud projects."
        }.Execute(Executor);
        var project = Assert.IsType<ProjectCreatedCommandResult>(
            await new CreateProjectCommand(handler)
            {
                ProjectName = "Policy project",
                ProjectDescription = "Query test project.",
                RepositoryPaths = [repositoryPath]
            }.Execute(Executor)
        );
        await new AddProjectPolicyCommand(handler)
        {
            ProjectId = project.ProjectId,
            Title = "Project policy",
            Description = "Applies only to this project."
        }.Execute(Executor);
        await new AddTopicRelationToProjectCommand(handler)
        {
            ProjectId = project.ProjectId,
            TopicName = "cloud"
        }.Execute(Executor);
        var policyTextRepository = new StubPolicyTextRepository();
        var projectAggregateId = AggregateId.FromDatabaseGuid(
            project.ProjectId
        );
        policyTextRepository.PolicyTexts[projectAggregateId] =
            "# General policy\nApplies to every project.\n\n"
            + "# Project policy\nApplies only to this project.\n\n"
            + "# Cloud policy\nApplies to cloud projects.";

        var result = await new GetPoliciesByRepositoryQuery(
            CreateCalculator(),
            eventStore,
            policyTextRepository
        )
        {
            RepositoryPath = repositoryPath
        }.Execute(Executor);

        Assert.Equal(
            "# General policy\nApplies to every project.\n\n"
                + "# Project policy\nApplies only to this project.\n\n"
                + "# Cloud policy\nApplies to cloud projects.",
            result
        );
        Assert.Equal(
            projectAggregateId,
            policyTextRepository.LastProjectId
        );
        Assert.Equal(
            "General policy",
            Assert.Single(
                await new ListGeneralPoliciesQuery(
                    CreateCalculator(),
                    eventStore
                ).Execute(Executor)
            ).Title
        );
        Assert.Equal(
            "Cloud policy",
            Assert.Single(
                await new ListTopicPoliciesQuery(
                    CreateCalculator(),
                    eventStore
                )
                {
                    TopicName = "cloud"
                }.Execute(Executor)
            )?.Title
        );
        Assert.Equal(
            "Project policy",
            Assert.Single(
                await new ListProjectPoliciesQuery(
                    CreateCalculator(),
                    eventStore
                )
                {
                    ProjectId = project.ProjectId
                }.Execute(Executor)
            )?.Title
        );

        await new RemoveTopicCommand(handler)
        {
            TopicName = "cloud"
        }.Execute(Executor);
        policyTextRepository.PolicyTexts[projectAggregateId] =
            "# General policy\nApplies to every project.\n\n"
            + "# Project policy\nApplies only to this project.";

        Assert.Equal(
            "# General policy\nApplies to every project.\n\n"
                + "# Project policy\nApplies only to this project.",
            await new GetPoliciesByRepositoryQuery(
                CreateCalculator(),
                eventStore,
                policyTextRepository
            )
            {
                RepositoryPath = repositoryPath
            }.Execute(Executor)
        );

        await new DeleteProjectCommand(handler)
        {
            ProjectId = project.ProjectId
        }.Execute(Executor);

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => new GetPoliciesByRepositoryQuery(
                CreateCalculator(),
                eventStore,
                policyTextRepository
            )
            {
                RepositoryPath = repositoryPath
            }.Execute(Executor)
        );
        Assert.Equal(
            $"Policies for repository '{repositoryPath}' were not found.",
            exception.Message
        );
    }

    private static StateMachineHandler CreateHandler(
        CapturingEventStoreWithOutbox eventStore
    ) =>
        new(CreateCalculator(), eventStore);

    private static void AssertMappingAdded(
        EventPayload payload,
        AggregateId projectId
    )
    {
        var mapping = Assert.IsType<RepositoryToProjectMapAddedV1>(
            payload.EventData
        );
        Assert.Equal(projectId, mapping.ProjectAggregateId);
    }

    private static void AssertMappingRemoved(
        EventPayload payload,
        AggregateId projectId
    )
    {
        var mapping = Assert.IsType<RepositoryToProjectMapRemovedV1>(
            payload.EventData
        );
        Assert.Equal(projectId, mapping.ProjectAggregateId);
    }

    private static StateCalculator CreateCalculator() =>
        CreateCalculator(CreateDefinitionProvider());

    private static StateCalculator CreateCalculator(
        YamlStateMachineDefinitionProvider definitionProvider
    ) =>
        new(
            new OrderNumberHelper(),
            new PolicyStateDataProvider(),
            new EventValidatorProvider(definitionProvider),
            new StateMachineUniqueEventConstraintProvider(
                definitionProvider
            )
        );

    private static YamlStateMachineDefinitionProvider
        CreateDefinitionProvider() =>
            new(
                Path.Combine(
                    AppContext.BaseDirectory,
                    "StateMachines"
                )
            );

    private static void RegisterPolicyTypesOnce()
    {
        lock (RegistrationLock)
        {
            if (_typesRegistered)
                return;

            new ServiceCollection().RegisterEventSourcingCore(
                typeof(GeneralPoliciesStateData).Assembly
            );
            _typesRegistered = true;
        }
    }

    private sealed class CapturingEventStoreWithOutbox
        : IEventStoreWithOutbox, IEventStore
    {
        private readonly Dictionary<AggregateId, List<EventPayload>> eventsByAggregate = [];

        public Dictionary<AggregateId, StateInfo> LastWritten { get; private set; } = [];

        public List<EventPayload> GetStoredEvents(
            AggregateId aggregateId
        ) =>
            eventsByAggregate.TryGetValue(
                aggregateId,
                out var events
            )
                ? events.ToList()
                : [];

        public Task Write(
            Dictionary<AggregateId, StateInfo> stateInfos
        )
        {
            LastWritten = stateInfos;

            foreach (var (aggregateId, stateInfo) in stateInfos)
            {
                if (!eventsByAggregate.TryGetValue(
                        aggregateId,
                        out var events
                    ))
                {
                    events = [];
                    eventsByAggregate.Add(aggregateId, events);
                }

                events.AddRange(stateInfo.LastExecutedPayloads);
            }

            return Task.CompletedTask;
        }

        public Task Write(List<EventPayload> payloads)
        {
            foreach (
                var payloadGroup in payloads.GroupBy(
                    payload =>
                        payload.EventExecutionInfo.AggregateId
                )
            )
            {
                if (!eventsByAggregate.TryGetValue(
                        payloadGroup.Key,
                        out var events
                    ))
                {
                    events = [];
                    eventsByAggregate.Add(
                        payloadGroup.Key,
                        events
                    );
                }

                events.AddRange(payloadGroup);
            }

            return Task.CompletedTask;
        }

        public Task<Dictionary<AggregateId, List<EventPayload>>> GetEvents(
            List<AggregateId> aggregateIds
        ) =>
            Task.FromResult(
                aggregateIds.ToDictionary(
                    aggregateId => aggregateId,
                    GetStoredEvents
                )
            );
    }

    private sealed class PolicyStateDataProvider : IStateDataProvider
    {
        public Task<object> GetStateDataByStateMachine(
            string stateMachineId,
            AggregateId aggregateId
        ) =>
            Task.FromResult<object>(
                stateMachineId switch
                {
                    PolicyModule.Application.Constants.StateMachineIds.GeneralPolicies =>
                        new GeneralPoliciesStateData(AggregateId.New()),
                    PolicyModule.Application.Constants.StateMachineIds.ProjectPolicies =>
                        new ProjectPoliciesStateData(aggregateId),
                    PolicyModule.Application.Constants.StateMachineIds.RepositoryToProjectMap =>
                        new RepositoryToProjectMapStateData(aggregateId),
                    _ => throw new InvalidOperationException(
                        $"Unknown state machine '{stateMachineId}'."
                    )
                }
            );
    }

    private sealed class StubPolicyTextRepository
        : IPolicyTextRepository
    {
        public Dictionary<AggregateId, string> PolicyTexts { get; } = [];
        public AggregateId? LastProjectId { get; private set; }

        public Task<string?> Get(AggregateId projectAggregateId)
        {
            LastProjectId = projectAggregateId;

            return Task.FromResult(
                PolicyTexts.TryGetValue(
                    projectAggregateId,
                    out var text
                )
                    ? text
                    : null
            );
        }

    }

}

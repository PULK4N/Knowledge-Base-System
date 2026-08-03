using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Core.Interfaces;
using EventSourcing.Core.Providers;
using EventSourcing.Persistence.Interfaces;
using EventSourcing.Shared.Models;
using PolicyModule.Application.Commands;
using PolicyModule.Application.Models;
using PolicyModule.Domain;
using PolicyModule.Domain.Events;
using PolicyModule.Domain.Models;
using Shared.Interfaces;
using SharedModule.Constants;
using UUIDNext;

namespace PolicyModule.Application.Tests;

public sealed class PolicyCommandTests
{
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
            }
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
        var addedPolicy = Assert.IsType<PolicyAddedCommandResult>(
            await new AddTopicPolicyCommand(handler)
            {
                TopicName = topicName,
                Title = "Use managed identities",
                Description = "Do not store service credentials."
            }.Execute(Executor)
        );
        await new RemoveTopicPolicyCommand(handler)
        {
            TopicName = topicName,
            PolicyId = addedPolicy.PolicyId
        }.Execute(Executor);

        var globalAggregateId = AggregateId.FromDatabaseGuid(
            StateDataAggregateIds.GeneralPolicies
        );
        Assert.Collection(
            eventStore.GetStoredEvents(globalAggregateId),
            payload => Assert.IsType<TopicCreatedV1>(payload.EventData),
            payload => Assert.IsType<TopicPolicyAddedV1>(payload.EventData),
            payload =>
            {
                var removed = Assert.IsType<TopicPolicyRemovedV1>(
                    payload.EventData
                );
                Assert.Equal(
                    addedPolicy.PolicyId,
                    removed.PolicyId.Value
                );
            }
        );
        var state = Assert.IsType<GeneralPoliciesStateData>(
            eventStore.LastWritten[globalAggregateId].StateData
        );
        Assert.Empty(state.Topics[new TopicName(topicName)].Policies);
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
        var created = Assert.IsType<ProjectForPoliciesCreatedCommandResult>(
            await new CreateProjectForPoliciesCommand(handler)
            {
                ProjectName = "MCP Skill System",
                ProjectDescription = "Event-sourced Codex tooling.",
                RepositoryPaths = repositoryPaths
            }.Execute(Executor)
        );
        var projectId = AggregateId.FromDatabaseGuid(created.ProjectId);

        var addedPolicy = Assert.IsType<PolicyAddedCommandResult>(
            await new AddProjectPolicyCommand(handler)
            {
                ProjectId = created.ProjectId,
                Title = "Keep policies separate from skills",
                Description = "Policies are injected into every chat."
            }.Execute(Executor)
        );
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

        Assert.NotEqual(Guid.Empty, created.ProjectId);
        Assert.Collection(
            eventStore.GetStoredEvents(projectId),
            payload => Assert.IsType<ProjectForPoliciesCreatedV1>(payload.EventData),
            payload => Assert.IsType<ProjectPolicyAddedV1>(payload.EventData),
            payload => Assert.IsType<ProjectPolicyRemovedV1>(payload.EventData),
            payload => Assert.IsType<TopicRelationAddedToProjectV1>(payload.EventData)
        );
        var mapAggregateId = AggregateId.FromDatabaseGuid(
            StateDataAggregateIds.RepositoryToProjectMap
        );
        Assert.All(
            eventStore.GetStoredEvents(mapAggregateId),
            payload =>
            {
                var mapping = Assert.IsType<RepositoryToProjectMapAddedV1>(
                    payload.EventData
                );
                Assert.Equal(projectId, mapping.ProjectAggregateId);
            }
        );
        var projectState = Assert.IsType<ProjectPoliciesStateData>(
            eventStore.LastWritten[projectId].StateData
        );
        Assert.Equal(repositoryPaths, projectState.RepositoryPaths);
        Assert.Empty(projectState.Policies);
        Assert.Equal(
            new TopicName("cloud"),
            Assert.Single(projectState.RelatedTopics)
        );
    }

    private static StateMachineHandler CreateHandler(
        CapturingEventStoreWithOutbox eventStore
    ) =>
        new(
            new StateCalculator(
                new OrderNumberHelper(),
                new PolicyStateDataProvider(),
                new EmptyEventValidatorProvider(),
                new EmptyUniqueEventConstraintProvider()
            ),
            eventStore
        );

    private sealed class CapturingEventStoreWithOutbox
        : IEventStoreWithOutbox
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

    private sealed class EmptyEventValidatorProvider
        : IEventValidatorProvider
    {
        public Task<List<IPreEventValidator>> GetPreEventStateValidators(
            EventPayload payload
        ) =>
            Task.FromResult(new List<IPreEventValidator>());

        public Task<List<IPostEventValidator>> GetPostEventStateValidators(
            EventPayload payload
        ) =>
            Task.FromResult(new List<IPostEventValidator>());
    }

    private sealed class EmptyUniqueEventConstraintProvider
        : IUniqueEventConstraintProvider
    {
        public IEnumerable<UniqueEventConstraintData> GetConstraintsToAdd(
            object stateData,
            EventPayload payload
        ) =>
            [];

        public IEnumerable<UniqueEventConstraintData> GetConstraintsToRemove(
            object stateData,
            EventPayload payload
        ) =>
            [];
    }
}

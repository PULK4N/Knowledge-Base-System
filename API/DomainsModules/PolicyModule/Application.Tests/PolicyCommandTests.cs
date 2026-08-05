using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Core.Interfaces;
using EventSourcing.Core.Providers;
using EventSourcing.Persistence.Interfaces;
using EventSourcing.Shared.Models;
using PolicyModule.Application.Commands;
using PolicyModule.Application.Models;
using PolicyModule.Application.Queries;
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
        Assert.Collection(
            eventStore.GetStoredEvents(globalAggregateId),
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
        Assert.Collection(
            eventStore.GetStoredEvents(projectId),
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
        Assert.Equal(
            "MCP Skill and Policy System",
            projectState.ProjectName
        );
        Assert.Equal(repositoryPaths, projectState.RepositoryPaths);
        Assert.Empty(projectState.Policies);
        Assert.Empty(projectState.RelatedTopics);
        Assert.True(projectState.IsDeleted);
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

        var result = await new GetPoliciesByRepositoryQuery(
            CreateCalculator(),
            eventStore
        )
        {
            RepositoryPath = repositoryPath
        }.Execute(Executor);

        Assert.Equal(
            "General policy\nApplies to every project.\n\n"
                + "Project policy\nApplies only to this project.\n\n"
                + "Cloud policy\nApplies to cloud projects.",
            result
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

        Assert.Equal(
            "General policy\nApplies to every project.\n\n"
                + "Project policy\nApplies only to this project.",
            await new GetPoliciesByRepositoryQuery(
                CreateCalculator(),
                eventStore
            )
            {
                RepositoryPath = repositoryPath
            }.Execute(Executor)
        );

        await new DeleteProjectCommand(handler)
        {
            ProjectId = project.ProjectId
        }.Execute(Executor);

        Assert.Null(
            await new GetPoliciesByRepositoryQuery(
                CreateCalculator(),
                eventStore
            )
            {
                RepositoryPath = repositoryPath
            }.Execute(Executor)
        );
    }

    private static StateMachineHandler CreateHandler(
        CapturingEventStoreWithOutbox eventStore
    ) =>
        new(CreateCalculator(), eventStore);

    private static StateCalculator CreateCalculator() =>
        new(
            new OrderNumberHelper(),
            new PolicyStateDataProvider(),
            new EmptyEventValidatorProvider(),
            new EmptyUniqueEventConstraintProvider()
        );

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

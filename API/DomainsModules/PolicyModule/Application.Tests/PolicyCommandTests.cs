using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Core.Interfaces;
using EventSourcing.Core.Providers;
using EventSourcing.Persistence.Interfaces;
using EventSourcing.Shared.Exceptions;
using EventSourcing.Shared.Models;
using Microsoft.Extensions.DependencyInjection;
using PolicyModule.Application.Commands;
using PolicyModule.Application.DTOs;
using PolicyModule.Application.Queries;
using PolicyModule.Domain;
using PolicyModule.Domain.Events;
using PolicyModule.Domain.Models;
using Shared.Interfaces;
using SharedModule.Constants;
using UUIDNext;

namespace PolicyModule.Application.Tests;

public sealed partial class PolicyCommandTests
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
    public void GeneralPolicyEvents_UseSharedProjections()
    {
        var definition = CreateDefinitionProvider().Get(
            PolicyModule.Application.Constants.StateMachineIds.GeneralPolicies
        );

        Assert.Equal(
            [
                "GeneralPolicyTextProjector",
                "TopicPolicyTextProjector",
                "AgentFamilyPolicyTextProjector"
            ],
            definition.Projections
        );
        Assert.Equal(
            [
                nameof(GeneralPolicyAddedV1), nameof(GeneralPolicyAddedV2),
                nameof(TopicCreatedV1), nameof(TopicCreatedV2),
                nameof(AgentFamilyCreatedV1), nameof(AgentFamilyCreatedV2)
            ],
            definition.InitializationEvents
        );
        Assert.All(
            definition.Events.Values,
            eventDefinition => Assert.Empty(eventDefinition.Projections)
        );
    }

    [Fact]
    public async Task GeneralPolicyCommands_UseFixedGlobalStream()
    {
        var eventStore = new CapturingEventStoreWithOutbox();
        var handler = CreateHandler(eventStore);
        var addResult = Assert.IsType<PolicyAddedCommandResult>(
            await ExecuteAsUser(new AddGeneralPolicyCommand(handler)
            {
                Title = "Always run focused tests",
                Description = "Verify only the affected behavior."
            })
        );

        await ExecuteAsUser(new UpdateGeneralPolicyCommand(handler)
        {
            PolicyId = addResult.PolicyId,
            Title = "Always run the smallest focused tests",
            Description = "Verify the affected behavior first."
        });
        await ExecuteAsUser(new RemoveGeneralPolicyCommand(handler)
        {
            PolicyId = addResult.PolicyId
        });

        var globalAggregateId = AggregateId.FromDatabaseGuid(
            StateDataAggregateIds.GeneralPolicies
        );
        var events = eventStore.GetStoredEvents(globalAggregateId);
        Assert.Collection(
            events,
            payload =>
            {
                var added = Assert.IsType<GeneralPolicyAddedV2>(
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
                var updated = Assert.IsType<GeneralPolicyUpdatedV2>(
                    payload.EventData
                );
                Assert.Equal(
                    "Always run the smallest focused tests",
                    updated.Policy.Title
                );
            },
            payload =>
                Assert.IsType<GeneralPolicyRemovedV2>(
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

        await ExecuteAsUser(new CreateTopicCommand(handler)
        {
            TopicName = topicName,
            Description = "Policies for cloud usage."
        });
        await ExecuteAsUser(new UpdateTopicCommand(handler)
        {
            TopicName = topicName,
            Description = "Updated cloud policies."
        });
        var addedPolicy = Assert.IsType<PolicyAddedCommandResult>(
            await ExecuteAsUser(new AddTopicPolicyCommand(handler)
            {
                TopicName = topicName,
                Title = "Use managed identities",
                Description = "Do not store service credentials."
            })
        );
        await ExecuteAsUser(new UpdateTopicPolicyCommand(handler)
        {
            TopicName = topicName,
            PolicyId = addedPolicy.PolicyId,
            Title = "Use workload identities",
            Description = "Do not store service credentials."
        });
        await ExecuteAsUser(new RemoveTopicPolicyCommand(handler)
        {
            TopicName = topicName,
            PolicyId = addedPolicy.PolicyId
        });
        await ExecuteAsUser(new RemoveTopicCommand(handler)
        {
            TopicName = topicName
        });

        var globalAggregateId = AggregateId.FromDatabaseGuid(
            StateDataAggregateIds.GeneralPolicies
        );
        var events = eventStore.GetStoredEvents(globalAggregateId);
        Assert.Collection(
            events,
            payload => Assert.IsType<TopicCreatedV2>(payload.EventData),
            payload => Assert.IsType<TopicUpdatedV2>(payload.EventData),
            payload => Assert.IsType<TopicPolicyAddedV2>(payload.EventData),
            payload =>
                Assert.IsType<TopicPolicyUpdatedV2>(
                    payload.EventData
                ),
            payload =>
            {
                var removed = Assert.IsType<TopicPolicyRemovedV2>(
                    payload.EventData
                );
                Assert.Equal(
                    addedPolicy.PolicyId,
                    removed.PolicyId.Value
                );
            },
            payload => Assert.IsType<TopicRemovedV2>(payload.EventData)
        );
        var state = Assert.IsType<GeneralPoliciesStateData>(
            eventStore.LastWritten[globalAggregateId].StateData
        );
        Assert.Empty(state.Topics);
    }

    [Theory]
    [InlineData("claude", "claude")]
    [InlineData("Codex", "codex")]
    public async Task AgentFamilyCommands_CreateAddAndRemovePolicyOnGlobalStream(
        string requestedName,
        string storedName
    )
    {
        var eventStore = new CapturingEventStoreWithOutbox();
        var handler = CreateHandler(eventStore);

        await ExecuteAsUser(new CreateAgentFamilyCommand(handler)
        {
            AgentFamilyName = requestedName,
            Description = "Policies for this agent family."
        });
        await ExecuteAsUser(new UpdateAgentFamilyCommand(handler)
        {
            AgentFamilyName = requestedName,
            Description = "Updated agent family policies."
        });
        var addedPolicy = Assert.IsType<PolicyAddedCommandResult>(
            await ExecuteAsUser(new AddAgentFamilyPolicyCommand(handler)
            {
                AgentFamilyName = requestedName,
                Title = "Prefer the dedicated file tools",
                Description = "Read and edit through the provided tools."
            })
        );
        await ExecuteAsUser(new UpdateAgentFamilyPolicyCommand(handler)
        {
            AgentFamilyName = requestedName,
            PolicyId = addedPolicy.PolicyId,
            Title = "Prefer the dedicated file tools",
            Description = "Read and edit through the dedicated tools."
        });
        await ExecuteAsUser(new RemoveAgentFamilyPolicyCommand(handler)
        {
            AgentFamilyName = requestedName,
            PolicyId = addedPolicy.PolicyId
        });
        await ExecuteAsUser(new RemoveAgentFamilyCommand(handler)
        {
            AgentFamilyName = requestedName
        });

        var globalAggregateId = AggregateId.FromDatabaseGuid(
            StateDataAggregateIds.GeneralPolicies
        );
        var events = eventStore.GetStoredEvents(globalAggregateId);
        Assert.Collection(
            events,
            payload =>
            {
                var created = Assert.IsType<AgentFamilyCreatedV2>(
                    payload.EventData
                );
                Assert.Equal(
                    storedName,
                    created.AgentFamilyName.Name
                );
            },
            payload =>
                Assert.IsType<AgentFamilyUpdatedV2>(payload.EventData),
            payload =>
                Assert.IsType<AgentFamilyPolicyAddedV2>(
                    payload.EventData
                ),
            payload =>
                Assert.IsType<AgentFamilyPolicyUpdatedV2>(
                    payload.EventData
                ),
            payload =>
            {
                var removed = Assert.IsType<AgentFamilyPolicyRemovedV2>(
                    payload.EventData
                );
                Assert.Equal(
                    addedPolicy.PolicyId,
                    removed.PolicyId.Value
                );
            },
            payload =>
                Assert.IsType<AgentFamilyRemovedV2>(payload.EventData)
        );
        var state = Assert.IsType<GeneralPoliciesStateData>(
            eventStore.LastWritten[globalAggregateId].StateData
        );
        Assert.Empty(state.AgentFamilies);
    }

    private static Task<object> ExecuteAsUser(PolicyCommand command)
    {
        command.UseUserOrigin();
        return command.Execute(Executor);
    }

    private static StateMachineHandler CreateHandler(
        CapturingEventStoreWithOutbox eventStore
    ) =>
        new(CreateCalculator(), eventStore);

    private static StateCalculator CreateCalculator() =>
        CreateCalculator(CreateDefinitionProvider());

    private static StateCalculator CreateCalculator(
        YamlStateMachineDefinitionProvider definitionProvider
    ) =>
        new(
            new OrderNumberHelper(),
            new PolicyStateDataProvider(),
            new EventValidatorProvider(definitionProvider),
            definitionProvider
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
                typeof(GeneralPoliciesStateData).Assembly,
                typeof(MemoryModule.Domain.MemoryStateData).Assembly
            );
            _typesRegistered = true;
        }
    }

    private sealed class CapturingEventStoreWithOutbox
        : IEventStoreWithOutbox, IEventStore
    {
        private readonly Dictionary<AggregateId, List<EventPayload>> eventsByAggregate = [];

        public int WriteCount { get; private set; }

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
            WriteCount++;
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

        public Task Write(List<StateInfo> stateInfos)
        {
            var payloads = stateInfos
                .SelectMany(stateInfo => stateInfo.LastExecutedPayloads)
                .ToList();

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
        public object GetStateDataByStateMachine(
            string stateMachineId,
            AggregateId aggregateId
        ) =>
            (
                stateMachineId switch
                {
                    PolicyModule.Application.Constants.StateMachineIds.GeneralPolicies =>
                        new GeneralPoliciesStateData(AggregateId.New()),
                    MemoryModule.Application.Constants.StateMachineIds.Memory =>
                        new MemoryModule.Domain.MemoryStateData(aggregateId),
                    MemoryModule.Application.Constants.StateMachineIds.SessionAggregateMap =>
                        new MemoryModule.Domain.SessionAggregateMapStateData(aggregateId),
                    _ => throw new InvalidOperationException(
                        $"Unknown state machine '{stateMachineId}'."
                    )
                }
            );
    }
}

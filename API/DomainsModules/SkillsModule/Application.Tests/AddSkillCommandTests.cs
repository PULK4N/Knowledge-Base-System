using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Core.Interfaces;
using EventSourcing.Core.Providers;
using EventSourcing.Persistence.Interfaces;
using EventSourcing.Shared.Models;
using MemoryModule.Domain;
using MemoryModule.Domain.Events;
using MemoryModule.Domain.Models;
using Shared.Interfaces;
using SkillsModule.Application.Commands;
using SkillsModule.Application.Models;
using SkillsModule.Domain;
using SkillsModule.Domain.Events;
using SkillsModule.Domain.Models;
using UUIDNext;

namespace SkillsModule.Application.Tests;

public sealed class AddSkillCommandTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task Execute_ReturnsGeneratedAggregateIdWrittenToStream(
        bool userOrigin, bool suppliedMemoryId
    )
    {
        DatabaseFriendlyGuidGenerator
            .SetDefaultGuidGenerationDatabase(Database.SqlServer);
        var outbox = new CapturingEventStoreWithOutbox();
        var stateCalculator = new StateCalculator(
            new OrderNumberHelper(),
            new SkillStateDataProvider(),
            new EmptyEventValidatorProvider(),
            new EmptyUniqueEventConstraintProvider(),
            new TestStateMachineDefinitionProvider()
        );
        var handler = new StateMachineHandler(
            stateCalculator,
            outbox
        );
        var command = new AddSkillCommand(handler)
        {
            Name = "skill-name",
            Description = "Description",
            Content = "Content",
            SessionId = SkillStateDataProvider.SessionId,
            References = new Dictionary<string, SkillReference2>
            {
                ["references/example.md"] = new(
                    "Reference content",
                    true
                )
            }
        };
        if (suppliedMemoryId)
            command.MemoryAggregateId = SkillStateDataProvider.MemoryAggregateId.Value;
        if (userOrigin)
            command.UseUserOrigin();

        var executor = new Executor
        {
            Id = EventExecutor.FromDatabaseGuid(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
            )
        };

        var result = Assert.IsType<SkillCreatedCommandResult>(
            await command.Execute(executor)
        );

        Assert.Equal(userOrigin ? 1 : 2, outbox.Written.Count);
        var payload = Assert.Single(
            outbox.Written,
            eventPayload => eventPayload.EventData is SkillCreatedV3
        );
        Assert.Equal("OK", result.Status);
        Assert.NotEqual(Guid.Empty, result.SkillId);
        Assert.Equal(
            result.SkillId,
            payload.EventExecutionInfo.AggregateId.Value
        );
        var created = Assert.IsType<SkillCreatedV3>(payload.EventData);
        Assert.True(
            created.References["references/example.md"].LoadAutomatically
        );
        var expectedMemoryId = userOrigin
            ? AggregateId.FromDatabaseGuid(SharedModule.Constants.MemoryAggregateIds.User)
            : SkillStateDataProvider.MemoryAggregateId;
        Assert.Equal(expectedMemoryId, created.MemoryAggregateId);
        var state = (SkillStateData)created.Apply(
            new SkillStateData(payload.EventExecutionInfo.AggregateId),
            payload.EventExecutionInfo
        );
        var history = Assert.Single(state.MemoryHistory);
        Assert.Equal(expectedMemoryId, history.AggregateId);
        Assert.Equal(payload.EventExecutionInfo.Timestamp, history.Timestamp);
        Assert.Equal(nameof(SkillCreatedV3), history.EventName);
        Assert.Equal(1, outbox.WriteCount);
        if (userOrigin)
        {
            Assert.Equal(Guid.Empty, created.SessionId);
            Assert.Equal(0, outbox.SessionMapReadCount);
            return;
        }
        var relationPayload = Assert.Single(
            outbox.Written,
            eventPayload => eventPayload.EventData is MemoryRelationAddedV1
        );
        Assert.Equal(expectedMemoryId, relationPayload.EventExecutionInfo.AggregateId);
        var relation = Assert.IsType<MemoryRelationAddedV1>(
            relationPayload.EventData
        );
        Assert.Equal(nameof(SkillCreatedV3), relation.Relation);
        Assert.Equal(
            AggregateId.FromDatabaseGuid(result.SkillId),
            relation.AggregateId
        );
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000", "00000000-0000-0000-0000-000000000000")]
    [InlineData("dddddddd-dddd-dddd-dddd-dddddddddddd", "00000000-0000-0000-0000-000000000000")]
    [InlineData("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb", "dddddddd-dddd-dddd-dddd-dddddddddddd")]
    [InlineData("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb", "10000000-0000-0000-0000-000000000000")]
    public async Task Chat_origin_rejects_missing_session_or_unrelated_memory(
        string sessionId, string memoryId
    )
    {
        DatabaseFriendlyGuidGenerator.SetDefaultGuidGenerationDatabase(Database.SqlServer);
        var outbox = new CapturingEventStoreWithOutbox();
        var handler = new StateMachineHandler(
            new StateCalculator(
                new OrderNumberHelper(), new SkillStateDataProvider(),
                new EmptyEventValidatorProvider(), new EmptyUniqueEventConstraintProvider(),
                new TestStateMachineDefinitionProvider()
            ),
            outbox
        );
        var command = new AddSkillCommand(handler)
        {
            Name = "skill", Description = "description", Content = "content",
            SessionId = Guid.Parse(sessionId), MemoryAggregateId = Guid.Parse(memoryId)
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => command.Execute(
            new Executor { Id = EventExecutor.FromDatabaseGuid(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")) }
        ));
        Assert.Equal(0, outbox.WriteCount);
    }

    private sealed class CapturingEventStoreWithOutbox
        : IEventStoreWithOutbox
    {
        public List<EventPayload> Written { get; private set; } = [];
        public int WriteCount { get; private set; }
        public int SessionMapReadCount { get; private set; }

        public Task Write(
            Dictionary<AggregateId, StateInfo> stateInfos
        )
        {
            WriteCount++;
            Written = stateInfos
                .Values
                .SelectMany(stateInfo => stateInfo.LastExecutedPayloads)
                .ToList();
            return Task.CompletedTask;
        }

        public Task<Dictionary<AggregateId, List<EventPayload>>> GetEvents(
            List<AggregateId> aggregateIds
        )
        {
            if (aggregateIds.Contains(MemoryAggregateIds.SessionAggregateMap))
                SessionMapReadCount++;
            return Task.FromResult(
                aggregateIds.ToDictionary(
                    aggregateId => aggregateId,
                    aggregateId => aggregateId == MemoryAggregateIds.SessionAggregateMap
                        ? new List<EventPayload>
                        {
                            CreateSessionMapPayload(aggregateId)
                        }
                        : new List<EventPayload>()
                )
            );
        }
    }

    private static EventPayload CreateSessionMapPayload(AggregateId aggregateId)
    {
        var payload = EventPayload.Create(
            EventExecutor.FromDatabaseGuid(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")),
            aggregateId,
            "memory-session-aggregate-map-state-machine",
            new SessionAggregateMapAddedV1(
                new ThreadId(SkillStateDataProvider.SessionId),
                SkillStateDataProvider.MemoryAggregateId
            )
        );
        payload.EventExecutionInfo.OrderNumber = 1;
        return payload;
    }

    private sealed class EmptyEventValidatorProvider
        : IEventValidatorProvider
    {
        public Task<List<IPreEventValidator>>
            GetPreEventStateValidators(EventPayload payload) =>
                Task.FromResult(new List<IPreEventValidator>());

        public Task<List<IPostEventValidator>>
            GetPostEventStateValidators(EventPayload payload) =>
                Task.FromResult(new List<IPostEventValidator>());
    }

    private sealed class EmptyUniqueEventConstraintProvider
        : IUniqueEventConstraintProvider
    {
        public IEnumerable<UniqueEventConstraintData>
            GetConstraintsToAdd(
                object stateData,
                EventPayload payload
            ) =>
                [];

        public IEnumerable<UniqueEventConstraintData>
            GetConstraintsToRemove(
                object stateData,
                EventPayload payload
            ) =>
                [];
    }

    private sealed class SkillStateDataProvider : IStateDataProvider
    {
        public static readonly Guid SessionId =
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        public static readonly AggregateId MemoryAggregateId =
            AggregateId.FromDatabaseGuid(
                Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc")
            );

        public Task<object> GetStateDataByStateMachine(
            string stateMachineId,
            AggregateId aggregateId
        ) =>
            Task.FromResult<object>(
                stateMachineId switch
                {
                    "memory-session-aggregate-map-state-machine" =>
                        CreateSessionMap(aggregateId),
                    "memory-state-machine" => new MemoryStateData(aggregateId),
                    _ => new SkillStateData(aggregateId)
                }
            );

        private static SessionAggregateMapStateData CreateSessionMap(
            AggregateId aggregateId
        )
        {
            var state = new SessionAggregateMapStateData(aggregateId);

            return state;
        }
    }
}

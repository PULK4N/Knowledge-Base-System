using System.Text.Json;
using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Core.Interfaces;
using EventSourcing.Core.Providers;
using EventSourcing.Persistence.Interfaces;
using EventSourcing.Persistence.Models;
using EventSourcing.Shared.Containers;
using EventSourcing.Shared.Models;
using MemoryModule.Application.Commands;
using MemoryModule.Application.Models;
using MemoryModule.Domain;
using MemoryModule.Domain.Events;
using MemoryModule.Domain.Models;
using Shared.Interfaces;
using UUIDNext;

namespace MemoryModule.Application.Tests;

public sealed class RecordCodexPromptHookCommandTests
{
    private static readonly ThreadId ThreadId =
        new(Guid.Parse("019fb72e-e0c3-7452-b32b-5bbf65433c98"));

    private static readonly PromptId FirstPromptId =
        new(Guid.Parse("019fb72e-e3c3-7093-a89d-050d309ca4ac"));

    private static readonly PromptId SecondPromptId =
        new(Guid.Parse("019fb72e-e4c3-7093-a89d-050d309ca4ac"));

    private static readonly Executor Executor =
        new()
        {
            Id = EventExecutor.FromDatabaseGuid(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
            )
        };

    static RecordCodexPromptHookCommandTests()
    {
        EventTypeContainer.AddEventType(
            typeof(CodexPromptHookRecordedV1)
        );
        EventTypeContainer.AddEventType(
            typeof(CodexMemoryMigratedV1)
        );
        EventTypeContainer.AddEventType(
            typeof(ChatSummaryAddedV1)
        );
    }

    [Fact]
    public async Task Execute_NewSession_WritesMapAndMemoryEvents()
    {
        DatabaseFriendlyGuidGenerator.SetDefaultGuidGenerationDatabase(
            Database.SqlServer
        );
        var eventStore = new CapturingEventStoreWithOutbox();
        var command = CreateCommand(eventStore, FirstPromptId);

        var result = await command.Execute(Executor);

        Assert.Equal("OK", result.Status);
        Assert.Equal(2, eventStore.LastWritten.Count);

        var mapState = Assert.IsType<SessionAggregateMapStateData>(
            eventStore.LastWritten[MemoryAggregateIds.SessionAggregateMap].StateData
        );
        var memoryAggregateId = mapState.AggregateIdsBySession[ThreadId];
        var memoryState = Assert.IsType<MemoryStateData>(
            eventStore.LastWritten[memoryAggregateId].StateData
        );

        Assert.Equal(ThreadId, memoryState.ThreadId);
        Assert.True(memoryState.ChatPrompts.ContainsKey(FirstPromptId));
        Assert.IsType<SessionAggregateMapAddedV1>(
            Assert.Single(
                eventStore.LastWritten[MemoryAggregateIds.SessionAggregateMap]
                    .LastExecutedPayloads
            ).EventData
        );
        Assert.IsType<CodexPromptHookRecordedV1>(
            Assert.Single(
                eventStore.LastWritten[memoryAggregateId].LastExecutedPayloads
            ).EventData
        );
    }

    [Fact]
    public async Task Execute_ExistingSession_ReusesMappedAggregate()
    {
        DatabaseFriendlyGuidGenerator.SetDefaultGuidGenerationDatabase(
            Database.SqlServer
        );
        var eventStore = new CapturingEventStoreWithOutbox();

        await CreateCommand(eventStore, FirstPromptId).Execute(Executor);
        var firstMapState = Assert.IsType<SessionAggregateMapStateData>(
            eventStore.LastWritten[MemoryAggregateIds.SessionAggregateMap].StateData
        );
        var firstMemoryAggregateId = firstMapState.AggregateIdsBySession[ThreadId];

        await CreateCommand(eventStore, SecondPromptId).Execute(Executor);

        var writtenAggregate = Assert.Single(eventStore.LastWritten);
        Assert.Equal(firstMemoryAggregateId, writtenAggregate.Key);
        var memoryState = Assert.IsType<MemoryStateData>(
            writtenAggregate.Value.StateData
        );
        Assert.Equal(2, memoryState.ChatPrompts.Count);
        Assert.True(memoryState.ChatPrompts.ContainsKey(FirstPromptId));
        Assert.True(memoryState.ChatPrompts.ContainsKey(SecondPromptId));
        var recordedEvent = Assert.IsType<CodexPromptHookRecordedV1>(
            Assert.Single(
                writtenAggregate.Value.LastExecutedPayloads
            ).EventData
        );
        Assert.Equal(SecondPromptId, recordedEvent.PromptId);
    }

    [Fact]
    public void PersistenceRoundTrip_PreservesJsonElementPayload()
    {
        var jsonPayload = JsonSerializer.SerializeToElement(
            new
            {
                value = "hook-data",
                nested = new { count = 2 }
            }
        );
        var payload = EventPayload.Create(
            Executor.Id,
            AggregateId.FromDatabaseGuid(
                Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")
            ),
            Constants.StateMachineIds.Memory,
            new CodexPromptHookRecordedV1(
                ThreadId,
                FirstPromptId,
                "after_agent",
                jsonPayload
            )
        );

        var storedEvent = SerializedEventPayload.FromPayload(payload);
        var eventRoundTrip = Assert.IsType<CodexPromptHookRecordedV1>(
            storedEvent.Deserialize().EventData
        );
        var storedMessage = SerializedPayloadMessage.FromPayload(payload);
        var messageRoundTrip = Assert.IsType<CodexPromptHookRecordedV1>(
            storedMessage.Deserialize().Payload.EventData
        );

        Assert.Equal(
            jsonPayload.GetRawText(),
            eventRoundTrip.Payload.GetRawText()
        );
        Assert.Equal(
            jsonPayload.GetRawText(),
            messageRoundTrip.Payload.GetRawText()
        );
    }

    [Fact]
    public async Task Execute_MigrationWritesDedicatedEventAndPayload()
    {
        DatabaseFriendlyGuidGenerator.SetDefaultGuidGenerationDatabase(
            Database.SqlServer
        );
        var eventStore = new CapturingEventStoreWithOutbox();
        var command = new MigrateCodexMemoryCommand(
            CreateHandler(eventStore)
        )
        {
            ThreadId = ThreadId,
            RawMemory = "Raw stage-one memory",
            RolloutSummary = "Thread rollout summary",
            Source = "codex-stage1-output"
        };

        var result = await command.Execute(Executor);

        Assert.Equal("OK", result.Status);

        var mapState = Assert.IsType<SessionAggregateMapStateData>(
            eventStore.LastWritten[MemoryAggregateIds.SessionAggregateMap].StateData
        );
        var memoryAggregateId = mapState.AggregateIdsBySession[ThreadId];
        var memoryState = Assert.IsType<MemoryStateData>(
            eventStore.LastWritten[memoryAggregateId].StateData
        );
        var prompt = Assert.Single(memoryState.ChatPrompts).Value;
        Assert.NotEqual(Guid.Empty, prompt.PromptId.Value);

        var hook = Assert.Single(prompt.PromptHookRecords);
        Assert.Equal(
            CodexMemoryMigratedV1.UserMigrationHookEventName,
            hook.HookEventName
        );
        Assert.Equal(3, hook.Payload.EnumerateObject().Count());
        Assert.Equal(
            "Raw stage-one memory",
            hook.Payload.GetProperty("raw_memory").GetString()
        );
        Assert.Equal(
            "Thread rollout summary",
            hook.Payload.GetProperty("rollout_summary").GetString()
        );
        Assert.Equal(
            "codex-stage1-output",
            hook.Payload.GetProperty("source").GetString()
        );
        Assert.IsType<CodexMemoryMigratedV1>(
            Assert.Single(
                eventStore.LastWritten[memoryAggregateId].LastExecutedPayloads
            ).EventData
        );
    }

    private static RecordCodexPromptHookCommand CreateCommand(
        CapturingEventStoreWithOutbox eventStore,
        PromptId promptId
    )
    {
        return new RecordCodexPromptHookCommand(CreateHandler(eventStore))
        {
            ThreadId = ThreadId,
            PromptId = promptId,
            HookEventName = "after_agent",
            Payload = JsonSerializer.SerializeToElement(
                new { value = "hook-data" }
            )
        };
    }

    private static StateMachineHandler CreateHandler(
        CapturingEventStoreWithOutbox eventStore
    ) =>
        new(
            new StateCalculator(
                new OrderNumberHelper(),
                new MemoryStateDataProvider(),
                new EmptyEventValidatorProvider(),
                new EmptyUniqueEventConstraintProvider()
            ),
            eventStore
        );

    private sealed class CapturingEventStoreWithOutbox
        : IEventStoreWithOutbox
    {
        private readonly Dictionary<AggregateId, List<EventPayload>> _events = [];

        public Dictionary<AggregateId, StateInfo> LastWritten { get; private set; } = [];

        public Task Write(
            Dictionary<AggregateId, StateInfo> stateInfos
        )
        {
            LastWritten = stateInfos;

            foreach (var (aggregateId, stateInfo) in stateInfos)
            {
                if (!_events.TryGetValue(aggregateId, out var events))
                {
                    events = [];
                    _events.Add(aggregateId, events);
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
                    aggregateId => _events.TryGetValue(
                        aggregateId,
                        out var events
                    )
                        ? events.ToList()
                        : []
                )
            );
    }

    private sealed class MemoryStateDataProvider : IStateDataProvider
    {
        public Task<object> GetStateDataByStateMachine(
            string stateMachineId,
            AggregateId aggregateId
        ) =>
            Task.FromResult<object>(
                stateMachineId switch
                {
                    Constants.StateMachineIds.SessionAggregateMap =>
                        new SessionAggregateMapStateData(aggregateId),
                    Constants.StateMachineIds.Memory =>
                        new MemoryStateData(aggregateId),
                    _ => throw new InvalidOperationException()
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

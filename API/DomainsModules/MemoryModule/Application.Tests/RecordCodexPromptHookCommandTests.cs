using System.Text.Json;
using ActionModule.Shared.Models;
using AttachmentsModule.Application;
using EventSourcing.Core;
using EventSourcing.Core.Interfaces;
using EventSourcing.Core.Providers;
using EventSourcing.Persistence.Interfaces;
using EventSourcing.Persistence.Models;
using EventSourcing.Shared.Containers;
using EventSourcing.Shared.Models;
using MemoryModule.Application.Commands;
using MemoryModule.Application.DTOs;
using MemoryModule.Application.Models;
using MemoryModule.Domain;
using MemoryModule.Domain.Events;
using MemoryModule.Domain.Models;
using Shared.Interfaces;
using UUIDNext;

namespace MemoryModule.Application.Tests;

[Collection(EventTypeRegistrationCollection.Name)]
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
            typeof(CodexToolCallRecordedV1)
        );
        MemoryAttachmentEventRegistration.EnsureRegistered();
        EventTypeContainer.AddEventType(
            typeof(CodexMemoryMigratedV1)
        );
        EventTypeContainer.AddEventType(
            typeof(ChatSummaryAddedV2)
        );
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Execute_WithAttachments_SavesContentThenRecordsItOnThePrompt(
        bool sessionExists
    )
    {
        DatabaseFriendlyGuidGenerator.SetDefaultGuidGenerationDatabase(
            Database.SqlServer
        );
        var eventStore = new CapturingEventStoreWithOutbox();
        var storage = new RecordingAttachmentContentStorage();
        if (sessionExists)
            await CreateCommand(eventStore, SecondPromptId).Execute(Executor);
        var attachment = new MemoryAttachment
        {
            Id = new AttachmentId(
                Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd")
            ),
            Name = "image-1.png",
            Size = 3,
            FileType = "image/png",
            Extension = "png",
            Placeholder = "[Image #1]"
        };
        var command = CreateCommand(eventStore, FirstPromptId, storage: storage);
        command.Attachments = [new MemoryAttachmentUpload(attachment, [1, 2, 3])];

        await command.Execute(Executor);

        var saved = Assert.Single(storage.Saved);
        Assert.Equal(
            (attachment.Id.Value, "image-1.png", "image/png"),
            (saved.FileId, saved.Name, saved.ContentType)
        );
        Assert.Equal([1, 2, 3], saved.Content);
        var memory = eventStore.LastWritten.Values.Single(
            stateInfo => stateInfo.StateData is MemoryStateData
        );
        Assert.Collection(
            memory.LastExecutedPayloads,
            payload => Assert.IsType<CodexPromptHookRecordedV1>(payload.EventData),
            payload => Assert.Equal(
                new MemoryAttachmentAddedV1(FirstPromptId, attachment),
                payload.EventData
            )
        );
        Assert.Equal(
            [attachment],
            ((MemoryStateData)memory.StateData).ChatPrompts[FirstPromptId].Attachments
        );
    }

    [Theory]
    [InlineData(null, null, 1)]
    [InlineData(0, 0, 1)]
    [InlineData(0, 1, 2)]
    public async Task Execute_StopFallback_DeduplicatesWithinEachSubmission(
        int? firstIndex, int? nextIndex, int savedCount)
    {
        DatabaseFriendlyGuidGenerator.SetDefaultGuidGenerationDatabase(Database.SqlServer);
        var eventStore = new CapturingEventStoreWithOutbox();
        var storage = new RecordingAttachmentContentStorage();
        var attachment = new MemoryAttachment
        {
            Id = new AttachmentId(Guid.NewGuid()),
            Name = "image-1.png", Size = 3, FileType = "image/png",
            Extension = "png", Placeholder = "[Image #1]", UserMessageIndex = firstIndex
        };
        var submit = CreateCommand(eventStore, FirstPromptId, storage);
        submit.HookEventName = "UserPromptSubmit";
        submit.Attachments = [new(attachment, [1, 2, 3])];
        await submit.Execute(Executor);
        var stop = CreateCommand(eventStore, FirstPromptId, storage);
        stop.HookEventName = "Stop";
        stop.Attachments = [new(attachment with { Id = new AttachmentId(Guid.NewGuid()), UserMessageIndex = nextIndex }, [1, 2, 3])];

        await stop.Execute(Executor);

        Assert.Equal(savedCount, storage.Saved.Count);
        var state = Assert.Single(eventStore.LastWritten).Value;
        Assert.IsType<CodexPromptHookRecordedV1>(state.LastExecutedPayloads[0].EventData);
        Assert.Equal(savedCount, state.LastExecutedPayloads.Count);
        Assert.Equal(savedCount, ((MemoryStateData)state.StateData).ChatPrompts[FirstPromptId].Attachments.Count);
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
    public async Task Execute_ExistingSession_WritesToolCallEventAndState()
    {
        DatabaseFriendlyGuidGenerator.SetDefaultGuidGenerationDatabase(
            Database.SqlServer
        );
        var eventStore = new CapturingEventStoreWithOutbox();
        await CreateCommand(eventStore, FirstPromptId).Execute(Executor);
        var command = new RecordCodexToolCallCommand(
            CreateHandler(eventStore)
        )
        {
            ThreadId = ThreadId,
            PromptId = FirstPromptId,
            Payload = JsonSerializer.SerializeToElement(
                new
                {
                    session_id = ThreadId.Value,
                    turn_id = FirstPromptId.Value,
                    tool_name = "Bash",
                    tool_use_id = "tool-use-1",
                    tool_input = new { command = "dotnet test" },
                    tool_response = new { output = "Passed", exit_code = 0 }
                }
            ),
            ToolName = "Bash",
            ToolUseId = "tool-use-1"
        };

        var result = await command.Execute(Executor);

        Assert.Equal("OK", result.Status);
        var writtenAggregate = Assert.Single(eventStore.LastWritten);
        var memoryState = Assert.IsType<MemoryStateData>(
            writtenAggregate.Value.StateData
        );
        var prompt = memoryState.ChatPrompts[FirstPromptId];
        var toolCall = Assert.Single(prompt.ToolCalls);
        Assert.Equal("Bash", toolCall.ToolName);
        Assert.Equal("tool-use-1", toolCall.ToolUseId);
        Assert.Equal(
            "Bash",
            toolCall.Payload.GetProperty("tool_name").GetString()
        );
        Assert.Equal(
            "tool-use-1",
            toolCall.Payload.GetProperty("tool_use_id").GetString()
        );
        Assert.IsType<CodexToolCallRecordedV1>(
            Assert.Single(
                writtenAggregate.Value.LastExecutedPayloads
            ).EventData
        );
    }

    [Fact]
    public async Task Execute_AddSummaryWritesEventToMappedMemory()
    {
        DatabaseFriendlyGuidGenerator.SetDefaultGuidGenerationDatabase(
            Database.SqlServer
        );
        var eventStore = new CapturingEventStoreWithOutbox();
        await CreateCommand(eventStore, FirstPromptId).Execute(Executor);
        var mapState = Assert.IsType<SessionAggregateMapStateData>(
            eventStore.LastWritten[MemoryAggregateIds.SessionAggregateMap].StateData
        );
        var memoryAggregateId = mapState.AggregateIdsBySession[ThreadId];
        var command = new AddChatSummaryCommand(CreateHandler(eventStore))
        {
            ThreadId = ThreadId,
            Summary = "The user asked to persist a chat summary.",
            RelatedEntities = Enum.GetValues<MemoryEntityType>()
                .Select(type => new MemoryRelatedEntityDto(type, ThreadId.Value))
                .Append(new MemoryRelatedEntityDto(MemoryEntityType.Feature, ThreadId.Value))
                .ToList()
        };

        var result = await command.Execute(Executor);

        Assert.Equal("OK", result.Status);
        var writtenAggregate = Assert.Single(eventStore.LastWritten);
        Assert.Equal(memoryAggregateId, writtenAggregate.Key);
        var memoryState = Assert.IsType<MemoryStateData>(
            writtenAggregate.Value.StateData
        );
        Assert.Equal(command.Summary, memoryState.ChatSummary.Summary);
        Assert.NotEqual(
            default,
            memoryState.ChatSummary.SummaryTimestamp
        );
        var summaryAdded = Assert.IsType<ChatSummaryAddedV2>(
            Assert.Single(
                writtenAggregate.Value.LastExecutedPayloads
            ).EventData
        );
        Assert.Equal(command.Summary, summaryAdded.Summary);
        command.RelatedEntities.Clear();
        Assert.Equal(Enum.GetValues<MemoryEntityType>().Length, summaryAdded.RelatedEntities.Count);
        Assert.True(memoryState.RelatedEntities.SetEquals(summaryAdded.RelatedEntities));
        Assert.All(memoryState.RelatedEntities, entity => Assert.Equal(ThreadId.Value, entity.Id.Value));
    }

    [Theory]
    [InlineData(MemoryEntityType.FeatureResearchDiscovery, true, true)]
    [InlineData(MemoryEntityType.FeatureRecord, true, true)]
    [InlineData(MemoryEntityType.FeatureRecord, false, false)]
    [InlineData((MemoryEntityType)999, true, false)]
    public async Task AddSummary_RequiresValidEntityTypeAndId(
        MemoryEntityType type,
        bool hasId,
        bool expected
    )
    {
        var command = new AddChatSummaryCommand(
            CreateHandler(new CapturingEventStoreWithOutbox())
        )
        {
            ThreadId = ThreadId,
            Summary = "Summary",
            RelatedEntities = [new(type, hasId ? ThreadId.Value : Guid.Empty)]
        };

        Assert.Equal(expected, await command.CanExecute(Executor));
    }

    [Fact]
    public async Task Execute_AddSummaryForUnknownThreadFailsClearly()
    {
        var command = new AddChatSummaryCommand(
            CreateHandler(new CapturingEventStoreWithOutbox())
        )
        {
            ThreadId = ThreadId,
            Summary = "Summary"
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => command.Execute(Executor)
        );

        Assert.Equal(
            $"No memory exists for thread '{ThreadId.Value}'.",
            exception.Message
        );
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
        PromptId promptId,
        IAttachmentContentStorage? storage = null
    )
    {
        return new RecordCodexPromptHookCommand(CreateHandler(eventStore), storage ?? new RecordingAttachmentContentStorage())
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
                new TestStateMachineDefinitionProvider()
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

    private sealed class RecordingAttachmentContentStorage
        : IAttachmentContentStorage
    {
        public List<AttachmentContentFile> Saved { get; } = [];

        public Task Save(AttachmentContentFile file)
        {
            Saved.Add(file);
            return Task.CompletedTask;
        }

        public Task<AttachmentContentFile?> Get(Guid fileId) =>
            throw new NotSupportedException();

        public Task Delete(Guid fileId) =>
            throw new NotSupportedException();
    }

    private sealed class MemoryStateDataProvider : IStateDataProvider
    {
        public object GetStateDataByStateMachine(
            string stateMachineId,
            AggregateId aggregateId
        ) =>
            (
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
        public List<IPreEventValidator> GetPreEventStateValidators(
            EventPayload payload
        ) =>
            new List<IPreEventValidator>();

        public List<IPostEventValidator> GetPostEventStateValidators(
            EventPayload payload
        ) =>
            new List<IPostEventValidator>();
    }
}

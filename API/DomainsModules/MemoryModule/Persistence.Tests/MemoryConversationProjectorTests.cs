using System.Text.Json;
using EventSourcing.Shared.Models;
using MemoryModule.Domain;
using MemoryModule.Domain.Models;
using MemoryModule.Persistence.Interfaces;

namespace MemoryModule.Persistence.Tests;

public sealed class MemoryConversationProjectorTests
{
    private static readonly AggregateId MemoryId =
        AggregateId.FromDatabaseGuid(
            Guid.Parse("11111111-1111-1111-1111-111111111111")
        );
    private static readonly PromptId FirstPromptId =
        new(Guid.Parse("cccccccc-cccc-cccc-cccc-000000000001"));
    private static readonly PromptId SecondPromptId =
        new(Guid.Parse("cccccccc-cccc-cccc-cccc-000000000002"));

    [Fact]
    public async Task Update_writes_each_hook_as_an_ordered_conversation_turn()
    {
        var repository = new FakeMemoryConversationRepository();
        var projector = new MemoryConversationProjector(repository);

        await projector.Update([CreateStateInfo(CreateMemory())]);

        Assert.Equal([MemoryId], repository.AggregateIds);
        Assert.Equal(
            [
                (PromptHookMessageRoles.User, "Refactor the outbox"),
                (PromptHookMessageRoles.Assistant, "Refactored it."),
                (PromptHookMessageRoles.Hook, "")
            ],
            repository.Messages
                .Select(message => (message.Role, message.Message))
                .ToList()
        );
        Assert.Equal(
            [FirstPromptId, FirstPromptId, SecondPromptId],
            repository.Messages.Select(message => message.PromptId).ToList()
        );
        Assert.Equal(
            [0, 1, 0],
            repository.Messages.Select(message => message.HookIndex).ToList()
        );
        Assert.Equal(
            "UserPromptSubmit",
            repository.Messages[0].HookEventName
        );
        Assert.Equal(
            DateTime.UnixEpoch,
            repository.Messages[0].Timestamp
        );
    }

    [Fact]
    public async Task Update_keeps_the_whole_payload_for_expanding_a_turn()
    {
        var repository = new FakeMemoryConversationRepository();
        var projector = new MemoryConversationProjector(repository);

        await projector.Update([CreateStateInfo(CreateMemory())]);

        var prompt = repository.Messages[0];
        using var payload = JsonDocument.Parse(prompt.PayloadJson);
        Assert.Equal(
            "Refactor the outbox",
            payload.RootElement.GetProperty("prompt").GetString()
        );
        Assert.Equal(
            "019f",
            payload.RootElement.GetProperty("session_id").GetString()
        );
        Assert.Contains(
            "session_id",
            repository.Messages[2].PayloadJson
        );
    }

    [Fact]
    public async Task Update_removes_the_conversation_of_a_deleted_memory()
    {
        var repository = new FakeMemoryConversationRepository();
        var projector = new MemoryConversationProjector(repository);
        var memory = CreateMemory();
        memory.IsDeleted = true;

        await projector.Update([CreateStateInfo(memory)]);

        Assert.Equal([MemoryId], repository.AggregateIds);
        Assert.Empty(repository.Messages);
    }

    private static MemoryStateData CreateMemory()
    {
        var memory = new MemoryStateData(MemoryId)
        {
            ThreadId = new ThreadId(
                Guid.Parse("33333333-3333-3333-3333-333333333333")
            )
        };
        memory.ChatPrompts.Add(
            FirstPromptId,
            new ChatPrompt
            {
                PromptId = FirstPromptId,
                PromptStartTimestamp = DateTime.UnixEpoch,
                PromptHookRecords =
                [
                    CreateHook(
                        "UserPromptSubmit",
                        """{"session_id":"019f","prompt":"Refactor the outbox"}"""
                    ),
                    CreateHook(
                        "Stop",
                        """{"session_id":"019f","last_assistant_message":"Refactored it."}"""
                    )
                ]
            }
        );
        memory.ChatPrompts.Add(
            SecondPromptId,
            new ChatPrompt
            {
                PromptId = SecondPromptId,
                PromptStartTimestamp = DateTime.UnixEpoch.AddMinutes(2),
                PromptHookRecords =
                [
                    CreateHook(
                        "SessionStart",
                        """{"session_id":"019f","source":"compact"}"""
                    )
                ]
            }
        );

        return memory;
    }

    private static PromptHookRecord CreateHook(
        string hookEventName,
        string payloadJson
    ) =>
        new()
        {
            HookEventName = hookEventName,
            Payload = JsonDocument.Parse(payloadJson).RootElement.Clone()
        };

    private static StateInfo CreateStateInfo(MemoryStateData memory) =>
        StateInfo.Create(memory, "memory-state-machine", memory.Id);

    private sealed class FakeMemoryConversationRepository
        : IMemoryConversationRepository
    {
        public IReadOnlyList<AggregateId> AggregateIds { get; private set; } = [];
        public IReadOnlyList<MemoryConversationMessage> Messages
        {
            get;
            private set;
        } = [];

        public Task<List<MemoryConversationMessage>> Get(
            AggregateId memoryAggregateId,
            CancellationToken cancellationToken = default
        ) => throw new NotSupportedException();

        public Task Write(
            IReadOnlyCollection<AggregateId> memoryAggregateIds,
            IReadOnlyCollection<MemoryConversationMessage> messages,
            CancellationToken cancellationToken = default
        )
        {
            AggregateIds = memoryAggregateIds.ToList();
            Messages = messages.ToList();
            return Task.CompletedTask;
        }
    }
}

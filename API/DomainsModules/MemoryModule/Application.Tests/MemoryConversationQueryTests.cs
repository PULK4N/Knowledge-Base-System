using ActionModule.Shared.Models;
using EventSourcing.Shared.Models;
using MemoryModule.Application.Queries;
using MemoryModule.Domain.Models;
using MemoryModule.Persistence.Interfaces;

namespace MemoryModule.Application.Tests;

public sealed class MemoryConversationQueryTests
{
    private static readonly Executor Executor =
        new()
        {
            Id = EventExecutor.FromDatabaseGuid(
                Guid.Parse("11111111-1111-1111-1111-111111111111")
            )
        };
    private static readonly Guid MemoryId =
        Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly ThreadId ConversationThreadId =
        new(Guid.Parse("33333333-3333-3333-3333-333333333333"));

    [Fact]
    public async Task Execute_returns_the_conversation_with_its_summary()
    {
        var query = CreateQuery(
            [CreateMessage(PromptHookMessageRoles.User, "Refactor the outbox")],
            CreateSummary()
        );

        var result = await query.Execute(Executor);

        Assert.NotNull(result);
        Assert.Equal(MemoryId, result.MemoryId);
        Assert.Equal(ConversationThreadId.Value, result.ThreadId);
        Assert.Equal("Session summary", result.Summary);
        Assert.Equal(DateTime.UnixEpoch, result.FirstPromptTimestamp);
        var message = Assert.Single(result.Messages);
        Assert.Equal(PromptHookMessageRoles.User, message.Role);
        Assert.Equal("Refactor the outbox", message.Message);
        Assert.Equal("UserPromptSubmit", message.HookEventName);
        Assert.Equal("""{"prompt":"Refactor the outbox"}""", message.PayloadJson);
    }

    [Fact]
    public async Task Execute_returns_a_conversation_that_has_no_summary_yet()
    {
        var query = CreateQuery(
            [CreateMessage(PromptHookMessageRoles.User, "Refactor the outbox")],
            null
        );

        var result = await query.Execute(Executor);

        Assert.NotNull(result);
        Assert.Equal(ConversationThreadId.Value, result.ThreadId);
        Assert.Equal(string.Empty, result.Summary);
        Assert.Single(result.Messages);
    }

    [Fact]
    public async Task Execute_returns_null_for_an_unknown_memory()
    {
        var query = CreateQuery([], null);

        Assert.Null(await query.Execute(Executor));
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000", false)]
    [InlineData("22222222-2222-2222-2222-222222222222", true)]
    public async Task CanExecute_requires_a_memory_id(
        string memoryId,
        bool canExecute
    )
    {
        var query = CreateQuery([], null);
        query.MemoryId = Guid.Parse(memoryId);

        Assert.Equal(canExecute, await query.CanExecute(Executor));
    }

    private static GetMemoryConversationQuery CreateQuery(
        List<MemoryConversationMessage> messages,
        MemorySummary? summary
    ) =>
        new(
            new FakeMemoryConversationRepository(messages),
            new FakeMemorySummaryRepository(summary)
        )
        {
            MemoryId = MemoryId
        };

    private static MemoryConversationMessage CreateMessage(
        string role,
        string message
    ) =>
        new(
            AggregateId.FromDatabaseGuid(MemoryId),
            ConversationThreadId,
            new PromptId(Guid.Parse("cccccccc-cccc-cccc-cccc-000000000001")),
            0,
            DateTime.UnixEpoch,
            "UserPromptSubmit",
            role,
            message,
            """{"prompt":"Refactor the outbox"}"""
        );

    private static MemorySummary CreateSummary() =>
        new(
            AggregateId.FromDatabaseGuid(MemoryId),
            ConversationThreadId,
            "Session summary",
            1,
            DateTime.UnixEpoch,
            DateTime.UnixEpoch.AddMinutes(2),
            DateTime.UnixEpoch.AddMinutes(3),
            DateTime.UnixEpoch.AddMinutes(3)
        );

    private sealed class FakeMemoryConversationRepository(
        List<MemoryConversationMessage> messages
    ) : IMemoryConversationRepository
    {
        public Task<List<MemoryConversationMessage>> Get(
            AggregateId memoryAggregateId,
            CancellationToken cancellationToken = default
        ) => Task.FromResult(messages);

        public Task Write(
            IReadOnlyCollection<AggregateId> memoryAggregateIds,
            IReadOnlyCollection<MemoryConversationMessage> messages,
            CancellationToken cancellationToken = default
        ) => throw new NotSupportedException();
    }

    private sealed class FakeMemorySummaryRepository(
        MemorySummary? summary
    ) : IMemorySummaryRepository
    {
        public Task<MemorySummary?> Get(
            AggregateId memoryAggregateId,
            CancellationToken cancellationToken = default
        ) => Task.FromResult(summary);

        public Task<MemorySummarySearchResult> Search(
            int page,
            int pageSize,
            string? search,
            CancellationToken cancellationToken = default
        ) => throw new NotSupportedException();

        public Task Write(
            IReadOnlyCollection<AggregateId> memoryAggregateIds,
            IReadOnlyCollection<MemorySummary> summaries,
            CancellationToken cancellationToken = default
        ) => throw new NotSupportedException();
    }
}

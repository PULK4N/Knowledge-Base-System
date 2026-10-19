using ActionModule.Shared.Models;
using EventSourcing.Shared.Models;
using MemoryModule.Application.Queries;
using MemoryModule.Application.DTOs;
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
        Assert.Equal("Session title", result.SessionTitle);
        Assert.Equal("Session summary", result.Summary);
        Assert.Equal(DateTime.UnixEpoch, result.FirstPromptTimestamp);
        var message = Assert.Single(result.Messages);
        Assert.Equal(PromptHookMessageRoles.User, message.Role);
        Assert.Equal("Refactor the outbox", message.Message);
        Assert.Equal("UserPromptSubmit", message.HookEventName);
        Assert.Equal("""{"prompt":"Refactor the outbox"}""", message.PayloadJson);
    }

    [Fact]
    public async Task Execute_returns_the_tool_calls_of_the_conversation()
    {
        var query = CreateQuery(
            [CreateMessage(PromptHookMessageRoles.User, "Refactor the outbox")],
            CreateSummary(),
            [CreateToolCall(0, "Read"), CreateToolCall(1, "Edit")]
        );

        var result = await query.Execute(Executor);

        Assert.NotNull(result);
        Assert.Equal(
            ["Read", "Edit"],
            result.ToolCalls.Select(toolCall => toolCall.ToolName).ToList()
        );
        var toolCall = result.ToolCalls[0];
        Assert.Equal(
            Guid.Parse("cccccccc-cccc-cccc-cccc-000000000001"),
            toolCall.PromptId
        );
        Assert.Equal(0, toolCall.ToolCallIndex);
        Assert.Equal("toolu_00", toolCall.ToolUseId);
        Assert.Equal("Run Read", toolCall.Description);
        Assert.Contains("Program.cs", toolCall.PayloadJson);
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
        Assert.Equal(string.Empty, result.SessionTitle);
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

    [Theory]
    [InlineData(0, 10, 3, false)]
    [InlineData(0, 2, 2, true)]
    [InlineData(2, 2, 1, false)]
    public async Task Text_query_returns_a_page_of_chat_text(
        int offset,
        int limit,
        int expectedCount,
        bool hasMore
    )
    {
        var query = new GetMemoryConversationTextQuery(
            new FakeMemoryConversationRepository(
                [
                    CreateMessage(PromptHookMessageRoles.User, "First") with { InputTokens = 10, OutputTokens = 5 },
                    CreateMessage(PromptHookMessageRoles.Hook, string.Empty),
                    CreateMessage(PromptHookMessageRoles.Assistant, "Second"),
                    CreateMessage(PromptHookMessageRoles.User, "Third")
                ]
            ),
            new FakeMemorySummaryRepository(CreateSummary() with { InputTokens = 100, OutputTokens = 50 })
        )
        {
            MemoryId = MemoryId,
            Offset = offset,
            Limit = limit
        };

        var result = await query.Execute(Executor);

        Assert.Equal(MemoryId, result.MemoryId);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(150L, result.TotalTokens);
        if (offset == 0)
            Assert.Equal(15L, result.Messages[0].TotalTokens);
        Assert.Equal(hasMore, result.HasMore);
        Assert.Equal(
            new List<string> { "First", "Second", "Third" }
                .Skip(offset)
                .Take(expectedCount),
            result.Messages.Select(message => message.Text)
        );
    }

    [Theory]
    [InlineData("22222222-2222-2222-2222-222222222222", 0, 10, true)]
    [InlineData("22222222-2222-2222-2222-222222222222", 0, 100, true)]
    [InlineData("00000000-0000-0000-0000-000000000000", 0, 10, false)]
    [InlineData("22222222-2222-2222-2222-222222222222", -1, 10, false)]
    [InlineData("22222222-2222-2222-2222-222222222222", 0, 0, false)]
    [InlineData("22222222-2222-2222-2222-222222222222", 0, 101, false)]
    public async Task Text_query_validates_memory_id_offset_and_limit(
        string memoryId,
        int offset,
        int limit,
        bool canExecute
    )
    {
        var query = new GetMemoryConversationTextQuery(
            new FakeMemoryConversationRepository([]),
            new FakeMemorySummaryRepository(null)
        )
        {
            MemoryId = Guid.Parse(memoryId),
            Offset = offset,
            Limit = limit
        };

        Assert.Equal(canExecute, await query.CanExecute(Executor));
    }

    [Fact]
    public async Task Conversation_maps_prompt_counts_and_complete_summary_totals()
    {
        var message = CreateMessage(PromptHookMessageRoles.User, "Prompt") with
        {
            InputTokens = 10, CachedInputTokens = 2, CacheWriteInputTokens = 0,
            OutputTokens = 5, ReasoningOutputTokens = 1
        };
        var summary = CreateSummary() with
        {
            InputTokens = 30, CachedInputTokens = 6, CacheWriteInputTokens = 0,
            OutputTokens = 15, ReasoningOutputTokens = 3
        };
        var result = await CreateQuery([message, message with { HookIndex = 1 }], summary).Execute(Executor);
        Assert.NotNull(result);
        Assert.Equal(45L, result!.TotalTokens);
        Assert.Equal(6L, result.CachedInputTokens);
        Assert.Equal(3L, result.ReasoningOutputTokens);
        Assert.All(result.Messages, item => Assert.Equal(15L, item.TotalTokens));
        var dto = MemorySummaryDto.FromReadModel(summary);
        Assert.Equal(45L, dto.TotalTokens);
        var partial = await CreateQuery([message], summary with { InputTokens = null }).Execute(Executor);
        Assert.Null(partial!.TotalTokens);
    }

    private static GetMemoryConversationQuery CreateQuery(
        List<MemoryConversationMessage> messages,
        MemorySummary? summary,
        List<MemoryToolCall>? toolCalls = null
    ) =>
        new(
            new FakeMemoryConversationRepository(messages),
            new FakeMemoryToolCallRepository(toolCalls ?? []),
            new FakeMemorySummaryRepository(summary)
        )
        {
            MemoryId = MemoryId
        };

    private static MemoryToolCall CreateToolCall(
        int toolCallIndex,
        string toolName
    ) =>
        new(
            AggregateId.FromDatabaseGuid(MemoryId),
            ConversationThreadId,
            new PromptId(Guid.Parse("cccccccc-cccc-cccc-cccc-000000000001")),
            toolCallIndex,
            DateTime.UnixEpoch,
            toolName,
            $"toolu_0{toolCallIndex}",
            $"Run {toolName}",
            """{"tool_input":{"file_path":"Program.cs"}}"""
        );

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
            "Session title",
            "Session summary",
            1,
            DateTime.UnixEpoch,
            DateTime.UnixEpoch.AddMinutes(2),
            DateTime.UnixEpoch.AddMinutes(3),
            DateTime.UnixEpoch.AddMinutes(3),
            []
        );

    private sealed class FakeMemoryConversationRepository(
        List<MemoryConversationMessage> messages
    ) : IMemoryConversationRepository
    {
        public Task<List<MemoryConversationMessage>> Get(
            AggregateId memoryAggregateId,
            CancellationToken cancellationToken = default
        ) => Task.FromResult(messages);

        public Task<MemoryConversationTextPage> GetText(
            AggregateId memoryAggregateId,
            int offset,
            int limit,
            CancellationToken cancellationToken = default
        )
        {
            var withText = messages
                .Where(message => message.Message.Length > 0)
                .ToList();

            return Task.FromResult(
                new MemoryConversationTextPage(
                    withText
                        .Skip(offset)
                        .Take(limit)
                        .Select(
                            message => new MemoryConversationText(
                                message.PromptId,
                                message.HookIndex,
                                message.Timestamp,
                                message.Role,
                                message.Message,
                                message.InputTokens,
                                message.CachedInputTokens,
                                message.CacheWriteInputTokens,
                                message.OutputTokens,
                                message.ReasoningOutputTokens
                            )
                        )
                        .ToList(),
                    withText.Count
                )
            );
        }

        public Task Write(
            IReadOnlyCollection<AggregateId> memoryAggregateIds,
            IReadOnlyCollection<MemoryConversationMessage> messages,
            CancellationToken cancellationToken = default
        ) => throw new NotSupportedException();
    }

    private sealed class FakeMemoryToolCallRepository(
        List<MemoryToolCall> toolCalls
    ) : IMemoryToolCallRepository
    {
        public Task<List<MemoryToolCall>> Get(
            AggregateId memoryAggregateId,
            CancellationToken cancellationToken = default
        ) => Task.FromResult(toolCalls);

        public Task<PagedResult<MemoryToolCall>> Search(
            EntityQuery<MemoryToolCallFilters, MemoryToolCallSortField> request,
            CancellationToken cancellationToken = default
        ) => throw new NotSupportedException();

        public Task Write(
            IReadOnlyCollection<AggregateId> memoryAggregateIds,
            IReadOnlyCollection<MemoryToolCall> toolCalls,
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

        public Task<List<MemorySummary>> GetMany(
            IReadOnlyCollection<AggregateId> memoryAggregateIds,
            CancellationToken cancellationToken = default
        ) => throw new NotSupportedException();

        public Task<List<string>> GetModels(
            CancellationToken cancellationToken = default
        ) => throw new NotSupportedException();

        public Task<MemorySummarySearchResult> Search(
            EntityQuery<MemorySummaryFilters, MemorySummarySortField> request,
            CancellationToken cancellationToken = default
        ) => throw new NotSupportedException();

        public Task Write(
            IReadOnlyCollection<AggregateId> memoryAggregateIds,
            IReadOnlyCollection<MemorySummary> summaries,
            CancellationToken cancellationToken = default
        ) => throw new NotSupportedException();
    }
}

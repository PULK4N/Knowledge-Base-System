using System.Text.Json;
using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Core.Interfaces;
using EventSourcing.Core.Providers;
using EventSourcing.Persistence.Interfaces;
using EventSourcing.Shared.Models;
using MemoryModule.Application.DTOs;
using MemoryModule.Application.Queries;
using MemoryModule.Domain;
using MemoryModule.Domain.Events;
using MemoryModule.Domain.Models;
using MemoryModule.Persistence.Interfaces;
using Shared.Interfaces;

namespace MemoryModule.Application.Tests;

public sealed class MemoryQueryTests
{
    private static readonly Executor Executor =
        new()
        {
            Id = EventExecutor.FromDatabaseGuid(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
            )
        };

    [Fact]
    public async Task Search_returns_memory_summaries_and_pagination_metadata()
    {
        var memoryId = AggregateId.FromDatabaseGuid(
            Guid.Parse("11111111-1111-1111-1111-111111111111")
        );
        var threadId = new ThreadId(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
        );
        var repository = new FakeMemorySummaryRepository(
            new MemorySummarySearchResult(
                [
                    new MemorySummary(
                        memoryId,
                        threadId,
                        "Session summary",
                        3,
                        DateTime.UnixEpoch,
                        DateTime.UnixEpoch.AddMinutes(2),
                        DateTime.UnixEpoch.AddMinutes(3),
                        DateTime.UnixEpoch.AddMinutes(3)
                    )
                ],
                6
            )
        );
        var query = new SearchMemoriesQuery(repository)
        {
            Page = 2,
            PageSize = 5,
            Search = "session"
        };

        var result = await query.Execute(Executor);

        Assert.Equal(2, result.Page);
        Assert.Equal(5, result.PageSize);
        Assert.Equal(6, result.TotalCount);
        Assert.Equal(memoryId.Value, Assert.Single(result.Items).MemoryId);
        Assert.Equal((2, 5, "session"), repository.LastSearchRequest);
    }

    [Fact]
    public async Task Search_returns_two_distinct_sessions_from_one_search()
    {
        var first = CreateMemory(
            "11111111-1111-1111-1111-111111111111",
            "aaaaaaaa-1111-1111-1111-111111111111",
            "User chose PostgreSQL vector memory search with bounded context retrieval",
            1
        );
        var duplicateFork = CreateMemory(
            "22222222-2222-2222-2222-222222222222",
            "aaaaaaaa-2222-2222-2222-222222222222",
            "User chose PostgreSQL vector memory search with bounded context retrieval today",
            1
        );
        var second = CreateMemory(
            "33333333-3333-3333-3333-333333333333",
            "aaaaaaaa-3333-3333-3333-333333333333",
            "Different session summary",
            1
        );
        var search = new FakeMemorySearch(
        [
            CreateSearchResult(first, matchedSummary: true, "summary"),
            CreateSearchResult(duplicateFork, matchedSummary: true, "duplicate"),
            CreateSearchResult(second, matchedSummary: false, "matched prompt text")
        ]);
        var query = new SearchMemoryQuery(
            CreateStateCalculator(),
            new FakeEventStore(first, duplicateFork, second),
            search
        )
        {
            SearchText = "event sourcing decision"
        };

        var result = await query.Execute(Executor);

        Assert.Equal(1, search.CallCount);
        Assert.Equal(20, search.LastOptions!.ResultCount);
        Assert.Equal(2, result.Results.Count);
        Assert.Equal(first.AggregateId.Value, result.Results[0].MemoryId);
        Assert.True(result.Results[0].MatchedSummary);
        Assert.Null(result.Results[0].PromptId);
        Assert.Null(result.Results[0].MatchedText);
        Assert.Equal(second.AggregateId.Value, result.Results[1].MemoryId);
        Assert.False(result.Results[1].MatchedSummary);
        Assert.Equal(second.PromptIds[0].Value, result.Results[1].PromptId);
        Assert.Equal("Different session summary", result.Results[1].Summary);
        Assert.Equal("matched prompt text", result.Results[1].MatchedText);
    }

    [Fact]
    public async Task Search_truncates_summary_and_text_to_the_requested_budget()
    {
        var memory = CreateMemory(
            "11111111-1111-1111-1111-111111111111",
            "aaaaaaaa-1111-1111-1111-111111111111",
            new string('s', 1000),
            1
        );
        var search = new FakeMemorySearch(
        [
            CreateSearchResult(
                memory,
                matchedSummary: false,
                new string('m', 1000)
            )
        ]);
        var query = new SearchMemoryQuery(
            CreateStateCalculator(),
            new FakeEventStore(memory),
            search
        )
        {
            SearchText = "long memory",
            MaxTokens = SearchMemoryQuery.MinimumMaximumTokens
        };

        var result = await query.Execute(Executor);

        Assert.True(result.IsTruncated);
        Assert.InRange(
            result.ApproximateTokenCount,
            1,
            query.MaxTokens
        );
        Assert.NotEmpty(Assert.Single(result.Results).Summary);
        Assert.Null(Assert.Single(result.Results).MatchedText);
    }

    [Fact]
    public async Task PromptWindow_returns_requested_neighbours_around_anchor()
    {
        var memory = CreateMemory(
            "11111111-1111-1111-1111-111111111111",
            "aaaaaaaa-1111-1111-1111-111111111111",
            "Summary",
            5
        );
        var query = new GetMemoryPromptWindowQuery(
            CreateStateCalculator(),
            new FakeEventStore(memory)
        )
        {
            MemoryId = memory.AggregateId.Value,
            PromptId = memory.PromptIds[2].Value,
            Before = 1,
            After = 1
        };

        var result = await query.Execute(Executor);

        var window = Assert.IsType<MemoryPromptWindowResult>(result);
        Assert.Equal(memory.PromptIds[2].Value, window.AnchorPromptId);
        Assert.Equal(
            memory.PromptIds.Skip(1).Take(3).Select(id => id.Value),
            window.Prompts.Select(prompt => prompt.PromptId)
        );
        Assert.True(window.HasMoreBefore);
        Assert.True(window.HasMoreAfter);
        Assert.All(
            window.Prompts,
            prompt => Assert.Contains("prompt-", prompt.Text)
        );
    }

    private static MemoryFixture CreateMemory(
        string aggregateId,
        string threadId,
        string summary,
        int promptCount
    )
    {
        var id = AggregateId.FromDatabaseGuid(Guid.Parse(aggregateId));
        var thread = new ThreadId(Guid.Parse(threadId));
        var promptIds = Enumerable.Range(1, promptCount)
            .Select(
                index => new PromptId(
                    Guid.Parse($"bbbbbbbb-bbbb-bbbb-bbbb-{index:D12}")
                )
            )
            .ToList();
        var events = promptIds
            .Select(
                (promptId, index) => CreatePayload(
                    id,
                    index + 1,
                    new CodexPromptHookRecordedV1(
                        thread,
                        promptId,
                        "after_agent",
                        JsonSerializer.SerializeToElement(
                            new { message = $"prompt-{index + 1}" }
                        )
                    )
                )
            )
            .ToList();
        events.Add(
            CreatePayload(
                id,
                promptCount + 1,
                new ChatSummaryAddedV1(summary)
            )
        );

        return new MemoryFixture(id, thread, promptIds, events);
    }

    private static EventPayload CreatePayload(
        AggregateId aggregateId,
        int orderNumber,
        EventSourcing.Shared.Interfaces.IEvent eventData
    ) =>
        new()
        {
            EventExecutionInfo = new EventExecutionInfo
            {
                AggregateId = aggregateId,
                EventExecutor = Executor.Id,
                EventName = eventData.GetType().Name,
                StateMachineId = Constants.StateMachineIds.Memory,
                OrderNumber = (uint)orderNumber,
                Timestamp = DateTime.UnixEpoch.AddMinutes(orderNumber)
            },
            EventData = eventData
        };

    private static MemorySearchResult CreateSearchResult(
        MemoryFixture memory,
        bool matchedSummary,
        string text
    ) =>
        new(
            new MemorySearchCandidate(
                memory.AggregateId,
                memory.ThreadId,
                matchedSummary ? default : memory.PromptIds[0],
                0,
                0,
                DateTime.UnixEpoch,
                matchedSummary
                    ? MemorySearchDocumentSources.ChatSummary
                    : "after_agent",
                text
            ),
            1,
            1,
            1
        );

    private static StateCalculator CreateStateCalculator() =>
        new(
            new OrderNumberHelper(),
            new MemoryStateDataProvider(),
            new EmptyEventValidatorProvider(),
            new EmptyUniqueEventConstraintProvider(),
            new TestStateMachineDefinitionProvider()
        );

    private sealed record MemoryFixture(
        AggregateId AggregateId,
        ThreadId ThreadId,
        List<PromptId> PromptIds,
        List<EventPayload> Events
    );

    private sealed class FakeMemorySearch(
        IReadOnlyList<MemorySearchResult> results
    ) : IMemorySearch
    {
        public int CallCount { get; private set; }
        public HybridMemorySearchOptions? LastOptions { get; private set; }

        public Task<IReadOnlyList<MemorySearchResult>> Search(
            string query,
            HybridMemorySearchOptions? options = null,
            CancellationToken cancellationToken = default
        )
        {
            CallCount++;
            LastOptions = options;
            return Task.FromResult(results);
        }
    }

    private sealed class FakeMemorySummaryRepository(
        MemorySummarySearchResult result
    ) : IMemorySummaryRepository
    {
        public (int Page, int PageSize, string? Search)? LastSearchRequest { get; private set; }

        public Task<MemorySummarySearchResult> Search(
            int page,
            int pageSize,
            string? search,
            CancellationToken cancellationToken = default
        )
        {
            LastSearchRequest = (page, pageSize, search);
            return Task.FromResult(result);
        }

        public Task Write(
            IReadOnlyCollection<AggregateId> memoryAggregateIds,
            IReadOnlyCollection<MemorySummary> summaries,
            CancellationToken cancellationToken = default
        ) => throw new NotSupportedException();
    }

    private sealed class FakeEventStore(
        params MemoryFixture[] memories
    ) : IEventStore
    {
        private readonly Dictionary<AggregateId, List<EventPayload>> _events =
            memories.ToDictionary(
                memory => memory.AggregateId,
                memory => memory.Events
            );

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

        public Task Write(List<EventPayload> payloads) =>
            throw new NotSupportedException();
    }

    private sealed class MemoryStateDataProvider : IStateDataProvider
    {
        public Task<object> GetStateDataByStateMachine(
            string stateMachineId,
            AggregateId aggregateId
        ) => Task.FromResult<object>(new MemoryStateData(aggregateId));
    }

    private sealed class EmptyEventValidatorProvider
        : IEventValidatorProvider
    {
        public Task<List<IPreEventValidator>> GetPreEventStateValidators(
            EventPayload payload
        ) => Task.FromResult(new List<IPreEventValidator>());

        public Task<List<IPostEventValidator>> GetPostEventStateValidators(
            EventPayload payload
        ) => Task.FromResult(new List<IPostEventValidator>());
    }

    private sealed class EmptyUniqueEventConstraintProvider
        : IUniqueEventConstraintProvider
    {
        public IEnumerable<UniqueEventConstraintData> GetConstraintsToAdd(
            object stateData,
            EventPayload payload
        ) => [];

        public IEnumerable<UniqueEventConstraintData> GetConstraintsToRemove(
            object stateData,
            EventPayload payload
        ) => [];
    }
}

using ActionModule.Shared.Models;
using EmbeddingModule;
using EventSourcing.Shared.Models;
using MemoryModule.Application.Queries;
using MemoryModule.Domain.Models;
using MemoryModule.Persistence.Interfaces;

namespace MemoryModule.Application.Tests;

public sealed class SearchMemoryToolCallsQueryTests
{
    private static readonly Executor Executor =
        new()
        {
            Id = EventExecutor.FromDatabaseGuid(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
            )
        };

    private static readonly AggregateId MemoryId =
        AggregateId.FromDatabaseGuid(
            Guid.Parse("11111111-1111-1111-1111-111111111111")
        );

    private static readonly ThreadId ThreadIdentifier =
        new(Guid.Parse("22222222-2222-2222-2222-222222222222"));

    private static readonly PromptId PromptIdentifier =
        new(Guid.Parse("33333333-3333-3333-3333-333333333333"));

    [Fact]
    public async Task Search_passes_the_requested_criteria_to_the_repository()
    {
        var repository = CreateRepository();
        var query = new SearchMemoryToolCallsQuery(repository)
        {
            Page = 2,
            PageSize = 5,
            Search = "bash",
            ToolName = " Bash ",
            MemoryId = MemoryId.Value,
            ThreadId = ThreadIdentifier.Value,
            PromptId = PromptIdentifier.Value,
            InputKeywords = [" git ", "status"],
            OutputKeywords = ["fatal"],
            SortBy = MemoryToolCallSortField.ToolName,
            SortDirection = SortDirection.Ascending
        };

        await query.Execute(Executor);

        var request = Assert.IsType<
            EntityQuery<MemoryToolCallFilters, MemoryToolCallSortField>
        >(repository.LastSearchRequest);
        Assert.Equal(2, request.Page.Number);
        Assert.Equal(5, request.Page.Size);
        Assert.Equal("bash", request.Search);
        Assert.Equal("Bash", request.Filters.ToolName);
        Assert.Equal(MemoryId, request.Filters.MemoryAggregateId);
        Assert.Equal(ThreadIdentifier, request.Filters.ThreadId);
        Assert.Equal(PromptIdentifier, request.Filters.PromptId);
        Assert.Equal(["git", "status"], request.Filters.InputKeywords);
        Assert.Equal(["fatal"], request.Filters.OutputKeywords);
        Assert.Equal(MemoryToolCallSortField.ToolName, request.Sort.Field);
        Assert.Equal(SortDirection.Ascending, request.Sort.Direction);
    }

    [Fact]
    public async Task Search_maps_tool_calls_and_pagination_metadata()
    {
        var query = new SearchMemoryToolCallsQuery(CreateRepository());

        var result = await query.Execute(Executor);

        Assert.Equal(1, result.Page);
        Assert.Equal(6, result.TotalCount);
        var toolCall = Assert.Single(result.Items);
        Assert.Equal(MemoryId.Value, toolCall.MemoryId);
        Assert.Equal(ThreadIdentifier.Value, toolCall.ThreadId);
        Assert.Equal(PromptIdentifier.Value, toolCall.PromptId);
        Assert.Equal(1, toolCall.ToolCallIndex);
        Assert.Equal("Bash", toolCall.ToolName);
        Assert.Equal("tool-use-1", toolCall.ToolUseId);
        Assert.Equal("List files", toolCall.Description);
        Assert.Equal("{}", toolCall.PayloadJson);
    }

    [Theory]
    [InlineData(0, 25, MemoryToolCallSortField.Timestamp, SortDirection.Descending)]
    [InlineData(1, 0, MemoryToolCallSortField.Timestamp, SortDirection.Descending)]
    [InlineData(1, 25, (MemoryToolCallSortField)99, SortDirection.Descending)]
    [InlineData(1, 25, MemoryToolCallSortField.Timestamp, (SortDirection)99)]
    public async Task Search_rejects_invalid_criteria(
        int page,
        int pageSize,
        MemoryToolCallSortField sortBy,
        SortDirection sortDirection
    )
    {
        var query = new SearchMemoryToolCallsQuery(CreateRepository())
        {
            Page = page,
            PageSize = pageSize,
            SortBy = sortBy,
            SortDirection = sortDirection
        };

        Assert.False(await query.CanExecute(Executor));
    }

    [Fact]
    public async Task Search_without_chat_prompt_or_keywords_leaves_them_unfiltered()
    {
        var repository = CreateRepository();

        await new SearchMemoryToolCallsQuery(repository).Execute(Executor);

        var request = Assert.IsType<
            EntityQuery<MemoryToolCallFilters, MemoryToolCallSortField>
        >(repository.LastSearchRequest);
        Assert.Null(request.Filters.MemoryAggregateId);
        Assert.Null(request.Filters.ThreadId);
        Assert.Null(request.Filters.PromptId);
        Assert.Empty(request.Filters.InputKeywords!);
        Assert.Empty(request.Filters.OutputKeywords!);
    }

    [Theory]
    [InlineData(" ", false)]
    [InlineData(" ", true)]
    [InlineData("long", false)]
    [InlineData("many", true)]
    public async Task Search_rejects_invalid_input_or_output_keywords(
        string keywordKind,
        bool isOutput
    )
    {
        List<string> keywords = keywordKind switch
        {
            "long" => [new string('a', SearchKeywordLimits.MaximumKeywordLength + 1)],
            "many" => Enumerable
                .Repeat("word", SearchKeywordLimits.MaximumCount + 1)
                .ToList(),
            _ => [keywordKind]
        };
        var query = new SearchMemoryToolCallsQuery(CreateRepository());
        if (isOutput)
            query.OutputKeywords = keywords;
        else
            query.InputKeywords = keywords;

        Assert.False(await query.CanExecute(Executor));
    }

    private static FakeMemoryToolCallRepository CreateRepository() =>
        new(
            new PagedResult<MemoryToolCall>(
                [
                    new MemoryToolCall(
                        MemoryId,
                        ThreadIdentifier,
                        PromptIdentifier,
                        1,
                        DateTime.UnixEpoch,
                        "Bash",
                        "tool-use-1",
                        "List files",
                        "{}"
                    )
                ],
                1,
                25,
                6
            )
        );

    private sealed class FakeMemoryToolCallRepository(
        PagedResult<MemoryToolCall> result
    ) : IMemoryToolCallRepository
    {
        public object? LastSearchRequest { get; private set; }

        public Task<List<MemoryToolCall>> Get(
            AggregateId memoryAggregateId,
            CancellationToken cancellationToken = default
        ) => throw new NotSupportedException();

        public Task<PagedResult<MemoryToolCall>> Search(
            EntityQuery<MemoryToolCallFilters, MemoryToolCallSortField> request,
            CancellationToken cancellationToken = default
        )
        {
            LastSearchRequest = request;
            return Task.FromResult(result);
        }

        public Task Write(
            IReadOnlyCollection<AggregateId> memoryAggregateIds,
            IReadOnlyCollection<MemoryToolCall> toolCalls,
            CancellationToken cancellationToken = default
        ) => throw new NotSupportedException();
    }
}

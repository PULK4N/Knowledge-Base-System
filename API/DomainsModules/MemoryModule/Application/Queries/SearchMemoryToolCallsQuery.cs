using ActionModule.Shared;
using ActionModule.Shared.Models;
using EmbeddingModule;
using EventSourcing.Shared.Models;
using MemoryModule.Application.DTOs;
using MemoryModule.Domain.Models;
using MemoryModule.Persistence.Interfaces;

namespace MemoryModule.Application.Queries;

/// <summary>
/// Lists recorded tool calls across every memory. Search matches call content,
/// while the tool-name, chat, and prompt filters narrow the results. Input and
/// output keywords are full-text filters on the tool input and the tool
/// response; every keyword of a side must appear in that side.
/// </summary>
public sealed class SearchMemoryToolCallsQuery(
    IMemoryToolCallRepository repository
) : PagedQuery<MemoryToolCallSearchItemDto>
{
    public MemoryToolCallSortField SortBy { get; set; } =
        MemoryToolCallSortField.Timestamp;
    public string? ToolName { get; set; }
    public Guid? MemoryId { get; set; }
    public Guid? ThreadId { get; set; }
    public Guid? PromptId { get; set; }
    public List<string> InputKeywords { get; set; } = [];
    public List<string> OutputKeywords { get; set; } = [];
    public SortDirection SortDirection { get; set; } =
        SortDirection.Descending;

    public override async Task<bool> CanExecute(Executor executor) =>
        await base.CanExecute(executor)
        && Enum.IsDefined(SortBy)
        && Enum.IsDefined(SortDirection)
        && (ToolName?.Length ?? 0) <= EntityQueryLimits.MaximumSearchLength
        && AreOptionalKeywordsValid(InputKeywords)
        && AreOptionalKeywordsValid(OutputKeywords);

    protected override async Task<
        PagedResult<MemoryToolCallSearchItemDto>
    > ExecuteInternal(Executor executor)
    {
        var result = await repository.Search(
            CreateEntityQuery(
                new MemoryToolCallFilters(
                    string.IsNullOrWhiteSpace(ToolName) ? null : ToolName.Trim(),
                    MemoryId is null
                        ? null
                        : AggregateId.FromDatabaseGuid(MemoryId.Value),
                    ThreadId is null
                        ? null
                        : new ThreadId(ThreadId.Value),
                    PromptId is null
                        ? null
                        : new PromptId(PromptId.Value),
                    NormalizeKeywords(InputKeywords),
                    NormalizeKeywords(OutputKeywords)
                ),
                SortBy,
                SortDirection
            )
        );

        return result.Map(MemoryToolCallSearchItemDto.FromReadModel);
    }

    private static bool AreOptionalKeywordsValid(List<string>? keywords) =>
        keywords is null
        || keywords.Count == 0
        || SearchKeywordLimits.AreValid(keywords);

    private static List<string> NormalizeKeywords(List<string>? keywords) =>
        keywords?.Select(keyword => keyword.Trim()).ToList() ?? [];
}

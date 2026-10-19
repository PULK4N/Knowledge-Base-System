using ActionModule.Shared.Models;
using EventSourcing.Shared.Models;
using MemoryModule.Domain;
using MemoryModule.Domain.Models;
using MemoryModule.Persistence.Interfaces;

namespace MemoryModule.Persistence.Tests;

public sealed class MemorySummaryProjectorTests
{
    [Fact]
    public async Task Update_writes_active_summary_and_removes_deleted_memory()
    {
        var repository = new FakeMemorySummaryRepository();
        var projector = new MemorySummaryProjector(repository);
        var active = CreateMemory(
            "11111111-1111-1111-1111-111111111111",
            "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"
        );
        var deleted = CreateMemory(
            "22222222-2222-2222-2222-222222222222",
            "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"
        );
        deleted.IsDeleted = true;

        await projector.Update(
            [CreateStateInfo(active), CreateStateInfo(deleted)]
        );

        Assert.Equal([active.Id, deleted.Id], repository.AggregateIds);
        var summary = Assert.Single(repository.Summaries);
        Assert.Equal(active.Id, summary.MemoryAggregateId);
        Assert.Equal(active.ThreadId, summary.ThreadId);
        Assert.Equal("Session title", summary.SessionTitle);
        Assert.Equal("Session summary", summary.Summary);
        Assert.Equal(4, summary.PromptCount);
        Assert.Equal(DateTime.UnixEpoch, summary.FirstPromptTimestamp);
        Assert.Equal(DateTime.UnixEpoch.AddMinutes(2), summary.LastPromptTimestamp);
        Assert.Equal(
            DateTime.UnixEpoch.AddMinutes(3),
            summary.LastActivityTimestamp
        );
        Assert.Equal(["claude-opus-5-5", "gpt-5.5-codex"], summary.Models);
    }

    private static MemoryStateData CreateMemory(
        string memoryId,
        string threadId
    )
    {
        var memory = new MemoryStateData(
            AggregateId.FromDatabaseGuid(Guid.Parse(memoryId))
        )
        {
            ThreadId = new ThreadId(Guid.Parse(threadId)),
            SessionTitle = "Session title",
            ChatSummary = new ChatSummary
            {
                Summary = "Session summary",
                SummaryTimestamp = DateTime.UnixEpoch.AddMinutes(3)
            }
        };
        memory.ChatPrompts.Add(
            new PromptId(Guid.Parse("cccccccc-cccc-cccc-cccc-000000000001")),
            new ChatPrompt
            {
                PromptId = new PromptId(
                    Guid.Parse("cccccccc-cccc-cccc-cccc-000000000001")
                ),
                PromptStartTimestamp = DateTime.UnixEpoch,
                Model = "gpt-5.5-codex"
            }
        );
        memory.ChatPrompts.Add(
            new PromptId(Guid.Parse("cccccccc-cccc-cccc-cccc-000000000002")),
            new ChatPrompt
            {
                PromptId = new PromptId(
                    Guid.Parse("cccccccc-cccc-cccc-cccc-000000000002")
                ),
                PromptStartTimestamp = DateTime.UnixEpoch.AddMinutes(2),
                Model = "claude-opus-5-5"
            }
        );
        memory.ChatPrompts.Add(
            new PromptId(Guid.Parse("cccccccc-cccc-cccc-cccc-000000000003")),
            new ChatPrompt
            {
                PromptId = new PromptId(
                    Guid.Parse("cccccccc-cccc-cccc-cccc-000000000003")
                ),
                PromptStartTimestamp = DateTime.UnixEpoch.AddMinutes(1),
                Model = ""
            }
        );
        memory.ChatPrompts.Add(
            new PromptId(Guid.Parse("cccccccc-cccc-cccc-cccc-000000000004")),
            new ChatPrompt
            {
                PromptId = new PromptId(
                    Guid.Parse("cccccccc-cccc-cccc-cccc-000000000004")
                ),
                PromptStartTimestamp = DateTime.UnixEpoch.AddMinutes(1),
                Model = "claude-opus-5-5"
            }
        );

        return memory;
    }

    private static StateInfo CreateStateInfo(MemoryStateData memory) =>
        StateInfo.Create(memory, "memory-state-machine", memory.Id);

    private sealed class FakeMemorySummaryRepository
        : IMemorySummaryRepository
    {
        public IReadOnlyCollection<AggregateId> AggregateIds { get; private set; } = [];
        public IReadOnlyCollection<MemorySummary> Summaries { get; private set; } = [];

        public Task<MemorySummary?> Get(
            AggregateId memoryAggregateId,
            CancellationToken cancellationToken = default
        ) => throw new NotSupportedException();

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
        )
        {
            AggregateIds = memoryAggregateIds;
            Summaries = summaries;
            return Task.CompletedTask;
        }
    }
}

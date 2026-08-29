using ActionModule.Shared.Models;
using AdministrationModule.Application.Commands;
using AdministrationModule.Application.DTOs;
using AdministrationModule.Application.Persistence;
using AdministrationModule.Application.Queries;
using EventSourcing.Shared.Models;
using Xunit;

namespace AdministrationModule.Application.Tests;

public sealed class OutboxAdministrationTests
{
    private static readonly Executor Executor =
        new()
        {
            Id = EventExecutor.FromDatabaseGuid(
                Guid.Parse("11111111-1111-1111-1111-111111111111")
            )
        };

    private static readonly Guid AggregateId =
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public async Task List_forwards_paging_search_filters_and_sorting()
    {
        var repository = new StubOutboxAdministrationRepository(
            [CreateEntry(17, "Failed", 3)]
        );
        var query = new ListOutboxPayloadsQuery(repository)
        {
            Page = 2,
            PageSize = 10,
            Search = " skill ",
            OnlyIncomplete = true,
            State = "Error",
            AggregateId = AggregateId,
            SortBy = OutboxPayloadSortField.RetryCount,
            SortDirection = SortDirection.Ascending
        };

        var result = await query.Execute(Executor);

        var request = Assert.IsType<
            EntityQuery<OutboxPayloadSearchFilters, OutboxPayloadSortField>
        >(repository.LastRequest);
        Assert.Equal(new PageRequest(2, 10), request.Page);
        Assert.Equal("skill", request.NormalizedSearch);
        Assert.Equal(
            new OutboxPayloadSearchFilters(true, "Error", AggregateId),
            request.Filters
        );
        Assert.Equal(
            new SortRequest<OutboxPayloadSortField>(
                OutboxPayloadSortField.RetryCount,
                SortDirection.Ascending
            ),
            request.Sort
        );
        Assert.Equal(1, result.TotalCount);
        var payload = Assert.Single(result.Items);
        Assert.Equal(17, payload.Id);
        Assert.Equal("Failed", payload.State);
        Assert.Equal(3, payload.RetryCount);
        Assert.Equal("Projection failed.", payload.ErrorMessage);
        Assert.Equal("SkillUpdatedV1", payload.EventName);
        Assert.Equal(
            "{\"eventName\":\"SkillUpdatedV1\"}",
            payload.ExecutionInfoJson
        );
        Assert.Equal(
            "{\"name\":\"Updated skill\"}",
            payload.EventDataJson
        );
    }

    [Fact]
    public async Task List_defaults_to_the_newest_payloads_first()
    {
        var repository = new StubOutboxAdministrationRepository([]);
        var query = new ListOutboxPayloadsQuery(repository);

        await query.Execute(Executor);

        var request = Assert.IsType<
            EntityQuery<OutboxPayloadSearchFilters, OutboxPayloadSortField>
        >(repository.LastRequest);
        Assert.Equal(
            new SortRequest<OutboxPayloadSortField>(
                OutboxPayloadSortField.Id,
                SortDirection.Descending
            ),
            request.Sort
        );
        Assert.Equal(
            new OutboxPayloadSearchFilters(false, null, null),
            request.Filters
        );
    }

    [Theory]
    [InlineData(0, Pagination.DefaultPageSize, "Error", false)]
    [InlineData(Pagination.DefaultPage, 0, "Error", false)]
    [InlineData(Pagination.DefaultPage, Pagination.DefaultPageSize, "Error", true)]
    public async Task List_rejects_invalid_paging(
        int page,
        int pageSize,
        string state,
        bool canExecute
    )
    {
        var query = new ListOutboxPayloadsQuery(
            new StubOutboxAdministrationRepository([])
        )
        {
            Page = page,
            PageSize = pageSize,
            State = state
        };

        Assert.Equal(canExecute, await query.CanExecute(Executor));
    }

    [Fact]
    public async Task Requeue_returns_reset_payload()
    {
        var repository = new StubOutboxAdministrationRepository(
            [CreateEntry(17, "Failed", 3)]
        );
        var command = new RequeueOutboxPayloadCommand(repository)
        {
            OutboxPayloadId = 17
        };

        var result = Assert.IsType<OutboxPayloadDto>(
            await command.Execute(Executor)
        );

        Assert.Equal(17, repository.LastRequeuedId);
        Assert.Equal("New", result.State);
        Assert.Equal(0, result.RetryCount);
    }

    private static OutboxPayloadEntry CreateEntry(
        long id,
        string state,
        int retryCount
    ) =>
        new(
            id,
            state,
            retryCount,
            "Projection failed.",
            "skills-state-machine",
            AggregateId,
            7,
            "SkillUpdatedV1",
            DateTime.UnixEpoch,
            "{\"eventName\":\"SkillUpdatedV1\"}",
            "{\"name\":\"Updated skill\"}"
        );

    private sealed class StubOutboxAdministrationRepository(
        List<OutboxPayloadEntry> entries
    ) : IOutboxAdministrationRepository
    {
        public EntityQuery<
            OutboxPayloadSearchFilters,
            OutboxPayloadSortField
        >? LastRequest
        {
            get;
            private set;
        }
        public long? LastRequeuedId { get; private set; }

        public Task<PagedResult<OutboxPayloadEntry>> Search(
            EntityQuery<
                OutboxPayloadSearchFilters,
                OutboxPayloadSortField
            > request,
            CancellationToken cancellationToken = default
        )
        {
            LastRequest = request;

            return Task.FromResult(
                new PagedResult<OutboxPayloadEntry>(
                    entries,
                    request.Page.Number,
                    request.Page.Size,
                    entries.Count
                )
            );
        }

        public Task<OutboxPayloadEntry?> Requeue(
            long outboxPayloadId,
            CancellationToken cancellationToken = default
        )
        {
            LastRequeuedId = outboxPayloadId;
            var entry = entries.SingleOrDefault(
                candidate => candidate.Id == outboxPayloadId
            );

            return Task.FromResult(
                entry is null
                    ? null
                    : entry with
                    {
                        State = "New",
                        RetryCount = 0
                    }
            );
        }
    }
}

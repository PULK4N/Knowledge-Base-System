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

    [Fact]
    public async Task List_returns_paged_outbox_payloads()
    {
        var repository = new StubOutboxAdministrationRepository(
            [CreateEntry(17, "Failed", 3)]
        );
        var query = new ListOutboxPayloadsQuery(repository)
        {
            Page = 2,
            PageSize = 10,
            OnlyIncomplete = true
        };

        var result = await query.Execute(Executor);

        Assert.Equal((2, 10, true), repository.LastSearch);
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
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
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
        public (int Page, int PageSize, bool OnlyIncomplete)? LastSearch
        {
            get;
            private set;
        }
        public long? LastRequeuedId { get; private set; }

        public Task<OutboxPayloadSearchResult> Search(
            int page,
            int pageSize,
            bool onlyIncomplete,
            CancellationToken cancellationToken = default
        )
        {
            LastSearch = (page, pageSize, onlyIncomplete);
            return Task.FromResult(
                new OutboxPayloadSearchResult(entries, entries.Count)
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

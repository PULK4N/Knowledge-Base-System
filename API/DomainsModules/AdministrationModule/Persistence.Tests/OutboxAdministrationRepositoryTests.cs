using ActionModule.Shared.Models;
using AdministrationModule.Application.Persistence;
using AdministrationModule.Persistence;
using EventSourcing.Persistence;
using EventSourcing.Persistence.Models;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AdministrationModule.Persistence.Tests;

public sealed class OutboxAdministrationRepositoryTests
{
    private static readonly Guid SkillAggregateId =
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid FeatureAggregateId =
        Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public async Task Search_pages_incomplete_payloads_by_retry_count()
    {
        await using var dbContext = CreateDbContext();
        Seed(dbContext);
        await dbContext.SaveChangesAsync();
        var repository = new OutboxAdministrationRepository(dbContext);

        var result = await repository.Search(
            CreateRequest(
                filters: new OutboxPayloadSearchFilters(true, null, null),
                sortBy: OutboxPayloadSortField.RetryCount,
                direction: SortDirection.Descending
            )
        );

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(
            [4, 0],
            result.Items.Select(item => item.RetryCount)
        );
        var failed = result.Items[0];
        Assert.Equal("Error", failed.State);
        Assert.Equal("Projection failed.", failed.ErrorMessage);
        Assert.Equal("skills-state-machine", failed.StateMachineId);
        Assert.Equal(SkillAggregateId, failed.AggregateId);
        Assert.Equal(
            nameof(AdministrationOutboxTestEvent),
            failed.EventName
        );
        Assert.Contains(
            "Administration outbox test",
            failed.EventDataJson
        );
    }

    [Theory]
    [InlineData("features-state", null, null, 1)]
    [InlineData("PROJECTION FAILED", null, null, 1)]
    [InlineData("administration outbox test", null, null, 3)]
    [InlineData(null, "sent", null, 1)]
    [InlineData(null, "unknown-state", null, 0)]
    [InlineData(null, null, "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", 2)]
    public async Task Search_applies_free_text_and_column_filters(
        string? search,
        string? state,
        string? aggregateId,
        int expectedCount
    )
    {
        await using var dbContext = CreateDbContext();
        Seed(dbContext);
        await dbContext.SaveChangesAsync();
        var repository = new OutboxAdministrationRepository(dbContext);

        var result = await repository.Search(
            CreateRequest(
                search: search,
                filters: new OutboxPayloadSearchFilters(
                    false,
                    state,
                    aggregateId is null ? null : Guid.Parse(aggregateId)
                )
            )
        );

        Assert.Equal(expectedCount, result.TotalCount);
        Assert.Equal(expectedCount, result.Items.Count);
    }

    [Fact]
    public async Task Requeue_resets_delivery_state_and_retry_count()
    {
        await using var dbContext = CreateDbContext();
        var failed = Seed(dbContext);
        await dbContext.SaveChangesAsync();
        var repository = new OutboxAdministrationRepository(dbContext);

        var requeued = Assert.IsType<OutboxPayloadEntry>(
            await repository.Requeue(failed.Id)
        );

        Assert.Equal("New", requeued.State);
        Assert.Equal(0, requeued.RetryCount);
        Assert.Equal("Projection failed.", requeued.ErrorMessage);
        Assert.Null(await repository.Requeue(failed.Id + 1_000));
    }

    private static EntityQuery<
        OutboxPayloadSearchFilters,
        OutboxPayloadSortField
    > CreateRequest(
        string? search = null,
        OutboxPayloadSearchFilters? filters = null,
        OutboxPayloadSortField sortBy = OutboxPayloadSortField.Id,
        SortDirection direction = SortDirection.Descending
    ) =>
        new(
            new PageRequest(Pagination.DefaultPage, 10),
            search,
            filters ?? new OutboxPayloadSearchFilters(false, null, null),
            new SortRequest<OutboxPayloadSortField>(sortBy, direction)
        );

    private static EventSourcingDbContext CreateDbContext() =>
        new(
            new DbContextOptionsBuilder<EventSourcingDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options
        );

    private static SerializedPayloadMessage Seed(
        EventSourcingDbContext dbContext
    )
    {
        var failed = Add(
            dbContext,
            "skills-state-machine",
            SkillAggregateId,
            MessageStatus.Error,
            4,
            "Projection failed."
        );
        Add(
            dbContext,
            "skills-state-machine",
            SkillAggregateId,
            MessageStatus.Sent,
            1,
            null
        );
        Add(
            dbContext,
            "features-state-machine",
            FeatureAggregateId,
            MessageStatus.New,
            0,
            null
        );

        return failed;
    }

    private static SerializedPayloadMessage Add(
        EventSourcingDbContext dbContext,
        string stateMachineId,
        Guid aggregateId,
        MessageStatus status,
        int executionAttempts,
        string? error
    )
    {
        var message = SerializedPayloadMessage.FromPayload(
            EventPayload.Create(
                EventExecutor.FromDatabaseGuid(
                    Guid.Parse("11111111-1111-1111-1111-111111111111")
                ),
                AggregateId.FromDatabaseGuid(aggregateId),
                stateMachineId,
                new AdministrationOutboxTestEvent()
            )
        );
        message.Status = status;
        message.ExecutionAttempts = executionAttempts;
        message.Error = error;
        message.Version = [];
        dbContext.Set<SerializedPayloadMessage>().Add(message);

        return message;
    }

    private sealed class AdministrationOutboxTestEvent : IEvent
    {
        public string Name { get; } = "Administration outbox test";

        public object Apply(
            object stateData,
            EventExecutionInfo eventExecutionInfo
        ) =>
            stateData;
    }
}

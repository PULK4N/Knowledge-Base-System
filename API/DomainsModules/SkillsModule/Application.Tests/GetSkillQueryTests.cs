using System.Collections.Immutable;
using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Core.Interfaces;
using EventSourcing.Core.Providers;
using EventSourcing.Persistence.Interfaces;
using EventSourcing.Shared.Models;
using Shared.Interfaces;
using SkillsModule.Application.DTOs;
using SkillsModule.Application.Queries;
using SkillsModule.Domain;
using SkillsModule.Domain.Events;
using SkillsModule.Domain.Models;

namespace SkillsModule.Application.Tests;

public sealed class GetSkillQueryTests
{
    private static readonly AggregateId SkillId =
        AggregateId.FromDatabaseGuid(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
        );
    private static readonly EventExecutor EventExecutor =
        EventExecutor.FromDatabaseGuid(
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")
        );
    private static readonly Executor Executor =
        new()
        {
            Id = EventExecutor
        };

    [Fact]
    public async Task Execute_WithZeroOrderNumber_ReturnsLatestState()
    {
        var query = CreateQuery(CreateEvents());

        var skill = Assert.IsType<SkillDto>(
            await query.Execute(Executor)
        );

        Assert.Equal("updated-name", skill.Name);
        Assert.Single(skill.Attachments);
    }

    [Fact]
    public async Task Execute_WithOrderNumber_ReturnsStateAtThatPoint()
    {
        var query = CreateQuery(CreateEvents(), orderNumber: 1);

        var skill = Assert.IsType<SkillDto>(
            await query.Execute(Executor)
        );

        Assert.Equal("original-name", skill.Name);
        Assert.Empty(skill.Attachments);
    }

    [Fact]
    public async Task Execute_WhenStreamDoesNotExist_ReturnsNull()
    {
        var query = CreateQuery([]);

        var skill = await query.Execute(Executor);

        Assert.Null(skill);
    }

    private static GetSkillQuery CreateQuery(
        List<EventPayload> events,
        uint orderNumber = 0
    )
    {
        var eventStore = new StubEventStore(events);
        var stateCalculator = new StateCalculator(
            new OrderNumberHelper(),
            new SkillStateDataProvider(),
            new StubEventValidatorProvider(),
            new StubUniqueEventConstraintProvider()
        );

        return new GetSkillQuery(stateCalculator, eventStore)
        {
            SkillId = SkillId.Value,
            OrderNumber = orderNumber
        };
    }

    private static List<EventPayload> CreateEvents()
    {
        var attachmentId = FileId.FromDatabaseGuid(
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc")
        );

        return
        [
            CreatePayload(
                1,
                new SkillCreatedV1(
                    "original-name",
                    "Description",
                    "Content",
                    ImmutableArray<string>.Empty,
                    ImmutableDictionary<string, SkillReference>.Empty
                )
            ),
            CreatePayload(
                2,
                new SkillDetailsUpdatedV1
                {
                    Name = "updated-name",
                    Description = "Updated description",
                    Content = "Updated content"
                }
            ),
            CreatePayload(
                3,
                new SkillAttachmentAddedV1(
                    new Attachment
                    {
                        Id = attachmentId,
                        Name = "example.pdf",
                        Size = 1_024,
                        FileType = "application/pdf",
                        Extension = "pdf"
                    }
                )
            )
        ];
    }

    private static EventPayload CreatePayload(
        uint orderNumber,
        EventSourcing.Shared.Interfaces.IEvent eventData
    )
    {
        var payload = EventPayload.Create(
            EventExecutor,
            SkillId,
            "skills-state-machine",
            eventData
        );
        payload.EventExecutionInfo.OrderNumber = orderNumber;

        return payload;
    }

    private sealed class StubEventStore(
        List<EventPayload> events
    ) : IEventStore
    {
        public Task<Dictionary<AggregateId, List<EventPayload>>> GetEvents(
            List<AggregateId> aggregateIds
        ) =>
            Task.FromResult(
                aggregateIds.ToDictionary(
                    aggregateId => aggregateId,
                    aggregateId =>
                        aggregateId == SkillId
                            ? new List<EventPayload>(events)
                            : new List<EventPayload>()
                )
            );

        public Task Write(List<EventPayload> payloads) =>
            throw new NotSupportedException();
    }

    private sealed class StubEventValidatorProvider
        : IEventValidatorProvider
    {
        public Task<List<IPreEventValidator>>
            GetPreEventStateValidators(EventPayload payload) =>
                Task.FromResult(new List<IPreEventValidator>());

        public Task<List<IPostEventValidator>>
            GetPostEventStateValidators(EventPayload payload) =>
                Task.FromResult(new List<IPostEventValidator>());
    }

    private sealed class StubUniqueEventConstraintProvider
        : IUniqueEventConstraintProvider
    {
        public IEnumerable<UniqueEventConstraintData>
            GetConstraintsToAdd(
                object stateData,
                EventPayload payload
            ) =>
                [];

        public IEnumerable<UniqueEventConstraintData>
            GetConstraintsToRemove(
                object stateData,
                EventPayload payload
            ) =>
                [];
    }

    private sealed class SkillStateDataProvider : IStateDataProvider
    {
        public Task<object> GetStateDataByStateMachine(
            string stateMachineId,
            AggregateId aggregateId
        ) =>
            Task.FromResult<object>(new SkillStateData(aggregateId));
    }
}

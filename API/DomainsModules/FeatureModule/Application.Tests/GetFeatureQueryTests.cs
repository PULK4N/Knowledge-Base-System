using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Core.Interfaces;
using EventSourcing.Core.Providers;
using EventSourcing.Persistence.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Application.DTOs;
using FeatureModule.Application.Queries;
using FeatureModule.Domain;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;
using Shared.Interfaces;

namespace FeatureModule.Application.Tests;

public sealed class GetFeatureQueryTests
{
    private static readonly AggregateId FeatureId =
        AggregateId.FromDatabaseGuid(
            Guid.Parse("11111111-1111-1111-1111-111111111111")
        );
    private static readonly EventExecutor EventExecutor =
        EventExecutor.FromDatabaseGuid(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
        );
    private static readonly Executor Executor = new() { Id = EventExecutor };

    [Theory]
    [InlineData(0U, "Backend implementation is complete.")]
    [InlineData(2U, "Backend implementation has started.")]
    public async Task Execute_ReturnsFeatureAtRequestedOrder(
        uint orderNumber,
        string expectedStatus
    )
    {
        var query = CreateQuery(CreateEvents(), orderNumber);

        var feature = Assert.IsType<FeatureDto>(
            await query.Execute(Executor)
        );

        Assert.Equal(expectedStatus, feature.Status);
        Assert.Equal(
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            feature.CurrentPlanId
        );
        Assert.Single(feature.Plans);
    }

    [Fact]
    public async Task Execute_WhenStreamDoesNotExist_ReturnsNull()
    {
        var query = CreateQuery([]);

        Assert.Null(await query.Execute(Executor));
    }

    private static GetFeatureQuery CreateQuery(
        List<EventPayload> events,
        uint orderNumber = 0
    ) =>
        new(
            new StateCalculator(
                new OrderNumberHelper(),
                new FeatureStateDataProvider(),
                new EmptyEventValidatorProvider(),
                new EmptyUniqueEventConstraintProvider(),
                new TestStateMachineDefinitionProvider()
            ),
            new StubEventStore(events)
        )
        {
            FeatureId = FeatureId.Value,
            OrderNumber = orderNumber
        };

    private static List<EventPayload> CreateEvents() =>
    [
        CreatePayload(
            1,
            new FeatureAddedV1(
                AggregateId.FromDatabaseGuid(
                    Guid.Parse("22222222-2222-2222-2222-222222222222")
                ),
                "Feature journal",
                "Trace implementation decisions.",
                "Backend implementation has started."
            )
        ),
        CreatePayload(
            2,
            new FeaturePlanAddedV1(
                FeaturePlanId.FromDatabaseGuid(
                    Guid.Parse("33333333-3333-3333-3333-333333333333")
                ),
                "Backend plan",
                "# Implement backend",
                FeaturePlanContentType.Markdown
            )
        ),
        CreatePayload(
            3,
            new FeatureStatusUpdatedV1(
                "Backend implementation is complete."
            )
        )
    ];

    private static EventPayload CreatePayload(
        uint orderNumber,
        EventSourcing.Shared.Interfaces.IEvent eventData
    )
    {
        var payload = EventPayload.Create(
            EventExecutor,
            FeatureId,
            "features-state-machine",
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
                        aggregateId == FeatureId
                            ? new List<EventPayload>(events)
                            : new List<EventPayload>()
                )
            );

        public Task Write(List<EventPayload> payloads) =>
            throw new NotSupportedException();
    }

    private sealed class EmptyEventValidatorProvider
        : IEventValidatorProvider
    {
        public Task<List<IPreEventValidator>>
            GetPreEventStateValidators(EventPayload payload) =>
                Task.FromResult(new List<IPreEventValidator>());

        public Task<List<IPostEventValidator>>
            GetPostEventStateValidators(EventPayload payload) =>
                Task.FromResult(new List<IPostEventValidator>());
    }

    private sealed class EmptyUniqueEventConstraintProvider
        : IUniqueEventConstraintProvider
    {
        public IEnumerable<UniqueEventConstraintData> GetConstraintsToAdd(
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

    private sealed class FeatureStateDataProvider : IStateDataProvider
    {
        public Task<object> GetStateDataByStateMachine(
            string stateMachineId,
            AggregateId aggregateId
        ) =>
            Task.FromResult<object>(new FeatureStateData(aggregateId));
    }
}

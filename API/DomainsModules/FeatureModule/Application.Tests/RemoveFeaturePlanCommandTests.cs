using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Core.Interfaces;
using EventSourcing.Core.Models;
using EventSourcing.Core.Providers;
using EventSourcing.Persistence.Interfaces;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Application.Commands;
using FeatureModule.Domain;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;
using Shared.Interfaces;

namespace FeatureModule.Application.Tests;

public sealed class RemoveFeaturePlanCommandTests
{
    private static readonly AggregateId FeatureId = AggregateId.FromDatabaseGuid(Guid.NewGuid());
    private static readonly AggregateId MemoryId = AggregateId.FromDatabaseGuid(Guid.NewGuid());
    private static readonly FeaturePlanId Research = new(Guid.NewGuid());
    private static readonly FeaturePlanId Implementation = new(Guid.NewGuid());
    private static readonly FeaturePlanId Retry = new(Guid.NewGuid());
    private static readonly FeaturePlanId Docs = new(Guid.NewGuid());

    [Theory]
    [MemberData(nameof(Removals))]
    public async Task Removing_a_plan_moves_its_followers_to_the_feature_branch(
        Guid removedPlanId,
        Guid[] expectedClearedPlanIds
    )
    {
        var store = new FeatureEventStore(
            Added(Research, null),
            Added(Implementation, Research),
            Added(Retry, Implementation),
            Added(Docs, Implementation)
        );
        var command = new RemoveFeaturePlanCommand(CreateHandler(store))
        {
            FeatureId = FeatureId.Value,
            PlanId = removedPlanId
        };
        command.UseUserOrigin();

        await command.Execute(new Executor { Id = EventExecutor.FromDatabaseGuid(Guid.NewGuid()) });

        Assert.Equal(1, store.WriteCount);
        Assert.IsType<FeaturePlanRemovedV2>(store.Written[0].EventData);
        Assert.Equal(
            expectedClearedPlanIds.Order(),
            store.Written
                .Skip(1)
                .Select(payload => Assert.IsType<FeaturePlanPreviousPlanSetV1>(payload.EventData))
                .Select(eventData =>
                {
                    Assert.Null(eventData.PreviousPlanId);
                    return eventData.PlanId.Value;
                })
                .Order()
        );
    }

    public static IEnumerable<object[]> Removals()
    {
        yield return [Implementation.Value, new[] { Retry.Value, Docs.Value }];
        yield return [Research.Value, new[] { Implementation.Value }];
        yield return [Docs.Value, Array.Empty<Guid>()];
    }

    private static IEvent Added(FeaturePlanId planId, FeaturePlanId? previousPlanId) =>
        new FeaturePlanAddedV4(planId, "Plan", "content", FeaturePlanContentType.Markdown, "", FeaturePlanStatus.NotStarted, $"branch-{planId.Value}", "", previousPlanId, Guid.Empty, MemoryId);

    private static StateMachineHandler CreateHandler(FeatureEventStore store) =>
        new(
            new StateCalculator(
                new OrderNumberHelper(),
                new FeatureStateProvider(),
                new NoValidators(),
                new NoConstraints(),
                new TestStateMachineDefinitionProvider()
            ),
            store
        );

    private sealed class FeatureEventStore(params IEvent[] planEvents) : IEventStoreWithOutbox
    {
        public List<EventPayload> Written { get; private set; } = [];
        public int WriteCount { get; private set; }

        public Task Write(Dictionary<AggregateId, StateInfo> stateInfos)
        {
            WriteCount++;
            Written = stateInfos.Values.SelectMany(stateInfo => stateInfo.LastExecutedPayloads).ToList();
            return Task.CompletedTask;
        }

        public Task<Dictionary<AggregateId, List<EventPayload>>> GetEvents(List<AggregateId> aggregateIds)
        {
            var events = new List<IEvent> { new FeatureAddedV1(AggregateId.FromDatabaseGuid(Guid.NewGuid()), "feature", "", "") }
                .Concat(planEvents)
                .Select((eventData, index) =>
                {
                    var payload = EventPayload.Create(
                        EventExecutor.FromDatabaseGuid(Guid.NewGuid()), FeatureId, "features-state-machine", eventData
                    );
                    payload.EventExecutionInfo.OrderNumber = (uint)index + 1;
                    return payload;
                })
                .ToList();

            return Task.FromResult(aggregateIds.ToDictionary(
                aggregateId => aggregateId,
                aggregateId => aggregateId == FeatureId ? events : []
            ));
        }
    }

    private sealed class FeatureStateProvider : IStateDataProvider
    {
        public Task<object> GetStateDataByStateMachine(string stateMachineId, AggregateId aggregateId) =>
            Task.FromResult<object>(new FeatureStateData(aggregateId));
    }

    private sealed class NoValidators : IEventValidatorProvider
    {
        public Task<List<IPreEventValidator>> GetPreEventStateValidators(EventPayload payload) =>
            Task.FromResult(new List<IPreEventValidator>());

        public Task<List<IPostEventValidator>> GetPostEventStateValidators(EventPayload payload) =>
            Task.FromResult(new List<IPostEventValidator>());
    }

    private sealed class NoConstraints : IUniqueEventConstraintProvider
    {
        public IEnumerable<UniqueEventConstraintData> GetConstraintsToAdd(object stateData, EventPayload payload) => [];

        public IEnumerable<UniqueEventConstraintData> GetConstraintsToRemove(object stateData, EventPayload payload) => [];
    }
}

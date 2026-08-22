using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Core.Interfaces;
using EventSourcing.Core.Providers;
using EventSourcing.Persistence.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Application.Commands;
using FeatureModule.Application.Models;
using FeatureModule.Domain;
using FeatureModule.Domain.Events;
using Shared.Interfaces;
using UUIDNext;

namespace FeatureModule.Application.Tests;

public sealed class AddFeatureCommandTests
{
    [Fact]
    public async Task Execute_ReturnsGeneratedFeatureIdWrittenToStream()
    {
        DatabaseFriendlyGuidGenerator.SetDefaultGuidGenerationDatabase(
            Database.SqlServer
        );
        var outbox = new CapturingEventStoreWithOutbox();
        var handler = new StateMachineHandler(
            CreateStateCalculator(),
            outbox
        );
        var projectId = Guid.Parse(
            "11111111-1111-1111-1111-111111111111"
        );
        var command = new AddFeatureCommand(handler)
        {
            ProjectId = projectId,
            Name = "Feature journal",
            Summary = "Trace implementation decisions.",
            Status = "Starting backend implementation."
        };
        var executor = new Executor
        {
            Id = EventExecutor.FromDatabaseGuid(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
            )
        };

        var result = Assert.IsType<FeatureCreatedCommandResult>(
            await command.Execute(executor)
        );

        var payload = Assert.Single(outbox.Written);
        var created = Assert.IsType<FeatureAddedV1>(payload.EventData);
        Assert.Equal("OK", result.Status);
        Assert.NotEqual(Guid.Empty, result.FeatureId);
        Assert.Equal(
            result.FeatureId,
            payload.EventExecutionInfo.AggregateId.Value
        );
        Assert.Equal(projectId, created.ProjectId.Value);
        Assert.Equal("Starting backend implementation.", created.Status);
    }

    private static StateCalculator CreateStateCalculator() =>
        new(
            new OrderNumberHelper(),
            new FeatureStateDataProvider(),
            new EmptyEventValidatorProvider(),
            new EmptyUniqueEventConstraintProvider(),
            new TestStateMachineDefinitionProvider()
        );

    private sealed class CapturingEventStoreWithOutbox
        : IEventStoreWithOutbox
    {
        public List<EventPayload> Written { get; private set; } = [];

        public Task Write(
            Dictionary<AggregateId, StateInfo> stateInfos
        )
        {
            Written = stateInfos.Values
                .SelectMany(
                    stateInfo => stateInfo.LastExecutedPayloads
                )
                .ToList();
            return Task.CompletedTask;
        }

        public Task<Dictionary<AggregateId, List<EventPayload>>> GetEvents(
            List<AggregateId> aggregateIds
        ) =>
            Task.FromResult(
                aggregateIds.ToDictionary(
                    aggregateId => aggregateId,
                    _ => new List<EventPayload>()
                )
            );
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

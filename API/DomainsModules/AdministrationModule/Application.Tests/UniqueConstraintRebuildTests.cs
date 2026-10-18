using ActionModule.Shared.Models;
using AdministrationModule.Application.Commands.UniqueConstraints;
using AdministrationModule.Application.DTOs.UniqueConstraints;
using AdministrationModule.Application.Persistence.UniqueConstraints;
using AdministrationModule.Application.Services;
using EventSourcing.Core;
using EventSourcing.Core.Interfaces;
using EventSourcing.Core.Models;
using EventSourcing.Core.Providers;
using EventSourcing.Persistence.Interfaces;
using EventSourcing.Shared.Containers;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using Shared.Interfaces;
using Xunit;

namespace AdministrationModule.Application.Tests;

public sealed class UniqueConstraintRebuildTests
{
    private const string StateMachineId = "rebuild-test-state-machine";

    private static readonly Executor Executor =
        new()
        {
            Id = EventExecutor.FromDatabaseGuid(
                Guid.Parse("11111111-1111-1111-1111-111111111111")
            )
        };

    private static readonly AggregateId FirstId = CreateId("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly AggregateId SecondId = CreateId("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly AggregateId DeletedId = CreateId("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly AggregateId EmptyHistoryId = CreateId("dddddddd-dddd-dddd-dddd-dddddddddddd");

    static UniqueConstraintRebuildTests() =>
        UniqueConstraintCreatorContainer.AddUniqueConstraintCreator(
            typeof(RebuildTestNameConstraintCreator)
        );

    [Fact]
    public async Task Rebuild_ReplacesRowsOfEveryAggregateWithHistory()
    {
        var repository = new CapturingRepository(FirstId, SecondId, DeletedId, EmptyHistoryId);
        var service = CreateService(
            repository,
            new()
            {
                [FirstId] = [CreatePayload(FirstId, new RebuildTestNameSetV1("first"))],
                [SecondId] = [CreatePayload(SecondId, new RebuildTestNameSetV1("second"))],
                [DeletedId] = [CreatePayload(DeletedId, new RebuildTestNameSetV1("deleted", IsDeleted: true))]
            }
        );

        var result = await service.Rebuild(StateMachineId);

        Assert.Equal(UniqueConstraintRebuildResult.CompletedStatus, result.Status);
        Assert.Equal(3, result.ProcessedAggregateCount);
        Assert.Equal(2, result.ConstraintCount);
        Assert.Empty(result.Conflicts);
        Assert.Equal(StateMachineId, repository.RequestedStateMachineId);
        var batch = Assert.IsType<UniqueConstraintRebuildBatch>(repository.WrittenBatch);
        Assert.Equal([FirstId, SecondId, DeletedId], batch.AggregateIds);
        Assert.Equal(
            [FirstId.Value, SecondId.Value],
            batch.Constraints.Select(constraint => constraint.AggregateId)
        );
    }

    [Fact]
    public async Task Rebuild_WithDuplicateValues_ReportsConflictWithoutWriting()
    {
        var repository = new CapturingRepository(FirstId, SecondId);
        var service = CreateService(
            repository,
            new()
            {
                [FirstId] = [CreatePayload(FirstId, new RebuildTestNameSetV1("same"))],
                [SecondId] = [CreatePayload(SecondId, new RebuildTestNameSetV1("same"))]
            }
        );

        var result = await service.Rebuild(StateMachineId);

        Assert.Equal(UniqueConstraintRebuildResult.ConflictsStatus, result.Status);
        var conflict = Assert.Single(result.Conflicts);
        Assert.Equal(nameof(RebuildTestNameConstraint), conflict.ConstraintName);
        Assert.Equal([FirstId.Value, SecondId.Value], conflict.AggregateIds);
        Assert.Null(repository.WrittenBatch);
    }

    [Theory]
    [InlineData(StateMachineId, true)]
    [InlineData("unknown-state-machine", false)]
    [InlineData(" ", false)]
    public async Task Command_CanExecute_OnlyForDefinedStateMachines(
        string stateMachineId,
        bool expected
    )
    {
        var command = new RebuildUniqueConstraintsCommand(
            new StubDefinitionProvider(),
            CreateService(new CapturingRepository(), [])
        )
        {
            StateMachineId = stateMachineId
        };

        Assert.Equal(expected, await command.CanExecute(Executor));
    }

    private static UniqueConstraintRebuildService CreateService(
        CapturingRepository repository,
        Dictionary<AggregateId, List<EventPayload>> histories
    ) =>
        new(
            repository,
            new StubEventStore(histories),
            new StateCalculator(
                new OrderNumberHelper(),
                new RebuildTestStateDataProvider(),
                new EmptyEventValidatorProvider(),
                new StubDefinitionProvider()
            )
        );

    private static AggregateId CreateId(string value) =>
        AggregateId.FromDatabaseGuid(Guid.Parse(value));

    private static EventPayload CreatePayload(AggregateId aggregateId, IEvent eventData)
    {
        var payload = EventPayload.Create(Executor.Id, aggregateId, StateMachineId, eventData);
        payload.EventExecutionInfo.OrderNumber = 1;

        return payload;
    }

    private sealed class CapturingRepository(params AggregateId[] aggregateIds)
        : IUniqueConstraintRepository
    {
        public string? RequestedStateMachineId { get; private set; }
        public UniqueConstraintRebuildBatch? WrittenBatch { get; private set; }

        public Task<List<AggregateId>> GetAggregateIds(string stateMachineId)
        {
            RequestedStateMachineId = stateMachineId;
            return Task.FromResult(aggregateIds.ToList());
        }

        public Task Write(UniqueConstraintRebuildBatch batch)
        {
            WrittenBatch = batch;
            return Task.CompletedTask;
        }
    }

    private sealed class StubEventStore(
        Dictionary<AggregateId, List<EventPayload>> histories
    ) : IEventStore
    {
        public Task<Dictionary<AggregateId, List<EventPayload>>> GetEvents(
            List<AggregateId> aggregateIds
        ) =>
            Task.FromResult(
                aggregateIds
                    .Where(histories.ContainsKey)
                    .ToDictionary(aggregateId => aggregateId, aggregateId => histories[aggregateId])
            );

        public Task Write(List<StateInfo> stateInfos) =>
            throw new NotSupportedException();
    }

    private sealed class StubDefinitionProvider : IStateMachineDefinitionProvider
    {
        private readonly List<StateMachineDefinition> _definitions =
        [
            new() { Id = StateMachineId, StateData = nameof(RebuildTestStateData) }
        ];

        public StateMachineDefinition Get(string stateMachineId) =>
            _definitions.Single(definition => definition.Id == stateMachineId);

        public IReadOnlyCollection<StateMachineDefinition> GetAll() => _definitions;
    }

    private sealed class RebuildTestStateDataProvider : IStateDataProvider
    {
        public object GetStateDataByStateMachine(string stateMachineId, AggregateId aggregateId) =>
            new RebuildTestStateData(aggregateId);
    }

    private sealed class EmptyEventValidatorProvider : IEventValidatorProvider
    {
        public List<IPreEventValidator> GetPreEventStateValidators(EventPayload payload) => [];

        public List<IPostEventValidator> GetPostEventStateValidators(EventPayload payload) => [];
    }
}

public sealed class RebuildTestStateData(AggregateId id) : ISharedStateData
{
    public AggregateId Id { get; init; } = id;
    public bool IsDeleted { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed record RebuildTestNameSetV1(string Name, bool IsDeleted = false) : IEvent
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (RebuildTestStateData)stateData;
        state.Name = Name;
        state.IsDeleted = IsDeleted;

        return state;
    }
}

public sealed class RebuildTestNameConstraint(RebuildTestStateData stateData, string name)
    : UniqueConstraint(stateData, name);

public sealed class RebuildTestNameConstraintCreator
    : IUniqueConstraintCreator<RebuildTestStateData, RebuildTestNameConstraint>
{
    public List<RebuildTestNameConstraint> Create(RebuildTestStateData stateData) =>
        [new RebuildTestNameConstraint(stateData, stateData.Name)];
}

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
using FeatureModule.Persistence.Interfaces;
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
        var command = new AddFeatureCommand(
            handler,
            new StubFeatureSummaryRepository()
        )
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

    [Fact]
    public async Task CanExecute_rejects_an_existing_feature_name()
    {
        var featureId = Guid.Parse(
            "22222222-2222-2222-2222-222222222222"
        );
        var command = new AddFeatureCommand(
            new StateMachineHandler(
                CreateStateCalculator(),
                new CapturingEventStoreWithOutbox()
            ),
            new StubFeatureSummaryRepository(
                new FeatureSummary(
                    featureId,
                    Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    "Feature journal",
                    "Summary",
                    "Active",
                    null,
                    0,
                    0
                )
            )
        )
        {
            ProjectId = Guid.Parse(
                "11111111-1111-1111-1111-111111111111"
            ),
            Name = "  FEATURE JOURNAL ",
            Summary = "Summary",
            Status = "Starting"
        };

        Assert.False(
            await command.CanExecute(
                new Executor
                {
                    Id = EventExecutor.FromDatabaseGuid(
                        Guid.Parse(
                            "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"
                        )
                    )
                }
            )
        );
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

    private sealed class StubFeatureSummaryRepository(
        FeatureSummary? feature = null
    ) : IFeatureSummaryRepository
    {
        public Task<List<FeatureSummary>> List(
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult(
                feature is null ? [] : new List<FeatureSummary> { feature }
            );

        public Task<FeatureSummary?> GetByName(
            string name,
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult(
                feature is not null
                && string.Equals(
                    feature.Name,
                    name.Trim(),
                    StringComparison.OrdinalIgnoreCase
                )
                    ? feature
                    : null
            );

        public Task<FeatureSummarySearchResult> Search(
            int page,
            int pageSize,
            string? search,
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult(
                new FeatureSummarySearchResult(
                    feature is null
                        ? []
                        : new List<FeatureSummary> { feature },
                    feature is null ? 0 : 1
                )
            );
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

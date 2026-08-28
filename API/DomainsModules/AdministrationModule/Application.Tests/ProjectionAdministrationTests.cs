using ActionModule.Shared.Models;
using AdministrationModule.Application.Commands;
using AdministrationModule.Application.Persistence;
using AdministrationModule.Application.Queries;
using EventSourcing.Core;
using EventSourcing.Core.Interfaces;
using EventSourcing.Core.Models;
using EventSourcing.Core.Providers;
using EventSourcing.Persistence.Interfaces;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using Shared.Interfaces;
using Xunit;

namespace AdministrationModule.Application.Tests;

public sealed class ProjectionAdministrationTests
{
    private static readonly Executor Executor =
        new()
        {
            Id = EventExecutor.FromDatabaseGuid(
                Guid.Parse(
                    "11111111-1111-1111-1111-111111111111"
                )
            )
        };

    [Fact]
    public async Task List_returns_only_state_machines_with_projections()
    {
        var query = new ListProjectionGroupsQuery(
            new StubDefinitionProvider(
                new StateMachineDefinition
                {
                    Id = "skills-state-machine",
                    Projections =
                    [
                        "SkillSummaryProjector",
                        "SkillSearchProjector"
                    ]
                },
                new StateMachineDefinition
                {
                    Id = "general-policies-state-machine",
                    Events = new Dictionary<
                        string,
                        StateMachineEventDefinition
                    >
                    {
                        ["GeneralPolicyAddedV1"] = new()
                        {
                            Projections =
                            [
                                "GeneralPolicyTextProjector"
                            ]
                        },
                        ["GeneralPolicyUpdatedV1"] = new()
                        {
                            Projections =
                            [
                                "GeneralPolicyTextProjector"
                            ]
                        }
                    }
                },
                new StateMachineDefinition
                {
                    Id = "without-projections"
                }
            )
        );

        var groups = await query.Execute(Executor);

        Assert.Collection(
            groups,
            group =>
            {
                Assert.Equal(
                    "general-policies-state-machine",
                    group.StateMachineId
                );
                Assert.Equal(
                    ["GeneralPolicyTextProjector"],
                    group.ProjectionNames
                );
            },
            group =>
            {
                Assert.Equal(
                    "skills-state-machine",
                    group.StateMachineId
                );
                Assert.Equal(
                    [
                        "SkillSearchProjector",
                        "SkillSummaryProjector"
                    ],
                    group.ProjectionNames
                );
            }
        );
    }

    [Fact]
    public async Task Replay_writes_one_last_event_per_aggregate_to_outbox()
    {
        const string stateMachineId = "skills-state-machine";
        var first = CreatePayload(
            stateMachineId,
            "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
            4
        );
        var second = CreatePayload(
            stateMachineId,
            "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
            7
        );
        var repository = new StubReplayRepository([first, second]);
        var outbox = new CapturingOutbox();
        var command = new QueueProjectionReplayCommand(
            new StubDefinitionProvider(
                new StateMachineDefinition
                {
                    Id = stateMachineId,
                    Projections = ["SkillSummaryProjector"]
                }
            ),
            repository,
            outbox
        )
        {
            StateMachineId = stateMachineId
        };

        var result = await command.Execute(Executor);

        Assert.Equal("Queued", result.Status);
        Assert.Equal(2, result.QueuedAggregateCount);
        Assert.Equal(stateMachineId, repository.StateMachineId);
        Assert.Collection(
            outbox.StateInfos
                .OrderBy(stateInfo => stateInfo.CurrentOrderNumber),
            stateInfo => Assert.Same(
                first,
                Assert.Single(stateInfo.LastExecutedPayloads)
            ),
            stateInfo => Assert.Same(
                second,
                Assert.Single(stateInfo.LastExecutedPayloads)
            )
        );
    }

    [Fact]
    public async Task Run_executes_an_unconfigured_projection_for_one_aggregate()
    {
        const string stateMachineId = "skills-state-machine";
        var aggregateId = AggregateId.FromDatabaseGuid(
            Guid.Parse(
                "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"
            )
        );
        var definitionProvider = new StubDefinitionProvider(
            new StateMachineDefinition
            {
                Id = stateMachineId
            }
        );
        var history = new List<EventPayload>
        {
            CreatePayload(stateMachineId, aggregateId.Value.ToString(), 1),
            CreatePayload(stateMachineId, aggregateId.Value.ToString(), 2)
        };
        var projector = new CapturingProjector();
        var command = new RunProjectionCommand(
            new StubReplayRepository([]),
            new StubEventStore(
                new Dictionary<AggregateId, List<EventPayload>>
                {
                    [aggregateId] = history
                }
            ),
            CreateStateCalculator(definitionProvider),
            [projector]
        )
        {
            ProjectionName = nameof(CapturingProjector),
            AggregateId = aggregateId.Value
        };

        var result = await command.Execute(Executor);

        Assert.NotNull(result);
        Assert.Equal("Completed", result!.Status);
        Assert.Equal(1, result.ProcessedAggregateCount);
        var stateInfo = Assert.Single(projector.StateInfos);
        Assert.Equal(aggregateId, stateInfo.AggregateId);
        Assert.Equal(2u, stateInfo.CurrentOrderNumber);
    }

    [Fact]
    public async Task Run_executes_an_unconfigured_projection_for_every_state_machine_aggregate()
    {
        const string stateMachineId = "skills-state-machine";
        var first = CreatePayload(
            stateMachineId,
            "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
            1
        );
        var second = CreatePayload(
            stateMachineId,
            "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
            1
        );
        var definitionProvider = new StubDefinitionProvider(
            new StateMachineDefinition
            {
                Id = stateMachineId
            }
        );
        var repository = new StubReplayRepository([first, second]);
        var projector = new CapturingProjector();
        var command = new RunProjectionCommand(
            repository,
            new StubEventStore(
                new Dictionary<AggregateId, List<EventPayload>>
                {
                    [first.EventExecutionInfo.AggregateId] = [first],
                    [second.EventExecutionInfo.AggregateId] = [second]
                }
            ),
            CreateStateCalculator(definitionProvider),
            [projector]
        )
        {
            ProjectionName = nameof(CapturingProjector),
            StateMachineId = stateMachineId
        };

        var result = await command.Execute(Executor);

        Assert.NotNull(result);
        Assert.Equal(2, result!.ProcessedAggregateCount);
        Assert.Equal(stateMachineId, repository.StateMachineId);
        Assert.Equal(2, projector.StateInfos.Count);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(
        "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
        "skills-state-machine"
    )]
    public async Task Run_requires_exactly_one_replay_scope(
        string? aggregateId,
        string? stateMachineId
    )
    {
        var definitionProvider = new StubDefinitionProvider(
            new StateMachineDefinition
            {
                Id = "skills-state-machine",
                Projections = [nameof(CapturingProjector)]
            }
        );
        var command = new RunProjectionCommand(
            new StubReplayRepository([]),
            new StubEventStore(
                new Dictionary<AggregateId, List<EventPayload>>()
            ),
            CreateStateCalculator(definitionProvider),
            [new CapturingProjector()]
        )
        {
            ProjectionName = nameof(CapturingProjector),
            AggregateId = aggregateId is null
                ? null
                : Guid.Parse(aggregateId),
            StateMachineId = stateMachineId
        };

        Assert.False(await command.CanExecute(Executor));
    }

    private static EventPayload CreatePayload(
        string stateMachineId,
        string aggregateId,
        uint orderNumber
    )
    {
        var payload = EventPayload.Create(
            Executor.Id,
            AggregateId.FromDatabaseGuid(
                Guid.Parse(aggregateId)
            ),
            stateMachineId,
            new ProjectionReplayTestEvent()
        );
        payload.EventExecutionInfo.OrderNumber = orderNumber;

        return payload;
    }

    private static StateCalculator CreateStateCalculator(
        IStateMachineDefinitionProvider definitionProvider
    ) =>
        new(
            new OrderNumberHelper(),
            new StubStateDataProvider(),
            new StubEventValidatorProvider(),
            new StubUniqueEventConstraintProvider(),
            definitionProvider
        );

    private sealed class StubDefinitionProvider(
        params StateMachineDefinition[] definitions
    ) : IStateMachineDefinitionProvider
    {
        private readonly List<StateMachineDefinition> _definitions =
            definitions.ToList();

        public StateMachineDefinition Get(string stateMachineId) =>
            _definitions.Single(
                definition => definition.Id == stateMachineId
            );

        public IReadOnlyCollection<StateMachineDefinition> GetAll() =>
            _definitions;
    }

    private sealed class StubReplayRepository(
        List<EventPayload> lastEvents
    ) : IProjectionReplayRepository
    {
        public string? StateMachineId { get; private set; }

        public Task<List<EventPayload>> GetLastEvents(
            string stateMachineId
        )
        {
            StateMachineId = stateMachineId;
            return Task.FromResult(lastEvents);
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
                aggregateIds.ToDictionary(
                    aggregateId => aggregateId,
                    aggregateId => histories.GetValueOrDefault(
                        aggregateId,
                        []
                    )
                )
            );

        public Task Write(List<EventPayload> payloads) =>
            throw new NotSupportedException();
    }

    private sealed class CapturingProjector : IProjector
    {
        public List<StateInfo> StateInfos { get; private set; } = [];

        public Task Update(List<StateInfo> stateInfo)
        {
            StateInfos = stateInfo;
            return Task.CompletedTask;
        }
    }

    private sealed class StubStateDataProvider : IStateDataProvider
    {
        public Task<object> GetStateDataByStateMachine(
            string stateMachineId,
            AggregateId aggregateId
        ) =>
            Task.FromResult(new object());
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

    private sealed class CapturingOutbox : IOutbox
    {
        public List<StateInfo> StateInfos { get; private set; } = [];

        public Task Write(
            Dictionary<AggregateId, StateInfo> stateInfos
        )
        {
            StateInfos = stateInfos.Values.ToList();
            return Task.CompletedTask;
        }

        public Task<MessagePayload?> ReadNext(
            CancellationToken cancellationToken = default
        ) =>
            throw new NotSupportedException();

        public Task UpdateCompleted(long id) =>
            throw new NotSupportedException();

        public Task UpdateFailed(long id, string errorMessage) =>
            throw new NotSupportedException();
    }

    private sealed class ProjectionReplayTestEvent : IEvent
    {
        public object Apply(
            object stateData,
            EventExecutionInfo eventExecutionInfo
        ) =>
            stateData;
    }
}

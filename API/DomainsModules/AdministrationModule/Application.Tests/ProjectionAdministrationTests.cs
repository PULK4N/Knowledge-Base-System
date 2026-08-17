using ActionModule.Shared.Models;
using AdministrationModule.Application.Commands;
using AdministrationModule.Application.Persistence;
using AdministrationModule.Application.Queries;
using EventSourcing.Core.Interfaces;
using EventSourcing.Core.Models;
using EventSourcing.Persistence.Interfaces;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
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

        public Task UpdateFailed(long id) =>
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

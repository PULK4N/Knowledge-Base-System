using ActionModule.Models;
using EventSourcing.Core;
using EventSourcing.Core.Interfaces;
using EventSourcing.Core.Providers;
using EventSourcing.Persistence.Interfaces;
using EventSourcing.Shared.Models;
using Shared.Interfaces;
using SkillsModule.Application.Commands;
using SkillsModule.Application.Models;
using SkillsModule.Domain;
using SkillsModule.Domain.Events;
using UUIDNext;

namespace SkillsModule.Application.Tests;

public sealed class AddSkillCommandTests
{
    [Fact]
    public async Task Execute_ReturnsGeneratedAggregateIdWrittenToStream()
    {
        DatabaseFriendlyGuidGenerator
            .SetDefaultGuidGenerationDatabase(Database.SqlServer);
        var eventStore = new EmptyEventStore();
        var outbox = new CapturingEventStoreWithOutbox();
        var handler = new StateMachineHandler(
            eventStore,
            outbox,
            new EmptyEventValidatorProvider(),
            new EmptyUniqueEventConstraintProvider(),
            new SkillStateDataProvider(),
            new OrderNumberHelper()
        );
        var command = new AddSkillCommand(handler)
        {
            Name = "skill-name",
            Description = "Description",
            Content = "Content"
        };
        var executor = new Executor
        {
            Id = EventExecutor.FromDatabaseGuid(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
            )
        };

        var result = Assert.IsType<SkillCreatedCommandResult>(
            await command.Execute(executor)
        );

        var payload = Assert.Single(outbox.Written);
        Assert.Equal("OK", result.Status);
        Assert.NotEqual(Guid.Empty, result.SkillId);
        Assert.Equal(
            result.SkillId,
            payload.EventExecutionInfo.AggregateId.Value
        );
        Assert.IsType<SkillCreatedV1>(payload.EventData);
    }

    private sealed class EmptyEventStore : IEventStore
    {
        public Task<Dictionary<AggregateId, EventPayload[]>> GetEvents(
            params AggregateId[] aggregateIds
        ) =>
            Task.FromResult(
                aggregateIds.ToDictionary(
                    aggregateId => aggregateId,
                    _ => Array.Empty<EventPayload>()
                )
            );

        public Task Write(List<EventPayload> payloads) =>
            throw new NotSupportedException();
    }

    private sealed class CapturingEventStoreWithOutbox
        : IEventStoreWithOutbox
    {
        public List<EventPayload> Written { get; private set; } = [];

        public Task Write(List<EventPayload> payloads)
        {
            Written = [.. payloads];
            return Task.CompletedTask;
        }

        public Task<Dictionary<AggregateId, EventPayload[]>> GetEvents(
            params AggregateId[] aggregateIds
        ) =>
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
            string stateMachineId
        ) =>
            Task.FromResult<object>(new SkillStateData());
    }
}

using System.Collections.Immutable;
using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Shared.Models;
using ProjectModule.Application.Models;
using ProjectModule.Domain;
using ProjectModule.Domain.Events;

namespace ProjectModule.Application.Commands;

public sealed class RemoveTopicRelationFromProjectCommand(
    StateMachineHandler stateMachineHandler
) : ExistingProjectCommand(stateMachineHandler)
{
    public required string TopicName { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            ProjectId != Guid.Empty
            && !string.IsNullOrWhiteSpace(TopicName)
        );

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();

        return await ExecuteProjectEvent(
            executor,
            new TopicRelationRemovedFromProjectV2(
                new TopicName(TopicName),
                SessionId,
                memoryAggregateId
            ),
            memoryAggregateId
        );
    }
}

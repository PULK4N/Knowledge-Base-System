using System.Collections.Immutable;
using ActionModule.Shared.Models;
using EventSourcing.Core;
using SkillsModule.Domain.Events;

namespace SkillsModule.Application.Commands;

public sealed class UpdateSkillCommand(StateMachineHandler stateMachineHandler)
    : ExistingSkillCommand(stateMachineHandler)
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string Content { get; set; }
    public List<string> Tags { get; set; } = [];

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(!string.IsNullOrWhiteSpace(Name));

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();
        return await ExecuteEvent(
            executor,
            new SkillDetailsUpdatedV2(
                Name,
                Description,
                Content,
                Tags.ToImmutableArray(),
                SessionId,
                memoryAggregateId
            ),
            memoryAggregateId
        );
    }
}

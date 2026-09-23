using ActionModule.Shared.Models;
using EventSourcing.Core;
using SkillsModule.Domain.Events;

namespace SkillsModule.Application.Commands;

public sealed class UpdateSkillReferenceAutoLoadCommand(
    StateMachineHandler stateMachineHandler
) : ExistingSkillCommand(stateMachineHandler)
{
    public required string RelativePath { get; set; }
    public required bool LoadAutomatically { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(!string.IsNullOrWhiteSpace(RelativePath));

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();
        return await ExecuteEvent(
            executor,
            new SkillReferenceAutoLoadUpdatedV2(
                RelativePath,
                LoadAutomatically,
                SessionId,
                memoryAggregateId
            ),
            memoryAggregateId
        );
    }
}

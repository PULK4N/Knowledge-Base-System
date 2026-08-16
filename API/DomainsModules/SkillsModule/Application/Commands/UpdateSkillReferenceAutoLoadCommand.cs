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

    protected override Task<object> ExecuteInternal(Executor executor) =>
        ExecuteEvent(
            executor,
            new SkillReferenceAutoLoadUpdatedV1(
                RelativePath,
                LoadAutomatically
            )
        );
}

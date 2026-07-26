using EventSourcing.Core;
using SkillsModule.Domain.Events;

namespace SkillsModule.Application.Commands;

public sealed class DeleteSkillReferenceCommand(StateMachineHandler stateMachineHandler)
    : SkillCommand(stateMachineHandler)
{
    public required string RelativePath { get; init; }

    public override Task<bool> CanExecute() =>
        Task.FromResult(!string.IsNullOrWhiteSpace(RelativePath));

    protected override Task<object> ExecuteInternal() =>
        ExecuteEvent(new SkillReferenceDeleted { RelativePath = RelativePath });
}

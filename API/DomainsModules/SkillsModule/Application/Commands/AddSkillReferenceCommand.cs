using EventSourcing.Core;
using SkillsModule.Domain.Events;

namespace SkillsModule.Application.Commands;

public sealed class AddSkillReferenceCommand(StateMachineHandler stateMachineHandler)
    : SkillCommand(stateMachineHandler)
{
    public required string RelativePath { get; init; }
    public required string Content { get; init; }

    public override Task<bool> CanExecute() =>
        Task.FromResult(!string.IsNullOrWhiteSpace(RelativePath));

    protected override Task<object> ExecuteInternal() =>
        ExecuteEvent(
            new SkillReferenceAdded
            {
                RelativePath = RelativePath,
                Content = Content
            }
        );
}

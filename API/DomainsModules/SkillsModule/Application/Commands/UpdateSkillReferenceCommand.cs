using EventSourcing.Core;
using SkillsModule.Domain.Events;

namespace SkillsModule.Application.Commands;

public sealed class UpdateSkillReferenceCommand(StateMachineHandler stateMachineHandler)
    : SkillCommand(stateMachineHandler)
{
    public required string RelativePath { get; init; }
    public required string Content { get; init; }

    public override Task<bool> CanExecute() =>
        Task.FromResult(!string.IsNullOrWhiteSpace(RelativePath));

    protected override Task<object> ExecuteInternal() =>
        ExecuteEvent(
            new SkillReferenceUpdated
            {
                RelativePath = RelativePath,
                Content = Content
            }
        );
}

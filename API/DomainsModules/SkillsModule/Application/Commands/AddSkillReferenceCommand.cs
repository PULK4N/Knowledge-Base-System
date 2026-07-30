using ActionModule.Shared.Models;
using EventSourcing.Core;
using SkillsModule.Domain.Events;

namespace SkillsModule.Application.Commands;

public sealed class AddSkillReferenceCommand(StateMachineHandler stateMachineHandler)
    : ExistingSkillCommand(stateMachineHandler)
{
    public required string RelativePath { get; set; }
    public required string Content { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(!string.IsNullOrWhiteSpace(RelativePath));

    protected override Task<object> ExecuteInternal(Executor executor) =>
        ExecuteEvent(
            executor,
            new SkillReferenceAddedV1
            {
                RelativePath = RelativePath,
                Content = Content
            }
        );
}

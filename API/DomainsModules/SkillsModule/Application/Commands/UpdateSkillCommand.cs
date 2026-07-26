using EventSourcing.Core;
using SkillsModule.Domain.Events;

namespace SkillsModule.Application.Commands;

public sealed class UpdateSkillCommand(StateMachineHandler stateMachineHandler)
    : SkillCommand(stateMachineHandler)
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Content { get; init; }
    public List<string> Tags { get; init; } = [];

    public override Task<bool> CanExecute() =>
        Task.FromResult(!string.IsNullOrWhiteSpace(Name));

    protected override Task<object> ExecuteInternal() =>
        ExecuteEvent(
            new SkillDetailsUpdated
            {
                Name = Name,
                Description = Description,
                Content = Content,
                Tags = [.. Tags]
            }
        );
}

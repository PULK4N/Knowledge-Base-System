using EventSourcing.Core;
using SkillsModule.Domain.Events;
using SkillsModule.Domain.Models;

namespace SkillsModule.Application.Commands;

public sealed class AddSkillCommand(StateMachineHandler stateMachineHandler)
    : SkillCommand(stateMachineHandler)
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Content { get; init; }
    public List<string> Tags { get; init; } = [];
    public List<SkillReference> References { get; init; } = [];

    public override Task<bool> CanExecute() =>
        Task.FromResult(!string.IsNullOrWhiteSpace(Name));

    protected override Task<object> ExecuteInternal() =>
        ExecuteEvent(
            new SkillSaved
            {
                Name = Name,
                Description = Description,
                Content = Content,
                Tags = [.. Tags],
                References = References
                    .Select(reference => new SkillReference
                    {
                        RelativePath = reference.RelativePath,
                        Content = reference.Content
                    })
                    .ToList()
            }
        );
}

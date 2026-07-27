using System.Collections.Immutable;
using ActionModule.Models;
using EventSourcing.Core;
using EventSourcing.Shared.Models;
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
    public Dictionary<string, SkillReference> References { get; init; } =
        new(StringComparer.Ordinal);
    public Dictionary<string, SkillFile> Files { get; init; } =
        new(StringComparer.Ordinal);

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(!string.IsNullOrWhiteSpace(Name));

    protected override Task<object> ExecuteInternal(Executor executor) =>
        ExecuteEvent(
            executor,
            AggregateId.New(),
            new SkillCreatedV1(
                Name,
                Description,
                Content,
                Tags.ToImmutableArray(),
                References.ToImmutableDictionary(StringComparer.Ordinal),
                Files.ToImmutableDictionary(StringComparer.Ordinal)
            )
        );
}

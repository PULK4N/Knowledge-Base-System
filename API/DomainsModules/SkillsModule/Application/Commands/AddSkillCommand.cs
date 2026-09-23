using System.Collections.Immutable;
using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Shared.Models;
using SkillsModule.Application.Models;
using SkillsModule.Domain.Events;
using SkillsModule.Domain.Models;

namespace SkillsModule.Application.Commands;

public sealed class AddSkillCommand(StateMachineHandler stateMachineHandler)
    : SkillCommand(stateMachineHandler)
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string Content { get; set; }
    public List<string> Tags { get; set; } = [ ];
    public Dictionary<string, SkillReference2> References { get; set; } =
        new(StringComparer.Ordinal);

    protected override async Task<object> ExecuteInternal(
        Executor executor
    )
    {
        var skillId = AggregateId.New();
        var memoryAggregateId = await ResolveMemoryAggregateId();

        await ExecuteEvent(
            executor,
            skillId,
            new SkillCreatedV3(
                Name,
                Description,
                Content,
                Tags.ToImmutableArray(),
                References.ToImmutableDictionary(StringComparer.Ordinal),
                SessionId,
                memoryAggregateId
            ),
            memoryAggregateId
        );

        return SkillCreatedCommandResult.Ok(skillId.Value);
    }
}

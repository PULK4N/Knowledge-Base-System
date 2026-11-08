using System.Collections.Immutable;
using ActionModule.Shared.Models;
using EventSourcing.Core;
using SkillsModule.Domain.Events;

namespace SkillsModule.Application.Commands;

public sealed class SetSkillUseScenariosCommand(
    StateMachineHandler stateMachineHandler
) : ExistingSkillCommand(stateMachineHandler)
{
    public List<string> Scenarios { get; set; } = [];

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            SkillId != Guid.Empty
            && Scenarios.All(scenario => !string.IsNullOrWhiteSpace(scenario))
        );

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();
        return await ExecuteEvent(
            executor,
            new SkillUseScenariosSetV1(
                Scenarios
                    .Select(scenario => scenario.Trim())
                    .ToImmutableArray(),
                SessionId,
                memoryAggregateId
            ),
            memoryAggregateId
        );
    }
}

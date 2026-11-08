using ProjectModule.Shared;
using System.Collections.Immutable;
using ActionModule.Shared.Models;
using EventSourcing.Core;
using ProjectModule.Domain.Events;
using ProjectModule.Domain.Models;

namespace ProjectModule.Application.Commands;

/// <summary>Replaces the whole review level to model and reasoning mapping.</summary>
public sealed class SetProjectReviewLevelModelsCommand(
    StateMachineHandler stateMachineHandler
) : ExistingProjectCommand(stateMachineHandler)
{
    public Dictionary<ReviewLevel, ReviewModel> LevelModels { get; set; } = [];

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();

        return await ExecuteProjectEvent(
            executor,
            new ProjectReviewLevelModelsSetV1(
                LevelModels.ToImmutableDictionary(
                    levelModel => levelModel.Key,
                    levelModel => new ReviewModel(
                        levelModel.Value.Model?.Trim() ?? string.Empty,
                        levelModel.Value.Reasoning?.Trim() ?? string.Empty
                    )
                ),
                SessionId,
                memoryAggregateId
            ),
            memoryAggregateId
        );
    }
}

using ActionModule.Shared.Models;
using EventSourcing.Core;
using FeatureModule.Application.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;

namespace FeatureModule.Application.Commands;

public sealed class AddFeatureResearchDiscoveryCommand(
    StateMachineHandler stateMachineHandler
) : ExistingFeatureCommand(stateMachineHandler)
{
    public required string Title { get; set; }

    public required string Content { get; set; }

    public FeatureResearchDiscoverySourceType SourceType { get; set; }

    public string SourceReference { get; set; } = string.Empty;

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            FeatureId != Guid.Empty
            && !string.IsNullOrWhiteSpace(Title)
            && !string.IsNullOrWhiteSpace(Content)
            && Enum.IsDefined(SourceType)
        );

    protected override async Task<object> ExecuteInternal(
        Executor executor
    )
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();
        var discoveryId = FeatureResearchDiscoveryId.New();

        await ExecuteEvent(
            executor,
            new FeatureResearchDiscoveryAddedV3(
                discoveryId,
                Title.Trim(),
                Content,
                SourceType,
                SourceReference,
                SessionId,
                memoryAggregateId
            ),
            memoryAggregateId
        );

        return FeatureResearchDiscoveryCreatedCommandResult.Ok(
            discoveryId.Value
        );
    }
}

public sealed class UpdateFeatureResearchDiscoveryCommand(
    StateMachineHandler stateMachineHandler
) : ExistingFeatureCommand(stateMachineHandler)
{
    public required Guid DiscoveryId { get; set; }

    public required string Title { get; set; }

    public required string Content { get; set; }

    public FeatureResearchDiscoverySourceType SourceType { get; set; }

    public string SourceReference { get; set; } = string.Empty;

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            FeatureId != Guid.Empty
            && DiscoveryId != Guid.Empty
            && !string.IsNullOrWhiteSpace(Title)
            && !string.IsNullOrWhiteSpace(Content)
            && Enum.IsDefined(SourceType)
        );

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();

        return await ExecuteEvent(
            executor,
            new FeatureResearchDiscoveryUpdatedV3(
                FeatureResearchDiscoveryId.FromDatabaseGuid(DiscoveryId),
                Title.Trim(),
                Content,
                SourceType,
                SourceReference,
                SessionId,
                memoryAggregateId
            ),
            memoryAggregateId
        );
    }
}

public sealed class RemoveFeatureResearchDiscoveryCommand(
    StateMachineHandler stateMachineHandler
) : ExistingFeatureCommand(stateMachineHandler)
{
    public required Guid DiscoveryId { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            FeatureId != Guid.Empty
            && DiscoveryId != Guid.Empty
        );

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();

        return await ExecuteEvent(
            executor,
            new FeatureResearchDiscoveryRemovedV2(
                FeatureResearchDiscoveryId.FromDatabaseGuid(DiscoveryId),
                SessionId,
                memoryAggregateId
            ),
            memoryAggregateId
        );
    }
}

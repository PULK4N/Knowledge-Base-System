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
    public required string Content { get; set; }

    public FeatureResearchDiscoverySourceType SourceType { get; set; }

    public string SourceReference { get; set; } = string.Empty;

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            FeatureId != Guid.Empty
            && !string.IsNullOrWhiteSpace(Content)
            && Enum.IsDefined(SourceType)
        );

    protected override async Task<object> ExecuteInternal(
        Executor executor
    )
    {
        var discoveryId = FeatureResearchDiscoveryId.New();

        await ExecuteEvent(
            executor,
            new FeatureResearchDiscoveryAddedV1(
                discoveryId,
                Content,
                SourceType,
                SourceReference
            )
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

    public required string Content { get; set; }

    public FeatureResearchDiscoverySourceType SourceType { get; set; }

    public string SourceReference { get; set; } = string.Empty;

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            FeatureId != Guid.Empty
            && DiscoveryId != Guid.Empty
            && !string.IsNullOrWhiteSpace(Content)
            && Enum.IsDefined(SourceType)
        );

    protected override Task<object> ExecuteInternal(Executor executor) =>
        ExecuteEvent(
            executor,
            new FeatureResearchDiscoveryUpdatedV1(
                FeatureResearchDiscoveryId.FromDatabaseGuid(DiscoveryId),
                Content,
                SourceType,
                SourceReference
            )
        );
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

    protected override Task<object> ExecuteInternal(Executor executor) =>
        ExecuteEvent(
            executor,
            new FeatureResearchDiscoveryRemovedV1(
                FeatureResearchDiscoveryId.FromDatabaseGuid(DiscoveryId)
            )
        );
}

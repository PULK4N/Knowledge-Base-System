using System.Collections.Immutable;
using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Application.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;

namespace FeatureModule.Application.Commands;

public abstract class FeaturePlanContentCommand(
    StateMachineHandler stateMachineHandler
) : ExistingFeatureCommand(stateMachineHandler)
{
    public required string Title { get; set; }

    public required string Content { get; set; }

    public FeaturePlanContentType ContentType { get; set; }

    public string AdditionalInfo { get; set; } = string.Empty;

    public FeaturePlanStatus Status { get; set; }

    /// <summary>
    /// Records of the feature to link to the plan. Null leaves the current
    /// links unchanged; an empty list removes them.
    /// </summary>
    public List<Guid>? RecordIds { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            FeatureId != Guid.Empty
            && (RecordIds?.All(recordId => recordId != Guid.Empty) ?? true)
            && !string.IsNullOrWhiteSpace(Title)
            && !string.IsNullOrWhiteSpace(Content)
            && Enum.IsDefined(ContentType)
            && Enum.IsDefined(Status)
        );

    /// <summary>
    /// The event that sets the plan's record links, or none when RecordIds
    /// asks for no change.
    /// </summary>
    protected List<IEvent> CreateRecordLinkEvents(
        FeaturePlanId planId,
        AggregateId memoryAggregateId
    ) =>
        RecordIds is null
            ? []
            :
            [
                new FeaturePlanRecordsSetV1(
                    planId,
                    RecordIds
                        .Distinct()
                        .Select(FeatureRecordId.FromDatabaseGuid)
                        .ToImmutableArray(),
                    SessionId,
                    memoryAggregateId
                )
            ];
}

using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Events;

// Research discoveries were merged into records. These events are kept so
// released history replays, and each one now applies to a research record.

/// <summary>
/// Applies released research discovery events to records of type research.
/// </summary>
internal static class FeatureResearchDiscoveryReplay
{
    internal const string UntitledTitle = "Untitled discovery";

    internal static void Add(
        FeatureStateData state,
        FeatureResearchDiscoveryId discoveryId,
        string title,
        string content,
        FeatureRecordSourceType sourceType,
        string sourceReference,
        DateTime timestamp
    ) =>
        state.Records.Add(
            new Models.FeatureRecord
            {
                Id = discoveryId.ToRecordId(),
                Type = FeatureRecordTypes.Research,
                Title = title,
                Content = content,
                SourceType = sourceType,
                SourceReference = sourceReference,
                CreatedAt = timestamp,
                UpdatedAt = timestamp
            }
        );

    /// <param name="title">Null for V1 updates, which kept the title.</param>
    internal static void Update(
        FeatureStateData state,
        FeatureResearchDiscoveryId discoveryId,
        string? title,
        string content,
        FeatureRecordSourceType sourceType,
        string sourceReference,
        DateTime timestamp
    )
    {
        var recordId = discoveryId.ToRecordId();
        var record = state.Records.Single(item => item.Id == recordId);
        if (title is not null)
            record.Title = title;
        record.Content = content;
        record.SourceType = sourceType;
        record.SourceReference = sourceReference;
        record.UpdatedAt = timestamp;
    }
}

public interface IFeatureResearchDiscoveryAdded : IEvent;

public readonly record struct FeatureResearchDiscoveryAddedV1(
    FeatureResearchDiscoveryId DiscoveryId,
    string Content,
    FeatureRecordSourceType SourceType,
    string SourceReference
) : IFeatureResearchDiscoveryAdded
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        FeatureResearchDiscoveryReplay.Add(
            state,
            DiscoveryId,
            FeatureResearchDiscoveryReplay.UntitledTitle,
            Content,
            SourceType,
            SourceReference,
            eventExecutionInfo.Timestamp
        );
        return state;
    }
}

public readonly record struct FeatureResearchDiscoveryAddedV2(
    FeatureResearchDiscoveryId DiscoveryId,
    string Title,
    string Content,
    FeatureRecordSourceType SourceType,
    string SourceReference
) : IFeatureResearchDiscoveryAdded
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        FeatureResearchDiscoveryReplay.Add(
            state,
            DiscoveryId,
            Title,
            Content,
            SourceType,
            SourceReference,
            eventExecutionInfo.Timestamp
        );
        return state;
    }
}

public interface IFeatureResearchDiscoveryUpdated : IEvent;

public readonly record struct FeatureResearchDiscoveryUpdatedV1(
    FeatureResearchDiscoveryId DiscoveryId,
    string Content,
    FeatureRecordSourceType SourceType,
    string SourceReference
) : IFeatureResearchDiscoveryUpdated
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        FeatureResearchDiscoveryReplay.Update(
            state,
            DiscoveryId,
            null,
            Content,
            SourceType,
            SourceReference,
            eventExecutionInfo.Timestamp
        );
        return state;
    }
}

public readonly record struct FeatureResearchDiscoveryUpdatedV2(
    FeatureResearchDiscoveryId DiscoveryId,
    string Title,
    string Content,
    FeatureRecordSourceType SourceType,
    string SourceReference
) : IFeatureResearchDiscoveryUpdated
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        FeatureResearchDiscoveryReplay.Update(
            state,
            DiscoveryId,
            Title,
            Content,
            SourceType,
            SourceReference,
            eventExecutionInfo.Timestamp
        );
        return state;
    }
}

public interface IFeatureResearchDiscoveryRemoved : IEvent;

public readonly record struct FeatureResearchDiscoveryRemovedV1(
    FeatureResearchDiscoveryId DiscoveryId
) : IFeatureResearchDiscoveryRemoved
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        FeatureRecordRemoval.Remove(state, DiscoveryId.ToRecordId());
        return state;
    }
}

public readonly record struct FeatureResearchDiscoveryAddedV3(
    FeatureResearchDiscoveryId DiscoveryId,
    string Title,
    string Content,
    FeatureRecordSourceType SourceType,
    string SourceReference,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IFeatureResearchDiscoveryAdded
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        FeatureResearchDiscoveryReplay.Add(
            state,
            DiscoveryId,
            Title,
            Content,
            SourceType,
            SourceReference,
            eventExecutionInfo.Timestamp
        );
        state.MemoryHistory.Add(
            new MemoryHistoryRecord(
                eventExecutionInfo.EventName,
                eventExecutionInfo.Timestamp,
                MemoryAggregateId
            )
        );
        return state;
    }
}

public readonly record struct FeatureResearchDiscoveryUpdatedV3(
    FeatureResearchDiscoveryId DiscoveryId,
    string Title,
    string Content,
    FeatureRecordSourceType SourceType,
    string SourceReference,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IFeatureResearchDiscoveryUpdated
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        FeatureResearchDiscoveryReplay.Update(
            state,
            DiscoveryId,
            Title,
            Content,
            SourceType,
            SourceReference,
            eventExecutionInfo.Timestamp
        );
        state.MemoryHistory.Add(
            new MemoryHistoryRecord(
                eventExecutionInfo.EventName,
                eventExecutionInfo.Timestamp,
                MemoryAggregateId
            )
        );
        return state;
    }
}

public readonly record struct FeatureResearchDiscoveryRemovedV2(
    FeatureResearchDiscoveryId DiscoveryId,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IFeatureResearchDiscoveryRemoved
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        FeatureRecordRemoval.Remove(state, DiscoveryId.ToRecordId());
        state.MemoryHistory.Add(
            new MemoryHistoryRecord(
                eventExecutionInfo.EventName,
                eventExecutionInfo.Timestamp,
                MemoryAggregateId
            )
        );
        return state;
    }
}

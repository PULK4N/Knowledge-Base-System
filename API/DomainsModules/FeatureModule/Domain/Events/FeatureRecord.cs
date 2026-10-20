using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Events;

/// <summary>
/// Removes a record and its links from every plan, so no plan keeps an ID of a
/// record that no longer exists.
/// </summary>
internal static class FeatureRecordRemoval
{
    internal static void Remove(
        FeatureStateData state,
        FeatureRecordId recordId
    )
    {
        var record = state.Records.Single(item => item.Id == recordId);
        state.Records.Remove(record);

        foreach (var plan in state.Plans)
            plan.RecordIds.Remove(recordId);
    }
}

public interface IFeatureRecordAdded : IEvent;

public readonly record struct FeatureRecordAddedV1(
    FeatureRecordId RecordId,
    string UserMessage,
    string AiAnswer
) : IFeatureRecordAdded
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        var record = new Models.FeatureRecord
        {
            Id = RecordId,
            CreatedAt = eventExecutionInfo.Timestamp,
            UpdatedAt = eventExecutionInfo.Timestamp
        };
        record.SetConversation(UserMessage, AiAnswer);
        state.Records.Add(record);
        return state;
    }
}

public interface IFeatureRecordUpdated : IEvent;

public readonly record struct FeatureRecordUpdatedV1(
    FeatureRecordId RecordId,
    string UserMessage,
    string AiAnswer
) : IFeatureRecordUpdated
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        var recordId = RecordId;
        var record = state.Records.Single(item => item.Id == recordId);
        record.SetConversation(UserMessage, AiAnswer);
        record.UpdatedAt = eventExecutionInfo.Timestamp;
        return state;
    }
}

public interface IFeatureRecordRemoved : IEvent;

public readonly record struct FeatureRecordRemovedV1(
    FeatureRecordId RecordId
) : IFeatureRecordRemoved
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        FeatureRecordRemoval.Remove(state, RecordId);
        return state;
    }
}

public readonly record struct FeatureRecordRemovedV2(
    FeatureRecordId RecordId,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IFeatureRecordRemoved
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        FeatureRecordRemoval.Remove(state, RecordId);
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

public readonly record struct FeatureRecordUpdatedV2(
    FeatureRecordId RecordId,
    string UserMessage,
    string AiAnswer,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IFeatureRecordUpdated
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        var recordId = RecordId;
        var record = state.Records.Single(item => item.Id == recordId);
        record.SetConversation(UserMessage, AiAnswer);
        record.UpdatedAt = eventExecutionInfo.Timestamp;
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

public readonly record struct FeatureRecordAddedV2(
    FeatureRecordId RecordId,
    string UserMessage,
    string AiAnswer,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IFeatureRecordAdded
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        var record = new Models.FeatureRecord
        {
            Id = RecordId,
            CreatedAt = eventExecutionInfo.Timestamp,
            UpdatedAt = eventExecutionInfo.Timestamp
        };
        record.SetConversation(UserMessage, AiAnswer);
        state.Records.Add(record);
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

/// <summary>
/// Adds a titled Markdown record. Type is free text, already normalized to
/// lower case; SourceType is null when the record has no source.
/// </summary>
public readonly record struct FeatureRecordAddedV3(
    FeatureRecordId RecordId,
    string Type,
    string Title,
    string Content,
    FeatureRecordSourceType? SourceType,
    string SourceReference,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IFeatureRecordAdded
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        state.Records.Add(
            new Models.FeatureRecord
            {
                Id = RecordId,
                Type = Type,
                Title = Title,
                Content = Content,
                SourceType = SourceType,
                SourceReference = SourceReference,
                CreatedAt = eventExecutionInfo.Timestamp,
                UpdatedAt = eventExecutionInfo.Timestamp
            }
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

public readonly record struct FeatureRecordUpdatedV3(
    FeatureRecordId RecordId,
    string Type,
    string Title,
    string Content,
    FeatureRecordSourceType? SourceType,
    string SourceReference,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IFeatureRecordUpdated
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        var recordId = RecordId;
        var record = state.Records.Single(item => item.Id == recordId);
        record.Type = Type;
        record.Title = Title;
        record.Content = Content;
        record.SourceType = SourceType;
        record.SourceReference = SourceReference;
        record.UpdatedAt = eventExecutionInfo.Timestamp;
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

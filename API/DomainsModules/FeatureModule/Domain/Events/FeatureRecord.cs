using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Events;

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
        state.Records.Add(
            new Models.FeatureRecord
            {
                Id = RecordId,
                UserMessage = UserMessage,
                AiAnswer = AiAnswer,
                CreatedAt = eventExecutionInfo.Timestamp,
                UpdatedAt = eventExecutionInfo.Timestamp
            }
        );
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
        record.UserMessage = UserMessage;
        record.AiAnswer = AiAnswer;
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
        var recordId = RecordId;
        var record = state.Records.Single(item => item.Id == recordId);
        state.Records.Remove(record);
        return state;
    }
}

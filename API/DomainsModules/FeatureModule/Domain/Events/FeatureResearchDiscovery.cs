using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Events;

public interface IFeatureResearchDiscoveryAdded : IEvent;

public readonly record struct FeatureResearchDiscoveryAddedV1(
    FeatureResearchDiscoveryId DiscoveryId,
    string Content,
    FeatureResearchDiscoverySourceType SourceType,
    string SourceReference
) : IFeatureResearchDiscoveryAdded
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        state.ResearchDiscoveries.Add(
            new Models.FeatureResearchDiscovery
            {
                Id = DiscoveryId,
                Content = Content,
                SourceType = SourceType,
                SourceReference = SourceReference,
                CreatedAt = eventExecutionInfo.Timestamp,
                UpdatedAt = eventExecutionInfo.Timestamp
            }
        );
        return state;
    }
}

public readonly record struct FeatureResearchDiscoveryAddedV2(
    FeatureResearchDiscoveryId DiscoveryId,
    string Title,
    string Content,
    FeatureResearchDiscoverySourceType SourceType,
    string SourceReference
) : IFeatureResearchDiscoveryAdded
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        state.ResearchDiscoveries.Add(
            new Models.FeatureResearchDiscovery
            {
                Id = DiscoveryId,
                Title = Title,
                Content = Content,
                SourceType = SourceType,
                SourceReference = SourceReference,
                CreatedAt = eventExecutionInfo.Timestamp,
                UpdatedAt = eventExecutionInfo.Timestamp
            }
        );
        return state;
    }
}

public interface IFeatureResearchDiscoveryUpdated : IEvent;

public readonly record struct FeatureResearchDiscoveryUpdatedV1(
    FeatureResearchDiscoveryId DiscoveryId,
    string Content,
    FeatureResearchDiscoverySourceType SourceType,
    string SourceReference
) : IFeatureResearchDiscoveryUpdated
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        var discoveryId = DiscoveryId;
        var discovery = state.ResearchDiscoveries.Single(
            item => item.Id == discoveryId
        );
        discovery.Content = Content;
        discovery.SourceType = SourceType;
        discovery.SourceReference = SourceReference;
        discovery.UpdatedAt = eventExecutionInfo.Timestamp;
        return state;
    }
}

public readonly record struct FeatureResearchDiscoveryUpdatedV2(
    FeatureResearchDiscoveryId DiscoveryId,
    string Title,
    string Content,
    FeatureResearchDiscoverySourceType SourceType,
    string SourceReference
) : IFeatureResearchDiscoveryUpdated
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        var discoveryId = DiscoveryId;
        var discovery = state.ResearchDiscoveries.Single(
            item => item.Id == discoveryId
        );
        discovery.Title = Title;
        discovery.Content = Content;
        discovery.SourceType = SourceType;
        discovery.SourceReference = SourceReference;
        discovery.UpdatedAt = eventExecutionInfo.Timestamp;
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
        var discoveryId = DiscoveryId;
        var discovery = state.ResearchDiscoveries.Single(
            item => item.Id == discoveryId
        );
        state.ResearchDiscoveries.Remove(discovery);
        return state;
    }
}

public readonly record struct FeatureResearchDiscoveryAddedV3(
    FeatureResearchDiscoveryId DiscoveryId,
    string Title,
    string Content,
    FeatureResearchDiscoverySourceType SourceType,
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
        state.ResearchDiscoveries.Add(
            new Models.FeatureResearchDiscovery
            {
                Id = DiscoveryId,
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

public readonly record struct FeatureResearchDiscoveryUpdatedV3(
    FeatureResearchDiscoveryId DiscoveryId,
    string Title,
    string Content,
    FeatureResearchDiscoverySourceType SourceType,
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
        var discoveryId = DiscoveryId;
        var discovery = state.ResearchDiscoveries.Single(
            item => item.Id == discoveryId
        );
        discovery.Title = Title;
        discovery.Content = Content;
        discovery.SourceType = SourceType;
        discovery.SourceReference = SourceReference;
        discovery.UpdatedAt = eventExecutionInfo.Timestamp;
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
        var discoveryId = DiscoveryId;
        var discovery = state.ResearchDiscoveries.Single(
            item => item.Id == discoveryId
        );
        state.ResearchDiscoveries.Remove(discovery);
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

using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Events;

public interface IFeatureReviewNoteAdded : IEvent;

public readonly record struct FeatureReviewNoteAddedV1(
    FeatureReviewNoteId ReviewNoteId,
    string Title,
    string Content
) : IFeatureReviewNoteAdded
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        state.ReviewNotes.Add(
            new Models.FeatureReviewNote
            {
                Id = ReviewNoteId,
                Title = Title,
                Content = Content,
                CreatedAt = eventExecutionInfo.Timestamp,
                UpdatedAt = eventExecutionInfo.Timestamp
            }
        );
        return state;
    }
}

public interface IFeatureReviewNoteUpdated : IEvent;

public readonly record struct FeatureReviewNoteUpdatedV1(
    FeatureReviewNoteId ReviewNoteId,
    string Title,
    string Content
) : IFeatureReviewNoteUpdated
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        var reviewNoteId = ReviewNoteId;
        var reviewNote = state.ReviewNotes.Single(
            item => item.Id == reviewNoteId
        );
        reviewNote.Title = Title;
        reviewNote.Content = Content;
        reviewNote.UpdatedAt = eventExecutionInfo.Timestamp;
        return state;
    }
}

public interface IFeatureReviewNoteRemoved : IEvent;

public readonly record struct FeatureReviewNoteRemovedV1(
    FeatureReviewNoteId ReviewNoteId
) : IFeatureReviewNoteRemoved
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        var reviewNoteId = ReviewNoteId;
        var reviewNote = state.ReviewNotes.Single(
            item => item.Id == reviewNoteId
        );
        state.ReviewNotes.Remove(reviewNote);
        return state;
    }
}

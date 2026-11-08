using System.Text.Json;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;
using FeatureModule.Domain.Validators;
using ProjectModule.Shared;

namespace FeatureModule.Domain.Tests;

public sealed class FeatureReviewerTests
{
    [Fact]
    public void Serialized_events_replay_content_levels_timestamps_and_history()
    {
        var featureId = AggregateId.FromDatabaseGuid(Guid.NewGuid());
        var memoryId = AggregateId.FromDatabaseGuid(Guid.NewGuid());
        var reviewerId = FeatureReviewerId.FromDatabaseGuid(Guid.NewGuid());
        var sessionId = Guid.NewGuid();
        var state = new FeatureStateData(featureId);
        var addedAt = new DateTime(2026, 11, 8, 10, 0, 0, DateTimeKind.Utc);
        var added = new FeatureReviewerAddedV1(reviewerId, "Security", "## Check\n\nPermissions", ReviewLevel.Medium, sessionId, memoryId);
        var json = JsonSerializer.Serialize(added);
        Assert.Contains("\"Medium\"", json);
        Replay(JsonSerializer.Deserialize<FeatureReviewerAddedV1>(json)!, state, addedAt);
        var updated = new FeatureReviewerUpdatedV1(reviewerId, "Authorization", "Check every endpoint", ReviewLevel.Heavy, sessionId, memoryId);
        Replay(JsonSerializer.Deserialize<FeatureReviewerUpdatedV1>(JsonSerializer.Serialize(updated))!, state, addedAt.AddHours(1));

        var reviewer = Assert.Single(state.Reviewers);
        Assert.Equal("Authorization", reviewer.Title);
        Assert.Equal("Check every endpoint", reviewer.Description);
        Assert.Equal(ReviewLevel.Heavy, reviewer.ReviewLevel);
        Assert.Equal(addedAt, reviewer.CreatedAt);
        Assert.Equal(addedAt.AddHours(1), reviewer.UpdatedAt);
        Assert.All(state.MemoryHistory, entry => Assert.Equal(memoryId, entry.AggregateId));

        var removed = new FeatureReviewerRemovedV1(reviewerId, sessionId, memoryId);
        Replay(JsonSerializer.Deserialize<FeatureReviewerRemovedV1>(JsonSerializer.Serialize(removed))!, state, addedAt.AddHours(2));
        Assert.Empty(state.Reviewers);
        Assert.Equal(3, state.MemoryHistory.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Existence_validators_protect_add_update_and_remove(bool exists)
    {
        var id = FeatureReviewerId.FromDatabaseGuid(Guid.NewGuid());
        var state = new FeatureStateData(AggregateId.FromDatabaseGuid(Guid.NewGuid()));
        if (exists) state.Reviewers.Add(new FeatureReviewer { Id = id });
        var memoryId = AggregateId.FromDatabaseGuid(Guid.NewGuid());
        Assert.Equal(!exists, new FeatureReviewerMustNotExistValidator().Validate(state,
            Payload(new FeatureReviewerAddedV1(id, "Title", "Description", ReviewLevel.Easy, Guid.Empty, memoryId))).Succeded);
        Assert.Equal(exists, new FeatureReviewerMustExistValidator().Validate(state,
            Payload(new FeatureReviewerUpdatedV1(id, "Title", "Description", ReviewLevel.Heavy, Guid.Empty, memoryId))).Succeded);
        Assert.Equal(exists, new FeatureReviewerMustExistValidator().Validate(state,
            Payload(new FeatureReviewerRemovedV1(id, Guid.Empty, memoryId))).Succeded);
    }

    [Theory]
    [InlineData("Title", "Description", ReviewLevel.Easy, true)]
    [InlineData("Title", "Description", ReviewLevel.Medium, true)]
    [InlineData("Title", "Description", ReviewLevel.Heavy, true)]
    [InlineData(" ", "Description", ReviewLevel.Medium, false)]
    [InlineData("Title", " ", ReviewLevel.Medium, false)]
    [InlineData("Title", "Description", (ReviewLevel)999, false)]
    public void Content_validator_rejects_blank_fields_and_unknown_levels(string title, string description, ReviewLevel level, bool expected)
    {
        var id = FeatureReviewerId.FromDatabaseGuid(Guid.NewGuid());
        var memoryId = AggregateId.FromDatabaseGuid(Guid.NewGuid());
        var validator = new FeatureReviewerContentValidator();
        foreach (var data in new List<IEvent> {
            new FeatureReviewerAddedV1(id, title, description, level, Guid.Empty, memoryId),
            new FeatureReviewerUpdatedV1(id, title, description, level, Guid.Empty, memoryId) })
            Assert.Equal(expected, validator.Validate(new FeatureStateData(memoryId), Payload(data)).Succeded);
    }

    private static EventPayload Payload(IEvent data) => EventPayload.Create(
        EventExecutor.FromDatabaseGuid(Guid.NewGuid()), AggregateId.FromDatabaseGuid(Guid.NewGuid()), "features-state-machine", data);

    private static void Replay(IEvent data, FeatureStateData state, DateTime timestamp)
    {
        data.Apply(state, new EventExecutionInfo
        {
            AggregateId = state.Id,
            EventName = data.GetType().Name,
            Timestamp = timestamp
        });
    }
}

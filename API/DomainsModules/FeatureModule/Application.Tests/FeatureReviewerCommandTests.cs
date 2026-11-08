using ActionModule.Shared.Models;
using EventSourcing.Shared.Models;
using FeatureModule.Application.Commands;
using FeatureModule.Application.DTOs;
using FeatureModule.Domain;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;
using ProjectModule.Shared;
using UUIDNext;

namespace FeatureModule.Application.Tests;

public sealed class FeatureReviewerCommandTests
{
    [Theory]
    [InlineData("add")]
    [InlineData("update")]
    [InlineData("remove")]
    public async Task User_commands_preserve_history_and_map_reviewer_data(string operation)
    {
        DatabaseFriendlyGuidGenerator.SetDefaultGuidGenerationDatabase(Database.SqlServer);
        var featureId = AggregateId.FromDatabaseGuid(Guid.NewGuid());
        var reviewerId = FeatureReviewerId.FromDatabaseGuid(Guid.NewGuid());
        var memoryId = AggregateId.FromDatabaseGuid(SharedModule.Constants.MemoryAggregateIds.User);
        var added = new FeatureReviewerAddedV1(reviewerId, "Original", "Original instructions", ReviewLevel.Easy, Guid.Empty, memoryId);
        var store = new FeatureCommandEventStore(featureId, added);
        var handler = store.CreateHandler();
        FeatureCommand command = operation switch
        {
            "add" => new AddFeatureReviewerCommand(handler) { FeatureId = featureId.Value, Title = " Security ", Description = " Check access ", ReviewLevel = ReviewLevel.Heavy },
            "update" => new UpdateFeatureReviewerCommand(handler) { FeatureId = featureId.Value, ReviewerId = reviewerId.Value, Title = " Security ", Description = " Check access ", ReviewLevel = ReviewLevel.Heavy },
            _ => new RemoveFeatureReviewerCommand(handler) { FeatureId = featureId.Value, ReviewerId = reviewerId.Value }
        };
        command.UseUserOrigin();
        await command.Execute(new Executor { Id = EventExecutor.FromDatabaseGuid(Guid.NewGuid()) });

        Assert.Equal(1, store.WriteCount);
        var payload = Assert.Single(store.Written);
        var state = new FeatureStateData(featureId);
        if (operation != "add") added.Apply(state, payload.EventExecutionInfo);
        payload.EventData.Apply(state, payload.EventExecutionInfo);
        var dto = FeatureDto.FromStateData(state);
        Assert.All(dto.MemoryHistory, entry => Assert.True(entry.IsUserOriginated));
        if (operation == "remove") Assert.Empty(dto.Reviewers);
        else
        {
            var reviewer = Assert.Single(dto.Reviewers);
            Assert.Equal("Security", reviewer.Title);
            Assert.Equal("Check access", reviewer.Description);
            Assert.Equal(ReviewLevel.Heavy, reviewer.ReviewLevel);
        }
    }
}

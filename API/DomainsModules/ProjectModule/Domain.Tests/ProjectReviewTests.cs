using System.Collections.Immutable;
using System.Text.Json;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using ProjectModule.Domain.Events;
using ProjectModule.Domain.Models;
using ProjectModule.Domain.Validators;

namespace ProjectModule.Domain.Tests;

public sealed class ProjectReviewTests
{
    private static readonly AggregateId ProjectId = AggregateId.FromDatabaseGuid(
        Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")
    );
    private static readonly AggregateId SkillId = AggregateId.FromDatabaseGuid(
        Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd")
    );

    [Theory]
    [InlineData(false, null, true)]
    [InlineData(false, ReviewLevel.Easy, true)]
    [InlineData(true, ReviewLevel.Heavy, true)]
    [InlineData(true, null, false)]
    public void ReviewLevel_IsRequiredOnlyForSkillsUsedForReview(
        bool usedForReview,
        ReviewLevel? reviewLevel,
        bool expected
    )
    {
        var validator = new ProjectSkillReviewLevelRequiredValidator();
        var state = new ProjectStateData(ProjectId);

        Assert.All(
            new IEvent[]
            {
                new ProjectSkillRelationAddedV2(SkillId, usedForReview, "", reviewLevel, Guid.Empty, ProjectId),
                new ProjectSkillRelationUpdatedV1(SkillId, usedForReview, "", reviewLevel, Guid.Empty, ProjectId)
            },
            eventData => Assert.Equal(
                expected,
                validator.Validate(state, CreatePayload(eventData)).Succeded
            )
        );
    }

    [Theory]
    [InlineData("gpt-6.1-sol", "high", true)]
    [InlineData("gpt-6.1-sol", " ", false)]
    [InlineData("", "high", false)]
    public void ReviewLevelModels_NeedModelAndReasoning(
        string model,
        string reasoning,
        bool expected
    )
    {
        var result = new ProjectReviewLevelModelsMustBeCompleteValidator().Validate(
            new ProjectStateData(ProjectId),
            CreatePayload(
                new ProjectReviewLevelModelsSetV1(
                    ImmutableDictionary<ReviewLevel, ReviewModel>.Empty
                        .Add(ReviewLevel.Easy, new ReviewModel("gpt-6-luna", "low"))
                        .Add(ReviewLevel.Medium, new ReviewModel(model, reasoning)),
                    Guid.Empty,
                    ProjectId
                )
            )
        );

        Assert.Equal(expected, result.Succeded);
        if (!expected)
            Assert.Contains("Medium", result.FailureReason);
    }

    [Fact]
    public void ReviewEvents_RoundTripWithLevelNames()
    {
        var added = new ProjectSkillRelationAddedV2(
            SkillId, true, "Focus on RxJS.", ReviewLevel.Heavy, Guid.Empty, ProjectId
        );
        var modelsSet = new ProjectReviewLevelModelsSetV1(
            ImmutableDictionary<ReviewLevel, ReviewModel>.Empty
                .Add(ReviewLevel.Medium, new ReviewModel("gpt-6.1-sol", "high")),
            Guid.Empty,
            ProjectId
        );

        var addedJson = JsonSerializer.Serialize(added);
        var modelsJson = JsonSerializer.Serialize(modelsSet);

        Assert.Contains("\"Heavy\"", addedJson);
        Assert.Contains("\"Medium\"", modelsJson);
        Assert.Equal(added, JsonSerializer.Deserialize<ProjectSkillRelationAddedV2>(addedJson));
        Assert.Equal(
            new ReviewModel("gpt-6.1-sol", "high"),
            JsonSerializer.Deserialize<ProjectReviewLevelModelsSetV1>(modelsJson)
                .LevelModels[ReviewLevel.Medium]
        );
    }

    [Fact]
    public void SkillReview_IsSetByAddAndUpdateAndDroppedByRemove()
    {
        var state = new ProjectStateData(ProjectId);
        var info = CreatePayload(new ProjectSkillRelationRemovedV1(SkillId, Guid.Empty, ProjectId))
            .EventExecutionInfo;

        new ProjectSkillRelationAddedV1(SkillId, Guid.Empty, ProjectId).Apply(state, info);
        Assert.Empty(state.SkillReviews);

        new ProjectSkillRelationUpdatedV1(SkillId, true, "Notes", ReviewLevel.Medium, Guid.Empty, ProjectId)
            .Apply(state, info);
        Assert.Equal(
            new ProjectSkillReview(true, "Notes", ReviewLevel.Medium),
            state.SkillReviews[SkillId]
        );

        new ProjectSkillRelationRemovedV1(SkillId, Guid.Empty, ProjectId).Apply(state, info);
        Assert.Empty(state.RelatedSkillIds);
        Assert.Empty(state.SkillReviews);
    }

    private static EventPayload CreatePayload(IEvent eventData) =>
        EventPayload.Create(
            EventExecutor.FromDatabaseGuid(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
            ),
            ProjectId,
            "project-state-machine",
            eventData
        );
}

using System.Text.Json;
using EventSourcing.Shared.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Tests;

// Covers replay of released plan events that are now obsolete.
#pragma warning disable CS0618

public sealed class FeatureEventTests
{
    private static readonly AggregateId FeatureId = Id(
        "11111111-1111-1111-1111-111111111111"
    );
    private static readonly AggregateId ProjectId = Id(
        "22222222-2222-2222-2222-222222222222"
    );
    private static readonly AggregateId SkillId = Id(
        "33333333-3333-3333-3333-333333333333"
    );
    private static readonly AggregateId ParentFeatureId = Id(
        "88888888-8888-8888-8888-888888888888"
    );
    private static readonly FeatureRecordId RecordId =
        FeatureRecordId.FromDatabaseGuid(
            Guid.Parse("44444444-4444-4444-4444-444444444444")
        );
    private static readonly FeatureReviewNoteId ReviewNoteId =
        FeatureReviewNoteId.FromDatabaseGuid(
            Guid.Parse("99999999-9999-9999-9999-999999999999")
        );
    private static readonly FeatureResearchDiscoveryId DiscoveryId =
        FeatureResearchDiscoveryId.FromDatabaseGuid(
            Guid.Parse("77777777-7777-7777-7777-777777777777")
        );
    private static readonly FeaturePlanId FirstPlanId =
        FeaturePlanId.FromDatabaseGuid(
            Guid.Parse("55555555-5555-5555-5555-555555555555")
        );
    private static readonly FeaturePlanId SecondPlanId =
        FeaturePlanId.FromDatabaseGuid(
            Guid.Parse("66666666-6666-6666-6666-666666666666")
        );

    [Fact]
    public void Events_ApplyRequestedFeatureTransitions()
    {
        var executionInfo = new EventExecutionInfo
        {
            AggregateId = FeatureId,
            Timestamp = new DateTime(
                2026,
                8,
                22,
                12,
                0,
                0,
                DateTimeKind.Utc
            )
        };
        var state = new FeatureStateData(FeatureId);

        new FeatureAddedV1(
            ProjectId,
            "Feature journal",
            "Trace implementation discussions.",
            "Planning the initial implementation."
        ).Apply(state, executionInfo);
        new FeatureSkillAddedV1(SkillId).Apply(state, executionInfo);
        new FeatureStatusUpdatedV1(
            "Domain implementation is complete."
        ).Apply(state, executionInfo);
        new FeatureSummaryUpdatedV1(
            "Trace implementation decisions and outcomes."
        ).Apply(state, executionInfo);
        new FeatureRecordAddedV1(
            RecordId,
            "Why keep previous plans?",
            "They preserve the reasoning behind implementation changes."
        ).Apply(state, executionInfo);
        new FeatureRecordUpdatedV1(
            RecordId,
            "Why keep and switch previous plans?",
            "They preserve reasoning and allow an earlier approach to become current again."
        ).Apply(state, executionInfo);
        new FeatureReviewNoteAddedV1(
            ReviewNoteId,
            "Replay reloads state",
            "State replay is intentional."
        ).Apply(state, executionInfo);
        new FeatureReviewNoteUpdatedV1(
            ReviewNoteId,
            "Replay reloads state by design",
            "State replay is intentional and is not a defect."
        ).Apply(state, executionInfo);
        new FeatureResearchDiscoveryAddedV2(
            DiscoveryId,
            "YAML event selection",
            "Feature events are selected by YAML.",
            FeatureRecordSourceType.Code,
            "StateMachines/features.yaml"
        ).Apply(state, executionInfo);
        new FeatureResearchDiscoveryUpdatedV2(
            DiscoveryId,
            "YAML events and validators",
            "Feature events and validators are selected by YAML.",
            FeatureRecordSourceType.Code,
            "StateMachines/features.yaml"
        ).Apply(state, executionInfo);
        new FeaturePlanAddedV1(
            FirstPlanId,
            "First plan",
            "# First",
            FeaturePlanContentType.Markdown
        ).Apply(state, executionInfo);
        new FeaturePlanAddedV1(
            SecondPlanId,
            "Second plan",
            "<h1>Second</h1>",
            FeaturePlanContentType.Html
        ).Apply(state, executionInfo);
        new CurrentFeaturePlanChangedV1(FirstPlanId).Apply(
            state,
            executionInfo
        );
        new CurrentFeaturePlanUpdatedV1(
            "Updated first plan",
            "# Updated",
            FeaturePlanContentType.Markdown
        ).Apply(state, executionInfo);

        Assert.Equal(ProjectId, state.ProjectId);
        Assert.Equal("Feature journal", state.Name);
        Assert.Equal(
            "Trace implementation decisions and outcomes.",
            state.Summary
        );
        Assert.Equal("Domain implementation is complete.", state.Status);
        Assert.Equal(SkillId, Assert.Single(state.RelatedSkillIds));
        var record = state.Records.Single(item => item.Id == RecordId);
        Assert.Equal(FeatureRecordTypes.Conversation, record.Type);
        Assert.Equal("Why keep and switch previous plans?", record.Title);
        Assert.StartsWith(
            "## User\n\nWhy keep and switch previous plans?",
            record.Content
        );
        var reviewNote = Assert.Single(state.ReviewNotes);
        Assert.Equal("Replay reloads state by design", reviewNote.Title);
        Assert.Equal(
            "State replay is intentional and is not a defect.",
            reviewNote.Content
        );
        var discovery = state.Records.Single(
            item => item.Id == DiscoveryId.ToRecordId()
        );
        Assert.Equal(FeatureRecordTypes.Research, discovery.Type);
        Assert.Equal("YAML events and validators", discovery.Title);
        Assert.Equal(
            "Feature events and validators are selected by YAML.",
            discovery.Content
        );
        Assert.Equal(
            FeatureRecordSourceType.Code,
            discovery.SourceType
        );
        Assert.Equal(
            "StateMachines/features.yaml",
            discovery.SourceReference
        );
        Assert.Equal(FirstPlanId, state.CurrentPlanId);
        Assert.Equal(2, state.Plans.Count);
        Assert.Equal(
            "Updated first plan",
            state.Plans.Single(plan => plan.Id == FirstPlanId).Title
        );

        new FeaturePlanRemovedV1(FirstPlanId).Apply(state, executionInfo);
        new FeatureResearchDiscoveryRemovedV1(DiscoveryId).Apply(
            state,
            executionInfo
        );
        new FeatureReviewNoteRemovedV1(ReviewNoteId).Apply(
            state,
            executionInfo
        );
        new FeatureRecordRemovedV1(RecordId).Apply(state, executionInfo);
        new FeatureSkillRemovedV1(SkillId).Apply(state, executionInfo);
        new FeatureRemovedV1().Apply(state, executionInfo);

        Assert.Null(state.CurrentPlanId);
        Assert.Equal(SecondPlanId, Assert.Single(state.Plans).Id);
        Assert.Empty(state.ReviewNotes);
        Assert.Empty(state.Records);
        Assert.Empty(state.RelatedSkillIds);
        Assert.True(state.IsDeleted);
    }

    [Fact]
    public void FeatureParentSetV1_SetsAndClearsParentFeature()
    {
        var state = new FeatureStateData(FeatureId);
        var executionInfo = new EventExecutionInfo
        {
            AggregateId = FeatureId,
            Timestamp = DateTime.UtcNow
        };

        new FeatureParentSetV1(ParentFeatureId).Apply(
            state,
            executionInfo
        );

        Assert.Equal(ParentFeatureId, state.ParentFeatureId);

        new FeatureParentSetV1(null).Apply(state, executionInfo);

        Assert.Null(state.ParentFeatureId);
    }

    [Fact]
    public void AddedV1_ReplaysWithDeterministicLegacyTitle()
    {
        var state = new FeatureStateData(FeatureId);
        var executionInfo = new EventExecutionInfo
        {
            AggregateId = FeatureId,
            Timestamp = DateTime.UtcNow
        };

        new FeatureResearchDiscoveryAddedV1(
            DiscoveryId,
            "A discovery stored before titles existed.",
            FeatureRecordSourceType.Other,
            string.Empty
        ).Apply(state, executionInfo);

        Assert.Equal(
            "Untitled discovery",
            Assert.Single(state.Records).Title
        );
    }

    [Theory]
    [InlineData(FeatureRecordSourceType.Other, "\"Other\"")]
    [InlineData(FeatureRecordSourceType.Code, "\"Code\"")]
    [InlineData(FeatureRecordSourceType.Web, "\"Web\"")]
    [InlineData(FeatureRecordSourceType.Mcp, "\"Mcp\"")]
    public void ResearchDiscoverySourceType_SerializesAsApiName(
        FeatureRecordSourceType sourceType,
        string expectedJson
    )
    {
        Assert.Equal(expectedJson, JsonSerializer.Serialize(sourceType));
        Assert.Equal(
            sourceType,
            JsonSerializer.Deserialize<FeatureRecordSourceType>(
                expectedJson
            )
        );
    }

    private static AggregateId Id(string value) =>
        AggregateId.FromDatabaseGuid(Guid.Parse(value));
}

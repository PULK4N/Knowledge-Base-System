using System.Text.Json;
using EventSourcing.Shared.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Tests;

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
    private static readonly FeatureRecordId RecordId =
        FeatureRecordId.FromDatabaseGuid(
            Guid.Parse("44444444-4444-4444-4444-444444444444")
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
        new FeatureResearchDiscoveryAddedV1(
            DiscoveryId,
            "Feature events are selected by YAML.",
            FeatureResearchDiscoverySourceType.Code,
            "StateMachines/features.yaml"
        ).Apply(state, executionInfo);
        new FeatureResearchDiscoveryUpdatedV1(
            DiscoveryId,
            "Feature events and validators are selected by YAML.",
            FeatureResearchDiscoverySourceType.Code,
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
        Assert.Equal("Domain implementation is complete.", state.Status);
        Assert.Equal(SkillId, Assert.Single(state.RelatedSkillIds));
        var record = Assert.Single(state.Records);
        Assert.Equal("Why keep and switch previous plans?", record.UserMessage);
        var discovery = Assert.Single(state.ResearchDiscoveries);
        Assert.Equal(
            "Feature events and validators are selected by YAML.",
            discovery.Content
        );
        Assert.Equal(
            FeatureResearchDiscoverySourceType.Code,
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
        new FeatureRecordRemovedV1(RecordId).Apply(state, executionInfo);
        new FeatureSkillRemovedV1(SkillId).Apply(state, executionInfo);
        new FeatureRemovedV1().Apply(state, executionInfo);

        Assert.Null(state.CurrentPlanId);
        Assert.Equal(SecondPlanId, Assert.Single(state.Plans).Id);
        Assert.Empty(state.ResearchDiscoveries);
        Assert.Empty(state.Records);
        Assert.Empty(state.RelatedSkillIds);
        Assert.True(state.IsDeleted);
    }

    [Theory]
    [InlineData(FeatureResearchDiscoverySourceType.Other, "\"Other\"")]
    [InlineData(FeatureResearchDiscoverySourceType.Code, "\"Code\"")]
    [InlineData(FeatureResearchDiscoverySourceType.Web, "\"Web\"")]
    [InlineData(FeatureResearchDiscoverySourceType.Mcp, "\"Mcp\"")]
    public void ResearchDiscoverySourceType_SerializesAsApiName(
        FeatureResearchDiscoverySourceType sourceType,
        string expectedJson
    )
    {
        Assert.Equal(expectedJson, JsonSerializer.Serialize(sourceType));
        Assert.Equal(
            sourceType,
            JsonSerializer.Deserialize<FeatureResearchDiscoverySourceType>(
                expectedJson
            )
        );
    }

    private static AggregateId Id(string value) =>
        AggregateId.FromDatabaseGuid(Guid.Parse(value));
}

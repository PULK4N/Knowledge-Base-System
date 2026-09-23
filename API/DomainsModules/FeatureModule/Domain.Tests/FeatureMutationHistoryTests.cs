using System.Text.Json;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;
using FeatureModule.Domain.Validators;
using Shared.Interfaces;

namespace FeatureModule.Domain.Tests;

public sealed class FeatureMutationHistoryTests
{
    private static readonly AggregateId FeatureId = AggregateId.FromDatabaseGuid(Guid.NewGuid());
    private static readonly AggregateId ProjectId = AggregateId.FromDatabaseGuid(Guid.NewGuid());
    private static readonly AggregateId SkillId = AggregateId.FromDatabaseGuid(Guid.NewGuid());
    private static readonly AggregateId MemoryId = AggregateId.FromDatabaseGuid(Guid.NewGuid());
    private static readonly Guid SessionId = Guid.NewGuid();
    private static readonly FeatureRecordId RecordId = new(Guid.NewGuid());
    private static readonly FeatureReviewNoteId ReviewNoteId = new(Guid.NewGuid());
    private static readonly FeatureResearchDiscoveryId DiscoveryId = new(Guid.NewGuid());
    private static readonly FeaturePlanId PlanId = new(Guid.NewGuid());

    public static IEnumerable<object[]> Mutations()
    {
        yield return [new FeatureAddedV2(ProjectId, "name", "summary", "status", SessionId, MemoryId), new FeatureAddedV1(ProjectId, "name", "summary", "status")];
        yield return [new FeatureStatusUpdatedV2("new status", SessionId, MemoryId), new FeatureStatusUpdatedV1("new status")];
        yield return [new FeatureSummaryUpdatedV2("new summary", SessionId, MemoryId), new FeatureSummaryUpdatedV1("new summary")];
        yield return [new FeatureSkillAddedV2(SkillId, SessionId, MemoryId), new FeatureSkillAddedV1(SkillId)];
        yield return [new FeatureSkillRemovedV2(SkillId, SessionId, MemoryId), new FeatureSkillRemovedV1(SkillId)];
        yield return [new FeatureRecordAddedV2(RecordId, "user", "answer", SessionId, MemoryId), new FeatureRecordAddedV1(RecordId, "user", "answer")];
        yield return [new FeatureRecordUpdatedV2(RecordId, "updated user", "updated answer", SessionId, MemoryId), new FeatureRecordUpdatedV1(RecordId, "updated user", "updated answer")];
        yield return [new FeatureRecordRemovedV2(RecordId, SessionId, MemoryId), new FeatureRecordRemovedV1(RecordId)];
        yield return [new FeatureReviewNoteAddedV2(ReviewNoteId, "title", "content", SessionId, MemoryId), new FeatureReviewNoteAddedV1(ReviewNoteId, "title", "content")];
        yield return [new FeatureReviewNoteUpdatedV2(ReviewNoteId, "new title", "new content", SessionId, MemoryId), new FeatureReviewNoteUpdatedV1(ReviewNoteId, "new title", "new content")];
        yield return [new FeatureReviewNoteRemovedV2(ReviewNoteId, SessionId, MemoryId), new FeatureReviewNoteRemovedV1(ReviewNoteId)];
        yield return [new FeatureResearchDiscoveryAddedV3(DiscoveryId, "title", "content", FeatureResearchDiscoverySourceType.Code, "path", SessionId, MemoryId), new FeatureResearchDiscoveryAddedV2(DiscoveryId, "title", "content", FeatureResearchDiscoverySourceType.Code, "path")];
        yield return [new FeatureResearchDiscoveryUpdatedV3(DiscoveryId, "new title", "new content", FeatureResearchDiscoverySourceType.Web, "url", SessionId, MemoryId), new FeatureResearchDiscoveryUpdatedV2(DiscoveryId, "new title", "new content", FeatureResearchDiscoverySourceType.Web, "url")];
        yield return [new FeatureResearchDiscoveryRemovedV2(DiscoveryId, SessionId, MemoryId), new FeatureResearchDiscoveryRemovedV1(DiscoveryId)];
        yield return [new FeaturePlanAddedV2(PlanId, "title", "content", FeaturePlanContentType.Markdown, SessionId, MemoryId), new FeaturePlanAddedV1(PlanId, "title", "content", FeaturePlanContentType.Markdown)];
        yield return [new CurrentFeaturePlanUpdatedV2("new title", "new content", FeaturePlanContentType.Html, SessionId, MemoryId), new CurrentFeaturePlanUpdatedV1("new title", "new content", FeaturePlanContentType.Html)];
        yield return [new CurrentFeaturePlanChangedV2(PlanId, SessionId, MemoryId), new CurrentFeaturePlanChangedV1(PlanId)];
        yield return [new FeaturePlanRemovedV2(PlanId, SessionId, MemoryId), new FeaturePlanRemovedV1(PlanId)];
        yield return [new FeatureRemovedV2(SessionId, MemoryId), new FeatureRemovedV1()];
    }

    [Theory]
    [MemberData(nameof(Mutations))]
    public void Current_versions_round_trip_and_preserve_transitions_with_memory_history(
        IEvent current, IEvent legacy
    )
    {
        var eventType = current.GetType();
        var restored = (IEvent)JsonSerializer.Deserialize(
            JsonSerializer.Serialize(current, eventType), eventType
        )!;
        Assert.Equal(current, restored);
        var payload = CreatePayload(restored);
        var state = CreateState();
        var legacyState = CreateState();

        restored.Apply(state, payload.EventExecutionInfo);
        legacy.Apply(legacyState, payload.EventExecutionInfo);

        var history = Assert.Single(state.MemoryHistory);
        Assert.Equal(MemoryId, history.AggregateId);
        Assert.Equal(eventType.Name, history.EventName);
        Assert.Equal(payload.EventExecutionInfo.Timestamp, history.Timestamp);
        Assert.Empty(legacyState.MemoryHistory);
        state.MemoryHistory.Clear();
        Assert.Equal(JsonSerializer.Serialize(legacyState), JsonSerializer.Serialize(state));
    }

    [Theory]
    [MemberData(nameof(Mutations))]
    public void Relationship_validators_accept_current_versions_and_enforce_presence(
        IEvent current, IEvent legacy
    )
    {
        IPreEventValidator? validator = current switch
        {
            FeatureSkillAddedV2 => new FeatureSkillMustNotExistValidator(),
            FeatureSkillRemovedV2 => new FeatureSkillMustExistValidator(),
            FeatureRecordAddedV2 => new FeatureRecordMustNotExistValidator(),
            FeatureRecordUpdatedV2 or FeatureRecordRemovedV2 => new FeatureRecordMustExistValidator(),
            FeatureReviewNoteAddedV2 => new FeatureReviewNoteMustNotExistValidator(),
            FeatureReviewNoteUpdatedV2 or FeatureReviewNoteRemovedV2 => new FeatureReviewNoteMustExistValidator(),
            FeatureResearchDiscoveryAddedV3 => new FeatureResearchDiscoveryMustNotExistValidator(),
            FeatureResearchDiscoveryUpdatedV3 or FeatureResearchDiscoveryRemovedV2 => new FeatureResearchDiscoveryMustExistValidator(),
            FeaturePlanAddedV2 => new FeaturePlanMustNotExistValidator(),
            CurrentFeaturePlanChangedV2 or FeaturePlanRemovedV2 => new FeaturePlanMustExistValidator(),
            CurrentFeaturePlanUpdatedV2 => new CurrentFeaturePlanMustExistValidator(),
            _ => null
        };
        if (validator is null)
            return;

        var requiresAbsence = current is FeatureSkillAddedV2 or FeatureRecordAddedV2
            or FeatureReviewNoteAddedV2 or FeatureResearchDiscoveryAddedV3 or FeaturePlanAddedV2;
        Assert.Equal(!requiresAbsence, validator.Validate(CreateState(), CreatePayload(current)).Succeded);
        Assert.Equal(requiresAbsence, validator.Validate(new FeatureStateData(FeatureId), CreatePayload(current)).Succeded);
        Assert.Equal(
            validator.Validate(CreateState(), CreatePayload(legacy)).Succeded,
            validator.Validate(CreateState(), CreatePayload(current)).Succeded
        );
    }

    private static FeatureStateData CreateState() => new(FeatureId)
    {
        ProjectId = ProjectId,
        Name = "existing",
        RelatedSkillIds = [SkillId],
        Records = [new FeatureRecord { Id = RecordId }],
        ReviewNotes = [new FeatureReviewNote { Id = ReviewNoteId }],
        ResearchDiscoveries = [new FeatureResearchDiscovery { Id = DiscoveryId }],
        Plans = [new FeaturePlan { Id = PlanId }],
        CurrentPlanId = PlanId
    };

    private static EventPayload CreatePayload(IEvent eventData) => EventPayload.Create(
        EventExecutor.FromDatabaseGuid(Guid.NewGuid()), FeatureId, "features-state-machine", eventData
    );
}

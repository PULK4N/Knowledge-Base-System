using ActionModule.Shared.Models;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Application.Commands;
using FeatureModule.Application.DTOs;
using FeatureModule.Domain;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;
using FeatureModule.Domain.Validators;
using ProjectModule.Shared;
using UUIDNext;

namespace FeatureModule.Application.Tests;

public sealed class FeaturePlanFeatureCopyTests
{
    private static readonly AggregateId FeatureId = AggregateId.FromDatabaseGuid(Guid.NewGuid());
    private static readonly AggregateId MemoryId = AggregateId.FromDatabaseGuid(SharedModule.Constants.MemoryAggregateIds.User);
    private static readonly FeaturePlanId PlanId = FeaturePlanId.FromDatabaseGuid(Guid.NewGuid());
    private static readonly RequirementId Requirement = RequirementId.FromDatabaseGuid(Guid.NewGuid());
    private static readonly DefinitionOfDoneId Dod = DefinitionOfDoneId.FromDatabaseGuid(Guid.NewGuid());
    private static readonly PlaywrightScriptId Script = PlaywrightScriptId.FromDatabaseGuid(Guid.NewGuid());
    private static readonly FeatureReviewNoteId Note = FeatureReviewNoteId.FromDatabaseGuid(Guid.NewGuid());
    private static readonly FeatureReviewerId Reviewer = FeatureReviewerId.FromDatabaseGuid(Guid.NewGuid());

    [Fact]
    public async Task Completing_a_plan_copies_its_items_to_the_feature_with_new_ids_origins_numbers_and_links()
    {
        var history = PlanHistory();
        history.Add(new FeatureRequirementsAddedV1(null,
            [new(RequirementId.FromDatabaseGuid(Guid.NewGuid()), RequirementType.Functional, 6, "Earlier", "", "")],
            Guid.Empty, MemoryId));
        var store = new FeatureCommandEventStore(FeatureId, [.. history]);

        await Complete(store);

        var copy = Assert.IsType<FeaturePlanCopiedToFeatureV1>(store.Written.Last().EventData);
        var state = Replay(history, store.Written.Select(payload => payload.EventData));
        Assert.True(new FeaturePlanCopyToFeatureValidator().Validate(Replay(history, store.Written.SkipLast(1)
            .Select(payload => payload.EventData)), Payload(copy)).Succeded);
        var feature = SpecificationDto.FromModel(state.Specification);
        var requirement = feature.Requirements.Single(item => item.Origin is not null);
        var dod = Assert.Single(feature.DefinitionsOfDone);
        var script = Assert.Single(feature.PlaywrightScripts);
        Assert.Equal(("FR-7", new ItemOriginDto(PlanId.Value, Requirement.Value)), (requirement.Label, requirement.Origin));
        Assert.NotEqual(Requirement.Value, requirement.Id);
        Assert.Equal(("DOD-1", DefinitionOfDoneState.Confirmed, "Checked"), (dod.Label, dod.State, dod.StateDescription));
        Assert.Equal([requirement.Id], dod.RequirementIds);
        Assert.Equal([dod.Id], script.DefinitionOfDoneIds);
        Assert.True(script.HasSnapshot);
        Assert.Equal(Note.Value, Assert.Single(state.ReviewNotes).Origin!.PlanItemId);
        Assert.Equal(Reviewer.Value, Assert.Single(state.Reviewers).Origin!.PlanItemId);
        var plan = state.Plans.Single().Specification;
        Assert.Equal((Requirement, 1), (plan.Requirements.Single().Id, plan.Requirements.Single().Number));
    }

    [Fact]
    public async Task Completing_again_copies_only_items_added_since_and_links_them_to_earlier_copies()
    {
        var history = PlanHistory();
        var first = new FeatureCommandEventStore(FeatureId, [.. history]);
        await Complete(first);
        history.AddRange(first.Written.Select(payload => payload.EventData));
        var newScript = PlaywrightScriptId.FromDatabaseGuid(Guid.NewGuid());
        history.Add(new FeaturePlanStatusChangedV1(PlanId, FeaturePlanStatus.InProgress, Guid.Empty, MemoryId));
        history.Add(new FeaturePlaywrightScriptsAddedV1(PlanId, [new(newScript, "e2e/new.spec.ts", [Dod])], Guid.Empty, MemoryId));
        var second = new FeatureCommandEventStore(FeatureId, [.. history]);

        await Complete(second);

        var copy = Assert.IsType<FeaturePlanCopiedToFeatureV1>(second.Written.Last().EventData);
        Assert.Empty(copy.Requirements);
        Assert.Empty(copy.DefinitionsOfDone);
        Assert.Empty(copy.ReviewNotes);
        Assert.Empty(copy.Reviewers);
        var scriptCopy = Assert.Single(copy.Scripts);
        var state = Replay(history, []);
        Assert.Equal(newScript, scriptCopy.OriginId);
        Assert.Equal([state.Specification.DefinitionsOfDone.Single().Id], scriptCopy.DefinitionOfDoneIds.ToList());
        Assert.False(new FeaturePlanCopyToFeatureValidator().Validate(Replay(history, second.Written
            .Select(payload => payload.EventData)), Payload(copy)).Succeded);
    }

    private static async Task Complete(FeatureCommandEventStore store)
    {
        DatabaseFriendlyGuidGenerator.SetDefaultGuidGenerationDatabase(Database.SqlServer);
        var command = new ChangeFeaturePlanStatusCommand(store.CreateHandler())
        {
            FeatureId = FeatureId.Value,
            PlanId = PlanId.Value,
            Status = FeaturePlanStatus.Completed
        };
        command.UseUserOrigin();
        await command.Execute(new Executor { Id = EventExecutor.FromDatabaseGuid(Guid.NewGuid()) });
    }

    private static List<IEvent> PlanHistory() =>
    [
        new FeaturePlanAddedV4(PlanId, "Plan", "", FeaturePlanContentType.Markdown, "", FeaturePlanStatus.InProgress,
            "feature/plan", "", null, Guid.Empty, MemoryId),
        new FeatureRequirementsAddedV1(PlanId, [new(Requirement, RequirementType.Functional, 1, "Copy", "CONF-1", "")],
            Guid.Empty, MemoryId),
        new FeatureDefinitionsOfDoneAddedV1(PlanId, [new(Dod, 1, "Copied", [Requirement])], Guid.Empty, MemoryId),
        new FeatureDefinitionsOfDoneStateSetV1(PlanId, [new(Dod, DefinitionOfDoneState.Confirmed, "Checked")], Guid.Empty, MemoryId),
        new FeaturePlaywrightScriptsAddedV1(PlanId, [new(Script, "e2e/copy.spec.ts", [Dod])], Guid.Empty, MemoryId),
        new FeaturePlaywrightScriptSnapshotsSetV1(PlanId, [new(Script, "test()")], Guid.Empty, MemoryId),
        new FeaturePlanReviewNoteAddedV1(PlanId, Note, "Retries", "Intended", Guid.Empty, MemoryId),
        new FeaturePlanReviewerAddedV1(PlanId, Reviewer, "Security", "Check access", ReviewLevel.Heavy, Guid.Empty, MemoryId)
    ];

    private static FeatureStateData Replay(IEnumerable<IEvent> history, IEnumerable<IEvent> written)
    {
        var state = new FeatureStateData(FeatureId);
        foreach (var data in history.Concat(written))
            data.Apply(state, new EventExecutionInfo { AggregateId = FeatureId, EventName = data.GetType().Name });
        return state;
    }

    private static EventPayload Payload(IEvent data) =>
        EventPayload.Create(EventExecutor.FromDatabaseGuid(Guid.NewGuid()), FeatureId, "features-state-machine", data);
}

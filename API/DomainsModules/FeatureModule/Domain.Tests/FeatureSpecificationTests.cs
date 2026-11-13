using System.Collections.Immutable;
using System.Text.Json;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;
using FeatureModule.Domain.Validators;

namespace FeatureModule.Domain.Tests;

public sealed class FeatureSpecificationTests
{
    private static readonly AggregateId MemoryId = AggregateId.FromDatabaseGuid(Guid.NewGuid());
    private static readonly DateTime Start = new(2026, 11, 13, 10, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Serialized_events_replay_items_links_states_snapshots_and_retired_numbers(bool onPlan)
    {
        var (state, planId) = NewState(onPlan);
        var functional = RequirementId.FromDatabaseGuid(Guid.NewGuid());
        var technical = RequirementId.FromDatabaseGuid(Guid.NewGuid());
        var dod = DefinitionOfDoneId.FromDatabaseGuid(Guid.NewGuid());
        var script = PlaywrightScriptId.FromDatabaseGuid(Guid.NewGuid());

        Replay(state, new FeatureRequirementsAddedV1(planId, [
            new(functional, RequirementType.Functional, 1, "Copy notes", "CONF-1", "https://wiki/1"),
            new(technical, RequirementType.Technical, 1, "GUID IDs", "", "")], Guid.Empty, MemoryId));
        Replay(state, new FeatureRequirementsUpdatedV1(planId, [
            new(functional, RequirementType.NonFunctional, 1, "Fast copy", "CONF-1", "https://wiki/1")], Guid.Empty, MemoryId));
        Replay(state, new FeatureDefinitionsOfDoneAddedV1(planId, [new(dod, 1, "Notes on feature", [functional, technical])],
            Guid.Empty, MemoryId));
        Replay(state, new FeatureDefinitionsOfDoneStateSetV1(planId, [new(dod, DefinitionOfDoneState.Postponed, "Needs API")],
            Guid.Empty, MemoryId), Start.AddHours(1));
        Replay(state, new FeaturePlaywrightScriptsAddedV1(planId, [new(script, "e2e/a.spec.ts", [dod])], Guid.Empty, MemoryId));
        Replay(state, new FeaturePlaywrightScriptSnapshotsSetV1(planId, [new(script, "test()")], Guid.Empty, MemoryId),
            Start.AddHours(2));

        var specification = state.SpecificationOf(planId);
        var requirement = specification.Requirements.Single(item => item.Id == functional);
        Assert.Equal((RequirementType.NonFunctional, 1, "Fast copy", "CONF-1"),
            (requirement.Type, requirement.Number, requirement.Description, requirement.ExternalId));
        Assert.Equal(1, specification.LastRequirementNumber(RequirementType.Functional));
        var definitionOfDone = Assert.Single(specification.DefinitionsOfDone);
        Assert.Equal((DefinitionOfDoneState.Postponed, "Needs API", Start.AddHours(1)),
            (definitionOfDone.State, definitionOfDone.StateDescription, definitionOfDone.StateCheckedAt));
        Assert.Equal(new ScriptSnapshot("test()", Start.AddHours(2)), Assert.Single(specification.PlaywrightScripts).Snapshot);
        Assert.Equal(onPlan, state.Specification.Requirements.Count == 0);

        Replay(state, new FeatureRequirementsRemovedV1(planId, [technical], Guid.Empty, MemoryId));
        Assert.Equal([functional], definitionOfDone.RequirementIds);
        Replay(state, new FeatureDefinitionsOfDoneRemovedV1(planId, [dod], Guid.Empty, MemoryId));
        Assert.Empty(specification.PlaywrightScripts[0].DefinitionOfDoneIds);
        Assert.Equal((1, 1), (specification.LastRequirementNumber(RequirementType.Technical), specification.LastDefinitionOfDoneNumber));
        Replay(state, new FeaturePlaywrightScriptSnapshotsSetV1(planId, [new(script, null)], Guid.Empty, MemoryId));
        Assert.Null(specification.PlaywrightScripts[0].Snapshot);
        Assert.Equal(9, state.MemoryHistory.Count);
    }

    [Theory]
    [InlineData(RequirementType.Functional, 3, true)]
    [InlineData(RequirementType.Functional, 2, false)]
    [InlineData(RequirementType.Business, 1, true)]
    [InlineData((RequirementType)99, 1, false)]
    public void Added_requirements_need_an_unused_number_of_a_known_type(RequirementType type, int number, bool expected)
    {
        var (state, planId) = NewState(true);
        var specification = state.SpecificationOf(planId);
        specification.UseRequirementNumber(RequirementType.Functional, 2);

        Assert.Equal(expected, Validate(new FeatureRequirementsValidator(), state,
            new FeatureRequirementsAddedV1(planId, [new(RequirementId.FromDatabaseGuid(Guid.NewGuid()), type, number, "Description", "", "")],
                Guid.Empty, MemoryId)));
    }

    [Theory]
    [InlineData(RequirementType.Functional, 1, true)]
    [InlineData(RequirementType.Functional, 4, false)]
    [InlineData(RequirementType.Technical, 1, true)]
    [InlineData(RequirementType.Technical, 3, true)]
    [InlineData(RequirementType.Business, 1, false)]
    public void Updated_requirement_keeps_its_number_unless_the_type_changes(RequirementType type, int number, bool expected)
    {
        var (state, planId) = NewState(true);
        var specification = state.SpecificationOf(planId);
        var id = RequirementId.FromDatabaseGuid(Guid.NewGuid());
        specification.Requirements.Add(new Requirement { Id = id, Type = RequirementType.Functional, Number = 1 });
        specification.UseRequirementNumber(RequirementType.Functional, 3);
        specification.UseRequirementNumber(RequirementType.Business, 1);

        Assert.Equal(expected, Validate(new FeatureRequirementsValidator(), state,
            new FeatureRequirementsUpdatedV1(planId, [new(id, type, number, "Description", "", "")], Guid.Empty, MemoryId)));
    }

    [Theory]
    [InlineData("duplicate-ids")]
    [InlineData("duplicate-numbers")]
    [InlineData("blank-description")]
    [InlineData("empty-list")]
    [InlineData("missing-plan")]
    public void Requirement_batches_reject_invalid_entries(string error)
    {
        var (state, planId) = NewState(true);
        var id = RequirementId.FromDatabaseGuid(Guid.NewGuid());
        RequirementEntry Entry(int number, RequirementId? requirementId = null) => new(
            requirementId ?? RequirementId.FromDatabaseGuid(Guid.NewGuid()), RequirementType.Functional, number,
            error == "blank-description" ? " " : "Description", "", "");
        ImmutableArray<RequirementEntry> entries = error switch
        {
            "duplicate-ids" => [Entry(1, id), Entry(2, id)],
            "duplicate-numbers" => [Entry(1), Entry(1)],
            "empty-list" => [],
            _ => [Entry(1)]
        };
        var target = error == "missing-plan" ? FeaturePlanId.FromDatabaseGuid(Guid.NewGuid()) : planId;

        Assert.False(Validate(new FeatureRequirementsValidator(), state,
            new FeatureRequirementsAddedV1(target, entries, Guid.Empty, MemoryId)));
    }

    [Theory]
    [InlineData("valid", true)]
    [InlineData("other-specification", false)]
    [InlineData("duplicate-link", false)]
    [InlineData("used-number", false)]
    [InlineData("blank-description", false)]
    public void Definitions_of_done_link_requirements_of_the_same_specification(string variant, bool expected)
    {
        var (state, planId) = NewState(true);
        var own = RequirementId.FromDatabaseGuid(Guid.NewGuid());
        var featureRequirement = RequirementId.FromDatabaseGuid(Guid.NewGuid());
        state.SpecificationOf(planId).Requirements.Add(new Requirement { Id = own });
        state.SpecificationOf(planId).UseDefinitionOfDoneNumber(1);
        state.Specification.Requirements.Add(new Requirement { Id = featureRequirement });
        ImmutableArray<RequirementId> links = variant switch
        {
            "other-specification" => [featureRequirement],
            "duplicate-link" => [own, own],
            _ => [own]
        };

        Assert.Equal(expected, Validate(new FeatureDefinitionsOfDoneValidator(), state,
            new FeatureDefinitionsOfDoneAddedV1(planId, [new(DefinitionOfDoneId.FromDatabaseGuid(Guid.NewGuid()), variant == "used-number" ? 1 : 2,
                variant == "blank-description" ? "" : "Done", links)], Guid.Empty, MemoryId)));
    }

    [Theory]
    [InlineData(DefinitionOfDoneState.Confirmed, true, true)]
    [InlineData(DefinitionOfDoneState.Failed, true, true)]
    [InlineData((DefinitionOfDoneState)99, true, false)]
    [InlineData(DefinitionOfDoneState.Confirmed, false, false)]
    public void Definition_of_done_state_needs_a_known_state_of_an_existing_item(
        DefinitionOfDoneState newState, bool exists, bool expected)
    {
        var (state, planId) = NewState(false);
        var id = DefinitionOfDoneId.FromDatabaseGuid(Guid.NewGuid());
        if (exists)
            state.Specification.DefinitionsOfDone.Add(new DefinitionOfDone { Id = id });

        Assert.Equal(expected, Validate(new FeatureDefinitionsOfDoneValidator(), state,
            new FeatureDefinitionsOfDoneStateSetV1(planId, [new(id, newState, "")], Guid.Empty, MemoryId)));
    }

    [Theory]
    [InlineData("e2e/a.spec.ts", true, true)]
    [InlineData("e2e/a.spec.ts", false, false)]
    [InlineData(" ", true, false)]
    public void Scripts_need_a_path_and_existing_definitions_of_done(string path, bool linkExists, bool expected)
    {
        var (state, planId) = NewState(true);
        var dod = DefinitionOfDoneId.FromDatabaseGuid(Guid.NewGuid());
        if (linkExists)
            state.SpecificationOf(planId).DefinitionsOfDone.Add(new DefinitionOfDone { Id = dod });
        state.SpecificationOf(planId).PlaywrightScripts.Add(new PlaywrightScript { Path = path });

        Assert.Equal(expected, Validate(new FeaturePlaywrightScriptsValidator(), state,
            new FeaturePlaywrightScriptsAddedV1(planId, [new(PlaywrightScriptId.FromDatabaseGuid(Guid.NewGuid()), path, [dod])], Guid.Empty, MemoryId)));
    }

    [Theory]
    [InlineData(FeaturePlanStatus.Completed, new DefinitionOfDoneState[0], true)]
    [InlineData(FeaturePlanStatus.Completed, new[] { DefinitionOfDoneState.Confirmed }, true)]
    [InlineData(FeaturePlanStatus.Completed, new[] { DefinitionOfDoneState.Confirmed, DefinitionOfDoneState.NotConfirmed }, false)]
    [InlineData(FeaturePlanStatus.Completed, new[] { DefinitionOfDoneState.Failed }, false)]
    [InlineData(FeaturePlanStatus.Completed, new[] { DefinitionOfDoneState.Postponed }, false)]
    [InlineData(FeaturePlanStatus.InProgress, new[] { DefinitionOfDoneState.Failed }, true)]
    public void Plan_completes_only_when_every_definition_of_done_is_confirmed(
        FeaturePlanStatus status, DefinitionOfDoneState[] states, bool expected)
    {
        var (state, planId) = NewState(true);
        foreach (var (dodState, index) in states.Select((item, index) => (item, index)))
            state.SpecificationOf(planId).DefinitionsOfDone.Add(new DefinitionOfDone
            {
                Id = DefinitionOfDoneId.FromDatabaseGuid(Guid.NewGuid()), Number = index + 1, State = dodState
            });
        var validator = new FeaturePlanDefinitionsOfDoneMustBeConfirmedValidator();

        Assert.Equal(expected, Validate(validator, state, new FeaturePlanStatusChangedV1(planId!.Value, status, Guid.Empty, MemoryId)));
        Assert.Equal(expected, Validate(validator, state, new FeaturePlanUpdatedV1(planId.Value, "Plan", "", FeaturePlanContentType.Markdown,
            "", status, Guid.Empty, MemoryId)));
    }

    [Fact]
    public void Moving_definitions_of_done_carries_linked_requirements_and_scripts_and_renumbers_them()
    {
        var (state, sourceId) = NewState(true);
        var target = new FeaturePlan { Id = FeaturePlanId.FromDatabaseGuid(Guid.NewGuid()) };
        state.Plans.Add(target);
        target.Specification.UseRequirementNumber(RequirementType.Functional, 4);
        target.Specification.UseDefinitionOfDoneNumber(2);
        var shared = RequirementId.FromDatabaseGuid(Guid.NewGuid());
        var own = RequirementId.FromDatabaseGuid(Guid.NewGuid());
        var moved = DefinitionOfDoneId.FromDatabaseGuid(Guid.NewGuid());
        var stays = DefinitionOfDoneId.FromDatabaseGuid(Guid.NewGuid());
        var both = PlaywrightScriptId.FromDatabaseGuid(Guid.NewGuid());
        var other = PlaywrightScriptId.FromDatabaseGuid(Guid.NewGuid());
        Replay(state, new FeatureRequirementsAddedV1(sourceId, [
            new(shared, RequirementType.Functional, 1, "Shared", "", ""),
            new(own, RequirementType.Functional, 2, "Own", "", "")], Guid.Empty, MemoryId));
        Replay(state, new FeatureDefinitionsOfDoneAddedV1(sourceId, [
            new(moved, 1, "Moved", [shared]), new(stays, 2, "Stays", [shared, own])], Guid.Empty, MemoryId));
        Replay(state, new FeaturePlaywrightScriptsAddedV1(sourceId, [
            new(both, "e2e/both.spec.ts", [moved, stays]), new(other, "e2e/other.spec.ts", [stays])], Guid.Empty, MemoryId));
        var move = new FeatureDefinitionsOfDoneMovedV1(sourceId!.Value, target.Id, [new(moved, 3)], [new(shared, 5)], [both],
            Guid.Empty, MemoryId);
        var validator = new FeatureDefinitionsOfDoneMoveValidator();

        Assert.True(Validate(validator, state, move));
        Assert.False(Validate(validator, state, move with { ScriptIds = [] }));
        Assert.False(Validate(validator, state, move with { Requirements = [] }));
        Assert.False(Validate(validator, state, move with { DefinitionsOfDone = [new(moved, 2)] }));
        Assert.False(Validate(validator, state, move with { TargetPlanId = sourceId.Value }));
        target.Status = FeaturePlanStatus.Completed;
        Assert.False(Validate(validator, state, move));
        target.Status = FeaturePlanStatus.InProgress;

        Replay(state, move);
        var source = state.SpecificationOf(sourceId);
        Assert.Equal([own], source.Requirements.Select(item => item.Id));
        Assert.Equal([own], Assert.Single(source.DefinitionsOfDone).RequirementIds);
        Assert.Equal([other], source.PlaywrightScripts.Select(item => item.Id));
        Assert.Equal((5, 3), (target.Specification.Requirements.Single().Number, target.Specification.DefinitionsOfDone.Single().Number));
        Assert.Equal([moved], target.Specification.PlaywrightScripts.Single().DefinitionOfDoneIds);
        Assert.Equal((2, 2), (source.LastRequirementNumber(RequirementType.Functional), source.LastDefinitionOfDoneNumber));
    }

    [Fact]
    public void Plan_review_notes_and_reviewers_replay_and_validate_against_their_plan()
    {
        var (state, planId) = NewState(true);
        var plan = planId!.Value;
        var noteId = FeatureReviewNoteId.FromDatabaseGuid(Guid.NewGuid());
        var reviewerId = FeatureReviewerId.FromDatabaseGuid(Guid.NewGuid());
        var notes = new FeaturePlanReviewNoteValidator();
        var reviewers = new FeaturePlanReviewerValidator();
        var addNote = new FeaturePlanReviewNoteAddedV1(plan, noteId, "Retries", "Intended", Guid.Empty, MemoryId);
        var addReviewer = new FeaturePlanReviewerAddedV1(plan, reviewerId, "Security", "Check access",
            ProjectModule.Shared.ReviewLevel.Heavy, Guid.Empty, MemoryId);

        Assert.True(Validate(notes, state, addNote));
        Assert.False(Validate(notes, state, addNote with { PlanId = FeaturePlanId.FromDatabaseGuid(Guid.NewGuid()) }));
        Assert.False(Validate(notes, state, addNote with { Content = " " }));
        Assert.False(Validate(notes, state, new FeaturePlanReviewNoteRemovedV1(plan, noteId, Guid.Empty, MemoryId)));
        Assert.True(Validate(reviewers, state, addReviewer));
        Assert.False(Validate(reviewers, state, addReviewer with { ReviewLevel = (ProjectModule.Shared.ReviewLevel)99 }));

        Replay(state, addNote);
        Replay(state, addReviewer);
        Assert.False(Validate(notes, state, addNote));
        Replay(state, new FeaturePlanReviewNoteUpdatedV1(plan, noteId, "Retries are intended", "By design", Guid.Empty, MemoryId),
            Start.AddHours(1));
        Replay(state, new FeaturePlanReviewerUpdatedV1(plan, reviewerId, "Auth", "Every endpoint",
            ProjectModule.Shared.ReviewLevel.Medium, Guid.Empty, MemoryId));

        var featurePlan = state.Plans.Single();
        Assert.Equal(("Retries are intended", "By design", Start, Start.AddHours(1)),
            (featurePlan.ReviewNotes[0].Title, featurePlan.ReviewNotes[0].Content, featurePlan.ReviewNotes[0].CreatedAt,
                featurePlan.ReviewNotes[0].UpdatedAt));
        Assert.Equal(("Auth", ProjectModule.Shared.ReviewLevel.Medium),
            (featurePlan.Reviewers[0].Title, featurePlan.Reviewers[0].ReviewLevel));
        Assert.Empty(state.ReviewNotes);

        Replay(state, new FeaturePlanReviewNoteRemovedV1(plan, noteId, Guid.Empty, MemoryId));
        Replay(state, new FeaturePlanReviewerRemovedV1(plan, reviewerId, Guid.Empty, MemoryId));
        Assert.Empty(featurePlan.ReviewNotes);
        Assert.Empty(featurePlan.Reviewers);
    }

    private static (FeatureStateData State, FeaturePlanId? PlanId) NewState(bool onPlan)
    {
        var state = new FeatureStateData(AggregateId.FromDatabaseGuid(Guid.NewGuid()));
        if (!onPlan)
            return (state, null);
        var plan = new FeaturePlan { Id = FeaturePlanId.FromDatabaseGuid(Guid.NewGuid()) };
        state.Plans.Add(plan);
        return (state, plan.Id);
    }

    private static bool Validate(Shared.Interfaces.IPreEventValidator validator, FeatureStateData state, IEvent data) =>
        validator.Validate(state, EventPayload.Create(EventExecutor.FromDatabaseGuid(Guid.NewGuid()), state.Id,
            "features-state-machine", data)).Succeded;

    private static void Replay(FeatureStateData state, IEvent data, DateTime? timestamp = null)
    {
        var replayed = (IEvent)JsonSerializer.Deserialize(JsonSerializer.Serialize(data, data.GetType()), data.GetType())!;
        replayed.Apply(state, new EventExecutionInfo
        {
            AggregateId = state.Id,
            EventName = data.GetType().Name,
            Timestamp = timestamp ?? Start
        });
    }
}

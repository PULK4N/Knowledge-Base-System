using ActionModule.Shared.Models;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Application.Commands;
using FeatureModule.Application.DTOs;
using FeatureModule.Application.Models;
using FeatureModule.Domain;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;
using UUIDNext;

namespace FeatureModule.Application.Tests;

public sealed class FeatureSpecificationCommandTests
{
    private static readonly AggregateId FeatureId = AggregateId.FromDatabaseGuid(Guid.NewGuid());
    private static readonly AggregateId MemoryId = AggregateId.FromDatabaseGuid(SharedModule.Constants.MemoryAggregateIds.User);
    private static readonly FeaturePlanId PlanId = FeaturePlanId.FromDatabaseGuid(Guid.NewGuid());
    private static readonly RequirementId ExistingRequirement = RequirementId.FromDatabaseGuid(Guid.NewGuid());

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Added_requirements_continue_numbering_per_type(bool onPlan)
    {
        var (store, history) = Store(onPlan);
        var result = (FeatureSpecificationItemsCreatedCommandResult)await Run(
            new AddFeatureRequirementsCommand(store.CreateHandler())
            {
                FeatureId = FeatureId.Value,
                PlanId = onPlan ? PlanId.Value : null,
                Requirements =
                [
                    new(RequirementType.Functional, " Second "),
                    new(RequirementType.Business, "Goal", " CONF-7 ", " https://wiki "),
                    new(RequirementType.Functional, "Third")
                ]
            });

        Assert.Equal(["FR-3", "BR-1", "FR-4"], result.Items.Select(item => item.Label));
        var specification = Replay(history, store).SpecificationOf(onPlan ? PlanId : null);
        var business = specification.Requirements.Single(item => item.Type == RequirementType.Business);
        Assert.Equal(("Goal", "CONF-7", "https://wiki"), (business.Description, business.ExternalId, business.Source));
        Assert.Equal("Second", specification.Requirements.Single(item => item.Number == 3).Description);
    }

    [Theory]
    [InlineData(null, 1)]
    [InlineData(RequirementType.Functional, 1)]
    [InlineData(RequirementType.Technical, 1)]
    [InlineData(RequirementType.NonFunctional, 1)]
    public async Task Requirement_update_keeps_omitted_values_and_renumbers_only_on_type_change(
        RequirementType? newType, int expectedNumber)
    {
        var (store, history) = Store(true);
        await Run(new UpdateFeatureRequirementsCommand(store.CreateHandler())
        {
            FeatureId = FeatureId.Value,
            PlanId = PlanId.Value,
            Requirements = [new(ExistingRequirement.Value, newType, Description: "Changed")]
        });

        var requirement = Replay(history, store).SpecificationOf(PlanId).Requirements
            .Single(item => item.Id == ExistingRequirement);
        Assert.Equal((newType ?? RequirementType.Functional, expectedNumber, "Changed", "CONF-1"),
            (requirement.Type, requirement.Number, requirement.Description, requirement.ExternalId));
        Assert.Equal(2, Replay(history, store).SpecificationOf(PlanId).LastRequirementNumber(RequirementType.Functional));
    }

    [Fact]
    public async Task Definitions_of_done_and_scripts_are_numbered_linked_and_snapshotted_in_one_write()
    {
        var (store, history) = Store(true);
        var dods = (FeatureSpecificationItemsCreatedCommandResult)await Run(
            new AddFeatureDefinitionsOfDoneCommand(store.CreateHandler())
            {
                FeatureId = FeatureId.Value,
                PlanId = PlanId.Value,
                DefinitionsOfDone = [new("Copies notes", [ExistingRequirement.Value]), new("Blocks completion")]
            });
        Assert.Equal(["DOD-1", "DOD-2"], dods.Items.Select(item => item.Label));
        history.AddRange(store.Written.Select(payload => payload.EventData));

        var writesBefore = store.WriteCount;
        await Run(new AddFeaturePlaywrightScriptsCommand(store.CreateHandler())
        {
            FeatureId = FeatureId.Value,
            PlanId = PlanId.Value,
            Scripts = [new("e2e/a.spec.ts", [dods.Items[0].Id], "test()"), new("e2e/b.spec.ts")]
        });

        Assert.Equal(writesBefore + 1, store.WriteCount);
        Assert.Equal([nameof(FeaturePlaywrightScriptsAddedV1), nameof(FeaturePlaywrightScriptSnapshotsSetV1)],
            store.Written.Select(payload => payload.EventData.GetType().Name));
        var dto = SpecificationDto.FromModel(Replay(history, store).SpecificationOf(PlanId));
        Assert.Equal([true, false], dto.PlaywrightScripts.Select(script => script.HasSnapshot));
        Assert.Equal(
            ["DOD-2 fulfils no requirement", "DOD-2 has no Playwright script", "e2e/b.spec.ts confirms no definition of done"],
            dto.Warnings);
    }

    [Fact]
    public async Task Moving_assigns_next_target_numbers_and_reports_previous_and_new_labels()
    {
        var target = FeaturePlanId.FromDatabaseGuid(Guid.NewGuid());
        var dod = DefinitionOfDoneId.FromDatabaseGuid(Guid.NewGuid());
        var script = PlaywrightScriptId.FromDatabaseGuid(Guid.NewGuid());
        var (_, history) = Store(true);
        history.AddRange(new List<IEvent>
        {
            new FeaturePlanAddedV4(target, "Next", "", FeaturePlanContentType.Markdown, "", FeaturePlanStatus.NotStarted,
                "feature/next", "", PlanId, Guid.Empty, MemoryId),
            new FeatureRequirementsAddedV1(target, [new(RequirementId.FromDatabaseGuid(Guid.NewGuid()), RequirementType.Functional, 1, "Taken", "", "")],
                Guid.Empty, MemoryId),
            new FeatureDefinitionsOfDoneAddedV1(PlanId, [new(dod, 1, "Cannot confirm yet", [ExistingRequirement])], Guid.Empty, MemoryId),
            new FeaturePlaywrightScriptsAddedV1(PlanId, [new(script, "e2e/a.spec.ts", [dod])], Guid.Empty, MemoryId)
        });
        var store = new FeatureCommandEventStore(FeatureId, [.. history]);

        var result = (FeatureSpecificationItemsMovedCommandResult)await Run(new MoveFeatureDefinitionsOfDoneCommand(store.CreateHandler())
        {
            FeatureId = FeatureId.Value,
            PlanId = PlanId.Value,
            TargetPlanId = target.Value,
            DefinitionOfDoneIds = [dod.Value]
        });

        Assert.Equal([("FR-1", "FR-2"), ("DOD-1", "DOD-1")], result.Items.Select(item => (item.PreviousLabel, item.Label)));
        var moved = Assert.IsType<FeatureDefinitionsOfDoneMovedV1>(Assert.Single(store.Written).EventData);
        Assert.Equal([script], moved.ScriptIds.ToList());
    }

    private static async Task<object> Run(FeatureCommand command)
    {
        command.UseUserOrigin();
        return await command.Execute(new Executor { Id = EventExecutor.FromDatabaseGuid(Guid.NewGuid()) });
    }

    /// <summary>A feature with one plan holding FR-1 (number 2 already retired).</summary>
    private static (FeatureCommandEventStore Store, List<IEvent> History) Store(bool onPlan)
    {
        DatabaseFriendlyGuidGenerator.SetDefaultGuidGenerationDatabase(Database.SqlServer);
        var planScope = onPlan ? PlanId : (FeaturePlanId?)null;
        List<IEvent> history =
        [
            new FeaturePlanAddedV4(PlanId, "Plan", "", FeaturePlanContentType.Markdown, "", FeaturePlanStatus.InProgress,
                "feature/plan", "", null, Guid.Empty, MemoryId),
            new FeatureRequirementsAddedV1(planScope, [
                new(ExistingRequirement, RequirementType.Functional, 1, "First", "CONF-1", ""),
                new(RequirementId.FromDatabaseGuid(Guid.NewGuid()), RequirementType.Functional, 2, "Retired", "", "")],
                Guid.Empty, MemoryId),
        ];
        var retired = ((FeatureRequirementsAddedV1)history[1]).Requirements[1].Id;
        history.Add(new FeatureRequirementsRemovedV1(planScope, [retired], Guid.Empty, MemoryId));
        return (new FeatureCommandEventStore(FeatureId, [.. history]), history);
    }

    private static FeatureStateData Replay(List<IEvent> history, FeatureCommandEventStore store)
    {
        var state = new FeatureStateData(FeatureId);
        foreach (var data in history.Concat(store.Written.Select(payload => payload.EventData)))
            data.Apply(state, new EventExecutionInfo { AggregateId = FeatureId, EventName = data.GetType().Name });
        return state;
    }
}

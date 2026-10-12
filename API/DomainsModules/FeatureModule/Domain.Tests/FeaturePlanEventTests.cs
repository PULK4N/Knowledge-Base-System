using System.Text.Json;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;
using FeatureModule.Domain.Validators;
using Shared.Interfaces;

namespace FeatureModule.Domain.Tests;

public sealed class FeaturePlanEventTests
{
    private const string Root = "feature/plans";
    private const string Api = "feature/plans-api";
    private const string ApiTests = "feature/plans-api-tests";
    private const string Ui = "feature/plans-ui";

    private static readonly AggregateId FeatureId = AggregateId.FromDatabaseGuid(Guid.NewGuid());
    private static readonly AggregateId MemoryId = AggregateId.FromDatabaseGuid(Guid.NewGuid());
    private static readonly Guid SessionId = Guid.NewGuid();
    private static readonly FeaturePlanId PlanId = new(Guid.NewGuid());

    public static IEnumerable<object[]> Events()
    {
        yield return [new FeaturePlanAddedV3(PlanId, "Research", "content", FeaturePlanContentType.Markdown, "Research only.", FeaturePlanStatus.NotStarted, Root, "Plan storage rework", SessionId, MemoryId), false];
        yield return [new FeaturePlanUpdatedV1(PlanId, "Implementation", "new content", FeaturePlanContentType.Html, "Production.", FeaturePlanStatus.InProgress, SessionId, MemoryId), true];
        yield return [new FeaturePlanStatusChangedV1(PlanId, FeaturePlanStatus.Delayed, SessionId, MemoryId), true];
        yield return [new FeaturePlanBranchAddedV1(PlanId, "feature/new", "New work", Root, SessionId, MemoryId), true];
        yield return [new FeaturePlanBranchUpdatedV1(PlanId, Api, "feature/plans-backend", "Backend", SessionId, MemoryId), true];
        yield return [new FeaturePlanBranchRemovedV1(PlanId, Api, SessionId, MemoryId), true];
    }

    [Theory]
    [MemberData(nameof(Events))]
    public void Events_round_trip_and_record_memory_history(IEvent eventData, bool requiresPlan)
    {
        var eventType = eventData.GetType();
        var restored = (IEvent)JsonSerializer.Deserialize(
            JsonSerializer.Serialize(eventData, eventType), eventType
        )!;
        var state = requiresPlan ? StateWithPlan() : new FeatureStateData(FeatureId);

        restored.Apply(state, Payload(restored).EventExecutionInfo);

        Assert.Equal(eventData, restored);
        Assert.Equal(MemoryId, Assert.Single(state.MemoryHistory).AggregateId);
    }

    [Theory]
    [MemberData(nameof(Events))]
    public void Plan_validators_require_plan_presence_from_event(IEvent eventData, bool requiresPlan)
    {
        IPreEventValidator validator = requiresPlan
            ? new FeaturePlanMustExistValidator()
            : new FeaturePlanMustNotExistValidator();

        Assert.Equal(requiresPlan, validator.Validate(StateWithPlan(), Payload(eventData)).Succeded);
        Assert.Equal(!requiresPlan, validator.Validate(new FeatureStateData(FeatureId), Payload(eventData)).Succeded);
    }

    [Fact]
    public void Added_v3_stores_plan_details_and_base_branch_without_current_selection()
    {
        var state = new FeatureStateData(FeatureId);
        var eventData = (IEvent)Events().First()[0];

        eventData.Apply(state, Payload(eventData).EventExecutionInfo);

        var plan = Assert.Single(state.Plans);
        Assert.Equal(("Research only.", FeaturePlanStatus.NotStarted), (plan.AdditionalInfo, plan.Status));
        Assert.Equal((Root, "Plan storage rework"), (plan.Branch!.Name, plan.Branch.Description));
        Assert.Empty(plan.Branch.SubBranches);
#pragma warning disable CS0618
        Assert.Null(state.CurrentPlanId);
#pragma warning restore CS0618
    }

    [Fact]
    public void Updated_v1_replaces_plan_details_and_keeps_branches()
    {
        var state = StateWithPlan();
        var eventData = new FeaturePlanUpdatedV1(PlanId, "Implementation", "new content", FeaturePlanContentType.Html, "Production.", FeaturePlanStatus.InProgress, SessionId, MemoryId);

        eventData.Apply(state, Payload(eventData).EventExecutionInfo);

        var plan = Assert.Single(state.Plans);
        Assert.Equal(
            ("Implementation", "new content", FeaturePlanContentType.Html, "Production.", FeaturePlanStatus.InProgress),
            (plan.Title, plan.Content, plan.ContentType, plan.AdditionalInfo, plan.Status)
        );
        Assert.Equal([Root, Api, ApiTests, Ui], BranchNames(plan));
    }

    [Fact]
    public void Status_changed_v1_changes_only_status()
    {
        var state = StateWithPlan();
        var eventData = new FeaturePlanStatusChangedV1(PlanId, FeaturePlanStatus.Completed, SessionId, MemoryId);

        eventData.Apply(state, Payload(eventData).EventExecutionInfo);

        var plan = Assert.Single(state.Plans);
        Assert.Equal(FeaturePlanStatus.Completed, plan.Status);
        Assert.Equal(("Plan", "content", "info"), (plan.Title, plan.Content, plan.AdditionalInfo));
    }

    [Theory]
    [InlineData(Ui, true)]
    [InlineData(null, false)]
    public void Branch_added_v1_nests_under_parent_or_sets_missing_base_branch(
        string? parentBranchName,
        bool hasBaseBranch
    )
    {
        var state = StateWithPlan(hasBaseBranch);
        var eventData = new FeaturePlanBranchAddedV1(PlanId, "feature/new", "New work", parentBranchName, SessionId, MemoryId);

        eventData.Apply(state, Payload(eventData).EventExecutionInfo);

        var plan = state.Plans[0];
        var added = plan.FindBranch("feature/new")!;
        Assert.Equal("New work", added.Description);
        Assert.Equal(parentBranchName, plan.FindParentBranch("feature/new")?.Name);
    }

    [Fact]
    public void Branch_updated_v1_renames_the_branch_and_keeps_its_sub_branches()
    {
        var state = StateWithPlan();
        var eventData = new FeaturePlanBranchUpdatedV1(PlanId, Api, "feature/plans-backend", "Backend", SessionId, MemoryId);

        eventData.Apply(state, Payload(eventData).EventExecutionInfo);

        var plan = state.Plans[0];
        Assert.Null(plan.FindBranch(Api));
        Assert.Equal("Backend", plan.FindBranch("feature/plans-backend")!.Description);
        Assert.Equal("feature/plans-backend", plan.FindParentBranch(ApiTests)!.Name);
    }

    [Fact]
    public void Branch_removed_v1_removes_the_branch_with_its_sub_branches()
    {
        var state = StateWithPlan();
        var eventData = new FeaturePlanBranchRemovedV1(PlanId, Api, SessionId, MemoryId);

        eventData.Apply(state, Payload(eventData).EventExecutionInfo);

        Assert.Equal([Root, Ui], BranchNames(state.Plans[0]));
    }

    [Theory]
    [InlineData(false, null, true)]
    [InlineData(false, Root, false)]
    [InlineData(true, null, false)]
    [InlineData(true, Api, true)]
    [InlineData(true, "feature/missing", false)]
    public void Branch_parent_validator_keeps_one_base_branch(
        bool hasBaseBranch,
        string? parentBranchName,
        bool succeeds
    )
    {
        var eventData = new FeaturePlanBranchAddedV1(PlanId, "feature/new", "", parentBranchName, SessionId, MemoryId);

        var result = new FeaturePlanBranchParentValidator().Validate(StateWithPlan(hasBaseBranch), Payload(eventData));

        Assert.Equal(succeeds, result.Succeded);
    }

    [Theory]
    [InlineData("add", ApiTests, null, false)]
    [InlineData("add", "feature/new", null, true)]
    [InlineData("update", Api, Ui, false)]
    [InlineData("update", Api, Api, true)]
    [InlineData("update", Root, "feature/new", true)]
    public void Branch_name_validator_rejects_names_used_anywhere_in_the_tree(
        string operation,
        string branchName,
        string? newBranchName,
        bool succeeds
    )
    {
        IEvent eventData = operation == "add"
            ? new FeaturePlanBranchAddedV1(PlanId, branchName, "", Root, SessionId, MemoryId)
            : new FeaturePlanBranchUpdatedV1(PlanId, branchName, newBranchName!, "", SessionId, MemoryId);

        var result = new FeaturePlanBranchNameMustBeAvailableValidator().Validate(StateWithPlan(), Payload(eventData));

        Assert.Equal(succeeds, result.Succeded);
    }

    [Theory]
    [InlineData(ApiTests, true)]
    [InlineData("feature/missing", false)]
    public void Branch_must_exist_validator_checks_update_and_remove(string branchName, bool succeeds)
    {
        var validator = new FeaturePlanBranchMustExistValidator();

        Assert.Equal(succeeds, validator.Validate(StateWithPlan(), Payload(new FeaturePlanBranchUpdatedV1(PlanId, branchName, branchName, "", SessionId, MemoryId))).Succeded);
        Assert.Equal(succeeds, validator.Validate(StateWithPlan(), Payload(new FeaturePlanBranchRemovedV1(PlanId, branchName, SessionId, MemoryId))).Succeded);
    }

    [Theory]
    [InlineData(Root, false)]
    [InlineData(Api, true)]
    public void Base_branch_cannot_be_removed(string branchName, bool succeeds)
    {
        var result = new FeaturePlanBaseBranchCannotBeRemovedValidator().Validate(
            StateWithPlan(),
            Payload(new FeaturePlanBranchRemovedV1(PlanId, branchName, SessionId, MemoryId))
        );

        Assert.Equal(succeeds, result.Succeded);
    }

    private static List<string> BranchNames(FeaturePlan plan) =>
        plan.Branch!.SelfAndDescendants().Select(branch => branch.Name).ToList();

    private static FeatureStateData StateWithPlan(bool hasBaseBranch = true) => new(FeatureId)
    {
        Plans =
        [
            new FeaturePlan
            {
                Id = PlanId,
                Title = "Plan",
                Content = "content",
                AdditionalInfo = "info",
                Branch = hasBaseBranch
                    ? Branch(Root, Branch(Api, Branch(ApiTests)), Branch(Ui))
                    : null
            }
        ]
    };

    private static FeaturePlanBranch Branch(string name, params FeaturePlanBranch[] subBranches) =>
        new() { Name = name, Description = $"{name} work", SubBranches = [.. subBranches] };

    private static EventPayload Payload(IEvent eventData) => EventPayload.Create(
        EventExecutor.FromDatabaseGuid(Guid.NewGuid()), FeatureId, "features-state-machine", eventData
    );
}

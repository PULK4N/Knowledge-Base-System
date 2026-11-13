using EventSourcing.Shared.Models;
using FeatureModule.Application.Exceptions;
using FeatureModule.Application.Services;
using FeatureModule.Domain;
using FeatureModule.Domain.Models;
using Xunit;

namespace FeatureModule.Application.Tests;

public sealed class FeatureReviewContextTests
{
    [Theory]
    [InlineData("next", "previous")]
    [InlineData("child", "next")]
    public void Resolve_UsesRecordedOriginIncludingCompletedPlans(string branch, string expectedOrigin)
    {
        var state = State();
        var previous = Plan("previous");
        previous.Status = FeaturePlanStatus.Completed;
        var next = Plan("next");
        next.PreviousPlanId = previous.Id;
        next.CreatedAt = previous.CreatedAt.AddSeconds(1);
        next.Branches[0].SubBranches.Add(new() { Name = "child" });
        state.Plans.AddRange([previous, next]);

        var result = FeatureReviewContextResolver.Resolve(state, state.ProjectId.Value, branch);

        Assert.Equal(expectedOrigin, result!.OriginBranch);
        Assert.Equal(next.Id.Value, result.Plan.Id);
        Assert.Equal(state.Summary, result.Feature.Summary);
    }

    [Fact]
    public void Resolve_UsesFeatureBranchAndDoesNotCrossProjectOrDeletedFeature()
    {
        var state = State();
        state.Plans.Add(Plan("implementation"));
        Assert.Equal("feature", FeatureReviewContextResolver.Resolve(state, state.ProjectId.Value, "implementation")!.OriginBranch);
        Assert.Null(FeatureReviewContextResolver.Resolve(state, Guid.NewGuid(), "implementation"));
        Assert.Null(FeatureReviewContextResolver.Resolve(state, state.ProjectId.Value, "missing"));
        state.IsDeleted = true;
        Assert.Null(FeatureReviewContextResolver.Resolve(state, state.ProjectId.Value, "implementation"));
    }

    [Fact]
    public void Resolve_MergesFeatureAndPlanReviewInputsAndIncludesOnlyThePlanSpecification()
    {
        var state = State();
        var plan = Plan("implementation");
        var other = Plan("other");
        state.ReviewNotes.Add(new() { Id = FeatureReviewNoteId.FromDatabaseGuid(Guid.NewGuid()), Title = "Feature note" });
        state.Reviewers.Add(new() { Id = FeatureReviewerId.FromDatabaseGuid(Guid.NewGuid()), Title = "Feature reviewer" });
        state.Specification.Requirements.Add(new() { Id = RequirementId.FromDatabaseGuid(Guid.NewGuid()), Number = 1 });
        plan.ReviewNotes.Add(new() { Id = FeatureReviewNoteId.FromDatabaseGuid(Guid.NewGuid()), Title = "Plan note" });
        plan.Reviewers.Add(new() { Id = FeatureReviewerId.FromDatabaseGuid(Guid.NewGuid()), Title = "Plan reviewer" });
        plan.Specification.Requirements.Add(new() { Id = RequirementId.FromDatabaseGuid(Guid.NewGuid()), Type = RequirementType.Functional, Number = 4 });
        other.ReviewNotes.Add(new() { Id = FeatureReviewNoteId.FromDatabaseGuid(Guid.NewGuid()), Title = "Other plan note" });
        state.Plans.AddRange([plan, other]);

        var result = FeatureReviewContextResolver.Resolve(state, state.ProjectId.Value, "implementation")!;

        Assert.Equal(["Feature note", "Plan note"], result.ReviewNotes.Select(note => note.Title));
        Assert.Equal(["Feature reviewer", "Plan reviewer"], result.Reviewers.Select(reviewer => reviewer.Title));
        Assert.Equal(["FR-4"], result.Plan.Specification.Requirements.Select(requirement => requirement.Label));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Resolve_RejectsMissingRecordedOriginInsteadOfFallingBack(bool explicitSource)
    {
        var state = State();
        var plan = Plan("implementation");
        var missing = FeaturePlanId.FromDatabaseGuid(Guid.NewGuid());
        if (explicitSource)
            plan.Branches[0].Sources.Add(new(missing, "missing"));
        else
            plan.PreviousPlanId = missing;
        state.Plans.Add(plan);
        Assert.Throws<FeatureReviewContextConflictException>(() =>
            FeatureReviewContextResolver.Resolve(state, state.ProjectId.Value, "implementation"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Resolve_RejectsAmbiguousPlanOrOrigin(bool multipleOrigins)
    {
        var state = State();
        var first = Plan("first");
        var second = Plan(multipleOrigins ? "second" : "first");
        if (multipleOrigins)
        {
            second.Branches[0].Sources.Add(FeaturePlanBranchSource.FeatureBranch);
            second.Branches[0].Sources.Add(new(first.Id, "first"));
        }
        state.Plans.AddRange([first, second]);
        Assert.Throws<FeatureReviewContextConflictException>(() => FeatureReviewContextResolver.Resolve(
            state, state.ProjectId.Value, multipleOrigins ? "second" : "first"));
    }

    [Fact]
    public void Resolve_RejectsDuplicateBranchNameInOnePlanWithClearMessage()
    {
        var state = State();
        var plan = Plan("implementation");
        plan.Branches[0].SubBranches.Add(new() { Name = "implementation" });
        state.Plans.Add(plan);
        var error = Assert.Throws<FeatureReviewContextConflictException>(() =>
            FeatureReviewContextResolver.Resolve(state, state.ProjectId.Value, "implementation"));
        Assert.Contains("more than once in plan 'implementation'", error.Message);
    }

    private static FeatureStateData State() => new(AggregateId.FromDatabaseGuid(Guid.NewGuid()))
    {
        ProjectId = AggregateId.FromDatabaseGuid(Guid.NewGuid()),
        Branch = new() { Name = "feature" },
        Summary = "Feature overview"
    };

    private static FeaturePlan Plan(string branch) => new()
    {
        Id = FeaturePlanId.FromDatabaseGuid(Guid.NewGuid()),
        Title = branch,
        Content = "Plan content",
        CreatedAt = new DateTime(2026, 11, 8, 0, 0, 0, DateTimeKind.Utc),
        Branches = [new() { Name = branch }]
    };
}

using FeatureModule.Application.DTOs;
using FeatureModule.Domain.Models;

namespace FeatureModule.MCP.Tests;

public sealed class FeatureBranchTreeFormatterTests
{
    private static readonly DateTime Start = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Format_NestsPlanBranchesUnderTheirPreviousPlanAndLeavesOutCompletedPlans()
    {
        var research = Plan("RabbitMQ design", FeaturePlanStatus.Completed, 0, null,
            new FeaturePlanBranchDto("feature/outbox-research", "", []));
        var implementation = Plan("Outbox processing", FeaturePlanStatus.InProgress, 1, research.Id,
            new FeaturePlanBranchDto("feature/outbox", "Dispatch", [new("feature/outbox-api", "", [])]));
        var retry = Plan("Retry policy", FeaturePlanStatus.NotStarted, 2, implementation.Id,
            new FeaturePlanBranchDto("feature/outbox-retry", "", []));
        var legacy = Plan("Old notes", FeaturePlanStatus.Delayed, 3, null, null);

        var tree = FeatureBranchTreeFormatter.Format(Feature(
            new FeatureBranchDto("develop", "Main line"),
            [retry, legacy, implementation, research]
        ));

        Assert.Equal(
            """
            develop - Main line
            ├── feature/outbox - Dispatch  [Outbox processing · In progress]
            │   ├── feature/outbox-api
            │   └── feature/outbox-retry  [Retry policy · Not started]
            └── (no base branch)  [Old notes · Delayed]
            """,
            tree
        );
    }

    [Theory]
    [InlineData(false, null)]
    [InlineData(true, "(feature branch not set)\n└── feature/x  [Plan · Not started]")]
    public void Format_ShowsMissingFeatureBranchOnlyWhenThereArePlans(bool hasPlan, string? expected)
    {
        List<FeaturePlanDto> plans = hasPlan
            ? [Plan("Plan", FeaturePlanStatus.NotStarted, 0, null, new FeaturePlanBranchDto("feature/x", "", []))]
            : [];

        Assert.Equal(expected, FeatureBranchTreeFormatter.Format(Feature(null, plans)));
    }

    private static FeatureDto Feature(FeatureBranchDto? branch, List<FeaturePlanDto> plans) =>
        new()
        {
            Id = Guid.NewGuid(),
            IsDeleted = false,
            ProjectId = Guid.NewGuid(),
            Name = "Feature",
            Summary = "",
            Status = "",
            RelatedSkillIds = [],
            Records = [],
            ReviewNotes = [],
            Plans = plans,
            Branch = branch,
            MemoryHistory = []
        };

    private static FeaturePlanDto Plan(
        string title,
        FeaturePlanStatus status,
        int order,
        Guid? previousPlanId,
        FeaturePlanBranchDto? branch
    ) =>
        new()
        {
            Id = Guid.NewGuid(),
            Title = title,
            Content = "",
            ContentType = FeaturePlanContentType.Markdown,
            AdditionalInfo = "",
            Status = status,
            Branch = branch,
            PreviousPlanId = previousPlanId,
            RecordIds = [],
            CreatedAt = Start.AddDays(order),
            UpdatedAt = Start.AddDays(order)
        };
}

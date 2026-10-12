using FeatureModule.Application.DTOs;
using FeatureModule.Domain.Models;

namespace FeatureModule.MCP.Tests;

public sealed class FeatureMcpDtoTests
{
    [Fact]
    public void FromFeature_ReturnsPlanSummariesWithoutContentAndBoundsTheRest()
    {
        var now = DateTime.UtcNow;
        var olderPlan = Plan("Research", now.AddDays(-2));
        var newerPlan = Plan("Implementation", now.AddDays(-1)) with
        {
            Status = FeaturePlanStatus.InProgress,
            AdditionalInfo = "Production implementation.",
            Branch = new FeaturePlanBranchDto("feature/plans", "Plan rework", [])
        };
        var discoveries = Enumerable.Range(1, 7)
            .Select(index => Discovery($"Discovery {index}", now.AddDays(-index)))
            .ToList();
        var records = Enumerable.Range(1, 7)
            .Select(index => Record(index, now.AddDays(-index)))
            .ToList();
        var feature = new FeatureDto
        {
            Id = Guid.NewGuid(),
            IsDeleted = false,
            ProjectId = Guid.NewGuid(),
            Name = "Feature",
            Summary = "Summary",
            Status = "In progress",
            RelatedSkillIds = [],
            Records = records,
            ReviewNotes = [ReviewNote("Intended behaviour", now)],
            ResearchDiscoveries = discoveries,
            Plans = [olderPlan, newerPlan],
            MemoryHistory = []
        };

        var result = FeatureMcpDto.FromFeature(feature);

        Assert.Equal(
            [
                new FeatureMcpPlanSummaryDto
                {
                    Id = newerPlan.Id,
                    Title = "Implementation",
                    Status = FeaturePlanStatus.InProgress,
                    AdditionalInfo = "Production implementation."
                },
                new FeatureMcpPlanSummaryDto
                {
                    Id = olderPlan.Id,
                    Title = "Research",
                    Status = FeaturePlanStatus.NotStarted,
                    AdditionalInfo = string.Empty
                }
            ],
            result.Plans
        );
        Assert.DoesNotContain(
            typeof(FeatureMcpDto).GetProperties(),
            property => property.Name.Contains("ReviewNote")
        );
        Assert.Equal(records.Take(5), result.LatestConversationRecords);
        Assert.Equal(discoveries.Take(5), result.LatestResearchDiscoveries);
        Assert.Equal(
            discoveries.Skip(5).Select(discovery =>
                new FeatureMcpReferenceDto(discovery.Id, discovery.Title)
            ),
            result.OtherResearchDiscoveries
        );
    }

    private static FeaturePlanDto Plan(string title, DateTime updatedAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            Title = title,
            Content = $"{title} content",
            ContentType = FeaturePlanContentType.Markdown,
            AdditionalInfo = string.Empty,
            Status = FeaturePlanStatus.NotStarted,
            CreatedAt = updatedAt,
            UpdatedAt = updatedAt
        };

    private static FeatureResearchDiscoveryDto Discovery(
        string title,
        DateTime updatedAt
    ) =>
        new()
        {
            Id = Guid.NewGuid(),
            Title = title,
            Content = $"{title} content",
            SourceType = FeatureResearchDiscoverySourceType.Other,
            SourceReference = string.Empty,
            CreatedAt = updatedAt,
            UpdatedAt = updatedAt
        };

    private static FeatureReviewNoteDto ReviewNote(
        string title,
        DateTime updatedAt
    ) =>
        new()
        {
            Id = Guid.NewGuid(),
            Title = title,
            Content = $"{title} content",
            CreatedAt = updatedAt,
            UpdatedAt = updatedAt
        };

    private static FeatureRecordDto Record(int index, DateTime updatedAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserMessage = $"User {index}",
            AiAnswer = $"AI {index}",
            CreatedAt = updatedAt,
            UpdatedAt = updatedAt
        };
}

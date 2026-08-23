using FeatureModule.Application.DTOs;
using FeatureModule.Domain.Models;

namespace FeatureModule.MCP.Tests;

public sealed class FeatureMcpDtoTests
{
    [Fact]
    public void FromFeature_BoundsFullContentAndKeepsReferencesForTheRest()
    {
        var now = DateTime.UtcNow;
        var currentPlan = Plan("Current", now.AddDays(-2));
        var otherPlan = Plan("Previous", now.AddDays(-1));
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
            ResearchDiscoveries = discoveries,
            Plans = [currentPlan, otherPlan],
            CurrentPlanId = currentPlan.Id
        };

        var result = FeatureMcpDto.FromFeature(feature);

        Assert.Equal(currentPlan, result.CurrentPlan);
        Assert.Equal(records.Take(5), result.LatestConversationRecords);
        Assert.Equal(discoveries.Take(5), result.LatestResearchDiscoveries);
        Assert.Equal(
            [new FeatureMcpReferenceDto(otherPlan.Id, otherPlan.Title)],
            result.OtherPlans
        );
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

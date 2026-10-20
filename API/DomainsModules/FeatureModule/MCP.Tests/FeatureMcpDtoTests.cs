using FeatureModule.Application.DTOs;
using FeatureModule.Domain.Models;

namespace FeatureModule.MCP.Tests;

public sealed class FeatureMcpDtoTests
{
    [Fact]
    public void FromFeature_ReturnsPlanSummariesAndRecordReferencesWithoutContent()
    {
        var now = DateTime.UtcNow;
        var records = Enumerable.Range(1, 7)
            .Select(index => Record(index, now.AddDays(-index)))
            .ToList();
        var olderPlan = Plan("Research", now.AddDays(-2), []);
        var newerPlan = Plan(
            "Implementation",
            now.AddDays(-1),
            [records[2].Id]
        ) with
        {
            Status = FeaturePlanStatus.InProgress,
            AdditionalInfo = "Production implementation.",
            Branch = new FeaturePlanBranchDto("feature/plans", "Plan rework", [])
        };
        var feature = Feature(records, [olderPlan, newerPlan], now);

        var result = FeatureMcpDto.FromFeature(feature);

        Assert.Equal(
            [
                new FeatureMcpPlanSummaryDto
                {
                    Id = newerPlan.Id,
                    Title = "Implementation",
                    Status = FeaturePlanStatus.InProgress,
                    AdditionalInfo = "Production implementation.",
                    RecordIds = newerPlan.RecordIds
                },
                new FeatureMcpPlanSummaryDto
                {
                    Id = olderPlan.Id,
                    Title = "Research",
                    Status = FeaturePlanStatus.NotStarted,
                    AdditionalInfo = string.Empty,
                    RecordIds = olderPlan.RecordIds
                }
            ],
            result.Plans
        );
        Assert.DoesNotContain(
            typeof(FeatureMcpDto).GetProperties(),
            property => property.Name.Contains("ReviewNote")
        );
        Assert.Equal(
            records.Select(
                record => new FeatureRecordReferenceDto(
                    record.Id,
                    record.Title,
                    record.Type
                )
            ),
            result.Records
        );
    }

    [Fact]
    public void FromPlan_ReturnsLinkedRecordReferencesInLinkOrder()
    {
        var now = DateTime.UtcNow;
        var records = Enumerable.Range(1, 3)
            .Select(index => Record(index, now))
            .ToList();
        var plan = Plan(
            "Implementation",
            now,
            [records[2].Id, records[0].Id]
        );

        var result = FeatureMcpPlanDto.FromPlan(plan, records);

        Assert.Equal(
            [
                new FeatureRecordReferenceDto(records[2].Id, "Record 3", "research"),
                new FeatureRecordReferenceDto(records[0].Id, "Record 1", "research")
            ],
            result.Records
        );
        Assert.Equal("Implementation content", result.Content);
    }

    private static FeatureDto Feature(
        List<FeatureRecordDto> records,
        List<FeaturePlanDto> plans,
        DateTime now
    ) =>
        new()
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
            Plans = plans,
            MemoryHistory = []
        };

    private static FeaturePlanDto Plan(
        string title,
        DateTime updatedAt,
        List<Guid> recordIds
    ) =>
        new()
        {
            Id = Guid.NewGuid(),
            Title = title,
            Content = $"{title} content",
            ContentType = FeaturePlanContentType.Markdown,
            AdditionalInfo = string.Empty,
            Status = FeaturePlanStatus.NotStarted,
            RecordIds = recordIds,
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
            Type = index % 2 == 0 ? "conversation" : "research",
            Title = $"Record {index}",
            Content = $"Content {index}",
            SourceReference = string.Empty,
            CreatedAt = updatedAt,
            UpdatedAt = updatedAt
        };
}

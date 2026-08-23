using FeatureModule.Application.DTOs;

namespace FeatureModule.MCP;

public sealed record FeatureMcpDto
{
    public required Guid Id { get; init; }

    public required Guid ProjectId { get; init; }

    public required string Name { get; init; }

    public required string Summary { get; init; }

    public required string Status { get; init; }

    public required IReadOnlyCollection<Guid> RelatedSkillIds { get; init; }

    public FeaturePlanDto? CurrentPlan { get; init; }

    public required IReadOnlyCollection<FeatureResearchDiscoveryDto>
        LatestResearchDiscoveries { get; init; }

    public required IReadOnlyCollection<FeatureRecordDto>
        LatestConversationRecords { get; init; }

    public required IReadOnlyCollection<FeatureMcpReferenceDto>
        OtherPlans { get; init; }

    public required IReadOnlyCollection<FeatureMcpReferenceDto>
        OtherResearchDiscoveries { get; init; }

    public static FeatureMcpDto FromFeature(FeatureDto feature)
    {
        var latestDiscoveries = feature.ResearchDiscoveries
            .OrderByDescending(discovery => discovery.UpdatedAt)
            .ThenBy(discovery => discovery.Id)
            .Take(5)
            .ToList();
        var latestDiscoveryIds = latestDiscoveries
            .Select(discovery => discovery.Id)
            .ToHashSet();

        return new FeatureMcpDto
        {
            Id = feature.Id,
            ProjectId = feature.ProjectId,
            Name = feature.Name,
            Summary = feature.Summary,
            Status = feature.Status,
            RelatedSkillIds = feature.RelatedSkillIds,
            CurrentPlan = feature.Plans.SingleOrDefault(
                plan => plan.Id == feature.CurrentPlanId
            ),
            LatestResearchDiscoveries = latestDiscoveries,
            LatestConversationRecords = feature.Records
                .OrderByDescending(record => record.UpdatedAt)
                .ThenBy(record => record.Id)
                .Take(5)
                .ToList(),
            OtherPlans = feature.Plans
                .Where(plan => plan.Id != feature.CurrentPlanId)
                .OrderByDescending(plan => plan.UpdatedAt)
                .ThenBy(plan => plan.Id)
                .Select(plan => new FeatureMcpReferenceDto(plan.Id, plan.Title))
                .ToList(),
            OtherResearchDiscoveries = feature.ResearchDiscoveries
                .Where(discovery => !latestDiscoveryIds.Contains(discovery.Id))
                .OrderByDescending(discovery => discovery.UpdatedAt)
                .ThenBy(discovery => discovery.Id)
                .Select(discovery => new FeatureMcpReferenceDto(
                    discovery.Id,
                    discovery.Title
                ))
                .ToList()
        };
    }
}

public sealed record FeatureMcpReferenceDto(Guid Id, string Title);

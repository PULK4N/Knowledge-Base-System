using FeatureModule.Domain;
using FeatureModule.Domain.Models;

namespace FeatureModule.Application.DTOs;

public sealed record FeatureDto
{
    public required Guid Id { get; init; }

    public required bool IsDeleted { get; init; }

    public required Guid ProjectId { get; init; }

    public required string Name { get; init; }

    public required string Summary { get; init; }

    public required string Status { get; init; }

    public required IReadOnlyCollection<Guid> RelatedSkillIds { get; init; }

    public required IReadOnlyCollection<FeatureRecordDto> Records { get; init; }

    public required IReadOnlyCollection<FeaturePlanDto> Plans { get; init; }

    public Guid? CurrentPlanId { get; init; }

    public static FeatureDto FromStateData(FeatureStateData state) =>
        new()
        {
            Id = state.Id.Value,
            IsDeleted = state.IsDeleted,
            ProjectId = state.ProjectId.Value,
            Name = state.Name,
            Summary = state.Summary,
            Status = state.Status,
            RelatedSkillIds = state.RelatedSkillIds
                .Select(skillId => skillId.Value)
                .ToList(),
            Records = state.Records
                .Select(FeatureRecordDto.FromModel)
                .ToList(),
            Plans = state.Plans
                .Select(FeaturePlanDto.FromModel)
                .ToList(),
            CurrentPlanId = state.CurrentPlanId?.Value
        };
}

public sealed record FeatureRecordDto
{
    public required Guid Id { get; init; }

    public required string UserMessage { get; init; }

    public required string AiAnswer { get; init; }

    public required DateTime CreatedAt { get; init; }

    public required DateTime UpdatedAt { get; init; }

    public static FeatureRecordDto FromModel(
        FeatureRecord record
    ) =>
        new()
        {
            Id = record.Id.Value,
            UserMessage = record.UserMessage,
            AiAnswer = record.AiAnswer,
            CreatedAt = record.CreatedAt,
            UpdatedAt = record.UpdatedAt
        };
}

public sealed record FeaturePlanDto
{
    public required Guid Id { get; init; }

    public required string Title { get; init; }

    public required string Content { get; init; }

    public required FeaturePlanContentType ContentType { get; init; }

    public required DateTime CreatedAt { get; init; }

    public required DateTime UpdatedAt { get; init; }

    public static FeaturePlanDto FromModel(FeaturePlan plan) =>
        new()
        {
            Id = plan.Id.Value,
            Title = plan.Title,
            Content = plan.Content,
            ContentType = plan.ContentType,
            CreatedAt = plan.CreatedAt,
            UpdatedAt = plan.UpdatedAt
        };
}

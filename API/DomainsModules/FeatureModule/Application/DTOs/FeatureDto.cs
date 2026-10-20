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

    public required IReadOnlyCollection<FeatureReviewNoteDto>
        ReviewNotes { get; init; }

    public required IReadOnlyCollection<FeaturePlanDto> Plans { get; init; }

    public FeatureBranchDto? Branch { get; init; }

    public required IReadOnlyCollection<FeatureMemoryHistoryDto>
        MemoryHistory { get; init; }

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
            ReviewNotes = state.ReviewNotes
                .Select(FeatureReviewNoteDto.FromModel)
                .ToList(),
            Plans = state.Plans
                .Select(FeaturePlanDto.FromModel)
                .ToList(),
            Branch = state.Branch is null
                ? null
                : FeatureBranchDto.FromModel(state.Branch),
            MemoryHistory = state.MemoryHistory
                .Select(FeatureMemoryHistoryDto.FromModel)
                .ToList()
        };
}

public sealed record FeatureRecordDto
{
    public required Guid Id { get; init; }

    public required string Type { get; init; }

    public required string Title { get; init; }

    public required string Content { get; init; }

    public FeatureRecordSourceType? SourceType { get; init; }

    public required string SourceReference { get; init; }

    public required DateTime CreatedAt { get; init; }

    public required DateTime UpdatedAt { get; init; }

    public static FeatureRecordDto FromModel(
        FeatureRecord record
    ) =>
        new()
        {
            Id = record.Id.Value,
            Type = record.Type,
            Title = record.Title,
            Content = record.Content,
            SourceType = record.SourceType,
            SourceReference = record.SourceReference,
            CreatedAt = record.CreatedAt,
            UpdatedAt = record.UpdatedAt
        };
}

/// <summary>
/// A record without its content, so a caller can decide which records to load.
/// </summary>
public sealed record FeatureRecordReferenceDto(
    Guid Id,
    string Title,
    string Type
)
{
    public static FeatureRecordReferenceDto FromDto(FeatureRecordDto record) =>
        new(record.Id, record.Title, record.Type);
}

public sealed record FeatureReviewNoteDto
{
    public required Guid Id { get; init; }

    public required string Title { get; init; }

    public required string Content { get; init; }

    public required DateTime CreatedAt { get; init; }

    public required DateTime UpdatedAt { get; init; }

    public static FeatureReviewNoteDto FromModel(
        FeatureReviewNote reviewNote
    ) =>
        new()
        {
            Id = reviewNote.Id.Value,
            Title = reviewNote.Title,
            Content = reviewNote.Content,
            CreatedAt = reviewNote.CreatedAt,
            UpdatedAt = reviewNote.UpdatedAt
        };
}

public sealed record FeatureMemoryHistoryDto(
    string EventName,
    DateTime Timestamp,
    Guid MemoryId,
    bool IsUserOriginated
)
{
    public static FeatureMemoryHistoryDto FromModel(MemoryHistoryRecord record) =>
        new(
            record.EventName,
            record.Timestamp,
            record.AggregateId.Value,
            record.AggregateId.Value == SharedModule.Constants.MemoryAggregateIds.User
        );
}

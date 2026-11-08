using ProjectModule.Domain.Models;

namespace ProjectModule.API.Requests;

public sealed record UpdateProjectSkillRequest
{
    public required Guid ProjectId { get; init; }

    public required Guid SkillId { get; init; }

    public required bool UsedForReview { get; init; }

    public string? ReviewNotes { get; init; }

    public ReviewLevel? ReviewLevel { get; init; }
}

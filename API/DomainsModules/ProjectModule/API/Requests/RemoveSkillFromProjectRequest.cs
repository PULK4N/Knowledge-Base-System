namespace ProjectModule.API.Requests;

public sealed record RemoveSkillFromProjectRequest
{
    public required Guid ProjectId { get; init; }

    public required Guid SkillId { get; init; }
}

namespace ProjectModule.Application.Models;

public sealed record ProjectCreatedCommandResult(
    string Status,
    Guid ProjectId
)
{
    public static ProjectCreatedCommandResult Ok(
        Guid projectId
    ) =>
        new("OK", projectId);
}
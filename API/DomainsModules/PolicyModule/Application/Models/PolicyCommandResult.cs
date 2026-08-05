namespace PolicyModule.Application.Models;

public sealed record PolicyCommandResult(string Status)
{
    public static PolicyCommandResult Ok { get; } = new("OK");
}

public sealed record PolicyAddedCommandResult(
    string Status,
    Guid PolicyId
)
{
    public static PolicyAddedCommandResult Ok(Guid policyId) =>
        new("OK", policyId);
}

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

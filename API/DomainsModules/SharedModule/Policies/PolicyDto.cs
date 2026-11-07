namespace SharedModule.Policies;

public sealed record PolicyDto(
    Guid PolicyId,
    string Title,
    string Description
)
{
    public static PolicyDto FromModel(Policy policy) =>
        new(
            policy.PolicyId.Value,
            policy.Title,
            policy.Description
        );
}

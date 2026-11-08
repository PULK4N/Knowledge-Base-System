namespace PolicyModule.Shared;

public static class PolicyTextFormatter
{
    public static string Format(IEnumerable<PolicyDto>? policies) =>
        string.Join(
            "\n\n",
            (policies ?? [])
                .Select(policy =>
                    $"# {policy.Title} - {policy.PolicyId}\n\n{policy.Description}")
        );
}

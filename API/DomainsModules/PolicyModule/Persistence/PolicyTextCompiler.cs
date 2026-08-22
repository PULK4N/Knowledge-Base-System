using PolicyModule.Domain.Models;

namespace PolicyModule.Persistence;

internal static class PolicyTextCompiler
{
    public static string CompileGeneral(IEnumerable<Policy> policies) =>
        Compile("General policies", policies);

    public static string CompileProject(
        string projectName,
        IEnumerable<Policy> policies
    ) =>
        Compile($"Project \"{projectName}\" policies", policies);

    public static string CompileTopic(
        string topicName,
        IEnumerable<Policy> policies
    ) =>
        Compile($"Topic \"{topicName}\" policies", policies);

    private static string Compile(
        string scopeTitle,
        IEnumerable<Policy> policies
    )
    {
        var policyText = string.Join(
            "\n\n",
            policies.Select(
                policy => $"## {policy.Title}\n{policy.Description}"
            )
        );

        return string.IsNullOrWhiteSpace(policyText)
            ? string.Empty
            : $"# {scopeTitle}\n\n{policyText}";
    }
}

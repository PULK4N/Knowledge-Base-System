using PolicyModule.Domain.Models;

namespace PolicyModule.Persistence;

internal static class PolicyTextCompiler
{
    public static string Compile(IEnumerable<Policy> policies) =>
        string.Join(
            "\n\n",
            policies.Select(
                policy => $"# {policy.Title}\n{policy.Description}"
            )
        );
}

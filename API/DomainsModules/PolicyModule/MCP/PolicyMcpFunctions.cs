using Microsoft.Extensions.AI;

namespace PolicyModule.MCP;

public static class PolicyMcpFunctions
{
    public static List<AIFunction> Create() =>
    [
        .. GeneralPolicyMcpFunctions.Create(),
        .. TopicPolicyMcpFunctions.Create(),
        .. ProjectPolicyMcpFunctions.Create(),
        .. AgentFamilyPolicyMcpFunctions.Create()
        // PolicyQueryMcpFunctions is intentionally not registered. Repository
        // policies are served only over HTTP so that the plugin hook loads them
        // for a known agent family instead of an agent fetching them ad hoc.
    ];

    internal static AIFunction Create(
        Delegate method,
        string name,
        string description
    ) =>
        AIFunctionFactory.Create(
            method,
            new AIFunctionFactoryOptions
            {
                Name = name,
                Description = description
            }
        );
}

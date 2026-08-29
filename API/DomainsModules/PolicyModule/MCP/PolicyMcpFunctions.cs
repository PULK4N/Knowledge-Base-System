using Microsoft.Extensions.AI;

namespace PolicyModule.MCP;

public static class PolicyMcpFunctions
{
    public static List<AIFunction> Create() =>
    [
        .. GeneralPolicyMcpFunctions.Create(),
        .. TopicPolicyMcpFunctions.Create(),
        .. ProjectPolicyMcpFunctions.Create(),
        .. AgentFamilyPolicyMcpFunctions.Create(),
        .. PolicyQueryMcpFunctions.Create()
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

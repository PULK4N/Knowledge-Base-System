using Microsoft.Extensions.AI;

namespace PolicyModule.MCP;

public static class PolicyMcpFunctions
{
    public static List<AIFunction> Create() =>
    [
        .. GeneralPolicyMcpFunctions.Create(),
        .. TopicPolicyMcpFunctions.Create(),
        .. AgentFamilyPolicyMcpFunctions.Create()
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
                Description = description,
                ConfigureParameterBinding = parameter =>
                    parameter.Name is "sessionId" or "memoryAggregateId"
                        ? new AIFunctionFactoryOptions.ParameterBindingOptions
                        {
                            ExcludeFromSchema = true
                        }
                        : default
            }
        );
}

using Microsoft.Extensions.AI;

namespace ProjectModule.MCP;

public static class ProjectMcpFunctions
{
    public static List<AIFunction> Create() =>
    [
        .. ProjectPolicyMcpFunctions.Create(),
        .. ProjectSkillMcpFunctions.Create()
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

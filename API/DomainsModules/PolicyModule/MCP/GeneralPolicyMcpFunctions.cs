using Microsoft.Extensions.AI;
using PolicyModule.Application.Commands;
using PolicyModule.Application.DTOs;
using PolicyModule.Application.Models;
using PolicyModule.Application.Queries;

namespace PolicyModule.MCP;

internal static class GeneralPolicyMcpFunctions
{
    public static List<AIFunction> Create() =>
    [
        PolicyMcpFunctions.Create(
            (Func<IServiceProvider, Task<List<PolicyDto>>>)List,
            "policy_general_list",
            "Lists every general policy that applies to every project and chat."
        ),
        PolicyMcpFunctions.Create(
            (Func<IServiceProvider, string, string, Task<PolicyAddedCommandResult>>)Add,
            "policy_general_add",
            "Adds a general policy that applies to every project and chat."
        ),
        PolicyMcpFunctions.Create(
            (Func<IServiceProvider, Guid, string, string, Task<PolicyCommandResult>>)Update,
            "policy_general_update",
            "Updates an existing general policy."
        ),
        PolicyMcpFunctions.Create(
            (Func<IServiceProvider, Guid, Task<PolicyCommandResult>>)Remove,
            "policy_general_remove",
            "Removes an existing general policy."
        )
    ];

    private static Task<List<PolicyDto>> List(
        IServiceProvider services
    ) =>
        PolicyMcpActionExecutor.ExecuteQuery<
            ListGeneralPoliciesQuery,
            List<PolicyDto>
        >(services, _ => { });

    private static Task<PolicyAddedCommandResult> Add(
        IServiceProvider services,
        string title,
        string description
    ) =>
        PolicyMcpActionExecutor.ExecuteCommand<
            AddGeneralPolicyCommand,
            PolicyAddedCommandResult
        >(
            services,
            command =>
            {
                command.Title = title;
                command.Description = description;
            }
        );

    private static Task<PolicyCommandResult> Update(
        IServiceProvider services,
        Guid policyId,
        string title,
        string description
    ) =>
        PolicyMcpActionExecutor.ExecuteCommand<
            UpdateGeneralPolicyCommand,
            PolicyCommandResult
        >(
            services,
            command =>
            {
                command.PolicyId = policyId;
                command.Title = title;
                command.Description = description;
            }
        );

    private static Task<PolicyCommandResult> Remove(
        IServiceProvider services,
        Guid policyId
    ) =>
        PolicyMcpActionExecutor.ExecuteCommand<
            RemoveGeneralPolicyCommand,
            PolicyCommandResult
        >(
            services,
            command => command.PolicyId = policyId
        );
}

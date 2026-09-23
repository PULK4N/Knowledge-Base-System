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
            (Func<IServiceProvider, Task<string>>)List,
            "policy_general_list",
            "Returns every general policy that applies to every project and chat as joined Markdown text."
        ),
        PolicyMcpFunctions.Create(
            (Func<IServiceProvider, string, string, Guid?, Guid?, Task<PolicyAddedCommandResult>>)Add,
            "policy_general_add",
            "Adds a general policy that applies to every project and chat."
        ),
        PolicyMcpFunctions.Create(
            (Func<IServiceProvider, Guid, string, string, Guid?, Guid?, Task<PolicyCommandResult>>)Update,
            "policy_general_update",
            "Updates an existing general policy."
        ),
        PolicyMcpFunctions.Create(
            (Func<IServiceProvider, Guid, Guid?, Guid?, Task<PolicyCommandResult>>)Remove,
            "policy_general_remove",
            "Removes an existing general policy."
        )
    ];

    private static async Task<string> List(
        IServiceProvider services
    ) =>
        PolicyTextFormatter.Format(await PolicyMcpActionExecutor.ExecuteQuery<
            ListGeneralPoliciesQuery,
            List<PolicyDto>
        >(services, _ => { }));

    private static Task<PolicyAddedCommandResult> Add(
        IServiceProvider services,
        string title,
        string description,
        Guid? sessionId = null,
        Guid? memoryAggregateId = null
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
                command.SessionId = sessionId.GetValueOrDefault();
                command.MemoryAggregateId = memoryAggregateId.GetValueOrDefault();
            }
        );

    private static Task<PolicyCommandResult> Update(
        IServiceProvider services,
        Guid policyId,
        string title,
        string description,
        Guid? sessionId = null,
        Guid? memoryAggregateId = null
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
                command.SessionId = sessionId.GetValueOrDefault();
                command.MemoryAggregateId = memoryAggregateId.GetValueOrDefault();
            }
        );

    private static Task<PolicyCommandResult> Remove(
        IServiceProvider services,
        Guid policyId,
        Guid? sessionId = null,
        Guid? memoryAggregateId = null
    ) =>
        PolicyMcpActionExecutor.ExecuteCommand<
            RemoveGeneralPolicyCommand,
            PolicyCommandResult
        >(
            services,
            command =>
            {
                command.PolicyId = policyId;
                command.SessionId = sessionId.GetValueOrDefault();
                command.MemoryAggregateId = memoryAggregateId.GetValueOrDefault();
            }
        );
}

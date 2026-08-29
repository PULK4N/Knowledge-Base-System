using Microsoft.Extensions.AI;
using PolicyModule.Application.Commands;
using PolicyModule.Application.DTOs;
using PolicyModule.Application.Models;
using PolicyModule.Application.Queries;

namespace PolicyModule.MCP;

internal static class AgentFamilyPolicyMcpFunctions
{
    public static List<AIFunction> Create() =>
        [
            PolicyMcpFunctions.Create(
                ListAgentFamilies,
                "policy_agent_family_list",
                "Lists all agent families, such as claude and codex, with their descriptions and policy counts."
            ),
            PolicyMcpFunctions.Create(
                ListPolicies,
                "policy_agent_family_policy_list",
                "Lists the policies that apply only to one agent family."
            ),
            PolicyMcpFunctions.Create(
                CreateAgentFamily,
                "policy_agent_family_create",
                "Creates an agent family used to group policies that apply only to that kind of agent."
            ),
            PolicyMcpFunctions.Create(
                UpdateAgentFamily,
                "policy_agent_family_update",
                "Updates an existing agent family's description."
            ),
            PolicyMcpFunctions.Create(
                RemoveAgentFamily,
                "policy_agent_family_remove",
                "Removes an existing agent family."
            ),
            PolicyMcpFunctions.Create(
                AddPolicy,
                "policy_agent_family_policy_add",
                "Adds a policy that applies only to one agent family."
            ),
            PolicyMcpFunctions.Create(
                UpdatePolicy,
                "policy_agent_family_policy_update",
                "Updates an existing policy in an agent family."
            ),
            PolicyMcpFunctions.Create(
                RemovePolicy,
                "policy_agent_family_policy_remove",
                "Removes an existing policy from an agent family."
            )
        ];

    private static Task<List<PolicyAgentFamilySummaryDto>> ListAgentFamilies(
        IServiceProvider services
    ) =>
        PolicyMcpActionExecutor.ExecuteQuery<
            ListPolicyAgentFamiliesQuery,
            List<PolicyAgentFamilySummaryDto>
        >(services, _ => { });

    private static Task<List<PolicyDto>?> ListPolicies(
        IServiceProvider services,
        string agentFamilyName
    ) =>
        PolicyMcpActionExecutor.ExecuteQuery<
            ListAgentFamilyPoliciesQuery,
            List<PolicyDto>?
        >(
            services,
            query => query.AgentFamilyName = agentFamilyName
        );

    private static Task<PolicyCommandResult> CreateAgentFamily(
        IServiceProvider services,
        string agentFamilyName,
        string description
    ) =>
        PolicyMcpActionExecutor.ExecuteCommand<
            CreateAgentFamilyCommand,
            PolicyCommandResult
        >(
            services,
            command =>
            {
                command.AgentFamilyName = agentFamilyName;
                command.Description = description;
            }
        );

    private static Task<PolicyCommandResult> UpdateAgentFamily(
        IServiceProvider services,
        string agentFamilyName,
        string description
    ) =>
        PolicyMcpActionExecutor.ExecuteCommand<
            UpdateAgentFamilyCommand,
            PolicyCommandResult
        >(
            services,
            command =>
            {
                command.AgentFamilyName = agentFamilyName;
                command.Description = description;
            }
        );

    private static Task<PolicyCommandResult> RemoveAgentFamily(
        IServiceProvider services,
        string agentFamilyName
    ) =>
        PolicyMcpActionExecutor.ExecuteCommand<
            RemoveAgentFamilyCommand,
            PolicyCommandResult
        >(
            services,
            command => command.AgentFamilyName = agentFamilyName
        );

    private static Task<PolicyAddedCommandResult> AddPolicy(
        IServiceProvider services,
        string agentFamilyName,
        string title,
        string description
    ) =>
        PolicyMcpActionExecutor.ExecuteCommand<
            AddAgentFamilyPolicyCommand,
            PolicyAddedCommandResult
        >(
            services,
            command =>
            {
                command.AgentFamilyName = agentFamilyName;
                command.Title = title;
                command.Description = description;
            }
        );

    private static Task<PolicyCommandResult> UpdatePolicy(
        IServiceProvider services,
        string agentFamilyName,
        Guid policyId,
        string title,
        string description
    ) =>
        PolicyMcpActionExecutor.ExecuteCommand<
            UpdateAgentFamilyPolicyCommand,
            PolicyCommandResult
        >(
            services,
            command =>
            {
                command.AgentFamilyName = agentFamilyName;
                command.PolicyId = policyId;
                command.Title = title;
                command.Description = description;
            }
        );

    private static Task<PolicyCommandResult> RemovePolicy(
        IServiceProvider services,
        string agentFamilyName,
        Guid policyId
    ) =>
        PolicyMcpActionExecutor.ExecuteCommand<
            RemoveAgentFamilyPolicyCommand,
            PolicyCommandResult
        >(
            services,
            command =>
            {
                command.AgentFamilyName = agentFamilyName;
                command.PolicyId = policyId;
            }
        );
}

using Microsoft.Extensions.AI;
using PolicyModule.Application.Commands;
using PolicyModule.Application.DTOs;
using PolicyModule.Application.Models;
using PolicyModule.Application.Queries;

namespace PolicyModule.MCP;

internal static class TopicPolicyMcpFunctions
{
    public static List<AIFunction> Create() =>
        [
            PolicyMcpFunctions.Create(
                ListTopics,
                "policy_topic_list",
                "Lists all existing policy topics with their descriptions and policy counts."
            ),
            PolicyMcpFunctions.Create(
            ListPolicies,
            "policy_topic_policy_list",
            "Lists the policies grouped under a topic."
        ),
            PolicyMcpFunctions.Create(
            CreateTopic,
            "policy_topic_create",
            "Creates a topic used to group reusable policies."
        ),
            PolicyMcpFunctions.Create(
            UpdateTopic,
            "policy_topic_update",
            "Updates an existing policy topic's description."
        ),
            PolicyMcpFunctions.Create(
            RemoveTopic,
            "policy_topic_remove",
            "Removes an existing policy topic."
        ),
            PolicyMcpFunctions.Create(
            AddPolicy,
            "policy_topic_policy_add",
            "Adds a policy to an existing topic."
        ),
            PolicyMcpFunctions.Create(
            UpdatePolicy,
            "policy_topic_policy_update",
            "Updates an existing policy in a topic."
        ),
            PolicyMcpFunctions.Create(
            RemovePolicy,
            "policy_topic_policy_remove",
            "Removes an existing policy from a topic."
        )
        ];

    private static Task<List<PolicyTopicSummaryDto>> ListTopics(
        IServiceProvider services
    ) =>
        PolicyMcpActionExecutor.ExecuteQuery<
            ListPolicyTopicsQuery,
            List<PolicyTopicSummaryDto>
        >(services, _ => { });

    private static Task<List<PolicyDto>?> ListPolicies(
        IServiceProvider services,
        string topicName
    ) =>
        PolicyMcpActionExecutor.ExecuteQuery<ListTopicPoliciesQuery, List<PolicyDto>?>(
            services,
            query => query.TopicName = topicName
        );

    private static Task<PolicyCommandResult> CreateTopic(
        IServiceProvider services,
        string topicName,
        string description
    ) =>
        PolicyMcpActionExecutor.ExecuteCommand<CreateTopicCommand, PolicyCommandResult>(
            services,
            command =>
            {
                command.TopicName = topicName;
                command.Description = description;
            }
        );

    private static Task<PolicyCommandResult> UpdateTopic(
        IServiceProvider services,
        string topicName,
        string description
    ) =>
        PolicyMcpActionExecutor.ExecuteCommand<UpdateTopicCommand, PolicyCommandResult>(
            services,
            command =>
            {
                command.TopicName = topicName;
                command.Description = description;
            }
        );

    private static Task<PolicyCommandResult> RemoveTopic(
        IServiceProvider services,
        string topicName
    ) =>
        PolicyMcpActionExecutor.ExecuteCommand<RemoveTopicCommand, PolicyCommandResult>(
            services,
            command => command.TopicName = topicName
        );

    private static Task<PolicyAddedCommandResult> AddPolicy(
        IServiceProvider services,
        string topicName,
        string title,
        string description
    ) =>
        PolicyMcpActionExecutor.ExecuteCommand<AddTopicPolicyCommand, PolicyAddedCommandResult>(
            services,
            command =>
            {
                command.TopicName = topicName;
                command.Title = title;
                command.Description = description;
            }
        );

    private static Task<PolicyCommandResult> UpdatePolicy(
        IServiceProvider services,
        string topicName,
        Guid policyId,
        string title,
        string description
    ) =>
        PolicyMcpActionExecutor.ExecuteCommand<UpdateTopicPolicyCommand, PolicyCommandResult>(
            services,
            command =>
            {
                command.TopicName = topicName;
                command.PolicyId = policyId;
                command.Title = title;
                command.Description = description;
            }
        );

    private static Task<PolicyCommandResult> RemovePolicy(
        IServiceProvider services,
        string topicName,
        Guid policyId
    ) =>
        PolicyMcpActionExecutor.ExecuteCommand<RemoveTopicPolicyCommand, PolicyCommandResult>(
            services,
            command =>
            {
                command.TopicName = topicName;
                command.PolicyId = policyId;
            }
        );
}

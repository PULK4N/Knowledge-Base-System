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
            "Returns the policies grouped under a topic as joined Markdown text."
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

    private static async Task<string> ListPolicies(
        IServiceProvider services,
        string topicName
    ) =>
        PolicyTextFormatter.Format(await PolicyMcpActionExecutor.ExecuteQuery<ListTopicPoliciesQuery, List<PolicyDto>?>(
            services,
            query => query.TopicName = topicName
        ));

    private static Task<PolicyCommandResult> CreateTopic(
        IServiceProvider services,
        string topicName,
        string description,
        Guid? sessionId = null,
        Guid? memoryAggregateId = null
    ) =>
        PolicyMcpActionExecutor.ExecuteCommand<CreateTopicCommand, PolicyCommandResult>(
            services,
            command =>
            {
                command.TopicName = topicName;
                command.Description = description;
                command.SessionId = sessionId.GetValueOrDefault();
                command.MemoryAggregateId = memoryAggregateId.GetValueOrDefault();
            }
        );

    private static Task<PolicyCommandResult> UpdateTopic(
        IServiceProvider services,
        string topicName,
        string description,
        Guid? sessionId = null,
        Guid? memoryAggregateId = null
    ) =>
        PolicyMcpActionExecutor.ExecuteCommand<UpdateTopicCommand, PolicyCommandResult>(
            services,
            command =>
            {
                command.TopicName = topicName;
                command.Description = description;
                command.SessionId = sessionId.GetValueOrDefault();
                command.MemoryAggregateId = memoryAggregateId.GetValueOrDefault();
            }
        );

    private static Task<PolicyCommandResult> RemoveTopic(
        IServiceProvider services,
        string topicName,
        Guid? sessionId = null,
        Guid? memoryAggregateId = null
    ) =>
        PolicyMcpActionExecutor.ExecuteCommand<RemoveTopicCommand, PolicyCommandResult>(
            services,
            command =>
            {
                command.TopicName = topicName;
                command.SessionId = sessionId.GetValueOrDefault();
                command.MemoryAggregateId = memoryAggregateId.GetValueOrDefault();
            }
        );

    private static Task<PolicyAddedCommandResult> AddPolicy(
        IServiceProvider services,
        string topicName,
        string title,
        string description,
        Guid? sessionId = null,
        Guid? memoryAggregateId = null
    ) =>
        PolicyMcpActionExecutor.ExecuteCommand<AddTopicPolicyCommand, PolicyAddedCommandResult>(
            services,
            command =>
            {
                command.TopicName = topicName;
                command.Title = title;
                command.Description = description;
                command.SessionId = sessionId.GetValueOrDefault();
                command.MemoryAggregateId = memoryAggregateId.GetValueOrDefault();
            }
        );

    private static Task<PolicyCommandResult> UpdatePolicy(
        IServiceProvider services,
        string topicName,
        Guid policyId,
        string title,
        string description,
        Guid? sessionId = null,
        Guid? memoryAggregateId = null
    ) =>
        PolicyMcpActionExecutor.ExecuteCommand<UpdateTopicPolicyCommand, PolicyCommandResult>(
            services,
            command =>
            {
                command.TopicName = topicName;
                command.PolicyId = policyId;
                command.Title = title;
                command.Description = description;
                command.SessionId = sessionId.GetValueOrDefault();
                command.MemoryAggregateId = memoryAggregateId.GetValueOrDefault();
            }
        );

    private static Task<PolicyCommandResult> RemovePolicy(
        IServiceProvider services,
        string topicName,
        Guid policyId,
        Guid? sessionId = null,
        Guid? memoryAggregateId = null
    ) =>
        PolicyMcpActionExecutor.ExecuteCommand<RemoveTopicPolicyCommand, PolicyCommandResult>(
            services,
            command =>
            {
                command.TopicName = topicName;
                command.PolicyId = policyId;
                command.SessionId = sessionId.GetValueOrDefault();
                command.MemoryAggregateId = memoryAggregateId.GetValueOrDefault();
            }
        );
}

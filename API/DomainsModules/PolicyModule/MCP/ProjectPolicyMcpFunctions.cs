using Microsoft.Extensions.AI;
using PolicyModule.Application.Commands;
using PolicyModule.Application.DTOs;
using PolicyModule.Application.Models;
using PolicyModule.Application.Queries;

namespace PolicyModule.MCP;

internal static class ProjectPolicyMcpFunctions
{
    public static List<AIFunction> Create() =>
        [
            PolicyMcpFunctions.Create(
            ListPolicies,
            "policy_project_policy_list",
            "Lists the policies owned directly by a project."
        ),
            PolicyMcpFunctions.Create(
            CreateProject,
            "policy_project_create",
            "Creates a policy project and maps its absolute repository paths to it."
        ),
            PolicyMcpFunctions.Create(
            UpdateProject,
            "policy_project_update",
            "Updates an existing policy project's name and description."
        ),
            PolicyMcpFunctions.Create(
            DeleteProject,
            "policy_project_delete",
            "Deletes a policy project and removes its repository-path mappings."
        ),
            PolicyMcpFunctions.Create(
            AddRepository,
            "policy_project_repository_add",
            "Maps an absolute repository path to an existing policy project. Use the project ID selected by the user after policy_get_by_repository returns RepositoryMappingRequired."
        ),
            PolicyMcpFunctions.Create(
            AddPolicy,
            "policy_project_policy_add",
            "Adds a policy owned directly by a project."
        ),
            PolicyMcpFunctions.Create(
            UpdatePolicy,
            "policy_project_policy_update",
            "Updates a policy owned directly by a project."
        ),
            PolicyMcpFunctions.Create(
            RemovePolicy,
            "policy_project_policy_remove",
            "Removes a policy owned directly by a project."
        ),
            PolicyMcpFunctions.Create(
            AddTopic,
            "policy_project_topic_add",
            "Associates a policy topic with a project."
        ),
            PolicyMcpFunctions.Create(
            RemoveTopic,
            "policy_project_topic_remove",
            "Removes a policy topic association from a project."
        )
        ];

    private static Task<List<PolicyDto>?> ListPolicies(IServiceProvider services, Guid projectId) =>
        PolicyMcpActionExecutor.ExecuteQuery<ListProjectPoliciesQuery, List<PolicyDto>?>(
            services,
            query => query.ProjectId = projectId
        );

    private static Task<ProjectCreatedCommandResult> CreateProject(
        IServiceProvider services,
        string projectName,
        string projectDescription,
        List<string> repositoryPaths
    ) =>
        PolicyMcpActionExecutor.ExecuteCommand<CreateProjectCommand, ProjectCreatedCommandResult>(
            services,
            command =>
            {
                command.ProjectName = projectName;
                command.ProjectDescription = projectDescription;
                command.RepositoryPaths = repositoryPaths;
            }
        );

    private static Task<PolicyCommandResult> UpdateProject(
        IServiceProvider services,
        Guid projectId,
        string projectName,
        string projectDescription
    ) =>
        PolicyMcpActionExecutor.ExecuteCommand<UpdateProjectCommand, PolicyCommandResult>(
            services,
            command =>
            {
                command.ProjectId = projectId;
                command.ProjectName = projectName;
                command.ProjectDescription = projectDescription;
            }
        );

    private static Task<PolicyCommandResult> DeleteProject(
        IServiceProvider services,
        Guid projectId
    ) =>
        PolicyMcpActionExecutor.ExecuteCommand<DeleteProjectCommand, PolicyCommandResult>(
            services,
            command => command.ProjectId = projectId
        );

    private static Task<PolicyCommandResult> AddRepository(
        IServiceProvider services,
        Guid projectId,
        string repositoryPath
    ) =>
        PolicyMcpActionExecutor.ExecuteCommand<
            AddRepositoryToProjectCommand,
            PolicyCommandResult
        >(
            services,
            command =>
            {
                command.ProjectId = projectId;
                command.RepositoryPath = repositoryPath;
            }
        );

    private static Task<PolicyAddedCommandResult> AddPolicy(
        IServiceProvider services,
        Guid projectId,
        string title,
        string description
    ) =>
        PolicyMcpActionExecutor.ExecuteCommand<AddProjectPolicyCommand, PolicyAddedCommandResult>(
            services,
            command =>
            {
                command.ProjectId = projectId;
                command.Title = title;
                command.Description = description;
            }
        );

    private static Task<PolicyCommandResult> UpdatePolicy(
        IServiceProvider services,
        Guid projectId,
        Guid policyId,
        string title,
        string description
    ) =>
        PolicyMcpActionExecutor.ExecuteCommand<UpdateProjectPolicyCommand, PolicyCommandResult>(
            services,
            command =>
            {
                command.ProjectId = projectId;
                command.PolicyId = policyId;
                command.Title = title;
                command.Description = description;
            }
        );

    private static Task<PolicyCommandResult> RemovePolicy(
        IServiceProvider services,
        Guid projectId,
        Guid policyId
    ) =>
        PolicyMcpActionExecutor.ExecuteCommand<RemoveProjectPolicyCommand, PolicyCommandResult>(
            services,
            command =>
            {
                command.ProjectId = projectId;
                command.PolicyId = policyId;
            }
        );

    private static Task<PolicyCommandResult> AddTopic(
        IServiceProvider services,
        Guid projectId,
        string topicName
    ) =>
        PolicyMcpActionExecutor.ExecuteCommand<
            AddTopicRelationToProjectCommand,
            PolicyCommandResult
        >(
            services,
            command =>
            {
                command.ProjectId = projectId;
                command.TopicName = topicName;
            }
        );

    private static Task<PolicyCommandResult> RemoveTopic(
        IServiceProvider services,
        Guid projectId,
        string topicName
    ) =>
        PolicyMcpActionExecutor.ExecuteCommand<
            RemoveTopicRelationFromProjectCommand,
            PolicyCommandResult
        >(
            services,
            command =>
            {
                command.ProjectId = projectId;
                command.TopicName = topicName;
            }
        );
}

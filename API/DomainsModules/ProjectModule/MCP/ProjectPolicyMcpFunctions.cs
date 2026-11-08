using Microsoft.Extensions.AI;
using ProjectModule.Application.Commands;
using ProjectModule.Application.DTOs;
using ProjectModule.Application.Models;
using ProjectModule.Application.Queries;

namespace ProjectModule.MCP;

internal static class ProjectPolicyMcpFunctions
{
    public static List<AIFunction> Create() =>
        [
            ProjectMcpFunctions.Create(
            ListProjects,
            "policy_project_list",
            "Lists all active policy projects by name and ID."
        ),
            ProjectMcpFunctions.Create(
            GetProjectByName,
            "policy_project_get_by_name",
            "Gets an active policy project summary by its exact case-insensitive name."
        ),
            ProjectMcpFunctions.Create(
            ListPolicies,
            "policy_project_policy_list",
            "Returns the policies owned directly by a project as joined Markdown text."
        ),
            ProjectMcpFunctions.Create(
            CreateProject,
            "project_create",
            "Creates a policy project and maps its absolute repository paths to it."
        ),
            ProjectMcpFunctions.Create(
            UpdateProject,
            "policy_project_update",
            "Updates an existing policy project's name and description."
        ),
            ProjectMcpFunctions.Create(
            DeleteProject,
            "policy_project_delete",
            "Deletes a policy project and removes its repository-path mappings."
        ),
            ProjectMcpFunctions.Create(
            AddRepository,
            "policy_project_repository_add",
            "Maps an absolute repository path to an existing policy project. Use the project ID selected by the user after policy_get_by_repository returns RepositoryMappingRequired."
        ),
            ProjectMcpFunctions.Create(
            AddPolicy,
            "policy_project_policy_add",
            "Adds a policy owned directly by a project."
        ),
            ProjectMcpFunctions.Create(
            UpdatePolicy,
            "policy_project_policy_update",
            "Updates a policy owned directly by a project."
        ),
            ProjectMcpFunctions.Create(
            RemovePolicy,
            "policy_project_policy_remove",
            "Removes a policy owned directly by a project."
        ),
            ProjectMcpFunctions.Create(
            AddTopic,
            "policy_project_topic_add",
            "Associates a policy topic with a project."
        ),
            ProjectMcpFunctions.Create(
            RemoveTopic,
            "policy_project_topic_remove",
            "Removes a policy topic association from a project."
        )
        ];

    private static Task<List<PolicyProjectSummaryDto>> ListProjects(
        IServiceProvider services
    ) =>
        ProjectMcpActionExecutor.ExecuteQuery<
            ListPolicyProjectsQuery,
            List<PolicyProjectSummaryDto>
        >(services, _ => { });

    private static Task<PolicyProjectSummaryDto?> GetProjectByName(
        IServiceProvider services,
        string name
    ) =>
        ProjectMcpActionExecutor.ExecuteQuery<
            GetPolicyProjectByNameQuery,
            PolicyProjectSummaryDto?
        >(
            services,
            query => query.Name = name
        );

    private static async Task<string> ListPolicies(IServiceProvider services, Guid projectId) =>
        PolicyTextFormatter.Format(await ProjectMcpActionExecutor.ExecuteQuery<ListProjectPoliciesQuery, List<PolicyDto>?>(
            services,
            query => query.ProjectId = projectId
        ));

    private static Task<ProjectCreatedCommandResult> CreateProject(
        IServiceProvider services,
        string projectName,
        string projectDescription,
        List<string> repositoryPaths,
        Guid? sessionId = null,
        Guid? memoryAggregateId = null
    ) =>
        ProjectMcpActionExecutor.ExecuteCommand<CreateProjectCommand, ProjectCreatedCommandResult>(
            services,
            command =>
            {
                command.ProjectName = projectName;
                command.ProjectDescription = projectDescription;
                command.RepositoryPaths = repositoryPaths;
                command.SessionId = sessionId.GetValueOrDefault();
                command.MemoryAggregateId = memoryAggregateId.GetValueOrDefault();
            }
        );

    private static Task<PolicyCommandResult> UpdateProject(
        IServiceProvider services,
        Guid projectId,
        string projectName,
        string projectDescription,
        Guid? sessionId = null,
        Guid? memoryAggregateId = null
    ) =>
        ProjectMcpActionExecutor.ExecuteCommand<UpdateProjectCommand, PolicyCommandResult>(
            services,
            command =>
            {
                command.ProjectId = projectId;
                command.ProjectName = projectName;
                command.ProjectDescription = projectDescription;
                command.SessionId = sessionId.GetValueOrDefault();
                command.MemoryAggregateId = memoryAggregateId.GetValueOrDefault();
            }
        );

    private static Task<PolicyCommandResult> DeleteProject(
        IServiceProvider services,
        Guid projectId,
        Guid? sessionId = null,
        Guid? memoryAggregateId = null
    ) =>
        ProjectMcpActionExecutor.ExecuteCommand<DeleteProjectCommand, PolicyCommandResult>(
            services,
            command =>
            {
                command.ProjectId = projectId;
                command.SessionId = sessionId.GetValueOrDefault();
                command.MemoryAggregateId = memoryAggregateId.GetValueOrDefault();
            }
        );

    private static Task<PolicyCommandResult> AddRepository(
        IServiceProvider services,
        Guid projectId,
        string repositoryPath,
        Guid? sessionId = null,
        Guid? memoryAggregateId = null
    ) =>
        ProjectMcpActionExecutor.ExecuteCommand<
            AddRepositoryToProjectCommand,
            PolicyCommandResult
        >(
            services,
            command =>
            {
                command.ProjectId = projectId;
                command.RepositoryPath = repositoryPath;
                command.SessionId = sessionId.GetValueOrDefault();
                command.MemoryAggregateId = memoryAggregateId.GetValueOrDefault();
            }
        );

    private static Task<PolicyAddedCommandResult> AddPolicy(
        IServiceProvider services,
        Guid projectId,
        string title,
        string description,
        Guid? sessionId = null,
        Guid? memoryAggregateId = null
    ) =>
        ProjectMcpActionExecutor.ExecuteCommand<AddProjectPolicyCommand, PolicyAddedCommandResult>(
            services,
            command =>
            {
                command.ProjectId = projectId;
                command.Title = title;
                command.Description = description;
                command.SessionId = sessionId.GetValueOrDefault();
                command.MemoryAggregateId = memoryAggregateId.GetValueOrDefault();
            }
        );

    private static Task<PolicyCommandResult> UpdatePolicy(
        IServiceProvider services,
        Guid projectId,
        Guid policyId,
        string title,
        string description,
        Guid? sessionId = null,
        Guid? memoryAggregateId = null
    ) =>
        ProjectMcpActionExecutor.ExecuteCommand<UpdateProjectPolicyCommand, PolicyCommandResult>(
            services,
            command =>
            {
                command.ProjectId = projectId;
                command.PolicyId = policyId;
                command.Title = title;
                command.Description = description;
                command.SessionId = sessionId.GetValueOrDefault();
                command.MemoryAggregateId = memoryAggregateId.GetValueOrDefault();
            }
        );

    private static Task<PolicyCommandResult> RemovePolicy(
        IServiceProvider services,
        Guid projectId,
        Guid policyId,
        Guid? sessionId = null,
        Guid? memoryAggregateId = null
    ) =>
        ProjectMcpActionExecutor.ExecuteCommand<RemoveProjectPolicyCommand, PolicyCommandResult>(
            services,
            command =>
            {
                command.ProjectId = projectId;
                command.PolicyId = policyId;
                command.SessionId = sessionId.GetValueOrDefault();
                command.MemoryAggregateId = memoryAggregateId.GetValueOrDefault();
            }
        );

    private static Task<PolicyCommandResult> AddTopic(
        IServiceProvider services,
        Guid projectId,
        string topicName,
        Guid? sessionId = null,
        Guid? memoryAggregateId = null
    ) =>
        ProjectMcpActionExecutor.ExecuteCommand<
            AddTopicRelationToProjectCommand,
            PolicyCommandResult
        >(
            services,
            command =>
            {
                command.ProjectId = projectId;
                command.TopicName = topicName;
                command.SessionId = sessionId.GetValueOrDefault();
                command.MemoryAggregateId = memoryAggregateId.GetValueOrDefault();
            }
        );

    private static Task<PolicyCommandResult> RemoveTopic(
        IServiceProvider services,
        Guid projectId,
        string topicName,
        Guid? sessionId = null,
        Guid? memoryAggregateId = null
    ) =>
        ProjectMcpActionExecutor.ExecuteCommand<
            RemoveTopicRelationFromProjectCommand,
            PolicyCommandResult
        >(
            services,
            command =>
            {
                command.ProjectId = projectId;
                command.TopicName = topicName;
                command.SessionId = sessionId.GetValueOrDefault();
                command.MemoryAggregateId = memoryAggregateId.GetValueOrDefault();
            }
        );
}

using Microsoft.Extensions.AI;
using ProjectModule.Application.Commands;

namespace ProjectModule.MCP;

internal static class ProjectSkillMcpFunctions
{
    public static List<AIFunction> Create() =>
        [
            ProjectMcpFunctions.Create(
                AddSkill,
                "project_skill_add",
                "Links a skill to a project. The project's linked skills and their use scenarios are listed in the session policies under \"Project skills\"."
            ),
            ProjectMcpFunctions.Create(
                RemoveSkill,
                "project_skill_remove",
                "Removes the link between a skill and a project."
            )
        ];

    private static Task<PolicyCommandResult> AddSkill(
        IServiceProvider services,
        Guid projectId,
        Guid skillId,
        Guid? sessionId = null,
        Guid? memoryAggregateId = null
    ) =>
        ProjectMcpActionExecutor.ExecuteCommand<
            AddSkillToProjectCommand,
            PolicyCommandResult
        >(
            services,
            command =>
            {
                command.ProjectId = projectId;
                command.SkillId = skillId;
                command.SessionId = sessionId.GetValueOrDefault();
                command.MemoryAggregateId = memoryAggregateId.GetValueOrDefault();
            }
        );

    private static Task<PolicyCommandResult> RemoveSkill(
        IServiceProvider services,
        Guid projectId,
        Guid skillId,
        Guid? sessionId = null,
        Guid? memoryAggregateId = null
    ) =>
        ProjectMcpActionExecutor.ExecuteCommand<
            RemoveSkillFromProjectCommand,
            PolicyCommandResult
        >(
            services,
            command =>
            {
                command.ProjectId = projectId;
                command.SkillId = skillId;
                command.SessionId = sessionId.GetValueOrDefault();
                command.MemoryAggregateId = memoryAggregateId.GetValueOrDefault();
            }
        );
}

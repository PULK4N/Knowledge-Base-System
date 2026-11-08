using ProjectModule.Shared;
using Microsoft.Extensions.AI;
using System.ComponentModel;
using ProjectModule.Application.Commands;
using ProjectModule.Domain.Models;

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
                UpdateSkill,
                "project_skill_update",
                "Replaces how a linked skill takes part in the project's code review."
            ),
            ProjectMcpFunctions.Create(
                RemoveSkill,
                "project_skill_remove",
                "Removes the link between a skill and a project."
            ),
            ProjectMcpFunctions.Create(
                SetReviewLevelModels,
                "project_review_level_models_set",
                "Replaces the project's mapping from review level (Easy, Medium, Heavy) to the model and reasoning setting a review script runs."
            )
        ];

    private static Task<PolicyCommandResult> AddSkill(
        IServiceProvider services,
        Guid projectId,
        Guid skillId,
        [Description("Whether code review runs this skill. A review level is then required.")]
        bool usedForReview = false,
        [Description("Extra instructions for the reviewer that uses this skill.")]
        string? reviewNotes = null,
        ReviewLevel? reviewLevel = null,
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
                command.UsedForReview = usedForReview;
                command.ReviewNotes = reviewNotes ?? string.Empty;
                command.ReviewLevel = reviewLevel;
                command.SessionId = sessionId.GetValueOrDefault();
                command.MemoryAggregateId = memoryAggregateId.GetValueOrDefault();
            }
        );

    private static Task<PolicyCommandResult> UpdateSkill(
        IServiceProvider services,
        Guid projectId,
        Guid skillId,
        [Description("Whether code review runs this skill. A review level is then required.")]
        bool usedForReview,
        [Description("Extra instructions for the reviewer that uses this skill.")]
        string? reviewNotes = null,
        ReviewLevel? reviewLevel = null,
        Guid? sessionId = null,
        Guid? memoryAggregateId = null
    ) =>
        ProjectMcpActionExecutor.ExecuteCommand<
            UpdateProjectSkillCommand,
            PolicyCommandResult
        >(
            services,
            command =>
            {
                command.ProjectId = projectId;
                command.SkillId = skillId;
                command.UsedForReview = usedForReview;
                command.ReviewNotes = reviewNotes ?? string.Empty;
                command.ReviewLevel = reviewLevel;
                command.SessionId = sessionId.GetValueOrDefault();
                command.MemoryAggregateId = memoryAggregateId.GetValueOrDefault();
            }
        );

    private static Task<PolicyCommandResult> SetReviewLevelModels(
        IServiceProvider services,
        Guid projectId,
        [Description("Review level name mapped to its model and reasoning, for example {\"Medium\": {\"model\": \"gpt-6.1-sol\", \"reasoning\": \"high\"}}. Levels left out have no model.")]
        Dictionary<ReviewLevel, ReviewModel> levelModels,
        Guid? sessionId = null,
        Guid? memoryAggregateId = null
    ) =>
        ProjectMcpActionExecutor.ExecuteCommand<
            SetProjectReviewLevelModelsCommand,
            PolicyCommandResult
        >(
            services,
            command =>
            {
                command.ProjectId = projectId;
                command.LevelModels = levelModels;
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

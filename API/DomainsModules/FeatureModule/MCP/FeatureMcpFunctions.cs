using FeatureModule.Application.Commands;
using FeatureModule.Application.DTOs;
using FeatureModule.Application.Models;
using FeatureModule.Application.Queries;
using FeatureModule.Domain.Models;
using Microsoft.Extensions.AI;

namespace FeatureModule.MCP;

public static class FeatureMcpFunctions
{
    public static List<AIFunction> Create() =>
    [
        CreateFunction(
            (Func<IServiceProvider, Guid, uint, Task<FeatureDto?>>)Get,
            "feature_get",
            "Gets a feature, including its progress description, related skills, conversation records, plans, and current plan. Set orderNumber to zero for the latest state or to an event order number for historical state."
        ),
        CreateFunction(
            (Func<IServiceProvider, Guid, string, string, string, Task<FeatureCreatedCommandResult>>)Add,
            "feature_add",
            "Creates a feature for an existing project with a name, summary, and free-form progress status."
        ),
        CreateFunction(
            (Func<IServiceProvider, Guid, Task<FeatureCommandResult>>)Remove,
            "feature_remove",
            "Marks a feature as deleted while retaining its event history."
        ),
        CreateFunction(
            (Func<IServiceProvider, Guid, string, Task<FeatureCommandResult>>)UpdateStatus,
            "feature_status_update",
            "Replaces a feature's free-form progress description."
        ),
        CreateFunction(
            (Func<IServiceProvider, Guid, Guid, Task<FeatureCommandResult>>)AddSkill,
            "feature_skill_add",
            "Adds a skill ID that provides useful context for the feature."
        ),
        CreateFunction(
            (Func<IServiceProvider, Guid, Guid, Task<FeatureCommandResult>>)RemoveSkill,
            "feature_skill_remove",
            "Removes a related skill ID from the feature."
        ),
        CreateFunction(
            (Func<IServiceProvider, Guid, string, string, Task<FeatureRecordCreatedCommandResult>>)AddRecord,
            "feature_record_add",
            "Adds a curated user message and AI answer that affected the feature or its implementation plan."
        ),
        CreateFunction(
            (Func<IServiceProvider, Guid, Guid, string, string, Task<FeatureCommandResult>>)UpdateRecord,
            "feature_record_update",
            "Updates the user message and AI answer of an existing feature record."
        ),
        CreateFunction(
            (Func<IServiceProvider, Guid, Guid, Task<FeatureCommandResult>>)RemoveRecord,
            "feature_record_remove",
            "Removes a feature record from the current feature state."
        ),
        CreateFunction(
            (Func<IServiceProvider, Guid, string, string, FeaturePlanContentType, Task<FeaturePlanCreatedCommandResult>>)AddPlan,
            "feature_plan_add",
            "Adds a Markdown or HTML plan and selects it as the current feature plan."
        ),
        CreateFunction(
            (Func<IServiceProvider, Guid, string, string, FeaturePlanContentType, Task<FeatureCommandResult>>)UpdateCurrentPlan,
            "feature_plan_current_update",
            "Updates the title, content, and content type of the current feature plan."
        ),
        CreateFunction(
            (Func<IServiceProvider, Guid, Guid, Task<FeatureCommandResult>>)ChangeCurrentPlan,
            "feature_plan_current_change",
            "Selects an existing previous plan as the current feature plan."
        ),
        CreateFunction(
            (Func<IServiceProvider, Guid, Guid, Task<FeatureCommandResult>>)RemovePlan,
            "feature_plan_remove",
            "Removes a plan; removing the current plan clears the current selection."
        )
    ];

    private static AIFunction CreateFunction(
        Delegate method,
        string name,
        string description
    ) =>
        AIFunctionFactory.Create(
            method,
            new AIFunctionFactoryOptions
            {
                Name = name,
                Description = description
            }
        );

    private static Task<FeatureDto?> Get(
        IServiceProvider services,
        Guid featureId,
        uint orderNumber = 0
    ) =>
        FeatureMcpActionExecutor.ExecuteQuery<GetFeatureQuery, FeatureDto?>(
            services,
            query =>
            {
                query.FeatureId = featureId;
                query.OrderNumber = orderNumber;
            }
        );

    private static Task<FeatureCreatedCommandResult> Add(
        IServiceProvider services,
        Guid projectId,
        string name,
        string summary,
        string status
    ) =>
        FeatureMcpActionExecutor.ExecuteCommand<
            AddFeatureCommand,
            FeatureCreatedCommandResult
        >(
            services,
            command =>
            {
                command.ProjectId = projectId;
                command.Name = name;
                command.Summary = summary;
                command.Status = status;
            }
        );

    private static Task<FeatureCommandResult> Remove(
        IServiceProvider services,
        Guid featureId
    ) =>
        FeatureMcpActionExecutor.ExecuteCommand<
            RemoveFeatureCommand,
            FeatureCommandResult
        >(
            services,
            command => command.FeatureId = featureId
        );

    private static Task<FeatureCommandResult> UpdateStatus(
        IServiceProvider services,
        Guid featureId,
        string status
    ) =>
        FeatureMcpActionExecutor.ExecuteCommand<
            UpdateFeatureStatusCommand,
            FeatureCommandResult
        >(
            services,
            command =>
            {
                command.FeatureId = featureId;
                command.Status = status;
            }
        );

    private static Task<FeatureCommandResult> AddSkill(
        IServiceProvider services,
        Guid featureId,
        Guid skillId
    ) =>
        FeatureMcpActionExecutor.ExecuteCommand<
            AddFeatureSkillCommand,
            FeatureCommandResult
        >(
            services,
            command =>
            {
                command.FeatureId = featureId;
                command.SkillId = skillId;
            }
        );

    private static Task<FeatureCommandResult> RemoveSkill(
        IServiceProvider services,
        Guid featureId,
        Guid skillId
    ) =>
        FeatureMcpActionExecutor.ExecuteCommand<
            RemoveFeatureSkillCommand,
            FeatureCommandResult
        >(
            services,
            command =>
            {
                command.FeatureId = featureId;
                command.SkillId = skillId;
            }
        );

    private static Task<FeatureRecordCreatedCommandResult> AddRecord(
        IServiceProvider services,
        Guid featureId,
        string userMessage,
        string aiAnswer
    ) =>
        FeatureMcpActionExecutor.ExecuteCommand<
            AddFeatureRecordCommand,
            FeatureRecordCreatedCommandResult
        >(
            services,
            command =>
            {
                command.FeatureId = featureId;
                command.UserMessage = userMessage;
                command.AiAnswer = aiAnswer;
            }
        );

    private static Task<FeatureCommandResult> UpdateRecord(
        IServiceProvider services,
        Guid featureId,
        Guid recordId,
        string userMessage,
        string aiAnswer
    ) =>
        FeatureMcpActionExecutor.ExecuteCommand<
            UpdateFeatureRecordCommand,
            FeatureCommandResult
        >(
            services,
            command =>
            {
                command.FeatureId = featureId;
                command.RecordId = recordId;
                command.UserMessage = userMessage;
                command.AiAnswer = aiAnswer;
            }
        );

    private static Task<FeatureCommandResult> RemoveRecord(
        IServiceProvider services,
        Guid featureId,
        Guid recordId
    ) =>
        FeatureMcpActionExecutor.ExecuteCommand<
            RemoveFeatureRecordCommand,
            FeatureCommandResult
        >(
            services,
            command =>
            {
                command.FeatureId = featureId;
                command.RecordId = recordId;
            }
        );

    private static Task<FeaturePlanCreatedCommandResult> AddPlan(
        IServiceProvider services,
        Guid featureId,
        string title,
        string content,
        FeaturePlanContentType contentType =
            FeaturePlanContentType.Markdown
    ) =>
        FeatureMcpActionExecutor.ExecuteCommand<
            AddFeaturePlanCommand,
            FeaturePlanCreatedCommandResult
        >(
            services,
            command =>
            {
                command.FeatureId = featureId;
                command.Title = title;
                command.Content = content;
                command.ContentType = contentType;
            }
        );

    private static Task<FeatureCommandResult> UpdateCurrentPlan(
        IServiceProvider services,
        Guid featureId,
        string title,
        string content,
        FeaturePlanContentType contentType =
            FeaturePlanContentType.Markdown
    ) =>
        FeatureMcpActionExecutor.ExecuteCommand<
            UpdateCurrentFeaturePlanCommand,
            FeatureCommandResult
        >(
            services,
            command =>
            {
                command.FeatureId = featureId;
                command.Title = title;
                command.Content = content;
                command.ContentType = contentType;
            }
        );

    private static Task<FeatureCommandResult> ChangeCurrentPlan(
        IServiceProvider services,
        Guid featureId,
        Guid planId
    ) =>
        FeatureMcpActionExecutor.ExecuteCommand<
            ChangeCurrentFeaturePlanCommand,
            FeatureCommandResult
        >(
            services,
            command =>
            {
                command.FeatureId = featureId;
                command.PlanId = planId;
            }
        );

    private static Task<FeatureCommandResult> RemovePlan(
        IServiceProvider services,
        Guid featureId,
        Guid planId
    ) =>
        FeatureMcpActionExecutor.ExecuteCommand<
            RemoveFeaturePlanCommand,
            FeatureCommandResult
        >(
            services,
            command =>
            {
                command.FeatureId = featureId;
                command.PlanId = planId;
            }
        );
}

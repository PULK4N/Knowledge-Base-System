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
            (Func<IServiceProvider, Task<List<FeatureSummaryDto>>>)List,
            "feature_list",
            "Lists active features by name and ID."
        ),
        CreateFunction(
            (Func<IServiceProvider, string, Task<FeatureSummaryDto?>>)GetByName,
            "feature_get_by_name",
            "Gets an active feature summary by its exact case-insensitive name."
        ),
        CreateFunction(
            (Func<IServiceProvider, Guid, uint, Task<FeatureMcpDto?>>)Get,
            "feature_get",
            "Gets a bounded feature context: its current plan, five latest research discoveries, five latest conversation records, and title/ID references for other plans and discoveries. Set orderNumber to zero for the latest state or to an event order number for historical state."
        ),
        CreateFunction(
            (Func<IServiceProvider, Guid, Guid, uint, Task<FeaturePlanDto?>>)GetPlan,
            "feature_plan_get",
            "Gets one feature plan by feature ID and plan ID."
        ),
        CreateFunction(
            (Func<IServiceProvider, Guid, List<Guid>, uint, Task<List<FeatureResearchDiscoveryDto>>>)GetResearchDiscoveries,
            "feature_research_discovery_get",
            "Gets multiple research discoveries in one call by feature ID and discovery IDs."
        ),
        CreateFunction(
            (Func<IServiceProvider, string, int, Task<List<FeatureResearchSearchMatchDto>>>)SearchResearchDiscoveries,
            "feature_research_discovery_search",
            "Searches research discoveries across active features using hybrid semantic vector and full-text ranking. Returns the highest-ranked chunk per discovery; use feature_research_discovery_get with the returned feature and discovery IDs to load complete discoveries."
        ),
        CreateFunction(
            (Func<IServiceProvider, Guid, uint, Task<List<FeatureRecordDto>>>)ListRecords,
            "feature_record_list",
            "Gets all conversation records for a feature."
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
            (Func<IServiceProvider, Guid, string, Task<FeatureCommandResult>>)UpdateSummary,
            "feature_summary_update",
            "Replaces a feature's summary."
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
            (Func<IServiceProvider, Guid, string, string, FeatureResearchDiscoverySourceType, string, Task<FeatureResearchDiscoveryCreatedCommandResult>>)AddResearchDiscovery,
            "feature_research_discovery_add",
            "Stores a research discovery for a feature. The optional source reference can be a code path, URL, MCP tool name, or other provenance."
        ),
        CreateFunction(
            (Func<IServiceProvider, Guid, Guid, string, string, FeatureResearchDiscoverySourceType, string, Task<FeatureCommandResult>>)UpdateResearchDiscovery,
            "feature_research_discovery_update",
            "Updates a stored feature research discovery and its provenance."
        ),
        CreateFunction(
            (Func<IServiceProvider, Guid, Guid, Task<FeatureCommandResult>>)RemoveResearchDiscovery,
            "feature_research_discovery_remove",
            "Removes a research discovery from the current feature state while retaining event history."
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

    private static Task<List<FeatureSummaryDto>> List(
        IServiceProvider services
    ) =>
        FeatureMcpActionExecutor.ExecuteQuery<
            ListFeaturesQuery,
            List<FeatureSummaryDto>
        >(services, _ => { });

    private static Task<FeatureSummaryDto?> GetByName(
        IServiceProvider services,
        string name
    ) =>
        FeatureMcpActionExecutor.ExecuteQuery<
            GetFeatureByNameQuery,
            FeatureSummaryDto?
        >(
            services,
            query => query.Name = name
        );

    private static async Task<FeatureMcpDto?> Get(
        IServiceProvider services,
        Guid featureId,
        uint orderNumber = 0
    )
    {
        var feature = await GetFullFeature(services, featureId, orderNumber);

        return feature is null ? null : FeatureMcpDto.FromFeature(feature);
    }

    private static async Task<FeaturePlanDto?> GetPlan(
        IServiceProvider services,
        Guid featureId,
        Guid planId,
        uint orderNumber = 0
    ) =>
        (await GetFullFeature(services, featureId, orderNumber))?.Plans
            .SingleOrDefault(plan => plan.Id == planId);

    private static async Task<List<FeatureResearchDiscoveryDto>> GetResearchDiscoveries(
        IServiceProvider services,
        Guid featureId,
        List<Guid> discoveryIds,
        uint orderNumber = 0
    )
    {
        var feature = await GetFullFeature(services, featureId, orderNumber);

        if (feature is null || discoveryIds.Count == 0)
        {
            return [];
        }

        var discoveriesById = feature.ResearchDiscoveries.ToDictionary(
            discovery => discovery.Id
        );

        return discoveryIds
            .Distinct()
            .Where(discoveriesById.ContainsKey)
            .Select(discoveryId => discoveriesById[discoveryId])
            .ToList();
    }

    private static Task<List<FeatureResearchSearchMatchDto>> SearchResearchDiscoveries(
        IServiceProvider services,
        string query,
        int resultCount = SearchFeatureResearchQuery.DefaultResultCount
    ) =>
        FeatureMcpActionExecutor.ExecuteQuery<
            SearchFeatureResearchQuery,
            List<FeatureResearchSearchMatchDto>
        >(
            services,
            search =>
            {
                search.SearchText = query;
                search.ResultCount = resultCount;
            }
        );

    private static async Task<List<FeatureRecordDto>> ListRecords(
        IServiceProvider services,
        Guid featureId,
        uint orderNumber = 0
    ) =>
        (await GetFullFeature(services, featureId, orderNumber))?.Records
            .OrderByDescending(record => record.UpdatedAt)
            .ThenBy(record => record.Id)
            .ToList() ?? [];

    private static Task<FeatureDto?> GetFullFeature(
        IServiceProvider services,
        Guid featureId,
        uint orderNumber
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

    private static Task<FeatureCommandResult> UpdateSummary(
        IServiceProvider services,
        Guid featureId,
        string summary
    ) =>
        FeatureMcpActionExecutor.ExecuteCommand<
            UpdateFeatureSummaryCommand,
            FeatureCommandResult
        >(
            services,
            command =>
            {
                command.FeatureId = featureId;
                command.Summary = summary;
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

    private static Task<
        FeatureResearchDiscoveryCreatedCommandResult
    > AddResearchDiscovery(
        IServiceProvider services,
        Guid featureId,
        string title,
        string content,
        FeatureResearchDiscoverySourceType sourceType =
            FeatureResearchDiscoverySourceType.Other,
        string sourceReference = ""
    ) =>
        FeatureMcpActionExecutor.ExecuteCommand<
            AddFeatureResearchDiscoveryCommand,
            FeatureResearchDiscoveryCreatedCommandResult
        >(
            services,
            command =>
            {
                command.FeatureId = featureId;
                command.Title = title;
                command.Content = content;
                command.SourceType = sourceType;
                command.SourceReference = sourceReference;
            }
        );

    private static Task<FeatureCommandResult> UpdateResearchDiscovery(
        IServiceProvider services,
        Guid featureId,
        Guid discoveryId,
        string title,
        string content,
        FeatureResearchDiscoverySourceType sourceType =
            FeatureResearchDiscoverySourceType.Other,
        string sourceReference = ""
    ) =>
        FeatureMcpActionExecutor.ExecuteCommand<
            UpdateFeatureResearchDiscoveryCommand,
            FeatureCommandResult
        >(
            services,
            command =>
            {
                command.FeatureId = featureId;
                command.DiscoveryId = discoveryId;
                command.Title = title;
                command.Content = content;
                command.SourceType = sourceType;
                command.SourceReference = sourceReference;
            }
        );

    private static Task<FeatureCommandResult> RemoveResearchDiscovery(
        IServiceProvider services,
        Guid featureId,
        Guid discoveryId
    ) =>
        FeatureMcpActionExecutor.ExecuteCommand<
            RemoveFeatureResearchDiscoveryCommand,
            FeatureCommandResult
        >(
            services,
            command =>
            {
                command.FeatureId = featureId;
                command.DiscoveryId = discoveryId;
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

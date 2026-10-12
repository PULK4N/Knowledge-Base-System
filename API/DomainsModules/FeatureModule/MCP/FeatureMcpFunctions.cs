using System.ComponentModel;
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
            "Gets a bounded feature context: its overview, a branch tree that nests each plan's branches under the feature branch or the plan done before it, plan summaries (ID, title, status, additional info, previous plan ID) without plan content, five latest research discoveries, five latest conversation records, and title/ID references for other discoveries. Read each plan's status and additional info first and load a plan with feature_plan_get only when it is relevant. Review notes are excluded; load them with feature_review_note_list before reviewing code. Set orderNumber to zero for the latest state or to an event order number for historical state."
        ),
        CreateFunction(
            (Func<IServiceProvider, Guid, Guid, uint, Task<FeatureMcpPlanDto?>>)GetPlan,
            "feature_plan_get",
            "Gets one feature plan with its content by feature ID and plan ID."
        ),
        CreateFunction(
            (Func<IServiceProvider, Guid, List<Guid>, uint, Task<List<FeatureResearchDiscoveryDto>>>)GetResearchDiscoveries,
            "feature_research_discovery_get",
            "Gets multiple research discoveries in one call by feature ID and discovery IDs."
        ),
        CreateFunction(
            (Func<IServiceProvider, string, List<string>, int, Task<FeatureResearchSearchResultsDto>>)SearchResearchDiscoveries,
            "feature_research_discovery_search",
            "Searches research discoveries across active features. Two separate inputs: query is a meaningful sentence and drives semantic vector matching, while keywords are the words a discovery must all contain for full-text matching. Returns topMatches with the highest-ranked chunk per discovery, which may repeat one feature, and distinctSources with the best discovery of each different feature; use feature_research_discovery_get with the returned feature and discovery IDs to load complete discoveries."
        ),
        CreateFunction(
            (Func<IServiceProvider, Guid, uint, Task<List<FeatureRecordDto>>>)ListRecords,
            "feature_record_list",
            "Gets all conversation records for a feature."
        ),
        CreateFunction(
            (Func<IServiceProvider, Guid, uint, Task<List<FeatureReviewNoteDto>>>)ListReviewNotes,
            "feature_review_note_list",
            "Gets all review notes for a feature. Each note records a reviewed decision that intended behaviour is not a defect; read them before reporting review findings so a settled decision is not raised again."
        ),
        CreateFunction(
            (Func<IServiceProvider, Guid, string, string, string, Guid?, Guid?, Task<FeatureCreatedCommandResult>>)Add,
            "feature_add",
            "Creates a feature for an existing project with a name, an overview summary of what the feature is and does, and a free-form progress status."
        ),
        CreateFunction(
            (Func<IServiceProvider, Guid, Guid?, Guid?, Task<FeatureCommandResult>>)Remove,
            "feature_remove",
            "Marks a feature as deleted while retaining its event history."
        ),
        CreateFunction(
            (Func<IServiceProvider, Guid, string, Guid?, Guid?, Task<FeatureCommandResult>>)UpdateStatus,
            "feature_status_update",
            "Replaces a feature's free-form progress description."
        ),
        CreateFunction(
            (Func<IServiceProvider, Guid, string, Guid?, Guid?, Task<FeatureCommandResult>>)UpdateSummary,
            "feature_summary_update",
            "Replaces a feature's overview summary of what the feature is and does."
        ),
        CreateFunction(
            (Func<IServiceProvider, Guid, Guid, Guid?, Guid?, Task<FeatureCommandResult>>)AddSkill,
            "feature_skill_add",
            "Adds a skill ID that provides useful context for the feature."
        ),
        CreateFunction(
            (Func<IServiceProvider, Guid, Guid, Guid?, Guid?, Task<FeatureCommandResult>>)RemoveSkill,
            "feature_skill_remove",
            "Removes a related skill ID from the feature."
        ),
        CreateFunction(
            (Func<IServiceProvider, Guid, string, string, Guid?, Guid?, Task<FeatureRecordCreatedCommandResult>>)AddRecord,
            "feature_record_add",
            "Adds a curated user message and AI answer that affected the feature or its implementation plan."
        ),
        CreateFunction(
            (Func<IServiceProvider, Guid, Guid, string, string, Guid?, Guid?, Task<FeatureCommandResult>>)UpdateRecord,
            "feature_record_update",
            "Updates the user message and AI answer of an existing feature record."
        ),
        CreateFunction(
            (Func<IServiceProvider, Guid, Guid, Guid?, Guid?, Task<FeatureCommandResult>>)RemoveRecord,
            "feature_record_remove",
            "Removes a feature record from the current feature state."
        ),
        CreateFunction(
            (Func<IServiceProvider, Guid, string, string, Guid?, Guid?, Task<FeatureReviewNoteCreatedCommandResult>>)AddReviewNote,
            "feature_review_note_add",
            "Records a reviewed decision that intended behaviour is not a defect, as a Markdown note, so a later review does not report the same finding again."
        ),
        CreateFunction(
            (Func<IServiceProvider, Guid, Guid, string, string, Guid?, Guid?, Task<FeatureCommandResult>>)UpdateReviewNote,
            "feature_review_note_update",
            "Updates the title and Markdown content of an existing feature review note."
        ),
        CreateFunction(
            (Func<IServiceProvider, Guid, Guid, Guid?, Guid?, Task<FeatureCommandResult>>)RemoveReviewNote,
            "feature_review_note_remove",
            "Removes a review note from the current feature state while retaining event history."
        ),
        CreateFunction(
            (Func<IServiceProvider, Guid, string, string, FeatureResearchDiscoverySourceType, string, Guid?, Guid?, Task<FeatureResearchDiscoveryCreatedCommandResult>>)AddResearchDiscovery,
            "feature_research_discovery_add",
            "Stores a research discovery for a feature. The optional source reference can be a code path, URL, MCP tool name, or other provenance."
        ),
        CreateFunction(
            (Func<IServiceProvider, Guid, Guid, string, string, FeatureResearchDiscoverySourceType, string, Guid?, Guid?, Task<FeatureCommandResult>>)UpdateResearchDiscovery,
            "feature_research_discovery_update",
            "Updates a stored feature research discovery and its provenance."
        ),
        CreateFunction(
            (Func<IServiceProvider, Guid, Guid, Guid?, Guid?, Task<FeatureCommandResult>>)RemoveResearchDiscovery,
            "feature_research_discovery_remove",
            "Removes a research discovery from the current feature state while retaining event history."
        ),
        CreateFunction(
            (Func<IServiceProvider, Guid, string, string, string, FeaturePlanContentType, string, FeaturePlanStatus, string, Guid?, Guid?, Guid?, Task<FeaturePlanCreatedCommandResult>>)AddPlan,
            "feature_plan_add",
            "Adds a Markdown or HTML plan with its required base branch (branchName, optional branchDescription). additionalInfo is short text returned with feature_get so an AI can tell what the plan is for, for example research only or production implementation, without loading its content. previousPlanId is the plan done before this one, whose base branch this plan's base branch was checked out from; omit it when the branch comes from the feature branch. Add sub-branches with feature_plan_branch_add."
        ),
        CreateFunction(
            (Func<IServiceProvider, Guid, Guid, string, string, FeaturePlanContentType, string, FeaturePlanStatus, Guid?, Guid?, Task<FeatureCommandResult>>)UpdatePlan,
            "feature_plan_update",
            "Replaces a plan's title, content, content type, additional info, and status by plan ID. Branches are kept; change them with the feature_plan_branch tools."
        ),
        CreateFunction(
            (Func<IServiceProvider, Guid, Guid, FeaturePlanStatus, Guid?, Guid?, Task<FeatureCommandResult>>)UpdatePlanStatus,
            "feature_plan_status_update",
            "Changes only a plan's status: NotStarted, InProgress, Delayed, or Completed."
        ),
        CreateFunction(
            (Func<IServiceProvider, Guid, Guid, Guid?, Guid?, Guid?, Task<FeatureCommandResult>>)SetPlanPreviousPlan,
            "feature_plan_previous_set",
            "Sets the plan done before this plan, whose base branch this plan's base branch was checked out from. Omit previousPlanId when the branch comes from the feature branch. The previous plan must belong to the same feature and cannot come after this plan."
        ),
        CreateFunction(
            (Func<IServiceProvider, Guid, string, string, Guid?, Guid?, Task<FeatureCommandResult>>)SetBranch,
            "feature_branch_set",
            "Sets or replaces the feature's source-control branch. Plans without a previous plan check out their base branch from it."
        ),
        CreateFunction(
            (Func<IServiceProvider, Guid, Guid, string, string, string?, Guid?, Guid?, Task<FeatureCommandResult>>)AddPlanBranch,
            "feature_plan_branch_add",
            "Adds a sub-branch under parentBranchName, the existing branch it was created from. Branch names are unique within a plan. Omit parentBranchName only to add a base branch to an older plan that has none."
        ),
        CreateFunction(
            (Func<IServiceProvider, Guid, Guid, string, string, string?, Guid?, Guid?, Task<FeatureCommandResult>>)UpdatePlanBranch,
            "feature_plan_branch_update",
            "Replaces a plan branch's description and optionally renames it with newBranchName. Use it to change the base branch, which cannot be removed."
        ),
        CreateFunction(
            (Func<IServiceProvider, Guid, Guid, string, Guid?, Guid?, Task<FeatureCommandResult>>)RemovePlanBranch,
            "feature_plan_branch_remove",
            "Removes a sub-branch and all of its own sub-branches. The base branch cannot be removed."
        ),
        CreateFunction(
            (Func<IServiceProvider, Guid, Guid, Guid?, Guid?, Task<FeatureCommandResult>>)RemovePlan,
            "feature_plan_remove",
            "Removes a plan from the current feature state while retaining event history."
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
                Description = description,
                ConfigureParameterBinding = parameter =>
                    parameter.Name is "sessionId" or "memoryAggregateId"
                        ? new AIFunctionFactoryOptions.ParameterBindingOptions
                        {
                            ExcludeFromSchema = true
                        }
                        : default
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

    private static async Task<FeatureMcpPlanDto?> GetPlan(
        IServiceProvider services,
        Guid featureId,
        Guid planId,
        uint orderNumber = 0
    )
    {
        var plan = (await GetFullFeature(services, featureId, orderNumber))?.Plans
            .SingleOrDefault(plan => plan.Id == planId);

        return plan is null ? null : FeatureMcpPlanDto.FromPlan(plan);
    }

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

    private static Task<FeatureResearchSearchResultsDto> SearchResearchDiscoveries(
        IServiceProvider services,
        [Description("A meaningful sentence describing what you need. Only the semantic vector match reads it.")]
        string query,
        [Description("Words that must all appear in a matching record. Every word is required, so pass only the distinctive terms, and include common words only when the exact wording matters.")]
        List<string> keywords,
        int resultCount = SearchFeatureResearchQuery.DefaultResultCount
    ) =>
        FeatureMcpActionExecutor.ExecuteQuery<
            SearchFeatureResearchQuery,
            FeatureResearchSearchResultsDto
        >(
            services,
            search =>
            {
                search.SearchText = query;
                search.Keywords = keywords;
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

    private static async Task<List<FeatureReviewNoteDto>> ListReviewNotes(
        IServiceProvider services,
        Guid featureId,
        uint orderNumber = 0
    ) =>
        (await GetFullFeature(services, featureId, orderNumber))?.ReviewNotes
            .OrderByDescending(reviewNote => reviewNote.UpdatedAt)
            .ThenBy(reviewNote => reviewNote.Id)
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
        string status,
        Guid? sessionId = null,
        Guid? memoryAggregateId = null
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
                command.SessionId = sessionId.GetValueOrDefault();
                command.MemoryAggregateId = memoryAggregateId.GetValueOrDefault();
            }
        );

    private static Task<FeatureCommandResult> Remove(
        IServiceProvider services,
        Guid featureId,
        Guid? sessionId = null,
        Guid? memoryAggregateId = null
    ) =>
        FeatureMcpActionExecutor.ExecuteCommand<
            RemoveFeatureCommand,
            FeatureCommandResult
        >(
            services,
            command =>
            {
                command.FeatureId = featureId;
                command.SessionId = sessionId.GetValueOrDefault();
                command.MemoryAggregateId = memoryAggregateId.GetValueOrDefault();
            }
        );

    private static Task<FeatureCommandResult> UpdateStatus(
        IServiceProvider services,
        Guid featureId,
        string status,
        Guid? sessionId = null,
        Guid? memoryAggregateId = null
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
                command.SessionId = sessionId.GetValueOrDefault();
                command.MemoryAggregateId = memoryAggregateId.GetValueOrDefault();
            }
        );

    private static Task<FeatureCommandResult> UpdateSummary(
        IServiceProvider services,
        Guid featureId,
        string summary,
        Guid? sessionId = null,
        Guid? memoryAggregateId = null
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
                command.SessionId = sessionId.GetValueOrDefault();
                command.MemoryAggregateId = memoryAggregateId.GetValueOrDefault();
            }
        );

    private static Task<FeatureCommandResult> AddSkill(
        IServiceProvider services,
        Guid featureId,
        Guid skillId,
        Guid? sessionId = null,
        Guid? memoryAggregateId = null
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
                command.SessionId = sessionId.GetValueOrDefault();
                command.MemoryAggregateId = memoryAggregateId.GetValueOrDefault();
            }
        );

    private static Task<FeatureCommandResult> RemoveSkill(
        IServiceProvider services,
        Guid featureId,
        Guid skillId,
        Guid? sessionId = null,
        Guid? memoryAggregateId = null
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
                command.SessionId = sessionId.GetValueOrDefault();
                command.MemoryAggregateId = memoryAggregateId.GetValueOrDefault();
            }
        );

    private static Task<FeatureRecordCreatedCommandResult> AddRecord(
        IServiceProvider services,
        Guid featureId,
        string userMessage,
        string aiAnswer,
        Guid? sessionId = null,
        Guid? memoryAggregateId = null
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
                command.SessionId = sessionId.GetValueOrDefault();
                command.MemoryAggregateId = memoryAggregateId.GetValueOrDefault();
            }
        );

    private static Task<FeatureCommandResult> UpdateRecord(
        IServiceProvider services,
        Guid featureId,
        Guid recordId,
        string userMessage,
        string aiAnswer,
        Guid? sessionId = null,
        Guid? memoryAggregateId = null
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
                command.SessionId = sessionId.GetValueOrDefault();
                command.MemoryAggregateId = memoryAggregateId.GetValueOrDefault();
            }
        );

    private static Task<FeatureCommandResult> RemoveRecord(
        IServiceProvider services,
        Guid featureId,
        Guid recordId,
        Guid? sessionId = null,
        Guid? memoryAggregateId = null
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
                command.SessionId = sessionId.GetValueOrDefault();
                command.MemoryAggregateId = memoryAggregateId.GetValueOrDefault();
            }
        );

    private static Task<FeatureReviewNoteCreatedCommandResult> AddReviewNote(
        IServiceProvider services,
        Guid featureId,
        string title,
        string content,
        Guid? sessionId = null,
        Guid? memoryAggregateId = null
    ) =>
        FeatureMcpActionExecutor.ExecuteCommand<
            AddFeatureReviewNoteCommand,
            FeatureReviewNoteCreatedCommandResult
        >(
            services,
            command =>
            {
                command.FeatureId = featureId;
                command.Title = title;
                command.Content = content;
                command.SessionId = sessionId.GetValueOrDefault();
                command.MemoryAggregateId = memoryAggregateId.GetValueOrDefault();
            }
        );

    private static Task<FeatureCommandResult> UpdateReviewNote(
        IServiceProvider services,
        Guid featureId,
        Guid reviewNoteId,
        string title,
        string content,
        Guid? sessionId = null,
        Guid? memoryAggregateId = null
    ) =>
        FeatureMcpActionExecutor.ExecuteCommand<
            UpdateFeatureReviewNoteCommand,
            FeatureCommandResult
        >(
            services,
            command =>
            {
                command.FeatureId = featureId;
                command.ReviewNoteId = reviewNoteId;
                command.Title = title;
                command.Content = content;
                command.SessionId = sessionId.GetValueOrDefault();
                command.MemoryAggregateId = memoryAggregateId.GetValueOrDefault();
            }
        );

    private static Task<FeatureCommandResult> RemoveReviewNote(
        IServiceProvider services,
        Guid featureId,
        Guid reviewNoteId,
        Guid? sessionId = null,
        Guid? memoryAggregateId = null
    ) =>
        FeatureMcpActionExecutor.ExecuteCommand<
            RemoveFeatureReviewNoteCommand,
            FeatureCommandResult
        >(
            services,
            command =>
            {
                command.FeatureId = featureId;
                command.ReviewNoteId = reviewNoteId;
                command.SessionId = sessionId.GetValueOrDefault();
                command.MemoryAggregateId = memoryAggregateId.GetValueOrDefault();
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
        string sourceReference = "",
        Guid? sessionId = null,
        Guid? memoryAggregateId = null
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
                command.SessionId = sessionId.GetValueOrDefault();
                command.MemoryAggregateId = memoryAggregateId.GetValueOrDefault();
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
        string sourceReference = "",
        Guid? sessionId = null,
        Guid? memoryAggregateId = null
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
                command.SessionId = sessionId.GetValueOrDefault();
                command.MemoryAggregateId = memoryAggregateId.GetValueOrDefault();
            }
        );

    private static Task<FeatureCommandResult> RemoveResearchDiscovery(
        IServiceProvider services,
        Guid featureId,
        Guid discoveryId,
        Guid? sessionId = null,
        Guid? memoryAggregateId = null
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
                command.SessionId = sessionId.GetValueOrDefault();
                command.MemoryAggregateId = memoryAggregateId.GetValueOrDefault();
            }
        );

    private static Task<FeaturePlanCreatedCommandResult> AddPlan(
        IServiceProvider services,
        Guid featureId,
        string title,
        string content,
        string branchName,
        FeaturePlanContentType contentType =
            FeaturePlanContentType.Markdown,
        string additionalInfo = "",
        FeaturePlanStatus status = FeaturePlanStatus.NotStarted,
        string branchDescription = "",
        Guid? previousPlanId = null,
        Guid? sessionId = null,
        Guid? memoryAggregateId = null
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
                command.AdditionalInfo = additionalInfo;
                command.Status = status;
                command.BranchName = branchName;
                command.BranchDescription = branchDescription;
                command.PreviousPlanId = previousPlanId;
                command.SessionId = sessionId.GetValueOrDefault();
                command.MemoryAggregateId = memoryAggregateId.GetValueOrDefault();
            }
        );

    private static Task<FeatureCommandResult> UpdatePlan(
        IServiceProvider services,
        Guid featureId,
        Guid planId,
        string title,
        string content,
        FeaturePlanContentType contentType =
            FeaturePlanContentType.Markdown,
        string additionalInfo = "",
        FeaturePlanStatus status = FeaturePlanStatus.NotStarted,
        Guid? sessionId = null,
        Guid? memoryAggregateId = null
    ) =>
        FeatureMcpActionExecutor.ExecuteCommand<
            UpdateFeaturePlanCommand,
            FeatureCommandResult
        >(
            services,
            command =>
            {
                command.FeatureId = featureId;
                command.PlanId = planId;
                command.Title = title;
                command.Content = content;
                command.ContentType = contentType;
                command.AdditionalInfo = additionalInfo;
                command.Status = status;
                command.SessionId = sessionId.GetValueOrDefault();
                command.MemoryAggregateId = memoryAggregateId.GetValueOrDefault();
            }
        );

    private static Task<FeatureCommandResult> UpdatePlanStatus(
        IServiceProvider services,
        Guid featureId,
        Guid planId,
        FeaturePlanStatus status,
        Guid? sessionId = null,
        Guid? memoryAggregateId = null
    ) =>
        FeatureMcpActionExecutor.ExecuteCommand<
            ChangeFeaturePlanStatusCommand,
            FeatureCommandResult
        >(
            services,
            command =>
            {
                command.FeatureId = featureId;
                command.PlanId = planId;
                command.Status = status;
                command.SessionId = sessionId.GetValueOrDefault();
                command.MemoryAggregateId = memoryAggregateId.GetValueOrDefault();
            }
        );

    private static Task<FeatureCommandResult> SetPlanPreviousPlan(
        IServiceProvider services,
        Guid featureId,
        Guid planId,
        Guid? previousPlanId = null,
        Guid? sessionId = null,
        Guid? memoryAggregateId = null
    ) =>
        FeatureMcpActionExecutor.ExecuteCommand<
            SetFeaturePlanPreviousPlanCommand,
            FeatureCommandResult
        >(
            services,
            command =>
            {
                command.FeatureId = featureId;
                command.PlanId = planId;
                command.PreviousPlanId = previousPlanId;
                command.SessionId = sessionId.GetValueOrDefault();
                command.MemoryAggregateId = memoryAggregateId.GetValueOrDefault();
            }
        );

    private static Task<FeatureCommandResult> SetBranch(
        IServiceProvider services,
        Guid featureId,
        string branchName,
        string description = "",
        Guid? sessionId = null,
        Guid? memoryAggregateId = null
    ) =>
        FeatureMcpActionExecutor.ExecuteCommand<
            SetFeatureBranchCommand,
            FeatureCommandResult
        >(
            services,
            command =>
            {
                command.FeatureId = featureId;
                command.BranchName = branchName;
                command.Description = description;
                command.SessionId = sessionId.GetValueOrDefault();
                command.MemoryAggregateId = memoryAggregateId.GetValueOrDefault();
            }
        );

    private static Task<FeatureCommandResult> AddPlanBranch(
        IServiceProvider services,
        Guid featureId,
        Guid planId,
        string branchName,
        string description = "",
        string? parentBranchName = null,
        Guid? sessionId = null,
        Guid? memoryAggregateId = null
    ) =>
        FeatureMcpActionExecutor.ExecuteCommand<
            AddFeaturePlanBranchCommand,
            FeatureCommandResult
        >(
            services,
            command =>
            {
                command.FeatureId = featureId;
                command.PlanId = planId;
                command.BranchName = branchName;
                command.Description = description;
                command.ParentBranchName = parentBranchName;
                command.SessionId = sessionId.GetValueOrDefault();
                command.MemoryAggregateId = memoryAggregateId.GetValueOrDefault();
            }
        );

    private static Task<FeatureCommandResult> UpdatePlanBranch(
        IServiceProvider services,
        Guid featureId,
        Guid planId,
        string branchName,
        string description = "",
        string? newBranchName = null,
        Guid? sessionId = null,
        Guid? memoryAggregateId = null
    ) =>
        FeatureMcpActionExecutor.ExecuteCommand<
            UpdateFeaturePlanBranchCommand,
            FeatureCommandResult
        >(
            services,
            command =>
            {
                command.FeatureId = featureId;
                command.PlanId = planId;
                command.BranchName = branchName;
                command.Description = description;
                command.NewBranchName = newBranchName;
                command.SessionId = sessionId.GetValueOrDefault();
                command.MemoryAggregateId = memoryAggregateId.GetValueOrDefault();
            }
        );

    private static Task<FeatureCommandResult> RemovePlanBranch(
        IServiceProvider services,
        Guid featureId,
        Guid planId,
        string branchName,
        Guid? sessionId = null,
        Guid? memoryAggregateId = null
    ) =>
        FeatureMcpActionExecutor.ExecuteCommand<
            RemoveFeaturePlanBranchCommand,
            FeatureCommandResult
        >(
            services,
            command =>
            {
                command.FeatureId = featureId;
                command.PlanId = planId;
                command.BranchName = branchName;
                command.SessionId = sessionId.GetValueOrDefault();
                command.MemoryAggregateId = memoryAggregateId.GetValueOrDefault();
            }
        );

    private static Task<FeatureCommandResult> RemovePlan(
        IServiceProvider services,
        Guid featureId,
        Guid planId,
        Guid? sessionId = null,
        Guid? memoryAggregateId = null
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
                command.SessionId = sessionId.GetValueOrDefault();
                command.MemoryAggregateId = memoryAggregateId.GetValueOrDefault();
            }
        );
}

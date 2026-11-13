using FeatureModule.Application.Commands;
using FeatureModule.Application.DTOs;
using FeatureModule.Application.Models;
using Microsoft.Extensions.AI;
using ProjectModule.Shared;

namespace FeatureModule.MCP;

/// <summary>
/// Tools for one plan's review notes and reviewers. Reviews of the plan's branch
/// use them together with the feature's, and they are copied to the feature
/// when the plan completes.
/// </summary>
internal static class FeaturePlanReviewMcpFunctions
{
    internal static List<AIFunction> Create() =>
    [
        FeatureMcpFunctions.CreateFunction(
            (Func<IServiceProvider, Guid, Guid, Task<List<FeatureReviewNoteDto>?>>)ListReviewNotes,
            "feature_plan_review_note_list",
            "Gets a plan's own review notes. Reviews of the plan's branch use them together with the feature review notes."
        ),
        FeatureMcpFunctions.CreateFunction(
            (Func<IServiceProvider, Guid, Guid, string, string, Guid?, Guid?, Task<FeatureReviewNoteCreatedCommandResult>>)AddReviewNote,
            "feature_plan_review_note_add",
            "Records on a plan a reviewed decision that intended behaviour is not a defect, as a Markdown note, so later reviews do not report it again. It is copied to the feature when the plan completes."
        ),
        FeatureMcpFunctions.CreateFunction(
            (Func<IServiceProvider, Guid, Guid, Guid, string, string, Guid?, Guid?, Task<FeatureCommandResult>>)UpdateReviewNote,
            "feature_plan_review_note_update",
            "Updates the title and Markdown content of a plan review note."
        ),
        FeatureMcpFunctions.CreateFunction(
            (Func<IServiceProvider, Guid, Guid, Guid, Guid?, Guid?, Task<FeatureCommandResult>>)RemoveReviewNote,
            "feature_plan_review_note_remove",
            "Removes a plan review note."
        ),
        FeatureMcpFunctions.CreateFunction(
            (Func<IServiceProvider, Guid, Guid, Task<List<FeatureReviewerDto>?>>)ListReviewers,
            "feature_plan_reviewer_list",
            "Gets a plan's own reviewers. Reviews of the plan's branch run them together with the feature reviewers."
        ),
        FeatureMcpFunctions.CreateFunction(
            (Func<IServiceProvider, Guid, Guid, string, string, ReviewLevel, Guid?, Guid?, Task<FeatureReviewerCreatedCommandResult>>)AddReviewer,
            "feature_plan_reviewer_add",
            "Adds a plan reviewer with a title, Markdown description and required Easy, Medium or Heavy review level. It is copied to the feature when the plan completes."
        ),
        FeatureMcpFunctions.CreateFunction(
            (Func<IServiceProvider, Guid, Guid, Guid, string, string, ReviewLevel, Guid?, Guid?, Task<FeatureCommandResult>>)UpdateReviewer,
            "feature_plan_reviewer_update",
            "Updates a plan reviewer title, Markdown description and review level."
        ),
        FeatureMcpFunctions.CreateFunction(
            (Func<IServiceProvider, Guid, Guid, Guid, Guid?, Guid?, Task<FeatureCommandResult>>)RemoveReviewer,
            "feature_plan_reviewer_remove",
            "Removes a plan reviewer."
        )
    ];

    private static async Task<List<FeatureReviewNoteDto>?> ListReviewNotes(IServiceProvider services, Guid featureId, Guid planId) =>
        (await FeatureMcpFunctions.GetFullFeature(services, featureId, 0))?.Plans
            .SingleOrDefault(plan => plan.Id == planId)?.ReviewNotes.ToList();

    private static async Task<List<FeatureReviewerDto>?> ListReviewers(IServiceProvider services, Guid featureId, Guid planId) =>
        (await FeatureMcpFunctions.GetFullFeature(services, featureId, 0))?.Plans
            .SingleOrDefault(plan => plan.Id == planId)?.Reviewers.ToList();

    private static Task<FeatureReviewNoteCreatedCommandResult> AddReviewNote(IServiceProvider services, Guid featureId,
        Guid planId, string title, string content, Guid? sessionId = null, Guid? memoryAggregateId = null) =>
        Run<AddFeaturePlanReviewNoteCommand, FeatureReviewNoteCreatedCommandResult>(services, featureId, planId, sessionId,
            memoryAggregateId, command =>
            {
                command.Title = title;
                command.Content = content;
            });

    private static Task<FeatureCommandResult> UpdateReviewNote(IServiceProvider services, Guid featureId, Guid planId,
        Guid reviewNoteId, string title, string content, Guid? sessionId = null, Guid? memoryAggregateId = null) =>
        Run<UpdateFeaturePlanReviewNoteCommand, FeatureCommandResult>(services, featureId, planId, sessionId,
            memoryAggregateId, command =>
            {
                command.ReviewNoteId = reviewNoteId;
                command.Title = title;
                command.Content = content;
            });

    private static Task<FeatureCommandResult> RemoveReviewNote(IServiceProvider services, Guid featureId, Guid planId,
        Guid reviewNoteId, Guid? sessionId = null, Guid? memoryAggregateId = null) =>
        Run<RemoveFeaturePlanReviewNoteCommand, FeatureCommandResult>(services, featureId, planId, sessionId,
            memoryAggregateId, command => command.ReviewNoteId = reviewNoteId);

    private static Task<FeatureReviewerCreatedCommandResult> AddReviewer(IServiceProvider services, Guid featureId,
        Guid planId, string title, string description, ReviewLevel reviewLevel, Guid? sessionId = null,
        Guid? memoryAggregateId = null) =>
        Run<AddFeaturePlanReviewerCommand, FeatureReviewerCreatedCommandResult>(services, featureId, planId, sessionId,
            memoryAggregateId, command =>
            {
                command.Title = title;
                command.Description = description;
                command.ReviewLevel = reviewLevel;
            });

    private static Task<FeatureCommandResult> UpdateReviewer(IServiceProvider services, Guid featureId, Guid planId,
        Guid reviewerId, string title, string description, ReviewLevel reviewLevel, Guid? sessionId = null,
        Guid? memoryAggregateId = null) =>
        Run<UpdateFeaturePlanReviewerCommand, FeatureCommandResult>(services, featureId, planId, sessionId,
            memoryAggregateId, command =>
            {
                command.ReviewerId = reviewerId;
                command.Title = title;
                command.Description = description;
                command.ReviewLevel = reviewLevel;
            });

    private static Task<FeatureCommandResult> RemoveReviewer(IServiceProvider services, Guid featureId, Guid planId,
        Guid reviewerId, Guid? sessionId = null, Guid? memoryAggregateId = null) =>
        Run<RemoveFeaturePlanReviewerCommand, FeatureCommandResult>(services, featureId, planId, sessionId,
            memoryAggregateId, command => command.ReviewerId = reviewerId);

    private static Task<TResult> Run<TCommand, TResult>(IServiceProvider services, Guid featureId, Guid planId,
        Guid? sessionId, Guid? memoryAggregateId, Action<TCommand> configure)
        where TCommand : FeaturePlanReviewCommand =>
        FeatureMcpActionExecutor.ExecuteCommand<TCommand, TResult>(
            services,
            command =>
            {
                command.FeatureId = featureId;
                command.PlanId = planId;
                command.SessionId = sessionId.GetValueOrDefault();
                command.MemoryAggregateId = memoryAggregateId.GetValueOrDefault();
                configure(command);
            });
}

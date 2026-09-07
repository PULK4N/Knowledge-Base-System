using ActionModule.Shared.Models;
using EventSourcing.Core;
using FeatureModule.Application.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;

namespace FeatureModule.Application.Commands;

public sealed class AddFeatureReviewNoteCommand(
    StateMachineHandler stateMachineHandler
) : ExistingFeatureCommand(stateMachineHandler)
{
    public required string Title { get; set; }

    public required string Content { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            FeatureId != Guid.Empty
            && !string.IsNullOrWhiteSpace(Title)
            && !string.IsNullOrWhiteSpace(Content)
        );

    protected override async Task<object> ExecuteInternal(
        Executor executor
    )
    {
        var reviewNoteId = FeatureReviewNoteId.New();

        await ExecuteEvent(
            executor,
            new FeatureReviewNoteAddedV1(
                reviewNoteId,
                Title,
                Content
            )
        );

        return FeatureReviewNoteCreatedCommandResult.Ok(reviewNoteId.Value);
    }
}

public sealed class UpdateFeatureReviewNoteCommand(
    StateMachineHandler stateMachineHandler
) : ExistingFeatureCommand(stateMachineHandler)
{
    public required Guid ReviewNoteId { get; set; }

    public required string Title { get; set; }

    public required string Content { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            FeatureId != Guid.Empty
            && ReviewNoteId != Guid.Empty
            && !string.IsNullOrWhiteSpace(Title)
            && !string.IsNullOrWhiteSpace(Content)
        );

    protected override Task<object> ExecuteInternal(Executor executor) =>
        ExecuteEvent(
            executor,
            new FeatureReviewNoteUpdatedV1(
                FeatureReviewNoteId.FromDatabaseGuid(ReviewNoteId),
                Title,
                Content
            )
        );
}

public sealed class RemoveFeatureReviewNoteCommand(
    StateMachineHandler stateMachineHandler
) : ExistingFeatureCommand(stateMachineHandler)
{
    public required Guid ReviewNoteId { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            FeatureId != Guid.Empty
            && ReviewNoteId != Guid.Empty
        );

    protected override Task<object> ExecuteInternal(Executor executor) =>
        ExecuteEvent(
            executor,
            new FeatureReviewNoteRemovedV1(
                FeatureReviewNoteId.FromDatabaseGuid(ReviewNoteId)
            )
        );
}

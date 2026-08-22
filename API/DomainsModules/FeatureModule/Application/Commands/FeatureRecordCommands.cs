using ActionModule.Shared.Models;
using EventSourcing.Core;
using FeatureModule.Application.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;

namespace FeatureModule.Application.Commands;

public sealed class AddFeatureRecordCommand(
    StateMachineHandler stateMachineHandler
) : ExistingFeatureCommand(stateMachineHandler)
{
    public required string UserMessage { get; set; }

    public required string AiAnswer { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            FeatureId != Guid.Empty
            && !string.IsNullOrWhiteSpace(UserMessage)
            && !string.IsNullOrWhiteSpace(AiAnswer)
        );

    protected override async Task<object> ExecuteInternal(
        Executor executor
    )
    {
        var recordId = FeatureRecordId.New();

        await ExecuteEvent(
            executor,
            new FeatureRecordAddedV1(
                recordId,
                UserMessage,
                AiAnswer
            )
        );

        return FeatureRecordCreatedCommandResult.Ok(recordId.Value);
    }
}

public sealed class UpdateFeatureRecordCommand(
    StateMachineHandler stateMachineHandler
) : ExistingFeatureCommand(stateMachineHandler)
{
    public required Guid RecordId { get; set; }

    public required string UserMessage { get; set; }

    public required string AiAnswer { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            FeatureId != Guid.Empty
            && RecordId != Guid.Empty
            && !string.IsNullOrWhiteSpace(UserMessage)
            && !string.IsNullOrWhiteSpace(AiAnswer)
        );

    protected override Task<object> ExecuteInternal(Executor executor) =>
        ExecuteEvent(
            executor,
            new FeatureRecordUpdatedV1(
                FeatureRecordId.FromDatabaseGuid(RecordId),
                UserMessage,
                AiAnswer
            )
        );
}

public sealed class RemoveFeatureRecordCommand(
    StateMachineHandler stateMachineHandler
) : ExistingFeatureCommand(stateMachineHandler)
{
    public required Guid RecordId { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            FeatureId != Guid.Empty
            && RecordId != Guid.Empty
        );

    protected override Task<object> ExecuteInternal(Executor executor) =>
        ExecuteEvent(
            executor,
            new FeatureRecordRemovedV1(
                FeatureRecordId.FromDatabaseGuid(RecordId)
            )
        );
}

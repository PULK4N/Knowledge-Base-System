using System.Text.Json;
using ActionModule.Shared;
using ActionModule.Shared.Models;
using AttachmentsModule.Application;
using EventSourcing.Core;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using MemoryModule.Application.Models;
using MemoryModule.Domain;
using MemoryModule.Domain.Events;
using MemoryModule.Domain.Models;

namespace MemoryModule.Application.Commands;

public sealed class RecordCodexPromptHookCommand(
    StateMachineHandler stateMachineHandler,
    IAttachmentContentStorage attachmentContentStorage
) : Command<MemoryCommandResult>
{
    public required ThreadId ThreadId { get; set; }
    public required PromptId PromptId { get; set; }
    public required string HookEventName { get; set; }
    public required JsonElement Payload { get; set; }
    public List<MemoryAttachmentUpload> Attachments { get; set; } = [];

    /// <summary>
    /// Saves the attachment content first and then records the hook together
    /// with one attachment event per file. The two steps are not in one
    /// transaction: when the events fail, the saved content is left without an
    /// event that refers to it.
    /// </summary>
    protected override async Task<MemoryCommandResult> ExecuteInternal(
        Executor executor
    )
    {
        var sessionMap = await stateMachineHandler.GetByAggregateId(
            AggregateId.FromDatabaseGuid(
                SharedModule.Constants.StateDataAggregateIds.SessionAggregateMap
            )
        );

        if (sessionMap?.StateData is SessionAggregateMapStateData sessionAggregateMapStateData &&
                sessionAggregateMapStateData.AggregateIdsBySession.ContainsKey(ThreadId))
        {
            var aggregateId = sessionAggregateMapStateData.AggregateIdsBySession[ThreadId].Value;
            var memoryId = AggregateId.FromDatabaseGuid(aggregateId);
            var memory = await stateMachineHandler.GetByAggregateId(memoryId);
            var existingPlaceholders = memory?.StateData is MemoryStateData state
                && state.ChatPrompts.TryGetValue(PromptId, out var prompt)
                ? prompt.Attachments.Select(attachment => attachment.Placeholder)
                    .Where(placeholder => placeholder is not null)
                    .ToHashSet(StringComparer.Ordinal)
                : [];
            var uploads = Attachments.Where(upload =>
                upload.Attachment.Placeholder is null
                || !existingPlaceholders.Contains(upload.Attachment.Placeholder)).ToList();
            await SaveAttachments(uploads);

            await stateMachineHandler.ExecuteEvents(
                CreateMemoryPayloads(
                    executor,
                    memoryId,
                    uploads
                )
            );

            return MemoryCommandResult.Ok;
        }

        await SaveAttachments(Attachments);

        var mapEvent = EventPayload.Create(
            executor.Id,
            MemoryAggregateIds.SessionAggregateMap,
            Constants.StateMachineIds.SessionAggregateMap,
            new SessionAggregateMapAddedV1(
                ThreadId,
                AggregateId.New()
            )
        );

        await stateMachineHandler.ExecuteEvents(
            mapEvent,
            stateInfos =>
            {
                var state = (SessionAggregateMapStateData)stateInfos[0].StateData;
                var memoryAggregateId = state.AggregateIdsBySession[ThreadId];

                return CreateMemoryPayloads(executor, memoryAggregateId, Attachments);
            }
        );

        return MemoryCommandResult.Ok;
    }

    private async Task SaveAttachments(List<MemoryAttachmentUpload> uploads)
    {
        foreach (var upload in uploads)
        {
            await attachmentContentStorage.Save(
                new AttachmentContentFile(
                    upload.Attachment.Id.Value,
                    upload.Attachment.Name,
                    upload.Attachment.FileType,
                    upload.Content
                )
            );
        }
    }

    private List<EventPayload> CreateMemoryPayloads(
        Executor executor,
        AggregateId memoryAggregateId,
        List<MemoryAttachmentUpload> uploads
    ) =>
    [
        CreateMemoryPayload(
            executor,
            memoryAggregateId,
            new CodexPromptHookRecordedV1(
                ThreadId,
                PromptId,
                HookEventName,
                Payload
            )
        ),
        .. uploads.Select(
            upload => CreateMemoryPayload(
                executor,
                memoryAggregateId,
                new MemoryAttachmentAddedV1(PromptId, upload.Attachment)
            )
        )
    ];

    private static EventPayload CreateMemoryPayload(
        Executor executor,
        AggregateId memoryAggregateId,
        IEvent eventData
    ) =>
        EventPayload.Create(
            executor.Id,
            memoryAggregateId,
            Constants.StateMachineIds.Memory,
            eventData
        );
}

using ActionModule.Shared;
using ActionModule.Shared.Models;
using AdministrationModule.Application.DTOs;
using AdministrationModule.Application.Persistence;

namespace AdministrationModule.Application.Commands;

public sealed class RequeueOutboxPayloadCommand(
    IOutboxAdministrationRepository repository
) : Command<OutboxPayloadDto?>
{
    public override Task<bool> IsAuthorized(Executor executor) => Task.FromResult(executor.IsAdministrator);

    public required long OutboxPayloadId { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(OutboxPayloadId > 0);

    protected override async Task<OutboxPayloadDto?> ExecuteInternal(
        Executor executor
    )
    {
        var entry = await repository.Requeue(OutboxPayloadId);

        return entry is null
            ? null
            : OutboxPayloadDto.FromEntry(entry);
    }
}

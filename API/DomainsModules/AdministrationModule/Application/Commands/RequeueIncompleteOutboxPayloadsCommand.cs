using ActionModule.Shared;
using ActionModule.Shared.Models;
using AdministrationModule.Application.DTOs;
using AdministrationModule.Application.Persistence;

namespace AdministrationModule.Application.Commands;

public sealed class RequeueIncompleteOutboxPayloadsCommand(
    IOutboxAdministrationRepository repository
) : Command<OutboxRequeueSummaryDto>
{
    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(true);

    protected override async Task<OutboxRequeueSummaryDto> ExecuteInternal(
        Executor executor
    ) =>
        new(await repository.RequeueIncomplete());
}

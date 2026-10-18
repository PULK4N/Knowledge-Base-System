using ActionModule.Shared;
using ActionModule.Shared.Models;
using AdministrationModule.Application.DTOs.UniqueConstraints;
using AdministrationModule.Application.Services;
using EventSourcing.Core.Interfaces;

namespace AdministrationModule.Application.Commands.UniqueConstraints;

public sealed class RebuildUniqueConstraintsCommand(
    IStateMachineDefinitionProvider definitionProvider,
    UniqueConstraintRebuildService rebuildService
) : Command<UniqueConstraintRebuildResult>
{
    public required string StateMachineId { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            !string.IsNullOrWhiteSpace(StateMachineId)
            && definitionProvider
                .GetAll()
                .Any(definition => definition.Id == StateMachineId)
        );

    protected override Task<UniqueConstraintRebuildResult> ExecuteInternal(
        Executor executor
    ) =>
        rebuildService.Rebuild(StateMachineId);
}

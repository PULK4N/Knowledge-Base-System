using ActionModule.Shared;
using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Shared.Models;
using IdentityModule.Application.DTOs;
using IdentityModule.Application.SessionKeys;
using IdentityModule.Domain;

namespace IdentityModule.Application.Commands;

/// <summary>
/// Generates a session key an agent sends as <c>Authorization: Bearer</c>. Only the administrator may
/// generate keys. The key is returned once and not stored.
/// </summary>
public sealed class GenerateSessionKeyCommand(StateMachineHandler handler, SessionKeyProtector protector)
    : Command<SessionKeyDto>
{
    public Guid AgentId { get; set; }

    public override Task<bool> IsAuthorized(Executor executor) => Task.FromResult(executor.IsAdministrator);

    public override async Task<bool> CanExecute(Executor executor) =>
        (await handler.GetByAggregateId(AggregateId.FromDatabaseGuid(AgentId)))?.StateData
        is AccountStateData { IsCreated: true, IsDeleted: false, IsAgent: true };

    protected override Task<SessionKeyDto> ExecuteInternal(Executor executor) =>
        Task.FromResult(new SessionKeyDto(protector.Protect(AgentId)));
}

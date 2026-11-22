using ActionModule.Shared;
using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Shared.Models;
using IdentityModule.Application.DTOs;
using IdentityModule.Domain;
using IdentityModule.Domain.Events;

namespace IdentityModule.Application.Commands;

/// <summary>Replaces an agent's permissions. They apply to the agent's next request, whatever key it uses.</summary>
public sealed class SetAgentPermissionsCommand(StateMachineHandler handler) : Command<AgentDto>
{
    public Guid AgentId { get; set; }
    public bool IsAdministrator { get; set; }
    public List<string> Permissions { get; set; } = [];

    public override Task<bool> IsAuthorized(Executor executor) => Task.FromResult(executor.IsAdministrator);

    protected override async Task<AgentDto> ExecuteInternal(Executor executor)
    {
        var stateInfo = await handler.ExecuteEvents(EventPayload.Create(executor.Id,
            AggregateId.FromDatabaseGuid(AgentId), IdentityDefaults.StateMachineId,
            new AgentPermissionsSetV1(IsAdministrator, [..Permissions.Distinct()])));
        return AgentDto.FromStateData((AccountStateData)stateInfo.StateData);
    }
}

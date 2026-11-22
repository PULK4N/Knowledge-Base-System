using ActionModule.Shared;
using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Shared.Models;
using IdentityModule.Application.DTOs;
using IdentityModule.Domain;
using IdentityModule.Domain.Events;

namespace IdentityModule.Application.Commands;

public sealed class CreateAgentCommand(StateMachineHandler handler) : Command<AgentDto>
{
    public string UserName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public bool IsAdministrator { get; set; }
    public List<string> Permissions { get; set; } = [];

    public override Task<bool> IsAuthorized(Executor executor) => Task.FromResult(executor.IsAdministrator);

    protected override async Task<AgentDto> ExecuteInternal(Executor executor)
    {
        var userName = UserName.Trim();
        var displayName = string.IsNullOrWhiteSpace(DisplayName) ? userName : DisplayName.Trim();
        var stateInfo = await handler.ExecuteEvents(EventPayload.Create(executor.Id, AggregateId.New(),
            IdentityDefaults.StateMachineId,
            new AgentCreatedV1(userName, displayName, IsAdministrator, [..Permissions.Distinct()])));
        return AgentDto.FromStateData((AccountStateData)stateInfo.StateData);
    }
}

using ActionModule.Shared;
using ActionModule.Shared.Models;
using IdentityModule.Application.DTOs;
using IdentityModule.Application.Persistence;

namespace IdentityModule.Application.Queries;

public sealed class ListAgentsQuery(IAccountListRepository repository) : Query<List<AgentDto>>
{
    public override Task<bool> IsAuthorized(Executor executor) => Task.FromResult(executor.IsAdministrator);

    protected override Task<List<AgentDto>> ExecuteInternal(Executor executor) => repository.ListAgents();
}

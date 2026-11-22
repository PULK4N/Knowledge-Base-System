using IdentityModule.Application.DTOs;

namespace IdentityModule.Application.Persistence;

public interface IAccountListRepository
{
    /// <summary>Agents ordered by last update, newest first, then by username.</summary>
    Task<List<AgentDto>> ListAgents(CancellationToken cancellationToken = default);
}

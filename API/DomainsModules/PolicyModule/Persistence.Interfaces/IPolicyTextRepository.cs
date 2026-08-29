using EventSourcing.Shared.Models;

namespace PolicyModule.Persistence.Interfaces;

public interface IPolicyTextRepository
{
    /// <summary>
    /// Returns the compiled policy text for a project. When
    /// <paramref name="agentFamilyName"/> is supplied, the policies of that
    /// agent family are appended so Claude and Codex receive different text.
    /// </summary>
    Task<string?> Get(
        AggregateId projectAggregateId,
        string? agentFamilyName
    );

    Task<bool> AgentFamilyExists(string agentFamilyName);
}

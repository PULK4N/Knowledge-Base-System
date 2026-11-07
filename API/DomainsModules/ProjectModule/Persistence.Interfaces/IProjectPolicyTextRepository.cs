using EventSourcing.Shared.Models;

namespace ProjectModule.Persistence.Interfaces;

public interface IProjectPolicyTextRepository
{
    /// <summary>
    /// Returns the project's compiled policy text with its linked topic names
    /// in link order, or null when the project has no projected text.
    /// </summary>
    Task<ProjectPolicyTextEntry?> Get(AggregateId projectAggregateId);
}

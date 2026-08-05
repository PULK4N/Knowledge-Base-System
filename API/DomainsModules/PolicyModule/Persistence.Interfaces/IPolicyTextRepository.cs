using EventSourcing.Shared.Models;

namespace PolicyModule.Persistence.Interfaces;

public interface IPolicyTextRepository
{
    Task<string?> Get(AggregateId projectAggregateId);
}

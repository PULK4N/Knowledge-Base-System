using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using MemoryModule.Domain;
using SharedModule.Persistence;

namespace MemoryModule.Persistence;

public sealed class MemoryEntityRelationProjector(
    MemoryEntityRelationRepository repository
) : IProjector
{
    public Task Update(List<StateInfo> stateInfos)
    {
        var memories = stateInfos
            .Select(stateInfo => stateInfo.StateData)
            .OfType<MemoryStateData>()
            .ToList();
        var relations = memories
            .Where(memory => !memory.IsDeleted)
            .SelectMany(memory => memory.RelatedEntities
                .Select(entity => entity.Id)
                .Distinct()
                .Select(entityId => new EntityRelationWrite(
                    memory.Id.Value,
                    memory.ChatSummary.Summary,
                    entityId.Value,
                    string.Empty,
                    MemoryEntityRelationRepository.ChangedEntity,
                    MemoryEntityRelationRepository.ChangedInMemory
                )))
            .ToList();

        return repository.Write(
            memories.Select(memory => memory.Id).Distinct().ToList(),
            relations
        );
    }
}

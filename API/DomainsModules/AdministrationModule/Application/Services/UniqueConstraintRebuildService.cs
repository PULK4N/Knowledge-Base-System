using AdministrationModule.Application.DTOs.UniqueConstraints;
using AdministrationModule.Application.Persistence.UniqueConstraints;
using EventSourcing.Core;
using EventSourcing.Persistence.Interfaces;
using EventSourcing.Shared.Models;

namespace AdministrationModule.Application.Services;

/// <summary>
/// Recalculates the unique constraints of every aggregate in a state machine from its event
/// history and replaces the stored rows. Nothing is written when two aggregates hold the same
/// value, because the stored rows could not represent that history.
/// </summary>
/// <remarks>
/// An aggregate written while a rebuild runs may be stored with the constraints of the state
/// that was read; run the rebuild again to pick up the newer state.
/// </remarks>
public class UniqueConstraintRebuildService(
    IUniqueConstraintRepository repository,
    IEventStore eventStore,
    StateCalculator stateCalculator
)
{
    /// <summary>Bounds how many aggregate histories are held in memory at once.</summary>
    public const int AggregateChunkSize = 100;

    public async Task<UniqueConstraintRebuildResult> Rebuild(string stateMachineId)
    {
        var aggregateIds = await repository.GetAggregateIds(stateMachineId);
        var processedAggregateIds = new List<AggregateId>();
        var constraints = new List<UniqueConstraint>();

        foreach (var chunk in aggregateIds.Chunk(AggregateChunkSize))
        {
            var histories = await eventStore.GetEvents([.. chunk]);

            foreach (var aggregateId in chunk)
            {
                if (!histories.TryGetValue(aggregateId, out var history) || history.Count == 0)
                    continue;

                var stateInfo = stateCalculator.Calculate(history, []);
                processedAggregateIds.Add(aggregateId);
                constraints.AddRange(stateInfo.UniqueConstraints);
            }
        }

        var conflicts = FindConflicts(constraints);
        if (conflicts.Count > 0)
        {
            return UniqueConstraintRebuildResult.WithConflicts(
                processedAggregateIds.Count,
                constraints.Count,
                conflicts
            );
        }

        await repository.Write(new UniqueConstraintRebuildBatch(processedAggregateIds, constraints));

        return UniqueConstraintRebuildResult.Completed(
            processedAggregateIds.Count,
            constraints.Count
        );
    }

    private static List<UniqueConstraintConflictDto> FindConflicts(
        List<UniqueConstraint> constraints
    ) =>
        constraints
            .GroupBy(constraint => Convert.ToHexString(constraint.Hash))
            .Where(group => group.Select(constraint => constraint.AggregateId).Distinct().Count() > 1)
            .Select(
                group =>
                    new UniqueConstraintConflictDto(
                        group.First().ConstraintName,
                        group
                            .Select(constraint => constraint.AggregateId)
                            .Distinct()
                            .Order()
                            .ToList()
                    )
            )
            .OrderBy(conflict => conflict.ConstraintName, StringComparer.Ordinal)
            .ToList();
}

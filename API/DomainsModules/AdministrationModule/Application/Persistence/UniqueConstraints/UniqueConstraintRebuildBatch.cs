using EventSourcing.Shared.Models;

namespace AdministrationModule.Application.Persistence.UniqueConstraints;

/// <summary>
/// The complete set of constraint rows for <see cref="AggregateIds"/>. Writing it replaces every
/// stored row of those aggregates, so an aggregate without constraints loses its old rows.
/// </summary>
public sealed record UniqueConstraintRebuildBatch(
    List<AggregateId> AggregateIds,
    List<UniqueConstraint> Constraints
);

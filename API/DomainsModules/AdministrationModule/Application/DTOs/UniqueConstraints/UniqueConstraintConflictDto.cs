namespace AdministrationModule.Application.DTOs.UniqueConstraints;

public sealed record UniqueConstraintConflictDto(
    string ConstraintName,
    List<Guid> AggregateIds
);

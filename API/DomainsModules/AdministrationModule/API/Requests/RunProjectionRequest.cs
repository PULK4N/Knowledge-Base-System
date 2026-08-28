using System.ComponentModel.DataAnnotations;

namespace AdministrationModule.API.Requests;

public sealed record RunProjectionRequest : IValidatableObject
{
    public required string ProjectionName { get; init; }
    public Guid? AggregateId { get; init; }
    public string? StateMachineId { get; init; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext
    )
    {
        if (string.IsNullOrWhiteSpace(ProjectionName))
        {
            yield return new ValidationResult(
                "Projection name is required.",
                [nameof(ProjectionName)]
            );
        }

        if (AggregateId == Guid.Empty)
        {
            yield return new ValidationResult(
                "Aggregate ID must not be empty.",
                [nameof(AggregateId)]
            );
        }

        var hasAggregateId = AggregateId.HasValue
            && AggregateId != Guid.Empty;
        var hasStateMachineId = !string.IsNullOrWhiteSpace(
            StateMachineId
        );

        if (hasAggregateId == hasStateMachineId)
        {
            yield return new ValidationResult(
                "Provide either an aggregate ID or a state-machine ID.",
                [nameof(AggregateId), nameof(StateMachineId)]
            );
        }
    }
}

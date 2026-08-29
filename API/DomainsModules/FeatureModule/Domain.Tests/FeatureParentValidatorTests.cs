using EventSourcing.Shared.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Validators;

namespace FeatureModule.Domain.Tests;

public sealed class FeatureParentValidatorTests
{
    private static readonly AggregateId FeatureId =
        AggregateId.FromDatabaseGuid(
            Guid.Parse("11111111-1111-1111-1111-111111111111")
        );

    [Fact]
    public void RejectsFeatureAsItsOwnParent()
    {
        var validator = new FeatureCannotParentItselfValidator();

        var result = validator.Validate(
            new FeatureStateData(FeatureId),
            CreatePayload(new FeatureParentSetV1(FeatureId))
        );

        Assert.False(result.Succeded);
        Assert.Contains("own parent", result.FailureReason);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("22222222-2222-2222-2222-222222222222")]
    public void AcceptsClearedOrDifferentParent(string? parentFeatureId)
    {
        var validator = new FeatureCannotParentItselfValidator();
        var parentId = parentFeatureId is null
            ? (AggregateId?)null
            : AggregateId.FromDatabaseGuid(Guid.Parse(parentFeatureId));

        var result = validator.Validate(
            new FeatureStateData(FeatureId),
            CreatePayload(new FeatureParentSetV1(parentId))
        );

        Assert.True(result.Succeded);
        Assert.Null(result.FailureReason);
    }

    private static EventPayload CreatePayload(
        EventSourcing.Shared.Interfaces.IEvent eventData
    ) =>
        EventPayload.Create(
            EventExecutor.FromDatabaseGuid(Guid.NewGuid()),
            FeatureId,
            "features-state-machine",
            eventData
        );
}

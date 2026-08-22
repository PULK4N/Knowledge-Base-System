using EventSourcing.Shared.Models;
using FeatureModule.Domain.Constraints;
using FeatureModule.Domain.Events;

namespace FeatureModule.Domain.Tests;

public sealed class UniqueFeatureNameConstraintTests
{
    private readonly UniqueFeatureNameConstraint _constraint = new();

    [Fact]
    public void Add_reserves_normalized_feature_name()
    {
        var payload = CreatePayload(
            new FeatureAddedV1(
                Id("22222222-2222-2222-2222-222222222222"),
                "  Search Page  ",
                "Summary",
                "Planning"
            )
        );
        var state = new FeatureStateData(
            payload.EventExecutionInfo.AggregateId
        );

        payload.EventData.Apply(state, payload.EventExecutionInfo);
        var constraint = Assert.Single(
            _constraint.CreateConstraintsToAdd(state, payload)
        );

        Assert.Equal(
            UniqueFeatureNameConstraint.ConstraintName,
            constraint.ConstraintName
        );
        Assert.Equal("SEARCH PAGE", constraint.ValueToHash);
    }

    [Fact]
    public void Remove_releases_feature_name_without_adding_it_back()
    {
        var state = new FeatureStateData(
            Id("11111111-1111-1111-1111-111111111111")
        )
        {
            ProjectId = Id("22222222-2222-2222-2222-222222222222"),
            Name = "Reusable feature",
            Summary = "Summary",
            Status = "Complete"
        };
        var payload = CreatePayload(new FeatureRemovedV1());

        payload.UniqueEventConstraintsToRemove.AddRange(
            _constraint.CreateConstraintsToRemove(state, payload)
        );
        payload.EventData.Apply(state, payload.EventExecutionInfo);

        Assert.Equal(
            "REUSABLE FEATURE",
            Assert.Single(payload.UniqueEventConstraintsToRemove).ValueToHash
        );
        Assert.Empty(_constraint.CreateConstraintsToAdd(state, payload));
    }

    private static EventPayload CreatePayload(
        EventSourcing.Shared.Interfaces.IEvent eventData
    ) =>
        EventPayload.Create(
            EventExecutor.FromDatabaseGuid(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
            ),
            Id("11111111-1111-1111-1111-111111111111"),
            "features-state-machine",
            eventData
        );

    private static AggregateId Id(string value) =>
        AggregateId.FromDatabaseGuid(Guid.Parse(value));
}

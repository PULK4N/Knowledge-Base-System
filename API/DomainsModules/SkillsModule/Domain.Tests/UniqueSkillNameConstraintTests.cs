using System.Collections.Immutable;
using EventSourcing.Shared.Models;
using SkillsModule.Domain.Constraints;
using SkillsModule.Domain.Events;

namespace SkillsModule.Domain.Tests;

public sealed class UniqueSkillNameConstraintTests
{
    private readonly UniqueSkillNameConstraint _constraint = new();

    [Fact]
    public void Create_AddsNormalizedSkillName()
    {
        var payload = CreatePayload(
            new SkillCreatedV2(
                "  My-Skill  ",
                "Description",
                "Content",
                [],
                ImmutableDictionary<string, Models.SkillReference2>.Empty
            )
        );
        var state = new SkillStateData(
            payload.EventExecutionInfo.AggregateId
        );

        AddConstraintsToRemove(state, payload);
        Apply(state, payload);
        var constraint = Assert.Single(
            _constraint.CreateConstraintsToAdd(state, payload)
        );

        Assert.Equal(UniqueSkillNameConstraint.ConstraintName, constraint.ConstraintName);
        Assert.Equal("MY-SKILL", constraint.ValueToHash);
    }

    [Fact]
    public void Update_WithDifferentName_ReplacesConstraint()
    {
        var state = CreateActiveState("old-name");
        var payload = CreatePayload(
            new SkillDetailsUpdatedV1(
                "new-name",
                "Description",
                "Content",
                []
            )
        );

        AddConstraintsToRemove(state, payload);
        Apply(state, payload);
        var constraintToAdd = Assert.Single(
            _constraint.CreateConstraintsToAdd(state, payload)
        );

        Assert.Equal(
            "OLD-NAME",
            Assert.Single(payload.UniqueEventConstraintsToRemove).ValueToHash
        );
        Assert.Equal("NEW-NAME", constraintToAdd.ValueToHash);
    }

    [Fact]
    public void Update_WithEquivalentName_KeepsExistingConstraint()
    {
        var state = CreateActiveState("My-Skill");
        var payload = CreatePayload(
            new SkillDetailsUpdatedV1(
                "  my-skill  ",
                "Updated description",
                "Updated content",
                []
            )
        );

        AddConstraintsToRemove(state, payload);
        Apply(state, payload);

        Assert.Empty(payload.UniqueEventConstraintsToRemove);
        Assert.Empty(_constraint.CreateConstraintsToAdd(state, payload));
    }

    [Fact]
    public void Delete_RemovesConstraintWithoutAddingItBack()
    {
        var state = CreateActiveState("skill-to-delete");
        var payload = CreatePayload(new SkillDeletedV1());

        AddConstraintsToRemove(state, payload);
        Apply(state, payload);

        Assert.Equal(
            "SKILL-TO-DELETE",
            Assert.Single(payload.UniqueEventConstraintsToRemove).ValueToHash
        );
        Assert.Empty(_constraint.CreateConstraintsToAdd(state, payload));
    }

    private void AddConstraintsToRemove(SkillStateData state, EventPayload payload)
    {
        payload.UniqueEventConstraintsToRemove.AddRange(
            _constraint.CreateConstraintsToRemove(state, payload)
        );
    }

    private static SkillStateData CreateActiveState(string name) =>
        new(AggregateId.FromDatabaseGuid(Guid.Empty))
        {
            Name = name,
            Description = "Description",
            Content = "Content"
        };

    private static void Apply(SkillStateData state, EventPayload payload) =>
        payload.EventData.Apply(state, payload.EventExecutionInfo);

    private static EventPayload CreatePayload(
        EventSourcing.Shared.Interfaces.IEvent eventData
    ) =>
        EventPayload.Create(
            EventExecutor.FromDatabaseGuid(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")),
            AggregateId.FromDatabaseGuid(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")),
            "skills-state-machine",
            eventData
        );
}

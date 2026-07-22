using EventSourcing.Shared.Models;
using MemoryMcp.Domain.Skills;

namespace MemoryMcp.Tests;

public sealed class SkillDomainTests
{
    [Fact]
    public void SaveUpdateDeleteBuildsAuditableSkillState()
    {
        var id = Guid.NewGuid();
        var state = new SkillState();
        var created = Execution(id, 1, new DateTime(2026, 7, 12, 8, 0, 0, DateTimeKind.Utc));
        var updated = Execution(id, 2, created.Timestamp.AddMinutes(5));
        var deleted = Execution(id, 3, created.Timestamp.AddMinutes(10));

        new SkillSaved("dotnet-tests", "Runs tests", "Use dotnet test", [ "dotnet" ]).Apply(
            state,
            created
        );
        new SkillUpdated(
            "dotnet-verification",
            "Runs focused tests",
            "Build first, then test",
            [ "dotnet", "tests" ],
            "dotnet-tests"
        ).Apply(state, updated);
        new SkillDeleted("superseded").Apply(state, deleted);

        Assert.Equal(id, state.Id);
        Assert.Equal("dotnet-verification", state.Name);
        Assert.Equal([ "dotnet", "tests" ], state.Tags);
        Assert.Equal(created.Timestamp, state.CreatedAtUtc);
        Assert.Equal(deleted.Timestamp, state.UpdatedAtUtc);
        Assert.Equal((uint)3, state.Version);
        Assert.True(state.IsDeleted);
    }

    [Fact]
    public void NameConstraintChangesOnlyForSaveRenameAndDelete()
    {
        var state = new SkillState { Name = "Old Name" };
        var constraint = new SkillNameConstraint();
        var save = Payload(new SkillSaved("Old Name", "", "content", [ ]));
        var sameNameUpdate = Payload(new SkillUpdated("old name", "", "content", [ ], "Old Name"));
        var rename = Payload(new SkillUpdated("New Name", "", "content", [ ], "Old Name"));
        var delete = Payload(new SkillDeleted("done"));

        Assert.Equal(
            "OLD NAME",
            Assert.Single(constraint.CreateConstraintsToAdd(state, save)).ValueToHash
        );
        Assert.Empty(constraint.CreateConstraintsToRemove(state, sameNameUpdate));
        Assert.Empty(constraint.CreateConstraintsToAdd(state, sameNameUpdate));
        Assert.Equal(
            "OLD NAME",
            Assert.Single(constraint.CreateConstraintsToRemove(state, rename)).ValueToHash
        );

        state.Name = "New Name";
        Assert.Equal(
            "NEW NAME",
            Assert.Single(constraint.CreateConstraintsToAdd(state, rename)).ValueToHash
        );
        Assert.Equal(
            "NEW NAME",
            Assert.Single(constraint.CreateConstraintsToRemove(state, delete)).ValueToHash
        );
    }

    private static EventExecutionInfo Execution(Guid id, uint order, DateTime timestamp) =>
        new()
        {
            Id = Guid.NewGuid(),
            AggregateId = id,
            EventExecutor = Guid.NewGuid(),
            EventName = "test",
            StateMachineId = "skills",
            OrderNumber = order,
            Timestamp = timestamp
        };

    private static EventPayload Payload(EventSourcing.Shared.Interfaces.IEvent data) =>
        EventPayload.Create(Guid.NewGuid(), Guid.NewGuid(), "skills", data);
}

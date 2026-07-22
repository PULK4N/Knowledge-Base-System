using EventSourcing.Shared.Models;
using MemoryMcp.Domain.Policies;

namespace MemoryMcp.Tests;

public sealed class PolicyDomainTests
{
    [Fact]
    public void UpdateCanDisablePolicyAndRaisePriority()
    {
        var id = Guid.NewGuid();
        var state = new PolicyState();
        var created = Execution(id, 1);
        var updated = Execution(id, 2);

        new PolicySaved(
            "verify-builds",
            "Always build changed projects",
            "repository",
            10,
            true,
            [ "build" ]
        ).Apply(state, created);
        new PolicyUpdated(
            "verify-builds",
            "Build and test changed projects",
            "repository",
            50,
            false,
            [ "build", "test" ],
            "verify-builds"
        ).Apply(state, updated);

        Assert.False(state.Enabled);
        Assert.Equal(50, state.Priority);
        Assert.Equal((uint)2, state.Version);
        Assert.Equal([ "build", "test" ], state.Tags);
    }

    [Fact]
    public void DeletedPolicyCannotBeUpdatedAgain()
    {
        var id = Guid.NewGuid();
        var state = new PolicyState();
        new PolicySaved("policy", "instruction", "global", 0, true, [ ]).Apply(
            state,
            Execution(id, 1)
        );
        new PolicyDeleted("retired").Apply(state, Execution(id, 2));

        var exception = Assert.Throws<InvalidOperationException>(
            () =>
                new PolicyUpdated("policy", "changed", "global", 0, true, [ ], "policy").Apply(
                    state,
                    Execution(id, 3)
                )
        );

        Assert.Contains("was not found", exception.Message);
    }

    private static EventExecutionInfo Execution(Guid id, uint order) =>
        new()
        {
            Id = Guid.NewGuid(),
            AggregateId = id,
            EventExecutor = Guid.NewGuid(),
            EventName = "test",
            StateMachineId = "policies",
            OrderNumber = order,
            Timestamp = DateTime.UtcNow.AddMinutes(order)
        };
}

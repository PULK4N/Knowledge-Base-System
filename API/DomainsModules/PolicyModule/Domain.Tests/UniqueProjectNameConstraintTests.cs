using System.Collections.Immutable;
using EventSourcing.Shared.Models;
using PolicyModule.Domain.Constraints;
using PolicyModule.Domain.Events;

namespace PolicyModule.Domain.Tests;

public sealed class UniqueProjectNameConstraintTests
{
    private readonly UniqueProjectNameConstraint _constraint = new();

    [Fact]
    public void Create_AddsNormalizedProjectName()
    {
        var payload = CreatePayload(
            new ProjectCreatedV1(
                "  MCP Knowledge Base  ",
                "Description",
                ImmutableArray<string>.Empty
            )
        );
        var state = new ProjectPoliciesStateData(
            payload.EventExecutionInfo.AggregateId
        );

        Apply(state, payload);
        var constraint = Assert.Single(
            _constraint.CreateConstraintsToAdd(state, payload)
        );

        Assert.Equal(
            UniqueProjectNameConstraint.ConstraintName,
            constraint.ConstraintName
        );
        Assert.Equal("MCP KNOWLEDGE BASE", constraint.ValueToHash);
    }

    [Fact]
    public void Rename_ReplacesConstraintUnlessNameIsEquivalent()
    {
        var state = CreateActiveState("Original project");
        var renamed = CreatePayload(
            new ProjectUpdatedV1("Renamed project", "Description")
        );

        AddConstraintsToRemove(state, renamed);
        Apply(state, renamed);

        Assert.Equal(
            "ORIGINAL PROJECT",
            Assert.Single(renamed.UniqueEventConstraintsToRemove).ValueToHash
        );
        Assert.Equal(
            "RENAMED PROJECT",
            Assert.Single(
                _constraint.CreateConstraintsToAdd(state, renamed)
            ).ValueToHash
        );

        var equivalent = CreatePayload(
            new ProjectUpdatedV1("  renamed PROJECT ", "Updated")
        );
        AddConstraintsToRemove(state, equivalent);
        Apply(state, equivalent);

        Assert.Empty(equivalent.UniqueEventConstraintsToRemove);
        Assert.Empty(
            _constraint.CreateConstraintsToAdd(state, equivalent)
        );
    }

    [Fact]
    public void Delete_ReleasesProjectName()
    {
        var state = CreateActiveState("Reusable project");
        var payload = CreatePayload(new ProjectDeletedV1());

        AddConstraintsToRemove(state, payload);
        Apply(state, payload);

        Assert.Equal(
            "REUSABLE PROJECT",
            Assert.Single(payload.UniqueEventConstraintsToRemove).ValueToHash
        );
        Assert.Empty(
            _constraint.CreateConstraintsToAdd(state, payload)
        );
    }

    private void AddConstraintsToRemove(
        ProjectPoliciesStateData state,
        EventPayload payload
    ) =>
        payload.UniqueEventConstraintsToRemove.AddRange(
            _constraint.CreateConstraintsToRemove(state, payload)
        );

    private static ProjectPoliciesStateData CreateActiveState(
        string projectName
    ) =>
        new(
            AggregateId.FromDatabaseGuid(
                Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")
            )
        )
        {
            ProjectName = projectName,
            ProjectDescription = "Description"
        };

    private static void Apply(
        ProjectPoliciesStateData state,
        EventPayload payload
    ) =>
        payload.EventData.Apply(state, payload.EventExecutionInfo);

    private static EventPayload CreatePayload(
        EventSourcing.Shared.Interfaces.IEvent eventData
    ) =>
        EventPayload.Create(
            EventExecutor.FromDatabaseGuid(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
            ),
            AggregateId.FromDatabaseGuid(
                Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")
            ),
            "project-policies-state-machine",
            eventData
        );
}

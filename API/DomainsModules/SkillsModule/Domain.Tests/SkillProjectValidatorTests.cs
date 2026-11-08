using System.Collections.Immutable;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using SkillsModule.Domain.Events;
using SkillsModule.Domain.Validators;

namespace SkillsModule.Domain.Tests;

public sealed class SkillProjectValidatorTests
{
    private static readonly AggregateId SkillId = AggregateId.FromDatabaseGuid(
        Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")
    );
    private static readonly AggregateId ProjectId = AggregateId.FromDatabaseGuid(
        Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc")
    );

    [Theory]
    [InlineData("skill", false, null)]
    [InlineData("skill", true, "The skill is deleted.")]
    [InlineData("", false, "The skill does not exist.")]
    public void SkillMustBeActive_RejectsMissingAndDeletedSkills(
        string name,
        bool isDeleted,
        string? expectedReason
    )
    {
        var state = new SkillStateData(SkillId) { Name = name, IsDeleted = isDeleted };

        var result = new SkillMustBeActiveValidator().Validate(
            state,
            CreatePayload(new SkillUseScenariosSetV1([], Guid.Empty, SkillId))
        );

        Assert.Equal(expectedReason is null, result.Succeded);
        Assert.Equal(expectedReason, result.FailureReason);
    }

    [Fact]
    public void SkillProjectRelation_RequiresUniqueExistingRelation()
    {
        var state = new SkillStateData(SkillId) { Name = "skill" };
        var addPayload = CreatePayload(
            new SkillProjectRelationAddedV1(ProjectId, Guid.Empty, SkillId)
        );
        var removePayload = CreatePayload(
            new SkillProjectRelationRemovedV1(ProjectId, Guid.Empty, SkillId)
        );
        var mustNotExist = new SkillProjectMustNotExistValidator();
        var mustExist = new SkillProjectMustExistValidator();

        Assert.True(mustNotExist.Validate(state, addPayload).Succeded);
        Assert.False(mustExist.Validate(state, removePayload).Succeded);

        state.RelatedProjectIds.Add(ProjectId);

        Assert.False(mustNotExist.Validate(state, addPayload).Succeded);
        Assert.True(mustExist.Validate(state, removePayload).Succeded);
    }

    [Fact]
    public void SkillUseScenariosSet_ReplacesTheWholeList()
    {
        var state = new SkillStateData(SkillId) { UseScenarios = ["Old scenario"] };

        new SkillUseScenariosSetV1(
            ["Writing Angular code", "Reviewing Angular code"],
            Guid.Empty,
            SkillId
        ).Apply(
            state,
            CreatePayload(new SkillUseScenariosSetV1([], Guid.Empty, SkillId))
                .EventExecutionInfo
        );

        Assert.Equal(
            ["Writing Angular code", "Reviewing Angular code"],
            state.UseScenarios
        );
        Assert.Single(state.MemoryHistory);
    }

    private static EventPayload CreatePayload(IEvent eventData) =>
        EventPayload.Create(
            EventExecutor.FromDatabaseGuid(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
            ),
            SkillId,
            "skills-state-machine",
            eventData
        );
}

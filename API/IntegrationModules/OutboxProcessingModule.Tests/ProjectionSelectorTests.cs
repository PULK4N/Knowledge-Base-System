using EventSourcing.Core.Models;
using EventSourcing.Shared.Interfaces;
using OutboxProcessingModule.Application;

namespace OutboxProcessingModule.Tests;

public sealed class ProjectionSelectorTests
{
    private const string StateMachineId = "skills-state-machine";
    private const string EventName = "SkillCreatedV1";

    [Fact]
    public void SelectNamesUnionsDefinitionAndEventProjectionsWithoutDuplicates()
    {
        var selector = CreateSelector(
            TestData.Definition(
                StateMachineId,
                projections: [ nameof(RecordingProjector) ],
                events: new Dictionary<string, StateMachineEventDefinition>
                {
                    [EventName] = TestData.Event(
                        projections:
                        [
                            nameof(RecordingProjector),
                            nameof(SecondRecordingProjector)
                        ])
                }
            ),
            new RecordingProjector(),
            new SecondRecordingProjector()
        );

        Assert.Equal(
            [ nameof(RecordingProjector), nameof(SecondRecordingProjector) ],
            selector.SelectNames(TestData.ExecutionInfo(StateMachineId, EventName))
        );
    }

    [Fact]
    public void SelectNamesReturnsNothingWhenNoProjectionIsDeclared()
    {
        var selector = CreateSelector(TestData.Definition(StateMachineId));

        Assert.Empty(
            selector.SelectNames(TestData.ExecutionInfo(StateMachineId, EventName)));
    }

    [Fact]
    public void SelectResolvesEveryNamedProjector()
    {
        var projector = new RecordingProjector();
        var selector = CreateSelector(
            TestData.Definition(
                StateMachineId, projections: [ nameof(RecordingProjector) ]),
            projector
        );

        Assert.Same(
            projector,
            Assert.Single(
                selector.Select(TestData.ExecutionInfo(StateMachineId, EventName)))
        );
    }

    [Fact]
    public void SelectThrowsWhenAProjectorNamedInYamlIsNotRegistered()
    {
        var selector = CreateSelector(
            TestData.Definition(
                StateMachineId, projections: [ nameof(RecordingProjector) ])
        );

        var error = Assert.Throws<InvalidOperationException>(
            () => selector.Select(TestData.ExecutionInfo(StateMachineId, EventName)));

        Assert.Contains(nameof(RecordingProjector), error.Message);
    }

    private static ProjectionSelector CreateSelector(
        StateMachineDefinition definition,
        params IProjector[] projectors
    ) => new(
        new TestDefinitionProvider(definition),
        new ProjectorRegistry(projectors)
    );
}

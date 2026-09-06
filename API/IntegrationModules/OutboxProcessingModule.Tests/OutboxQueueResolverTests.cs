using EventSourcing.Core.Models;
using EventSourcing.Shared.Exceptions;
using OutboxProcessingModule.Application;

namespace OutboxProcessingModule.Tests;

public sealed class OutboxQueueResolverTests
{
    private const string StateMachineId = "skills-state-machine";
    private const string EventName = "SkillCreatedV1";

    private const string ProjectionsQueue =
        "knowledge-base.skills-state-machine.projections";
    private const string HooksQueue =
        "knowledge-base.skills-state-machine.hooks";

    public static TheoryData<StateMachineDefinition, List<string>> RoutingCases() => new()
    {
        {
            TestData.Definition(
                StateMachineId,
                projections: [ "SkillSummaryProjector" ],
                events: new Dictionary<string, StateMachineEventDefinition>
                {
                    [EventName] = TestData.Event()
                }
            ),
            [ ProjectionsQueue ]
        },
        {
            TestData.Definition(
                StateMachineId,
                events: new Dictionary<string, StateMachineEventDefinition>
                {
                    [EventName] = TestData.Event(
                        projections: [ "SkillSearchProjector" ])
                }
            ),
            [ ProjectionsQueue ]
        },
        {
            TestData.Definition(
                StateMachineId,
                projections: [ "SkillSummaryProjector" ],
                events: new Dictionary<string, StateMachineEventDefinition>
                {
                    [EventName] = TestData.Event(hooks: [ "NotifySkillHook" ])
                }
            ),
            [ ProjectionsQueue, HooksQueue ]
        },
        {
            TestData.Definition(
                StateMachineId,
                events: new Dictionary<string, StateMachineEventDefinition>
                {
                    [EventName] = TestData.Event(hooks: [ "NotifySkillHook" ])
                }
            ),
            [ HooksQueue ]
        },
        {
            TestData.Definition(
                StateMachineId,
                events: new Dictionary<string, StateMachineEventDefinition>
                {
                    [EventName] = TestData.Event()
                }
            ),
            [ ]
        },
        {
            TestData.Definition(StateMachineId),
            [ ]
        }
    };

    [Theory]
    [MemberData(nameof(RoutingCases))]
    public void ResolveQueuesRoutesByDeclaredProjectionsAndHooks(
        StateMachineDefinition definition,
        List<string> expectedQueues
    )
    {
        var resolver = new OutboxQueueResolver(new TestDefinitionProvider(definition));
        var row = TestData.Row(TestData.ExecutionInfo(StateMachineId, EventName));

        Assert.Equal(expectedQueues, resolver.ResolveQueues(row));
    }

    [Fact]
    public void ResolveQueuesThrowsForAnUnknownStateMachine()
    {
        var resolver = new OutboxQueueResolver(new TestDefinitionProvider());
        var row = TestData.Row(TestData.ExecutionInfo("missing-state-machine", EventName));

        Assert.Throws<StateMachineNotRegisteredException>(
            () => resolver.ResolveQueues(row));
    }

    [Fact]
    public void ResolvePairsEveryRowWithItsQueues()
    {
        var definition = TestData.Definition(
            StateMachineId,
            projections: [ "SkillSummaryProjector" ],
            events: new Dictionary<string, StateMachineEventDefinition>
            {
                [EventName] = TestData.Event(),
                ["SkillDeletedV1"] = TestData.Event(hooks: [ "NotifySkillHook" ])
            }
        );
        var resolver = new OutboxQueueResolver(new TestDefinitionProvider(definition));

        var dispatches = resolver.Resolve(
            [
                TestData.Row(TestData.ExecutionInfo(StateMachineId, EventName)),
                TestData.Row(TestData.ExecutionInfo(StateMachineId, "SkillDeletedV1"))
            ]
        );

        Assert.Equal(
            [ ProjectionsQueue ],
            dispatches[0].Queues
        );
        Assert.Equal(
            [ ProjectionsQueue, HooksQueue ],
            dispatches[1].Queues
        );
    }
}

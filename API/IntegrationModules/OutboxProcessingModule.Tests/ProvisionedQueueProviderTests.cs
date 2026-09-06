using EventSourcing.Core.Models;
using OutboxProcessingModule.Application;
using SharedModule.DistributedMessaging.Queues;

namespace OutboxProcessingModule.Tests;

public sealed class ProvisionedQueueProviderTests
{
    [Fact]
    public void EachStateMachineGetsAQueueOnlyForWhatItDeclares()
    {
        var provider = new ProvisionedQueueProvider(
            new TestDefinitionProvider(
                TestData.Definition(
                    "projections-only",
                    projections: [ "SkillSummaryProjector" ]),
                TestData.Definition(
                    "event-projections-only",
                    events: new Dictionary<string, StateMachineEventDefinition>
                    {
                        ["EventV1"] = TestData.Event(projections: [ "SkillListProjector" ])
                    }),
                TestData.Definition(
                    "both",
                    projections: [ "SkillSummaryProjector" ],
                    events: new Dictionary<string, StateMachineEventDefinition>
                    {
                        ["EventV1"] = TestData.Event(hooks: [ "NotifyHook" ])
                    }),
                TestData.Definition(
                    "hooks-only",
                    events: new Dictionary<string, StateMachineEventDefinition>
                    {
                        ["EventV1"] = TestData.Event(hooks: [ "NotifyHook" ])
                    }),
                TestData.Definition("neither")
            )
        );

        Assert.Equal(
            [
                new ProvisionedQueue("projections-only", DeliveryRole.Projections),
                new ProvisionedQueue("event-projections-only", DeliveryRole.Projections),
                new ProvisionedQueue("both", DeliveryRole.Projections),
                new ProvisionedQueue("both", DeliveryRole.Hooks),
                new ProvisionedQueue("hooks-only", DeliveryRole.Hooks)
            ],
            provider.GetAll()
        );
    }

    [Fact]
    public void QueueNamesMatchTheProvisionedBrokerNames() =>
        Assert.Equal(
            "knowledge-base.skills-state-machine.projections",
            new ProvisionedQueue("skills-state-machine", DeliveryRole.Projections).Name);
}

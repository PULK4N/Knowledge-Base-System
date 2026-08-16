using EventSourcing.Core.Providers;
using EventSourcing.Shared.Containers;
using MemoryModule.Domain.Events;
using MemoryModule.Domain.Validators;

namespace MemoryModule.Domain.Tests;

public sealed class MemoryStateMachineDefinitionTests
{
    [Fact]
    public void Memory_events_trigger_search_and_summary_projections()
    {
        StateDataTypeContainer.AddStateDataType(typeof(MemoryStateData));
        StateDataTypeContainer.AddStateDataType(
            typeof(SessionAggregateMapStateData)
        );
        EventTypeContainer.AddEventType(typeof(CodexPromptHookRecordedV1));
        EventTypeContainer.AddEventType(typeof(CodexMemoryMigratedV1));
        EventTypeContainer.AddEventType(typeof(ChatSummaryAddedV1));
        EventTypeContainer.AddEventType(typeof(SessionAggregateMapAddedV1));
        EventValidatorContainer.AddEventValidator(
            typeof(SessionAggregateMappingMustNotExistValidator)
        );
        var provider = new YamlStateMachineDefinitionProvider(
            Path.Combine(AppContext.BaseDirectory, "StateMachines")
        );

        var definition = provider.Get("memory-state-machine");

        Assert.Equal(nameof(MemoryStateData), definition.StateData);
        Assert.Equal(
            [
                nameof(MemorySearchProjector),
                nameof(MemorySummaryProjector)
            ],
            definition.Projections
        );
        Assert.All(
            definition.Events.Values,
            eventDefinition => Assert.Empty(eventDefinition.Projections)
        );
    }

    private sealed class MemorySearchProjector;
    private sealed class MemorySummaryProjector;
}

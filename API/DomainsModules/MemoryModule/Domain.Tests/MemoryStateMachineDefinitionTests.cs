using EventSourcing.Core.Providers;
using EventSourcing.Shared.Containers;
using MemoryModule.Domain.Events;
using MemoryModule.Domain.Validators;

namespace MemoryModule.Domain.Tests;

public sealed class MemoryStateMachineDefinitionTests
{
    [Fact]
    public void Memory_events_trigger_search_projection()
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
            [nameof(MemorySearchProjector)],
            definition.Events[nameof(CodexPromptHookRecordedV1)].Projections
        );
        Assert.Equal(
            [nameof(MemorySearchProjector)],
            definition.Events[nameof(CodexMemoryMigratedV1)].Projections
        );
        Assert.Empty(
            definition.Events[nameof(ChatSummaryAddedV1)].Projections
        );
    }

    private sealed class MemorySearchProjector;
}

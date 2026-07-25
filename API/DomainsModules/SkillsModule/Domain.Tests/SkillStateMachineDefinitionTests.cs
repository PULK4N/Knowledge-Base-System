using EventSourcing.Core.Providers;
using EventSourcing.Shared.Containers;
using SkillsModule.Domain.Constraints;
using SkillsModule.Domain.Events;

namespace SkillsModule.Domain.Tests;

public sealed class SkillStateMachineDefinitionTests
{
    private static readonly object RegistrationLock = new();
    private static bool _typesRegistered;

    [Fact]
    public void LoadsSkillStateMachineDefinition()
    {
        RegisterTypesOnce();
        var stateMachinesPath = Path.Combine(
            AppContext.BaseDirectory,
            "StateMachines"
        );

        var provider = new YamlStateMachineDefinitionProvider(stateMachinesPath);
        var definition = provider.Get("skills-state-machine");

        Assert.Equal(nameof(SkillStateData), definition.StateData);
        Assert.Equal(
            [nameof(SkillSaved), nameof(SkillUpdated), nameof(SkillDeleted)],
            definition.Events.Keys
        );
        Assert.All(
            definition.Events.Values,
            eventDefinition =>
                Assert.Equal(
                    [nameof(UniqueSkillNameConstraint)],
                    eventDefinition.UniqueConstraints
                )
        );
    }

    private static void RegisterTypesOnce()
    {
        lock (RegistrationLock)
        {
            if (_typesRegistered)
                return;

            StateDataTypeContainer.AddStateDataType(typeof(SkillStateData));
            EventTypeContainer.AddEventType(typeof(SkillSaved));
            EventTypeContainer.AddEventType(typeof(SkillUpdated));
            EventTypeContainer.AddEventType(typeof(SkillDeleted));
            ConstraintCreatorTypeContainer.AddUniqueEventConstraintCreator(
                typeof(UniqueSkillNameConstraint)
            );

            _typesRegistered = true;
        }
    }
}

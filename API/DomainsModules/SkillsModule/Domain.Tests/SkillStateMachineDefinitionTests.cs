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
            [
                nameof(SkillSaved),
                nameof(SkillUpdated),
                nameof(SkillDetailsUpdated),
                nameof(SkillDeleted),
                nameof(SkillReferenceAdded),
                nameof(SkillReferenceUpdated),
                nameof(SkillReferenceDeleted)
            ],
            definition.Events.Keys
        );
        Assert.Equal(
            [nameof(UniqueSkillNameConstraint)],
            definition.Events[nameof(SkillSaved)].UniqueConstraints
        );
        Assert.Equal(
            [nameof(UniqueSkillNameConstraint)],
            definition.Events[nameof(SkillUpdated)].UniqueConstraints
        );
        Assert.Equal(
            [nameof(UniqueSkillNameConstraint)],
            definition.Events[nameof(SkillDetailsUpdated)].UniqueConstraints
        );
        Assert.Equal(
            [nameof(UniqueSkillNameConstraint)],
            definition.Events[nameof(SkillDeleted)].UniqueConstraints
        );
        Assert.Empty(definition.Events[nameof(SkillReferenceAdded)].UniqueConstraints);
        Assert.Empty(definition.Events[nameof(SkillReferenceUpdated)].UniqueConstraints);
        Assert.Empty(definition.Events[nameof(SkillReferenceDeleted)].UniqueConstraints);
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
            EventTypeContainer.AddEventType(typeof(SkillDetailsUpdated));
            EventTypeContainer.AddEventType(typeof(SkillDeleted));
            EventTypeContainer.AddEventType(typeof(SkillReferenceAdded));
            EventTypeContainer.AddEventType(typeof(SkillReferenceUpdated));
            EventTypeContainer.AddEventType(typeof(SkillReferenceDeleted));
            ConstraintCreatorTypeContainer.AddUniqueEventConstraintCreator(
                typeof(UniqueSkillNameConstraint)
            );

            _typesRegistered = true;
        }
    }
}

using EventSourcing.Core.Providers;
using EventSourcing.Shared.Containers;
using EventSourcing.Shared.Models;
using SkillsModule.Domain.Constraints;
using SkillsModule.Domain.Events;
using SkillsModule.Domain.Validators;

namespace SkillsModule.Domain.Tests;

public sealed class SkillStateMachineDefinitionTests
{
    private static readonly object RegistrationLock = new();
    private static bool _typesRegistered;

    [Fact]
    public void LoadsSkillStateMachineDefinition()
    {
        RegisterTypesOnce();
        var provider = CreateDefinitionProvider();
        var definition = provider.Get("skills-state-machine");

        Assert.Equal(nameof(SkillStateData), definition.StateData);
        Assert.Equal(
            [
                nameof(SkillCreatedV1),
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
            definition.Events[nameof(SkillCreatedV1)].UniqueConstraints
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
        Assert.Equal(
            [nameof(SkillReferenceMustNotExistValidator)],
            definition.Events[nameof(SkillReferenceAdded)].PreEventValidators
        );
        Assert.Equal(
            [nameof(SkillReferenceMustExistValidator)],
            definition.Events[nameof(SkillReferenceUpdated)].PreEventValidators
        );
        Assert.Equal(
            [nameof(SkillReferenceMustExistValidator)],
            definition.Events[nameof(SkillReferenceDeleted)].PreEventValidators
        );
        Assert.All(
            definition.Events.Values,
            eventDefinition => Assert.Empty(eventDefinition.PostEventValidators)
        );
    }

    [Fact]
    public async Task ResolvesReferenceValidatorsFromYaml()
    {
        RegisterTypesOnce();
        var provider = new EventValidatorProvider(CreateDefinitionProvider());
        var addedPayload = CreatePayload(
            new SkillReferenceAdded
            {
                RelativePath = "references/example.md",
                Content = "Content"
            }
        );
        var updatedPayload = CreatePayload(
            new SkillReferenceUpdated
            {
                RelativePath = "references/example.md",
                Content = "Updated content"
            }
        );

        var addedValidators = await provider.GetPreEventStateValidators(addedPayload);
        var updatedValidators = await provider.GetPreEventStateValidators(updatedPayload);

        Assert.IsType<SkillReferenceMustNotExistValidator>(
            Assert.Single(addedValidators)
        );
        Assert.IsType<SkillReferenceMustExistValidator>(
            Assert.Single(updatedValidators)
        );
        Assert.Empty(await provider.GetPostEventStateValidators(addedPayload));
    }

    private static YamlStateMachineDefinitionProvider CreateDefinitionProvider() =>
        new(
            Path.Combine(
                AppContext.BaseDirectory,
                "StateMachines"
            )
        );

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
            "skills-state-machine",
            eventData
        );

    private static void RegisterTypesOnce()
    {
        lock (RegistrationLock)
        {
            if (_typesRegistered)
                return;

            StateDataTypeContainer.AddStateDataType(typeof(SkillStateData));
            EventTypeContainer.AddEventType(typeof(SkillCreatedV1));
            EventTypeContainer.AddEventType(typeof(SkillUpdated));
            EventTypeContainer.AddEventType(typeof(SkillDetailsUpdated));
            EventTypeContainer.AddEventType(typeof(SkillDeleted));
            EventTypeContainer.AddEventType(typeof(SkillReferenceAdded));
            EventTypeContainer.AddEventType(typeof(SkillReferenceUpdated));
            EventTypeContainer.AddEventType(typeof(SkillReferenceDeleted));
            ConstraintCreatorTypeContainer.AddUniqueEventConstraintCreator(
                typeof(UniqueSkillNameConstraint)
            );
            EventValidatorContainer.AddEventValidator(
                typeof(SkillReferenceMustNotExistValidator)
            );
            EventValidatorContainer.AddEventValidator(
                typeof(SkillReferenceMustExistValidator)
            );

            _typesRegistered = true;
        }
    }
}

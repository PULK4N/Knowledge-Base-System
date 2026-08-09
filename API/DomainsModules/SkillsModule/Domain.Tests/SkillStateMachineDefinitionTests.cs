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
                nameof(SkillUpdatedV1),
                nameof(SkillDetailsUpdatedV1),
                nameof(SkillDeletedV1),
                nameof(SkillReferenceAddedV1),
                nameof(SkillReferenceUpdatedV1),
                nameof(SkillReferenceDeletedV1),
                nameof(SkillAttachmentAddedV1),
                nameof(SkillAttachmentDeletedV1)
            ],
            definition.Events.Keys
        );
        Assert.Equal(
            [nameof(UniqueSkillNameConstraint)],
            definition.Events[nameof(SkillCreatedV1)].UniqueConstraints
        );
        Assert.Equal(
            [nameof(UniqueSkillNameConstraint)],
            definition.Events[nameof(SkillUpdatedV1)].UniqueConstraints
        );
        Assert.Equal(
            [nameof(UniqueSkillNameConstraint)],
            definition.Events[nameof(SkillDetailsUpdatedV1)].UniqueConstraints
        );
        Assert.Equal(
            [nameof(UniqueSkillNameConstraint)],
            definition.Events[nameof(SkillDeletedV1)].UniqueConstraints
        );
        Assert.Empty(definition.Events[nameof(SkillReferenceAddedV1)].UniqueConstraints);
        Assert.Empty(definition.Events[nameof(SkillReferenceUpdatedV1)].UniqueConstraints);
        Assert.Empty(definition.Events[nameof(SkillReferenceDeletedV1)].UniqueConstraints);
        Assert.Equal(
            [nameof(SkillReferenceMustNotExistValidator)],
            definition.Events[nameof(SkillReferenceAddedV1)].PreEventValidators
        );
        Assert.Equal(
            [nameof(SkillReferenceMustExistValidator)],
            definition.Events[nameof(SkillReferenceUpdatedV1)].PreEventValidators
        );
        Assert.Equal(
            [nameof(SkillReferenceMustExistValidator)],
            definition.Events[nameof(SkillReferenceDeletedV1)].PreEventValidators
        );
        Assert.Equal(
            [nameof(SkillAttachmentMustNotExistValidator)],
            definition.Events[nameof(SkillAttachmentAddedV1)].PreEventValidators
        );
        Assert.Equal(
            [nameof(SkillAttachmentMustExistValidator)],
            definition.Events[nameof(SkillAttachmentDeletedV1)].PreEventValidators
        );
        Assert.All(
            new[]
            {
                nameof(SkillCreatedV1),
                nameof(SkillUpdatedV1),
                nameof(SkillDetailsUpdatedV1),
                nameof(SkillDeletedV1),
                nameof(SkillReferenceAddedV1),
                nameof(SkillReferenceUpdatedV1),
                nameof(SkillReferenceDeletedV1)
            },
            eventName => Assert.Contains(
                "SkillSearchProjector",
                definition.Events[eventName].Projections
            )
        );
        Assert.DoesNotContain(
            "SkillSearchProjector",
            definition.Events[nameof(SkillAttachmentAddedV1)].Projections
        );
        Assert.DoesNotContain(
            "SkillSearchProjector",
            definition.Events[nameof(SkillAttachmentDeletedV1)].Projections
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
            new SkillReferenceAddedV1
            {
                RelativePath = "references/example.md",
                Content = "Content"
            }
        );
        var updatedPayload = CreatePayload(
            new SkillReferenceUpdatedV1
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

    [Fact]
    public async Task ResolvesAttachmentValidatorsFromYaml()
    {
        RegisterTypesOnce();
        var provider = new EventValidatorProvider(
            CreateDefinitionProvider()
        );
        var attachment = new Models.Attachment
        {
            Id = Models.FileId.FromDatabaseGuid(
                Guid.Parse("11111111-1111-1111-1111-111111111111")
            ),
            Name = "example.pdf",
            Size = 1_024,
            FileType = "application/pdf",
            Extension = "pdf"
        };

        var addedValidators = await provider.GetPreEventStateValidators(
            CreatePayload(new SkillAttachmentAddedV1(attachment))
        );
        var deletedValidators = await provider.GetPreEventStateValidators(
            CreatePayload(
                new SkillAttachmentDeletedV1(attachment.Id)
            )
        );

        Assert.IsType<SkillAttachmentMustNotExistValidator>(
            Assert.Single(addedValidators)
        );
        Assert.IsType<SkillAttachmentMustExistValidator>(
            Assert.Single(deletedValidators)
        );
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
            EventTypeContainer.AddEventType(typeof(SkillUpdatedV1));
            EventTypeContainer.AddEventType(typeof(SkillDetailsUpdatedV1));
            EventTypeContainer.AddEventType(typeof(SkillDeletedV1));
            EventTypeContainer.AddEventType(typeof(SkillReferenceAddedV1));
            EventTypeContainer.AddEventType(typeof(SkillReferenceUpdatedV1));
            EventTypeContainer.AddEventType(typeof(SkillReferenceDeletedV1));
            EventTypeContainer.AddEventType(typeof(SkillAttachmentAddedV1));
            EventTypeContainer.AddEventType(typeof(SkillAttachmentDeletedV1));
            ConstraintCreatorTypeContainer.AddUniqueEventConstraintCreator(
                typeof(UniqueSkillNameConstraint)
            );
            EventValidatorContainer.AddEventValidator(
                typeof(SkillReferenceMustNotExistValidator)
            );
            EventValidatorContainer.AddEventValidator(
                typeof(SkillReferenceMustExistValidator)
            );
            EventValidatorContainer.AddEventValidator(
                typeof(SkillAttachmentMustNotExistValidator)
            );
            EventValidatorContainer.AddEventValidator(
                typeof(SkillAttachmentMustExistValidator)
            );

            _typesRegistered = true;
        }
    }
}

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
                nameof(SkillCreatedV2),
                nameof(SkillCreatedV3)
            ],
            definition.InitializationEvents
        );
        Assert.Equal(
            [
                "SkillSummaryProjector",
                "SkillListProjector",
                "SkillSearchProjector"
            ],
            definition.Projections
        );
        Assert.Equal(
            [
                nameof(SkillCreatedV1),
                nameof(SkillCreatedV2),
                nameof(SkillCreatedV3),
                nameof(SkillUpdatedV1),
                nameof(SkillDetailsUpdatedV1),
                nameof(SkillDetailsUpdatedV2),
                nameof(SkillDeletedV1),
                nameof(SkillDeletedV2),
                nameof(SkillReferenceAddedV1),
                nameof(SkillReferenceAddedV2),
                nameof(SkillReferenceAddedV3),
                nameof(SkillReferenceUpdatedV1),
                nameof(SkillReferenceUpdatedV2),
                nameof(SkillReferenceUpdatedV3),
                nameof(SkillReferenceAutoLoadUpdatedV1),
                nameof(SkillReferenceAutoLoadUpdatedV2),
                nameof(SkillReferenceDeletedV1),
                nameof(SkillReferenceDeletedV2),
                nameof(SkillAttachmentAddedV1),
                nameof(SkillAttachmentAddedV2),
                nameof(SkillAttachmentDeletedV1),
                nameof(SkillAttachmentDeletedV2)
            ],
            definition.Events.Keys
        );
        Assert.Equal(
            [nameof(UniqueSkillNameConstraint)],
            definition.Events[nameof(SkillCreatedV1)].UniqueConstraints
        );
        Assert.Equal(
            [nameof(UniqueSkillNameConstraint)],
            definition.Events[nameof(SkillCreatedV2)].UniqueConstraints
        );
        Assert.Equal(
            [nameof(UniqueSkillNameConstraint)],
            definition.Events[nameof(SkillCreatedV3)].UniqueConstraints
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
            definition.Events[nameof(SkillDetailsUpdatedV2)].UniqueConstraints
        );
        Assert.Equal(
            [nameof(UniqueSkillNameConstraint)],
            definition.Events[nameof(SkillDeletedV1)].UniqueConstraints
        );
        Assert.Equal(
            [nameof(UniqueSkillNameConstraint)],
            definition.Events[nameof(SkillDeletedV2)].UniqueConstraints
        );
        Assert.Empty(definition.Events[nameof(SkillReferenceAddedV1)].UniqueConstraints);
        Assert.Empty(definition.Events[nameof(SkillReferenceAddedV2)].UniqueConstraints);
        Assert.Empty(definition.Events[nameof(SkillReferenceAddedV3)].UniqueConstraints);
        Assert.Empty(definition.Events[nameof(SkillReferenceUpdatedV1)].UniqueConstraints);
        Assert.Empty(definition.Events[nameof(SkillReferenceUpdatedV2)].UniqueConstraints);
        Assert.Empty(definition.Events[nameof(SkillReferenceUpdatedV3)].UniqueConstraints);
        Assert.Empty(
            definition.Events[nameof(SkillReferenceAutoLoadUpdatedV1)]
                .UniqueConstraints
        );
        Assert.Empty(
            definition.Events[nameof(SkillReferenceAutoLoadUpdatedV2)]
                .UniqueConstraints
        );
        Assert.Empty(definition.Events[nameof(SkillReferenceDeletedV1)].UniqueConstraints);
        Assert.Empty(definition.Events[nameof(SkillReferenceDeletedV2)].UniqueConstraints);
        Assert.Equal(
            [nameof(SkillReferenceMustNotExistValidator)],
            definition.Events[nameof(SkillReferenceAddedV1)].PreEventValidators
        );
        Assert.Equal(
            [nameof(SkillReferenceMustNotExistValidator)],
            definition.Events[nameof(SkillReferenceAddedV2)].PreEventValidators
        );
        Assert.Equal(
            [nameof(SkillReferenceMustNotExistValidator)],
            definition.Events[nameof(SkillReferenceAddedV3)].PreEventValidators
        );
        Assert.Equal(
            [nameof(SkillReferenceMustExistValidator)],
            definition.Events[nameof(SkillReferenceUpdatedV1)].PreEventValidators
        );
        Assert.Equal(
            [nameof(SkillReferenceMustExistValidator)],
            definition.Events[nameof(SkillReferenceUpdatedV2)].PreEventValidators
        );
        Assert.Equal(
            [nameof(SkillReferenceMustExistValidator)],
            definition.Events[nameof(SkillReferenceUpdatedV3)].PreEventValidators
        );
        Assert.Equal(
            [nameof(SkillReferenceMustExistValidator)],
            definition.Events[nameof(SkillReferenceAutoLoadUpdatedV1)]
                .PreEventValidators
        );
        Assert.Equal(
            [nameof(SkillReferenceMustExistValidator)],
            definition.Events[nameof(SkillReferenceAutoLoadUpdatedV2)]
                .PreEventValidators
        );
        Assert.Equal(
            [nameof(SkillReferenceMustExistValidator)],
            definition.Events[nameof(SkillReferenceDeletedV1)].PreEventValidators
        );
        Assert.Equal(
            [nameof(SkillReferenceMustExistValidator)],
            definition.Events[nameof(SkillReferenceDeletedV2)].PreEventValidators
        );
        Assert.Equal(
            [nameof(SkillAttachmentMustNotExistValidator)],
            definition.Events[nameof(SkillAttachmentAddedV1)].PreEventValidators
        );
        Assert.Equal(
            [nameof(SkillAttachmentMustNotExistValidator)],
            definition.Events[nameof(SkillAttachmentAddedV2)].PreEventValidators
        );
        Assert.Equal(
            [nameof(SkillAttachmentMustExistValidator)],
            definition.Events[nameof(SkillAttachmentDeletedV1)].PreEventValidators
        );
        Assert.Equal(
            [nameof(SkillAttachmentMustExistValidator)],
            definition.Events[nameof(SkillAttachmentDeletedV2)].PreEventValidators
        );
        Assert.All(
            definition.Events.Values,
            eventDefinition => Assert.Empty(eventDefinition.Projections)
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
            new SkillReferenceAddedV3(
                "references/example.md",
                "Content",
                true,
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                AggregateId.FromDatabaseGuid(
                    Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")
                )
            )
        );
        var updatedPayload = CreatePayload(
            new SkillReferenceUpdatedV3(
                "references/example.md",
                "Updated content",
                true,
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                AggregateId.FromDatabaseGuid(
                    Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")
                )
            )
        );
        var autoLoadUpdatedPayload = CreatePayload(
            new SkillReferenceAutoLoadUpdatedV2(
                "references/example.md",
                false,
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                AggregateId.FromDatabaseGuid(
                    Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")
                )
            )
        );

        var addedValidators = await provider.GetPreEventStateValidators(addedPayload);
        var updatedValidators = await provider.GetPreEventStateValidators(updatedPayload);
        var autoLoadUpdatedValidators =
            await provider.GetPreEventStateValidators(autoLoadUpdatedPayload);

        Assert.IsType<SkillReferenceMustNotExistValidator>(
            Assert.Single(addedValidators)
        );
        Assert.IsType<SkillReferenceMustExistValidator>(
            Assert.Single(updatedValidators)
        );
        Assert.IsType<SkillReferenceMustExistValidator>(
            Assert.Single(autoLoadUpdatedValidators)
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
            CreatePayload(
                new SkillAttachmentAddedV2(
                    attachment,
                    Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    AggregateId.FromDatabaseGuid(
                        Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")
                    )
                )
            )
        );
        var deletedValidators = await provider.GetPreEventStateValidators(
            CreatePayload(
                new SkillAttachmentDeletedV2(
                    attachment.Id,
                    Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    AggregateId.FromDatabaseGuid(
                        Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")
                    )
                )
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
            EventTypeContainer.AddEventType(typeof(SkillCreatedV2));
            EventTypeContainer.AddEventType(typeof(SkillCreatedV3));
            EventTypeContainer.AddEventType(typeof(SkillUpdatedV1));
            EventTypeContainer.AddEventType(typeof(SkillDetailsUpdatedV1));
            EventTypeContainer.AddEventType(typeof(SkillDetailsUpdatedV2));
            EventTypeContainer.AddEventType(typeof(SkillDeletedV1));
            EventTypeContainer.AddEventType(typeof(SkillDeletedV2));
            EventTypeContainer.AddEventType(typeof(SkillReferenceAddedV1));
            EventTypeContainer.AddEventType(typeof(SkillReferenceAddedV2));
            EventTypeContainer.AddEventType(typeof(SkillReferenceAddedV3));
            EventTypeContainer.AddEventType(typeof(SkillReferenceUpdatedV1));
            EventTypeContainer.AddEventType(typeof(SkillReferenceUpdatedV2));
            EventTypeContainer.AddEventType(typeof(SkillReferenceUpdatedV3));
            EventTypeContainer.AddEventType(
                typeof(SkillReferenceAutoLoadUpdatedV1)
            );
            EventTypeContainer.AddEventType(
                typeof(SkillReferenceAutoLoadUpdatedV2)
            );
            EventTypeContainer.AddEventType(typeof(SkillReferenceDeletedV1));
            EventTypeContainer.AddEventType(typeof(SkillReferenceDeletedV2));
            EventTypeContainer.AddEventType(typeof(SkillAttachmentAddedV1));
            EventTypeContainer.AddEventType(typeof(SkillAttachmentAddedV2));
            EventTypeContainer.AddEventType(typeof(SkillAttachmentDeletedV1));
            EventTypeContainer.AddEventType(typeof(SkillAttachmentDeletedV2));
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

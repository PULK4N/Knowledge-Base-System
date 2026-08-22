using EventSourcing.Core.Providers;
using EventSourcing.Shared.Containers;
using EventSourcing.Shared.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;
using FeatureModule.Domain.Validators;

namespace FeatureModule.Domain.Tests;

public sealed class FeatureStateMachineDefinitionTests
{
    private static readonly object RegistrationLock = new();
    private static bool _typesRegistered;

    [Fact]
    public void LoadsFeatureStateMachineDefinition()
    {
        RegisterTypesOnce();
        var definition = CreateDefinitionProvider().Get(
            "features-state-machine"
        );

        Assert.Equal(nameof(FeatureStateData), definition.StateData);
        Assert.Equal(
            [nameof(FeatureAddedV1)],
            definition.InitializationEvents
        );
        Assert.Empty(definition.Projections);
        Assert.Equal(
            [
                nameof(FeatureAddedV1),
                nameof(FeatureRemovedV1),
                nameof(FeatureStatusUpdatedV1),
                nameof(FeatureSkillAddedV1),
                nameof(FeatureSkillRemovedV1),
                nameof(FeatureRecordAddedV1),
                nameof(FeatureRecordUpdatedV1),
                nameof(FeatureRecordRemovedV1),
                nameof(FeaturePlanAddedV1),
                nameof(CurrentFeaturePlanUpdatedV1),
                nameof(CurrentFeaturePlanChangedV1),
                nameof(FeaturePlanRemovedV1)
            ],
            definition.Events.Keys
        );
        Assert.Equal(
            [
                nameof(FeatureMustBeActiveValidator),
                nameof(CurrentFeaturePlanMustExistValidator)
            ],
            definition.Events[nameof(CurrentFeaturePlanUpdatedV1)]
                .PreEventValidators
        );
        Assert.Equal(
            [
                nameof(FeatureMustBeActiveValidator),
                nameof(FeaturePlanMustExistValidator)
            ],
            definition.Events[nameof(CurrentFeaturePlanChangedV1)]
                .PreEventValidators
        );
        Assert.All(
            definition.Events.Values,
            eventDefinition =>
            {
                Assert.Empty(eventDefinition.PostEventValidators);
                Assert.Empty(eventDefinition.UniqueConstraints);
            }
        );
    }

    [Fact]
    public void EventFamilyInterfaces_AreEmptyMarkers()
    {
        var eventFamilies = typeof(IFeatureAdded).Assembly
            .GetTypes()
            .Where(
                type =>
                    type.IsInterface
                    && type.Namespace == typeof(IFeatureAdded).Namespace
            )
            .ToList();

        Assert.NotEmpty(eventFamilies);
        Assert.All(
            eventFamilies,
            eventFamily => Assert.Empty(eventFamily.GetMembers())
        );
    }

    [Fact]
    public async Task ResolvesPlanValidatorsFromYaml()
    {
        RegisterTypesOnce();
        var provider = new EventValidatorProvider(
            CreateDefinitionProvider()
        );
        var payload = CreatePayload(
            new CurrentFeaturePlanChangedV1(
                FeaturePlanId.FromDatabaseGuid(Guid.NewGuid())
            )
        );

        var validators = await provider.GetPreEventStateValidators(payload);

        Assert.Collection(
            validators,
            validator =>
                Assert.IsType<FeatureMustBeActiveValidator>(validator),
            validator =>
                Assert.IsType<FeaturePlanMustExistValidator>(validator)
        );
    }

    private static YamlStateMachineDefinitionProvider
        CreateDefinitionProvider() =>
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
            EventExecutor.FromDatabaseGuid(Guid.NewGuid()),
            AggregateId.FromDatabaseGuid(Guid.NewGuid()),
            "features-state-machine",
            eventData
        );

    private static void RegisterTypesOnce()
    {
        lock (RegistrationLock)
        {
            if (_typesRegistered)
                return;

            StateDataTypeContainer.AddStateDataType(
                typeof(FeatureStateData)
            );
            foreach (var eventType in EventTypes)
                EventTypeContainer.AddEventType(eventType);

            foreach (var validatorType in ValidatorTypes)
                EventValidatorContainer.AddEventValidator(validatorType);

            _typesRegistered = true;
        }
    }

    private static List<Type> EventTypes { get; } =
    [
        typeof(FeatureAddedV1),
        typeof(FeatureRemovedV1),
        typeof(FeatureStatusUpdatedV1),
        typeof(FeatureSkillAddedV1),
        typeof(FeatureSkillRemovedV1),
        typeof(FeatureRecordAddedV1),
        typeof(FeatureRecordUpdatedV1),
        typeof(FeatureRecordRemovedV1),
        typeof(FeaturePlanAddedV1),
        typeof(CurrentFeaturePlanUpdatedV1),
        typeof(CurrentFeaturePlanChangedV1),
        typeof(FeaturePlanRemovedV1)
    ];

    private static List<Type> ValidatorTypes { get; } =
    [
        typeof(FeatureMustBeActiveValidator),
        typeof(FeatureSkillMustNotExistValidator),
        typeof(FeatureSkillMustExistValidator),
        typeof(FeatureRecordMustNotExistValidator),
        typeof(FeatureRecordMustExistValidator),
        typeof(FeaturePlanMustNotExistValidator),
        typeof(FeaturePlanMustExistValidator),
        typeof(CurrentFeaturePlanMustExistValidator)
    ];
}

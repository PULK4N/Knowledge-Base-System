using EventSourcing.Core.Providers;
using EventSourcing.Shared.Containers;
using EventSourcing.Shared.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;
using FeatureModule.Domain.Validators;

namespace FeatureModule.Domain.Tests;

// Covers replay of released plan events that are now obsolete.
#pragma warning disable CS0618

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
            [nameof(FeatureAddedV1), nameof(FeatureAddedV2)],
            definition.InitializationEvents
        );
        Assert.Equal(
            [
                "FeatureSummaryProjector",
                "FeatureSearchProjector",
                "FeatureRecordSearchProjector"
            ],
            definition.Projections
        );
        Assert.Equal(
            [
                nameof(FeatureAddedV1),
                nameof(FeatureAddedV2),
                nameof(FeatureRemovedV1),
                nameof(FeatureRemovedV2),
                nameof(FeatureStatusUpdatedV1),
                nameof(FeatureStatusUpdatedV2),
                nameof(FeatureSummaryUpdatedV1),
                nameof(FeatureSummaryUpdatedV2),
                nameof(FeatureParentSetV1),
                nameof(FeatureSkillAddedV1),
                nameof(FeatureSkillAddedV2),
                nameof(FeatureSkillRemovedV1),
                nameof(FeatureSkillRemovedV2),
                nameof(FeatureRecordAddedV1),
                nameof(FeatureRecordAddedV2),
                nameof(FeatureRecordUpdatedV1),
                nameof(FeatureRecordUpdatedV2),
                nameof(FeatureRecordAddedV3),
                nameof(FeatureRecordUpdatedV3),
                nameof(FeatureRecordRemovedV1),
                nameof(FeatureRecordRemovedV2),
                nameof(FeatureReviewNoteAddedV1),
                nameof(FeatureReviewNoteAddedV2),
                nameof(FeatureReviewNoteUpdatedV1),
                nameof(FeatureReviewNoteUpdatedV2),
                nameof(FeatureReviewNoteRemovedV1),
                nameof(FeatureReviewNoteRemovedV2),
                nameof(FeatureResearchDiscoveryAddedV1),
                nameof(FeatureResearchDiscoveryAddedV2),
                nameof(FeatureResearchDiscoveryAddedV3),
                nameof(FeatureResearchDiscoveryUpdatedV1),
                nameof(FeatureResearchDiscoveryUpdatedV2),
                nameof(FeatureResearchDiscoveryUpdatedV3),
                nameof(FeatureResearchDiscoveryRemovedV1),
                nameof(FeatureResearchDiscoveryRemovedV2),
                nameof(FeaturePlanAddedV1),
                nameof(FeaturePlanAddedV2),
                nameof(CurrentFeaturePlanUpdatedV1),
                nameof(CurrentFeaturePlanUpdatedV2),
                nameof(CurrentFeaturePlanChangedV1),
                nameof(CurrentFeaturePlanChangedV2),
                nameof(FeaturePlanRemovedV1),
                nameof(FeaturePlanRemovedV2),
                nameof(FeaturePlanAddedV3),
                nameof(FeaturePlanUpdatedV1),
                nameof(FeaturePlanStatusChangedV1),
                nameof(FeaturePlanBranchAddedV1),
                nameof(FeaturePlanBranchUpdatedV1),
                nameof(FeaturePlanBranchRemovedV1),
                nameof(FeatureBranchSetV1),
                nameof(FeaturePlanAddedV4),
                nameof(FeaturePlanPreviousPlanSetV1),
                nameof(FeaturePlanRecordsSetV1),
                nameof(FeaturePlanBaseBranchSourcesSetV1),
                nameof(FeatureCompositeBranchSetV1)
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
        Assert.Equal(
            [
                nameof(FeatureMustBeActiveValidator),
                nameof(FeaturePlanMustExistValidator),
                nameof(FeaturePlanRecordsMustExistValidator)
            ],
            definition.Events[nameof(FeaturePlanRecordsSetV1)]
                .PreEventValidators
        );
        Assert.All(
            definition.Events.Values,
            eventDefinition => Assert.Empty(
                eventDefinition.PostEventValidators
            )
        );
    }

    [Fact]
    public void ResolvesFeatureParentValidatorsFromYaml()
    {
        RegisterTypesOnce();
        var provider = new EventValidatorProvider(
            CreateDefinitionProvider()
        );
        var payload = CreatePayload(
            new FeatureParentSetV1(
                AggregateId.FromDatabaseGuid(Guid.NewGuid())
            )
        );

        var validators = provider.GetPreEventStateValidators(payload);

        Assert.Collection(
            validators,
            validator =>
                Assert.IsType<FeatureMustBeActiveValidator>(validator),
            validator =>
                Assert.IsType<FeatureCannotParentItselfValidator>(
                    validator
                )
        );
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Composite_branch_changes_require_an_active_feature(bool isDeleted)
    {
        RegisterTypesOnce();
        var provider = new EventValidatorProvider(CreateDefinitionProvider());
        var payload = CreatePayload(new FeatureCompositeBranchSetV1(
            "feature/composite", Guid.NewGuid(), AggregateId.FromDatabaseGuid(Guid.NewGuid())));
        var validator = Assert.IsType<FeatureMustBeActiveValidator>(
            Assert.Single(provider.GetPreEventStateValidators(payload)));
        var state = new FeatureStateData(payload.EventExecutionInfo.AggregateId) { IsDeleted = isDeleted };

        Assert.Equal(!isDeleted, validator.Validate(state, payload).Succeded);
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
    public void ResolvesPlanValidatorsFromYaml()
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

        var validators = provider.GetPreEventStateValidators(payload);

        Assert.Collection(
            validators,
            validator =>
                Assert.IsType<FeatureMustBeActiveValidator>(validator),
            validator =>
                Assert.IsType<FeaturePlanMustExistValidator>(validator)
        );
    }

    [Fact]
    public void ResolvesResearchDiscoveryValidatorsFromYaml()
    {
        RegisterTypesOnce();
        var provider = new EventValidatorProvider(
            CreateDefinitionProvider()
        );
        var payload = CreatePayload(
            new FeatureResearchDiscoveryUpdatedV2(
                FeatureResearchDiscoveryId.FromDatabaseGuid(Guid.NewGuid()),
                "Discovery title",
                "A research discovery",
                FeatureRecordSourceType.Web,
                "https://example.com"
            )
        );

        var validators = provider.GetPreEventStateValidators(payload);

        Assert.Collection(
            validators,
            validator =>
                Assert.IsType<FeatureMustBeActiveValidator>(validator),
            validator =>
                Assert.IsType<FeatureResearchDiscoveryMustExistValidator>(
                    validator
                )
        );
    }

    [Fact]
    public void ResolvesReviewNoteValidatorsFromYaml()
    {
        RegisterTypesOnce();
        var provider = new EventValidatorProvider(
            CreateDefinitionProvider()
        );
        var payload = CreatePayload(
            new FeatureReviewNoteUpdatedV1(
                FeatureReviewNoteId.FromDatabaseGuid(Guid.NewGuid()),
                "Outbox retries are intentional",
                "The projector retries by design; it is not a defect."
            )
        );

        var validators = provider.GetPreEventStateValidators(payload);

        Assert.Collection(
            validators,
            validator =>
                Assert.IsType<FeatureMustBeActiveValidator>(validator),
            validator =>
                Assert.IsType<FeatureReviewNoteMustExistValidator>(
                    validator
                )
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
        typeof(FeatureAddedV2),
        typeof(FeatureRemovedV1),
        typeof(FeatureRemovedV2),
        typeof(FeatureStatusUpdatedV1),
        typeof(FeatureStatusUpdatedV2),
        typeof(FeatureSummaryUpdatedV1),
        typeof(FeatureSummaryUpdatedV2),
        typeof(FeatureParentSetV1),
        typeof(FeatureSkillAddedV1),
        typeof(FeatureSkillAddedV2),
        typeof(FeatureSkillRemovedV1),
        typeof(FeatureSkillRemovedV2),
        typeof(FeatureRecordAddedV1),
        typeof(FeatureRecordAddedV2),
        typeof(FeatureRecordUpdatedV1),
        typeof(FeatureRecordUpdatedV2),
        typeof(FeatureRecordRemovedV1),
        typeof(FeatureRecordRemovedV2),
        typeof(FeatureReviewNoteAddedV1),
        typeof(FeatureReviewNoteAddedV2),
        typeof(FeatureReviewNoteUpdatedV1),
        typeof(FeatureReviewNoteUpdatedV2),
        typeof(FeatureReviewNoteRemovedV1),
        typeof(FeatureReviewNoteRemovedV2),
        typeof(FeatureResearchDiscoveryAddedV1),
        typeof(FeatureResearchDiscoveryAddedV2),
        typeof(FeatureResearchDiscoveryAddedV3),
        typeof(FeatureResearchDiscoveryUpdatedV1),
        typeof(FeatureResearchDiscoveryUpdatedV2),
        typeof(FeatureResearchDiscoveryUpdatedV3),
        typeof(FeatureResearchDiscoveryRemovedV1),
        typeof(FeatureResearchDiscoveryRemovedV2),
        typeof(FeaturePlanAddedV1),
        typeof(FeaturePlanAddedV2),
        typeof(CurrentFeaturePlanUpdatedV1),
        typeof(CurrentFeaturePlanUpdatedV2),
        typeof(CurrentFeaturePlanChangedV1),
        typeof(CurrentFeaturePlanChangedV2),
        typeof(FeaturePlanRemovedV1),
        typeof(FeaturePlanRemovedV2),
        typeof(FeaturePlanAddedV3),
        typeof(FeaturePlanUpdatedV1),
        typeof(FeaturePlanStatusChangedV1),
        typeof(FeaturePlanBranchAddedV1),
        typeof(FeaturePlanBranchUpdatedV1),
        typeof(FeaturePlanBranchRemovedV1),
        typeof(FeatureBranchSetV1),
        typeof(FeaturePlanAddedV4),
        typeof(FeaturePlanPreviousPlanSetV1),
        typeof(FeatureRecordAddedV3),
        typeof(FeatureRecordUpdatedV3),
        typeof(FeaturePlanRecordsSetV1),
        typeof(FeaturePlanBaseBranchSourcesSetV1),
        typeof(FeatureCompositeBranchSetV1)
    ];

    private static List<Type> ValidatorTypes { get; } =
    [
        typeof(FeatureMustBeActiveValidator),
        typeof(FeatureCannotParentItselfValidator),
        typeof(FeatureSkillMustNotExistValidator),
        typeof(FeatureSkillMustExistValidator),
        typeof(FeatureRecordMustNotExistValidator),
        typeof(FeatureRecordMustExistValidator),
        typeof(FeatureReviewNoteMustNotExistValidator),
        typeof(FeatureReviewNoteMustExistValidator),
        typeof(FeatureResearchDiscoveryMustNotExistValidator),
        typeof(FeatureResearchDiscoveryMustExistValidator),
        typeof(FeaturePlanMustNotExistValidator),
        typeof(FeaturePlanMustExistValidator),
        typeof(CurrentFeaturePlanMustExistValidator),
        typeof(FeaturePlanBranchParentValidator),
        typeof(FeaturePlanBranchNameMustBeAvailableValidator),
        typeof(FeaturePlanBranchMustExistValidator),
        typeof(FeaturePlanLastBaseBranchCannotBeRemovedValidator),
        typeof(FeaturePreviousPlanValidator),
        typeof(FeaturePlanRecordsMustExistValidator),
        typeof(FeaturePlanBranchSourcesValidator)
    ];
}

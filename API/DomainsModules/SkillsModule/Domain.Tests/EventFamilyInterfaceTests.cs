using System.Reflection;
using System.Runtime.CompilerServices;
using SkillsModule.Domain.Events;

namespace SkillsModule.Domain.Tests;

public sealed class EventFamilyInterfaceTests
{
    private static readonly Type[] ConcreteEventVersions =
    [
        typeof(SkillCreatedV1),
        typeof(SkillCreatedV2),
        typeof(SkillUpdatedV1),
        typeof(SkillDetailsUpdatedV1),
        typeof(SkillDeletedV1),
        typeof(SkillReferenceAddedV1),
        typeof(SkillReferenceAddedV2),
        typeof(SkillReferenceUpdatedV1),
        typeof(SkillReferenceUpdatedV2),
        typeof(SkillReferenceDeletedV1),
        typeof(SkillAttachmentAddedV1),
        typeof(SkillAttachmentDeletedV1)
    ];

    [Fact]
    public void EventFamilyInterfaces_AreEmptyMarkers()
    {
        Type[] eventFamilies =
        [
            typeof(ISkillCreated),
            typeof(ISkillUpdated),
            typeof(ISkillDetailsUpdated),
            typeof(ISkillDeleted),
            typeof(ISkillReferenceAdded),
            typeof(ISkillReferenceUpdated),
            typeof(ISkillReferenceDeleted),
            typeof(ISkillAttachmentAdded),
            typeof(ISkillAttachmentDeleted)
        ];

        foreach (var eventFamily in eventFamilies)
        {
            var declaredMembers = eventFamily.GetMembers(
                BindingFlags.Public
                    | BindingFlags.Instance
                    | BindingFlags.DeclaredOnly
            );

            Assert.Empty(declaredMembers);
        }
    }

    [Fact]
    public void ConcreteEventVersions_AreReadonlyAndConstructorBacked()
    {
        foreach (var eventType in ConcreteEventVersions)
        {
            Assert.True(eventType.IsValueType);
            Assert.True(
                eventType.IsDefined(
                    typeof(IsReadOnlyAttribute),
                    inherit: false
                )
            );

            var propertyNames = eventType
                .GetProperties(
                    BindingFlags.Public
                        | BindingFlags.Instance
                        | BindingFlags.DeclaredOnly
                )
                .Select(property => property.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var constructors = eventType.GetConstructors();

            if (propertyNames.Count == 0)
            {
                Assert.DoesNotContain(
                    constructors,
                    constructor => constructor.GetParameters().Length > 0
                );
                continue;
            }

            var constructorParameterNames = constructors
                .OrderByDescending(
                    constructor => constructor.GetParameters().Length
                )
                .First()
                .GetParameters()
                .Select(parameter => parameter.Name!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            Assert.Equal(propertyNames, constructorParameterNames);
        }
    }
}

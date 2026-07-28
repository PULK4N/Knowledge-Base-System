using System.Reflection;
using SkillsModule.Domain.Events;

namespace SkillsModule.Domain.Tests;

public sealed class EventFamilyInterfaceTests
{
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
}

namespace MemoryModule.Application.Tests;

/// <summary>
/// Test classes that register event types in their static constructors share
/// this collection, so they run one at a time. EventTypeContainer keeps the
/// types in a static dictionary that is not safe for parallel writes.
/// </summary>
[CollectionDefinition(Name)]
public sealed class EventTypeRegistrationCollection
{
    public const string Name = "Event type registration";
}

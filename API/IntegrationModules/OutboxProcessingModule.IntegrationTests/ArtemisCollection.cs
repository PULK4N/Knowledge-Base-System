namespace OutboxProcessingModule.IntegrationTests;

/// <summary>
/// Every test here drains and reads the same broker queues, so they must not
/// run at the same time.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ArtemisCollection
{
    public const string Name = "Artemis";
}

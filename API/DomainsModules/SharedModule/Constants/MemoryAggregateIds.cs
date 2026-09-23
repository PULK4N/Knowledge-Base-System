namespace SharedModule.Constants;

public static class MemoryAggregateIds
{
    /// <summary>Identifies user-originated changes that have no chat memory stream.</summary>
    public static Guid User => StateDataAggregateIds.UserActionMemory;
}

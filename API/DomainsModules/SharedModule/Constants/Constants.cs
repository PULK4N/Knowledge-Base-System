namespace SharedModule.Constants;

public static class StateDataAggregateIds
{
    public static Guid UserActionMemory =>
        Guid.Parse("10000000-0000-0000-0000-000000000000");
    public static Guid SessionAggregateMap =>
        Guid.Parse("00000000-0000-0000-0000-000000000001");
    public static Guid GeneralPolicies =>
        Guid.Parse("00000000-0000-0000-0000-000000000002");
    public static Guid RepositoryToProjectMap =>
        Guid.Parse("00000000-0000-0000-0000-000000000003");
}

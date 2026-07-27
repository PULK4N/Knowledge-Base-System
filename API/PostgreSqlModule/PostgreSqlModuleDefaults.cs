namespace PostgreSqlModule;

public static class PostgreSqlModuleDefaults
{
    public const string ConnectionStringName = "ApplicationDatabase";
    public const string DesignTimeConnectionStringEnvironmentVariable =
        "SKILL_MEMORY_POSTGRES_CONNECTION_STRING";
    public const string LocalDevelopmentConnectionString =
        "Host=localhost;Port=5432;Database=skill_memory;Username=skill_memory;Password=skill_memory";
    public const string EventSourcingMigrationsHistoryTable =
        "__EFMigrationsHistory_EventSourcing";
    public const string SkillsMigrationsHistoryTable =
        "__EFMigrationsHistory_Skills";
}

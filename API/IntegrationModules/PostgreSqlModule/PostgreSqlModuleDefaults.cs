namespace PostgreSqlModule;

public static class PostgreSqlModuleDefaults
{
    public const string ConnectionStringName = "ApplicationDatabase";
    public const string DesignTimeConnectionStringEnvironmentVariable =
        "KNOWLEDGE_BASE_POSTGRES_CONNECTION_STRING";
    public const string LocalDevelopmentConnectionString =
        "Host=localhost;Port=5432;Database=skill_memory;Username=skill_memory;Password=skill_memory";
    public const string EventSourcingMigrationsHistoryTable =
        "__EFMigrationsHistory_EventSourcing";

    /// <summary>
    /// Attachment content started out owned by the skills module. The history
    /// table keeps its original name so databases that already applied the
    /// first attachment migration do not run it again.
    /// </summary>
    public const string AttachmentsMigrationsHistoryTable =
        "__EFMigrationsHistory_Skills";
}

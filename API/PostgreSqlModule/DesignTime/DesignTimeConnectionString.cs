namespace PostgreSqlModule.DesignTime;

internal static class DesignTimeConnectionString
{
    public static string Get() =>
        Environment.GetEnvironmentVariable(
            PostgreSqlModuleDefaults
                .DesignTimeConnectionStringEnvironmentVariable
        )
        ?? PostgreSqlModuleDefaults.LocalDevelopmentConnectionString;
}

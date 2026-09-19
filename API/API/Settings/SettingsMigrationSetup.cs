using Microsoft.EntityFrameworkCore;

namespace Api.Settings;

public static class SettingsMigrationSetup
{
    public static async Task ApplySettingsMigration(
        this IServiceProvider services
    )
    {
        using var scope = services.CreateScope();

        await scope
            .ServiceProvider
            .GetRequiredService<SettingsDbContext>()
            .Database
            .MigrateAsync();
    }
}

using Microsoft.EntityFrameworkCore;

namespace Api.Settings;

public sealed class SettingsRepository(SettingsDbContext dbContext)
{
    public async Task<Settings> Update(
        Theme theme,
        CancellationToken cancellationToken = default
    )
    {
        var settings = await dbContext.Settings.SingleOrDefaultAsync(
            cancellationToken
        );

        if (settings is null)
        {
            throw new KeyNotFoundException("Settings have not been added.");
        }

        settings.Theme = theme;
        await dbContext.SaveChangesAsync(cancellationToken);

        return settings;
    }
}

using Microsoft.EntityFrameworkCore;

namespace Api.Settings;

public sealed class SettingsDbContext(
    DbContextOptions<SettingsDbContext> options
) : DbContext(options)
{
    public const string MigrationsHistoryTable =
        "__EFMigrationsHistory_Settings";

    public DbSet<Settings> Settings => Set<Settings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Settings>(settings =>
        {
            settings.ToTable(
                "Settings",
                table => table.HasCheckConstraint(
                    "CK_Settings_Singleton",
                    "\"Id\" = 1"
                )
            );
            settings.HasKey(value => value.Id);
            settings.Property(value => value.Id).ValueGeneratedNever();
            settings.Property(value => value.Theme).IsRequired();
        });
    }
}

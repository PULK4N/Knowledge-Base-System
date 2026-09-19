using Api.Settings;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace API.Migrations.Settings;

[DbContext(typeof(SettingsDbContext))]
[Migration("20260919000000_CreateSettings")]
public partial class CreateSettings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Settings",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false),
                Theme = table.Column<int>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Settings", x => x.Id);
                table.CheckConstraint("CK_Settings_Singleton", "\"Id\" = 1");
            }
        );

        migrationBuilder.Sql(
            "INSERT INTO \"Settings\" (\"Id\", \"Theme\") VALUES (1, 0);"
        );
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Settings");
    }
}

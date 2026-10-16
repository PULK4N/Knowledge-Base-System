using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PostgreSqlModule.Migrations.Attachments
{
    /// <inheritdoc />
    public partial class AddAttachmentContentMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "AttachmentContents",
                type: "text",
                nullable: false,
                defaultValue: "application/octet-stream");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "AttachmentContents",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "AttachmentContents");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "AttachmentContents");
        }
    }
}

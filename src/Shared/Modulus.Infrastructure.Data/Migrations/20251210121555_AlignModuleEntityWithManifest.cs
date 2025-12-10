using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modulus.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AlignModuleEntityWithManifest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Modules",
                newName: "DisplayName");

            migrationBuilder.RenameColumn(
                name: "Author",
                table: "Modules",
                newName: "Tags");

            migrationBuilder.AddColumn<string>(
                name: "Dependencies",
                table: "Modules",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBundled",
                table: "Modules",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "Modules",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Publisher",
                table: "Modules",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupportedHosts",
                table: "Modules",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Dependencies",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "IsBundled",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "Language",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "Publisher",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "SupportedHosts",
                table: "Modules");

            migrationBuilder.RenameColumn(
                name: "Tags",
                table: "Modules",
                newName: "Author");

            migrationBuilder.RenameColumn(
                name: "DisplayName",
                table: "Modules",
                newName: "Name");
        }
    }
}

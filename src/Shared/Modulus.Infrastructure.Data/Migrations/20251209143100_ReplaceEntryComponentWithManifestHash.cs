using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modulus.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceEntryComponentWithManifestHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EntryComponent",
                table: "Modules",
                newName: "ValidatedAt");

            migrationBuilder.AddColumn<string>(
                name: "ManifestHash",
                table: "Modules",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ManifestHash",
                table: "Modules");

            migrationBuilder.RenameColumn(
                name: "ValidatedAt",
                table: "Modules",
                newName: "EntryComponent");
        }
    }
}

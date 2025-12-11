using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modulus.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingCleanups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PendingCleanups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DirectoryPath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    ModuleId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RetryCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LastAttemptAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingCleanups", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PendingCleanups_DirectoryPath",
                table: "PendingCleanups",
                column: "DirectoryPath",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PendingCleanups_ModuleId",
                table: "PendingCleanups",
                column: "ModuleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PendingCleanups");
        }
    }
}

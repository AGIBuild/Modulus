using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modulus.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixNonSystemMenuLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Force non-system modules to Main menu
            migrationBuilder.Sql(@"
UPDATE Modules
SET MenuLocation = 0
WHERE IsSystem = 0;

UPDATE Menus
SET Location = 0
WHERE ModuleId IN (SELECT Id FROM Modules WHERE IsSystem = 0);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: cannot restore previous location safely
        }
    }
}

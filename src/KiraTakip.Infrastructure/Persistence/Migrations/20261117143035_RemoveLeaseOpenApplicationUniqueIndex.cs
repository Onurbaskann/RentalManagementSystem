using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLeaseOpenApplicationUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Sozlesmeler_BirimId_AcikBasvuru",
                table: "Sozlesmeler");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "UX_Sozlesmeler_BirimId_AcikBasvuru",
                table: "Sozlesmeler",
                column: "BirimId",
                unique: true,
                filter: "[IsDeleted] = 0 AND [Durum] IN (4, 5)");
        }
    }
}

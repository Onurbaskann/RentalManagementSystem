using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTenantUniqueIndexesForSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Kiracilar_KiraciNo",
                table: "Kiracilar");

            migrationBuilder.DropIndex(
                name: "UX_Kiraciler_VergiNo",
                table: "Kiracilar");

            migrationBuilder.CreateIndex(
                name: "UX_Kiraciler_KiraciNo_Silinmemis",
                table: "Kiracilar",
                column: "KiraciNo",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_Kiraciler_VergiNo",
                table: "Kiracilar",
                column: "VergiNo",
                unique: true,
                filter: "[VergiNo] IS NOT NULL AND [VergiNo] <> '' AND [IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Kiraciler_KiraciNo_Silinmemis",
                table: "Kiracilar");

            migrationBuilder.DropIndex(
                name: "UX_Kiraciler_VergiNo",
                table: "Kiracilar");

            migrationBuilder.CreateIndex(
                name: "IX_Kiracilar_KiraciNo",
                table: "Kiracilar",
                column: "KiraciNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Kiraciler_VergiNo",
                table: "Kiracilar",
                column: "VergiNo",
                unique: true,
                filter: "[VergiNo] IS NOT NULL AND [VergiNo] <> ''");
        }
    }
}

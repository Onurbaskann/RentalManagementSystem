using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class BirimRate_AddKiraciKategori : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Mevcut veriler dummy — temizle
            migrationBuilder.Sql("DELETE FROM BirimRateler");

            // Eski unique index kaldır
            migrationBuilder.DropIndex(
                name: "IX_BirimRateler_BirimId_BorcTipiId",
                table: "BirimRateler");

            // KiraciKategoriId kolonu ekle
            migrationBuilder.AddColumn<int>(
                name: "KiraciKategoriId",
                table: "BirimRateler",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // FK ekle
            migrationBuilder.AddForeignKey(
                name: "FK_BirimRateler_KiraciKategorileri_KiraciKategoriId",
                table: "BirimRateler",
                column: "KiraciKategoriId",
                principalTable: "KiraciKategorileri",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Yeni unique index
            migrationBuilder.CreateIndex(
                name: "IX_BirimRateler_BirimId_KiraciKategoriId_BorcTipiId",
                table: "BirimRateler",
                columns: new[] { "BirimId", "KiraciKategoriId", "BorcTipiId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BirimRateler_KiraciKategorileri_KiraciKategoriId",
                table: "BirimRateler");

            migrationBuilder.DropIndex(
                name: "IX_BirimRateler_BirimId_KiraciKategoriId_BorcTipiId",
                table: "BirimRateler");

            migrationBuilder.DropColumn(
                name: "KiraciKategoriId",
                table: "BirimRateler");

            migrationBuilder.CreateIndex(
                name: "IX_BirimRateler_BirimId_BorcTipiId",
                table: "BirimRateler",
                columns: new[] { "BirimId", "BorcTipiId" },
                unique: true);
        }
    }
}

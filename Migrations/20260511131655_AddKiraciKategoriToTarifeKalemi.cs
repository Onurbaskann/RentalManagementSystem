using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class AddKiraciKategoriToTarifeKalemi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Mevcut veriler dummy — migration öncesi temizle
            migrationBuilder.Sql("DELETE FROM TarifeKalemleri");
            migrationBuilder.Sql("DELETE FROM Tarifeler");

            // Eski unique index kaldır
            migrationBuilder.DropIndex(
                name: "IX_TarifeKalemleri_TarifeId_BorcTipiId",
                table: "TarifeKalemleri");

            // KiraciKategoriId kolonu ekle (NOT NULL)
            migrationBuilder.AddColumn<int>(
                name: "KiraciKategoriId",
                table: "TarifeKalemleri",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // FK ekle
            migrationBuilder.AddForeignKey(
                name: "FK_TarifeKalemleri_KiraciKategorileri_KiraciKategoriId",
                table: "TarifeKalemleri",
                column: "KiraciKategoriId",
                principalTable: "KiraciKategorileri",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Yeni unique index (TarifeId, KiraciKategoriId, BorcTipiId)
            migrationBuilder.CreateIndex(
                name: "IX_TarifeKalemleri_TarifeId_KiraciKategoriId_BorcTipiId",
                table: "TarifeKalemleri",
                columns: new[] { "TarifeId", "KiraciKategoriId", "BorcTipiId" },
                unique: true);

            // KiraciKategoriId için ayrı index (FK navigasyonu için)
            migrationBuilder.CreateIndex(
                name: "IX_TarifeKalemleri_KiraciKategoriId",
                table: "TarifeKalemleri",
                column: "KiraciKategoriId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TarifeKalemleri_KiraciKategorileri_KiraciKategoriId",
                table: "TarifeKalemleri");

            migrationBuilder.DropIndex(
                name: "IX_TarifeKalemleri_TarifeId_KiraciKategoriId_BorcTipiId",
                table: "TarifeKalemleri");

            migrationBuilder.DropIndex(
                name: "IX_TarifeKalemleri_KiraciKategoriId",
                table: "TarifeKalemleri");

            migrationBuilder.DropColumn(
                name: "KiraciKategoriId",
                table: "TarifeKalemleri");

            migrationBuilder.CreateIndex(
                name: "IX_TarifeKalemleri_TarifeId_BorcTipiId",
                table: "TarifeKalemleri",
                columns: new[] { "TarifeId", "BorcTipiId" },
                unique: true);
        }
    }
}

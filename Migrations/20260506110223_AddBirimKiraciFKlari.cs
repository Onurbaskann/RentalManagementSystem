using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class AddBirimKiraciFKlari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "KiraciKategoriId",
                table: "Kiraciler",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SektorId",
                table: "Kiraciler",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BirimTuruId",
                table: "Birimler",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Kiraciler_KiraciKategoriId",
                table: "Kiraciler",
                column: "KiraciKategoriId");

            migrationBuilder.CreateIndex(
                name: "IX_Kiraciler_SektorId",
                table: "Kiraciler",
                column: "SektorId");

            migrationBuilder.CreateIndex(
                name: "IX_Birimler_BirimTuruId",
                table: "Birimler",
                column: "BirimTuruId");

            migrationBuilder.AddForeignKey(
                name: "FK_Birimler_BirimTurleri_BirimTuruId",
                table: "Birimler",
                column: "BirimTuruId",
                principalTable: "BirimTurleri",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Kiraciler_KiraciKategorileri_KiraciKategoriId",
                table: "Kiraciler",
                column: "KiraciKategoriId",
                principalTable: "KiraciKategorileri",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Kiraciler_Sektorler_SektorId",
                table: "Kiraciler",
                column: "SektorId",
                principalTable: "Sektorler",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Birimler_BirimTurleri_BirimTuruId",
                table: "Birimler");

            migrationBuilder.DropForeignKey(
                name: "FK_Kiraciler_KiraciKategorileri_KiraciKategoriId",
                table: "Kiraciler");

            migrationBuilder.DropForeignKey(
                name: "FK_Kiraciler_Sektorler_SektorId",
                table: "Kiraciler");

            migrationBuilder.DropIndex(
                name: "IX_Kiraciler_KiraciKategoriId",
                table: "Kiraciler");

            migrationBuilder.DropIndex(
                name: "IX_Kiraciler_SektorId",
                table: "Kiraciler");

            migrationBuilder.DropIndex(
                name: "IX_Birimler_BirimTuruId",
                table: "Birimler");

            migrationBuilder.DropColumn(
                name: "KiraciKategoriId",
                table: "Kiraciler");

            migrationBuilder.DropColumn(
                name: "SektorId",
                table: "Kiraciler");

            migrationBuilder.DropColumn(
                name: "BirimTuruId",
                table: "Birimler");
        }
    }
}

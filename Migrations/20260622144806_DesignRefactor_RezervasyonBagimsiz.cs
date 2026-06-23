using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class DesignRefactor_RezervasyonBagimsiz : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rezervasyonlari_Sozlesmeler_KiraSozlesmesiId",
                table: "Rezervasyonlari");

            migrationBuilder.DropIndex(
                name: "IX_Rezervasyonlari_KiraSozlesmesiId",
                table: "Rezervasyonlari");

            migrationBuilder.DropColumn(
                name: "KiraSozlesmesiId",
                table: "Rezervasyonlari");

            migrationBuilder.AddColumn<decimal>(
                name: "EslesenTutar",
                table: "OdemeBankaEslesmeleri",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "KiraciId",
                table: "KiraTahakkuklar",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("UPDATE kt SET kt.KiraciId = COALESCE(s.KiraciId, (SELECT TOP 1 Id FROM Kiraciler)) FROM KiraTahakkuklar kt LEFT JOIN Sozlesmeler s ON kt.KiraSozlesmesiId = s.Id");

            migrationBuilder.CreateIndex(
                name: "IX_KiraTahakkuklar_KiraciId",
                table: "KiraTahakkuklar",
                column: "KiraciId");

            migrationBuilder.AddForeignKey(
                name: "FK_KiraTahakkuklar_Kiraciler_KiraciId",
                table: "KiraTahakkuklar",
                column: "KiraciId",
                principalTable: "Kiraciler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_KiraTahakkuklar_Kiraciler_KiraciId",
                table: "KiraTahakkuklar");

            migrationBuilder.DropIndex(
                name: "IX_KiraTahakkuklar_KiraciId",
                table: "KiraTahakkuklar");

            migrationBuilder.DropColumn(
                name: "EslesenTutar",
                table: "OdemeBankaEslesmeleri");

            migrationBuilder.DropColumn(
                name: "KiraciId",
                table: "KiraTahakkuklar");

            migrationBuilder.AddColumn<int>(
                name: "KiraSozlesmesiId",
                table: "Rezervasyonlari",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rezervasyonlari_KiraSozlesmesiId",
                table: "Rezervasyonlari",
                column: "KiraSozlesmesiId");

            migrationBuilder.AddForeignKey(
                name: "FK_Rezervasyonlari_Sozlesmeler_KiraSozlesmesiId",
                table: "Rezervasyonlari",
                column: "KiraSozlesmesiId",
                principalTable: "Sozlesmeler",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}

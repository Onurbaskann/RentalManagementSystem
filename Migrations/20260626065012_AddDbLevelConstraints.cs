using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class AddDbLevelConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tahakkuklar_KiraSozlesmesiId_DonemBaslangic",
                table: "Tahakkuklar");

            migrationBuilder.DropIndex(
                name: "IX_Sozlesmeler_BirimId",
                table: "Sozlesmeler");

            migrationBuilder.DropIndex(
                name: "IX_OdemeBankaEslesmeleri_BankaHareketiId",
                table: "OdemeBankaEslesmeleri");

            migrationBuilder.DropIndex(
                name: "IX_OdemeBankaEslesmeleri_TahakkukOdemeId",
                table: "OdemeBankaEslesmeleri");

            migrationBuilder.DropColumn(
                name: "Aktif",
                table: "RezervasyonTarifeler");

            migrationBuilder.DropColumn(
                name: "OlusturmaTarihi",
                table: "RezervasyonTarifeler");

            migrationBuilder.CreateIndex(
                name: "IX_Tahakkuklar_KiraSozlesmesiId_DonemBaslangic",
                table: "Tahakkuklar",
                columns: new[] { "KiraSozlesmesiId", "DonemBaslangic" },
                unique: true,
                filter: "[KiraSozlesmesiId] IS NOT NULL AND [KaynakTipi] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Sozlesmeler_BirimId",
                table: "Sozlesmeler",
                column: "BirimId",
                unique: true,
                filter: "[Durum] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_OdemeBankaEslesmeleri_BankaHareketiId",
                table: "OdemeBankaEslesmeleri",
                column: "BankaHareketiId",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_OdemeBankaEslesmeleri_TahakkukOdemeId",
                table: "OdemeBankaEslesmeleri",
                column: "TahakkukOdemeId",
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tahakkuklar_KiraSozlesmesiId_DonemBaslangic",
                table: "Tahakkuklar");

            migrationBuilder.DropIndex(
                name: "IX_Sozlesmeler_BirimId",
                table: "Sozlesmeler");

            migrationBuilder.DropIndex(
                name: "IX_OdemeBankaEslesmeleri_BankaHareketiId",
                table: "OdemeBankaEslesmeleri");

            migrationBuilder.DropIndex(
                name: "IX_OdemeBankaEslesmeleri_TahakkukOdemeId",
                table: "OdemeBankaEslesmeleri");

            migrationBuilder.AddColumn<bool>(
                name: "Aktif",
                table: "RezervasyonTarifeler",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "OlusturmaTarihi",
                table: "RezervasyonTarifeler",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_Tahakkuklar_KiraSozlesmesiId_DonemBaslangic",
                table: "Tahakkuklar",
                columns: new[] { "KiraSozlesmesiId", "DonemBaslangic" });

            migrationBuilder.CreateIndex(
                name: "IX_Sozlesmeler_BirimId",
                table: "Sozlesmeler",
                column: "BirimId");

            migrationBuilder.CreateIndex(
                name: "IX_OdemeBankaEslesmeleri_BankaHareketiId",
                table: "OdemeBankaEslesmeleri",
                column: "BankaHareketiId");

            migrationBuilder.CreateIndex(
                name: "IX_OdemeBankaEslesmeleri_TahakkukOdemeId",
                table: "OdemeBankaEslesmeleri",
                column: "TahakkukOdemeId");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class TahakkukBirimMerkezli : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BirimId",
                table: "Tahakkuklar",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RezervasyonId",
                table: "Tahakkuklar",
                type: "int",
                nullable: true);

            // Backfill: Sözleşme tahakkukları için BirimId'yi sözleşmeden al
            migrationBuilder.Sql(@"
                UPDATE t SET t.BirimId = s.BirimId
                FROM Tahakkuklar t
                INNER JOIN Sozlesmeler s ON s.Id = t.KiraSozlesmesiId
                WHERE t.KiraSozlesmesiId IS NOT NULL;
            ");

            // Backfill: Rezervasyon tahakkukları için RezervasyonId ve BirimId'yi güncelle
            migrationBuilder.Sql(@"
                UPDATE t SET t.RezervasyonId = r.Id, t.BirimId = r.BirimId
                FROM Tahakkuklar t
                INNER JOIN Rezervasyonlari r ON r.TahakkukId = t.Id;
            ");

            // Backfill: BirimId hâlâ 0 olan manuel borçlar için ilk aktif birimi ata
            migrationBuilder.Sql(@"
                UPDATE t SET t.BirimId = (SELECT TOP 1 Id FROM Birimler WHERE IsDeleted = 0)
                FROM Tahakkuklar t
                WHERE t.BirimId = 0;
            ");

            migrationBuilder.DropForeignKey(
                name: "FK_Rezervasyonlari_Tahakkuklar_TahakkukId",
                table: "Rezervasyonlari");

            migrationBuilder.DropIndex(
                name: "IX_Rezervasyonlari_TahakkukId",
                table: "Rezervasyonlari");

            migrationBuilder.DropColumn(
                name: "TahakkukId",
                table: "Rezervasyonlari");

            migrationBuilder.AddColumn<string>(
                name: "BirimIds",
                table: "Davetiyeler",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "HedefEntite",
                table: "BelgeTurleri",
                type: "int",
                nullable: false,
                comment: "Kiraci=1, Odeme=2, Sozlesme=3, Sablon=99",
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "Kiraci=1, Odeme=2, Sablon=99");

            migrationBuilder.AlterColumn<int>(
                name: "OwnerType",
                table: "Belgeler",
                type: "int",
                nullable: false,
                comment: "Kiraci=1, Odeme=2, Sozlesme=3, Sablon=99",
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "Kiraci=1, Odeme=2, Sablon=99");

            migrationBuilder.CreateIndex(
                name: "IX_Tahakkuklar_BirimId_Active",
                table: "Tahakkuklar",
                column: "BirimId",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_Tahakkuklar_RezervasyonId_TekTahakkuk",
                table: "Tahakkuklar",
                column: "RezervasyonId",
                unique: true,
                filter: "[RezervasyonId] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_Tahakkuklar_Birimler_BirimId",
                table: "Tahakkuklar",
                column: "BirimId",
                principalTable: "Birimler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tahakkuklar_Rezervasyonlari_RezervasyonId",
                table: "Tahakkuklar",
                column: "RezervasyonId",
                principalTable: "Rezervasyonlari",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tahakkuklar_Birimler_BirimId",
                table: "Tahakkuklar");

            migrationBuilder.DropForeignKey(
                name: "FK_Tahakkuklar_Rezervasyonlari_RezervasyonId",
                table: "Tahakkuklar");

            migrationBuilder.DropIndex(
                name: "IX_Tahakkuklar_BirimId_Active",
                table: "Tahakkuklar");

            migrationBuilder.DropIndex(
                name: "UX_Tahakkuklar_RezervasyonId_TekTahakkuk",
                table: "Tahakkuklar");

            migrationBuilder.DropColumn(
                name: "BirimId",
                table: "Tahakkuklar");

            migrationBuilder.DropColumn(
                name: "RezervasyonId",
                table: "Tahakkuklar");

            migrationBuilder.DropColumn(
                name: "BirimIds",
                table: "Davetiyeler");

            migrationBuilder.AddColumn<int>(
                name: "TahakkukId",
                table: "Rezervasyonlari",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "HedefEntite",
                table: "BelgeTurleri",
                type: "int",
                nullable: false,
                comment: "Kiraci=1, Odeme=2, Sablon=99",
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "Kiraci=1, Odeme=2, Sozlesme=3, Sablon=99");

            migrationBuilder.AlterColumn<int>(
                name: "OwnerType",
                table: "Belgeler",
                type: "int",
                nullable: false,
                comment: "Kiraci=1, Odeme=2, Sablon=99",
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "Kiraci=1, Odeme=2, Sozlesme=3, Sablon=99");

            migrationBuilder.CreateIndex(
                name: "IX_Rezervasyonlari_TahakkukId",
                table: "Rezervasyonlari",
                column: "TahakkukId");

            migrationBuilder.AddForeignKey(
                name: "FK_Rezervasyonlari_Tahakkuklar_TahakkukId",
                table: "Rezervasyonlari",
                column: "TahakkukId",
                principalTable: "Tahakkuklar",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}

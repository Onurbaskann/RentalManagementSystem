using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraTakip.Migrations
{
    /// <inheritdoc />
    public partial class ExpandReservationLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Rezervasyonlar_BirimId_BaslangicTarihi",
                table: "Rezervasyonlar");

            migrationBuilder.AlterColumn<int>(
                name: "Durum",
                table: "Rezervasyonlar",
                type: "int",
                nullable: false,
                comment: "Confirmed=1, Completed=2, Cancelled=3, PendingApproval=5, Rejected=6",
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "Planned=1, Completed=2, Cancelled=3, TransferredToCharge=4");

            migrationBuilder.AddColumn<string>(
                name: "Baslik",
                table: "Rezervasyonlar",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IcNotlar",
                table: "Rezervasyonlar",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IptalEdenKullaniciId",
                table: "Rezervasyonlar",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IptalNedeni",
                table: "Rezervasyonlar",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "IptalTarihi",
                table: "Rezervasyonlar",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notlar",
                table: "Rezervasyonlar",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OnayTarihi",
                table: "Rezervasyonlar",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OnaylayanKullaniciId",
                table: "Rezervasyonlar",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReddedenKullaniciId",
                table: "Rezervasyonlar",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RetNedeni",
                table: "Rezervasyonlar",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RetTarihi",
                table: "Rezervasyonlar",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Rezervasyonlar",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "TalepEdenAdSoyad",
                table: "Rezervasyonlar",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TalepEdenEposta",
                table: "Rezervasyonlar",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TalepEdenKullaniciId",
                table: "Rezervasyonlar",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TamamlanmaTarihi",
                table: "Rezervasyonlar",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT 1
                    FROM Rezervasyonlar r
                    WHERE r.IsDeleted = 0
                      AND r.Durum = 4
                      AND NOT EXISTS (
                          SELECT 1
                          FROM Tahakkuklar t
                          WHERE t.IsDeleted = 0
                            AND t.RezervasyonId = r.Id))
                    THROW 51000, 'Tahakkuksuz TransferredToCharge rezervasyonu bulundu. Migration durduruldu.', 1;

                IF EXISTS (
                    SELECT 1
                    FROM Tahakkuklar t
                    WHERE t.IsDeleted = 0
                      AND t.RezervasyonId IS NOT NULL
                    GROUP BY t.RezervasyonId
                    HAVING COUNT(*) > 1)
                    THROW 51001, 'Ayni rezervasyona bagli birden fazla tahakkuk bulundu. Migration durduruldu.', 1;

                IF EXISTS (
                    SELECT 1
                    FROM Rezervasyonlar r
                    WHERE r.IsDeleted = 0
                      AND r.BitisTarihi <= r.BaslangicTarihi)
                    THROW 51002, 'Gecersiz tarih araligina sahip rezervasyon bulundu. Migration durduruldu.', 1;

                IF EXISTS (
                    SELECT 1
                    FROM Rezervasyonlar a
                    INNER JOIN Rezervasyonlar b
                        ON a.Id < b.Id
                       AND a.BirimId = b.BirimId
                       AND a.BaslangicTarihi < b.BitisTarihi
                       AND a.BitisTarihi > b.BaslangicTarihi
                    WHERE a.IsDeleted = 0
                      AND b.IsDeleted = 0
                      AND a.Durum IN (1, 4)
                      AND b.Durum IN (1, 4))
                    THROW 51003, 'Birbiriyle cakisan aktif rezervasyonlar bulundu. Migration durduruldu.', 1;

                UPDATE Rezervasyonlar
                SET IptalNedeni = LTRIM(RTRIM(SUBSTRING(Aciklama, 7, 450)))
                WHERE IsDeleted = 0
                  AND Durum = 3
                  AND Aciklama LIKE N'Iptal:%'
                  AND IptalNedeni IS NULL;

                UPDATE Rezervasyonlar
                SET Durum = CASE WHEN BitisTarihi <= GETDATE() THEN 2 ELSE 1 END
                WHERE IsDeleted = 0
                  AND Durum IN (1, 4);
                """);

            migrationBuilder.CreateTable(
                name: "RezervasyonKatilimcilari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RezervasyonId = table.Column<int>(type: "int", nullable: false),
                    AdSoyad = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EpostaAdresi = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NormalizeEpostaAdresi = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    RezervasyonSahibiMi = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RezervasyonKatilimcilari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RezervasyonKatilimcilari_Rezervasyonlar_RezervasyonId",
                        column: x => x.RezervasyonId,
                        principalTable: "Rezervasyonlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Rezervasyonlar_IptalEdenKullaniciId",
                table: "Rezervasyonlar",
                column: "IptalEdenKullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_Rezervasyonlar_OnaylayanKullaniciId",
                table: "Rezervasyonlar",
                column: "OnaylayanKullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_Rezervasyonlar_ReddedenKullaniciId",
                table: "Rezervasyonlar",
                column: "ReddedenKullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_Rezervasyonlar_TalepEdenKullaniciId",
                table: "Rezervasyonlar",
                column: "TalepEdenKullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_Rezervasyonlari_BirimTarihDurum_Aktif",
                table: "Rezervasyonlar",
                columns: new[] { "BirimId", "BaslangicTarihi", "BitisTarihi", "Durum" },
                filter: "[IsDeleted] = 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Rezervasyonlari_Durum",
                table: "Rezervasyonlar",
                sql: "[Durum] IN (1, 2, 3, 5, 6)");

            migrationBuilder.CreateIndex(
                name: "UX_RezervasyonKatilimcilari_RezervasyonEposta",
                table: "RezervasyonKatilimcilari",
                columns: new[] { "RezervasyonId", "NormalizeEpostaAdresi" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_Rezervasyonlar_AspNetUsers_IptalEdenKullaniciId",
                table: "Rezervasyonlar",
                column: "IptalEdenKullaniciId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Rezervasyonlar_AspNetUsers_OnaylayanKullaniciId",
                table: "Rezervasyonlar",
                column: "OnaylayanKullaniciId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Rezervasyonlar_AspNetUsers_ReddedenKullaniciId",
                table: "Rezervasyonlar",
                column: "ReddedenKullaniciId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Rezervasyonlar_AspNetUsers_TalepEdenKullaniciId",
                table: "Rezervasyonlar",
                column: "TalepEdenKullaniciId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rezervasyonlar_AspNetUsers_IptalEdenKullaniciId",
                table: "Rezervasyonlar");

            migrationBuilder.DropForeignKey(
                name: "FK_Rezervasyonlar_AspNetUsers_OnaylayanKullaniciId",
                table: "Rezervasyonlar");

            migrationBuilder.DropForeignKey(
                name: "FK_Rezervasyonlar_AspNetUsers_ReddedenKullaniciId",
                table: "Rezervasyonlar");

            migrationBuilder.DropForeignKey(
                name: "FK_Rezervasyonlar_AspNetUsers_TalepEdenKullaniciId",
                table: "Rezervasyonlar");

            migrationBuilder.DropTable(
                name: "RezervasyonKatilimcilari");

            migrationBuilder.DropIndex(
                name: "IX_Rezervasyonlar_IptalEdenKullaniciId",
                table: "Rezervasyonlar");

            migrationBuilder.DropIndex(
                name: "IX_Rezervasyonlar_OnaylayanKullaniciId",
                table: "Rezervasyonlar");

            migrationBuilder.DropIndex(
                name: "IX_Rezervasyonlar_ReddedenKullaniciId",
                table: "Rezervasyonlar");

            migrationBuilder.DropIndex(
                name: "IX_Rezervasyonlar_TalepEdenKullaniciId",
                table: "Rezervasyonlar");

            migrationBuilder.DropIndex(
                name: "IX_Rezervasyonlari_BirimTarihDurum_Aktif",
                table: "Rezervasyonlar");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Rezervasyonlari_Durum",
                table: "Rezervasyonlar");

            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT 1
                    FROM Rezervasyonlar
                    WHERE IsDeleted = 0
                      AND Durum IN (5, 6))
                    THROW 51004, 'Onay bekleyen veya reddedilmis rezervasyonlar eski durum modeline donusturulemez.', 1;

                UPDATE r
                SET Durum = 4
                FROM Rezervasyonlar r
                WHERE r.IsDeleted = 0
                  AND r.Durum = 1
                  AND EXISTS (
                      SELECT 1
                      FROM Tahakkuklar t
                      WHERE t.IsDeleted = 0
                        AND t.RezervasyonId = r.Id);
                """);

            migrationBuilder.DropColumn(
                name: "Baslik",
                table: "Rezervasyonlar");

            migrationBuilder.DropColumn(
                name: "IcNotlar",
                table: "Rezervasyonlar");

            migrationBuilder.DropColumn(
                name: "IptalEdenKullaniciId",
                table: "Rezervasyonlar");

            migrationBuilder.DropColumn(
                name: "IptalNedeni",
                table: "Rezervasyonlar");

            migrationBuilder.DropColumn(
                name: "IptalTarihi",
                table: "Rezervasyonlar");

            migrationBuilder.DropColumn(
                name: "Notlar",
                table: "Rezervasyonlar");

            migrationBuilder.DropColumn(
                name: "OnayTarihi",
                table: "Rezervasyonlar");

            migrationBuilder.DropColumn(
                name: "OnaylayanKullaniciId",
                table: "Rezervasyonlar");

            migrationBuilder.DropColumn(
                name: "ReddedenKullaniciId",
                table: "Rezervasyonlar");

            migrationBuilder.DropColumn(
                name: "RetNedeni",
                table: "Rezervasyonlar");

            migrationBuilder.DropColumn(
                name: "RetTarihi",
                table: "Rezervasyonlar");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Rezervasyonlar");

            migrationBuilder.DropColumn(
                name: "TalepEdenAdSoyad",
                table: "Rezervasyonlar");

            migrationBuilder.DropColumn(
                name: "TalepEdenEposta",
                table: "Rezervasyonlar");

            migrationBuilder.DropColumn(
                name: "TalepEdenKullaniciId",
                table: "Rezervasyonlar");

            migrationBuilder.DropColumn(
                name: "TamamlanmaTarihi",
                table: "Rezervasyonlar");

            migrationBuilder.AlterColumn<int>(
                name: "Durum",
                table: "Rezervasyonlar",
                type: "int",
                nullable: false,
                comment: "Planned=1, Completed=2, Cancelled=3, TransferredToCharge=4",
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "Confirmed=1, Completed=2, Cancelled=3, PendingApproval=5, Rejected=6");

            migrationBuilder.CreateIndex(
                name: "IX_Rezervasyonlar_BirimId_BaslangicTarihi",
                table: "Rezervasyonlar",
                columns: new[] { "BirimId", "BaslangicTarihi" });
        }
    }
}
